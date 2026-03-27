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

  it('loads captures newest first by default', async () => {
    const loadPromise = service.loadCaptures(true);

    const request = http.expectOne('http://localhost:5000/api/v1/capture');
    request.flush([
      {
        id: 'older',
        sourceUrl: 'https://example.com/older',
        contentType: 'Article',
        status: 'Completed',
        createdAt: '2026-03-15T10:00:00Z',
        processedAt: null,
        rawContent: 'older',
        metadata: null,
        labels: [],
        tags: [],
        processedInsight: null
      },
      {
        id: 'newer',
        sourceUrl: 'https://example.com/newer',
        contentType: 'Tweet',
        status: 'Pending',
        createdAt: '2026-03-16T10:00:00Z',
        processedAt: null,
        rawContent: 'newer',
        metadata: null,
        labels: [],
        tags: [],
        processedInsight: null
      }
    ]);

    await loadPromise;

    expect(service.captures().map(item => item.id)).toEqual(['newer', 'older']);
  });

  it('sorts by columns and toggles direction', async () => {
    const loadPromise = service.loadCaptures(true);

    const request = http.expectOne('http://localhost:5000/api/v1/capture');
    request.flush([
      {
        id: 'b',
        sourceUrl: 'https://example.com/b',
        contentType: 'Tweet',
        status: 'Pending',
        createdAt: '2026-03-15T10:00:00Z',
        processedAt: null,
        rawContent: 'b',
        metadata: null,
        labels: [],
        tags: [],
        processedInsight: null
      },
      {
        id: 'a',
        sourceUrl: 'https://example.com/a',
        contentType: 'Article',
        status: 'Completed',
        createdAt: '2026-03-14T10:00:00Z',
        processedAt: null,
        rawContent: 'a',
        metadata: null,
        labels: [],
        tags: [],
        processedInsight: null
      }
    ]);

    await loadPromise;

    service.setSort('contentType');
    expect(service.captures().map(item => item.id)).toEqual(['a', 'b']);

    service.setSort('contentType');
    expect(service.captures().map(item => item.id)).toEqual(['b', 'a']);

    service.setSort('status');
    expect(service.captures().map(item => item.id)).toEqual(['a', 'b']);

    service.setSort('sourceUrl');
    expect(service.captures().map(item => item.id)).toEqual(['a', 'b']);
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
});
