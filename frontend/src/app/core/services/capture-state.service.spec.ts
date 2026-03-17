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
});
