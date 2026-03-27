import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { SearchStateService } from './search-state.service';

describe('SearchStateService', () => {
  let service: SearchStateService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(SearchStateService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('passes labels through semantic search results', async () => {
    const searchPromise = service.search('language');

    const request = http.expectOne('http://localhost:5000/api/v1/search/semantic');
    expect(request.request.method).toBe('POST');
    request.flush([
      {
        id: 'result-1',
        title: 'Result',
        summary: 'Summary',
        sourceUrl: 'https://example.com/item',
        similarity: 0.92,
        tags: ['alpha'],
        labels: [{ category: 'Language', value: 'English' }]
      }
    ]);

    await searchPromise;

    expect(service.results()).toEqual([
      {
        id: 'result-1',
        title: 'Result',
        sourceUrl: 'https://example.com/item',
        capturedAt: null,
        status: null,
        tags: ['alpha'],
        summary: 'Summary',
        similarity: 0.92,
        labels: [{ category: 'Language', value: 'English' }]
      }
    ]);
  });
});
