import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap } from '@angular/router';

import { SearchCriteria, SearchStateService } from './search-state.service';

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

  it('parses URL query params into search criteria', () => {
    const criteria = service.parseQueryParams(convertToParamMap({
      q: 'angular',
      tag: ['frontend', 'angular'],
      label: ['Language::English', 'Source::Docs'],
      tagMode: 'all',
      labelMode: 'any'
    }));

    expect(criteria).toEqual({
      query: 'angular',
      tags: ['frontend', 'angular'],
      tagMatchMode: 'all',
      labels: [
        { category: 'Language', value: 'English' },
        { category: 'Source', value: 'Docs' }
      ],
      labelMatchMode: 'any',
      limit: 20,
      threshold: 0.3
    });
  });

  it('builds URL query params from normalized search criteria', () => {
    const criteria: SearchCriteria = {
      query: '  angular  ',
      tags: ['frontend', 'Frontend', 'angular'],
      tagMatchMode: 'all',
      labels: [
        { category: 'Language', value: 'English' },
        { category: ' Language ', value: ' English ' }
      ],
      labelMatchMode: 'all',
      limit: 20,
      threshold: 0.3
    };

    expect(service.buildQueryParams(criteria)).toEqual({
      q: 'angular',
      tag: ['frontend', 'angular'],
      label: ['Language::English'],
      tagMode: 'all',
      labelMode: null
    });
  });

  it('posts combined search payloads and normalizes results', async () => {
    const criteria: SearchCriteria = {
      query: 'angular',
      tags: ['frontend'],
      tagMatchMode: 'all',
      labels: [{ category: 'Language', value: 'English' }],
      labelMatchMode: 'any',
      limit: 20,
      threshold: 0.3
    };

    const searchPromise = service.search(criteria);

    const request = http.expectOne('http://localhost:5000/api/v1/search');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      query: 'angular',
      tags: ['frontend'],
      tagMatchMode: 'all',
      labels: [{ category: 'Language', value: 'English' }],
      labelMatchMode: 'any',
      limit: 20,
      threshold: 0.3
    });
    request.flush([
      {
        id: 'result-1',
        title: 'Result',
        summary: ' Summary ',
        sourceUrl: 'https://example.com/item',
        processedAt: '2026-04-09T09:00:00Z',
        similarity: 0.92,
        tags: ['frontend'],
        labels: [{ category: 'Language', value: 'English' }]
      }
    ]);

    await searchPromise;

    expect(service.results()).toEqual([
      {
        id: 'result-1',
        title: 'Result',
        summary: 'Summary',
        sourceUrl: 'https://example.com/item',
        processedAt: '2026-04-09T09:00:00Z',
        similarity: 0.92,
        tags: ['frontend'],
        labels: [{ category: 'Language', value: 'English' }]
      }
    ]);
  });
});
