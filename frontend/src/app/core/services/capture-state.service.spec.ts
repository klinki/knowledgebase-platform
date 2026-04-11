import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CaptureStateService } from './capture-state.service';

describe('CaptureStateService', () => {
  let service: CaptureStateService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(CaptureStateService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('loads captures from the paged backend endpoint with default query params', async () => {
    const loadPromise = service.loadCaptures(true);

    const request = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('page') === '1' &&
      req.params.get('pageSize') === '10' &&
      req.params.get('sortField') === 'createdAt' &&
      req.params.get('sortDirection') === 'desc'
    );

    request.flush({
      items: [
        {
          id: 'capture-1',
          sourceUrl: 'https://example.com/1',
          contentType: 'Article',
          status: 'Completed',
          createdAt: '2026-03-15T10:00:00Z',
          processedAt: null,
          failureReason: null,
          skipReason: null
        }
      ],
      totalCount: 23,
      page: 1,
      pageSize: 10
    });

    await loadPromise;

    expect(service.captures().map(item => item.id)).toEqual(['capture-1']);
    expect(service.totalFilteredCount()).toBe(23);
    expect(service.totalPages()).toBe(3);
  });

  it('requests backend sorting when sort changes', async () => {
    const initialLoadPromise = service.loadCaptures(true);
    http.expectOne(() => true).flush({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10
    });
    await initialLoadPromise;

    service.setSort('contentType');

    const request = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('sortField') === 'contentType' &&
      req.params.get('sortDirection') === 'asc' &&
      req.params.get('page') === '1'
    );

    request.flush({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10
    });

    expect(service.currentSort()).toEqual({ field: 'contentType', direction: 'asc' });
  });

  it('requests backend filtering and page size changes', async () => {
    const initialLoadPromise = service.loadCaptures(true);
    http.expectOne(() => true).flush({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10
    });
    await initialLoadPromise;

    service.setFilter({ contentType: 'Article', status: 'Failed' });

    const filterRequest = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('contentType') === 'Article' &&
      req.params.get('status') === 'Failed' &&
      req.params.get('page') === '1'
    );
    filterRequest.flush({
      items: [],
      totalCount: 2,
      page: 1,
      pageSize: 10
    });
    await Promise.resolve();

    service.setPageSize(50);

    const pageSizeRequest = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('pageSize') === '50' &&
      req.params.get('contentType') === 'Article' &&
      req.params.get('status') === 'Failed'
    );
    pageSizeRequest.flush({
      items: [],
      totalCount: 2,
      page: 1,
      pageSize: 50
    });

    expect(service.currentPagination()).toEqual({ page: 1, pageSize: 50 });
  });

  it('maps URL-only create capture to Article with generated content', async () => {
    const createPromise = service.createCapture({
      sourceUrl: 'https://example.com/url-only',
      contentType: '',
      rawContent: '',
      tags: [' alpha ', ' ', 'beta']
    });

    const request = http.expectOne('http://localhost:5000/api/v1/capture');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.sourceUrl).toBe('https://example.com/url-only');
    expect(request.request.body.contentType).toBe('Article');
    expect(request.request.body.rawContent).toBe('https://example.com/url-only');
    expect(request.request.body.tags).toEqual(['alpha', 'beta']);
    expect(request.request.body.labels).toEqual([]);
    const urlOnlyMetadata = JSON.parse(request.request.body.metadata) as { source: string; capturedAt: string };
    expect(urlOnlyMetadata.source).toBe('frontend_url_input');
    expect(typeof urlOnlyMetadata.capturedAt).toBe('string');
    request.flush({ id: 'capture-1', message: 'accepted' });

    await expect(createPromise).resolves.toEqual({ id: 'capture-1', message: 'accepted' });
  });

  it('maps direct content create capture without a URL', async () => {
    const createPromise = service.createCapture({
      sourceUrl: '   ',
      contentType: 'Note',
      rawContent: 'Manual body',
      tags: [' one ', 'two'],
      labels: [
        { category: 'Language', value: 'English' },
        { category: 'Source', value: 'Web' }
      ]
    });

    const request = http.expectOne('http://localhost:5000/api/v1/capture');
    expect(request.request.body.sourceUrl).toBe('');
    expect(request.request.body.contentType).toBe('Note');
    expect(request.request.body.rawContent).toBe('Manual body');
    expect(request.request.body.tags).toEqual(['one', 'two']);
    expect(request.request.body.labels).toEqual([
      { category: 'Language', value: 'English' },
      { category: 'Source', value: 'Web' }
    ]);
    const directMetadata = JSON.parse(request.request.body.metadata) as { source: string; capturedAt: string };
    expect(directMetadata.source).toBe('frontend_manual_input');
    expect(typeof directMetadata.capturedAt).toBe('string');
    request.flush({ id: 'capture-2', message: 'accepted' });

    await expect(createPromise).resolves.toEqual({ id: 'capture-2', message: 'accepted' });
  });

  it('posts selected failed capture ids to the bulk retry endpoint and reloads the list', async () => {
    const retryPromise = service.retryFailedCaptures(['capture-1', 'capture-2']);

    const retryRequest = http.expectOne('http://localhost:5000/api/v1/capture/retry-failed');
    expect(retryRequest.request.method).toBe('POST');
    expect(retryRequest.request.body).toEqual({
      captureIds: ['capture-1', 'capture-2'],
      retryAllMatching: false
    });
    retryRequest.flush({
      retriedCount: 2,
      enqueuedCount: 2,
      message: 'accepted'
    });
    await Promise.resolve();

    const reloadRequest = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('page') === '1' &&
      req.params.get('pageSize') === '10'
    );
    reloadRequest.flush({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10
    });

    await expect(retryPromise).resolves.toEqual({
      retriedCount: 2,
      enqueuedCount: 2,
      message: 'accepted'
    });
  });

  it('posts retry-all scope to the bulk retry endpoint and reloads the list', async () => {
    service.setFilter({ contentType: 'Article', status: 'Failed' });

    const filterRequest = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('contentType') === 'Article' &&
      req.params.get('status') === 'Failed'
    );
    filterRequest.flush({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10
    });
    await Promise.resolve();

    const retryPromise = service.retryAllFailedCaptures({
      contentType: 'Article',
      status: 'Failed'
    });

    const retryRequest = http.expectOne('http://localhost:5000/api/v1/capture/retry-failed');
    expect(retryRequest.request.method).toBe('POST');
    expect(retryRequest.request.body).toEqual({
      retryAllMatching: true,
      contentType: 'Article',
      status: 'Failed'
    });
    retryRequest.flush({
      retriedCount: 4,
      enqueuedCount: 4,
      message: 'accepted'
    });
    await Promise.resolve();

    const reloadRequest = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/capture/list' &&
      req.params.get('contentType') === 'Article' &&
      req.params.get('status') === 'Failed'
    );
    reloadRequest.flush({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 10
    });

    await expect(retryPromise).resolves.toEqual({
      retriedCount: 4,
      enqueuedCount: 4,
      message: 'accepted'
    });
  });
});
