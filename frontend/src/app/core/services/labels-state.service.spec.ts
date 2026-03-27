import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { LabelsStateService } from './labels-state.service';

describe('LabelsStateService', () => {
  let service: LabelsStateService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(LabelsStateService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      http.verify();
    } finally {
      TestBed.resetTestingModule();
    }
  });

  it('loads grouped label categories and values', async () => {
    const loadPromise = service.loadLabels(true);

    const request = http.expectOne('http://localhost:5000/api/v1/labels');
    request.flush({
      categories: [
        {
          id: 'category-1',
          name: 'Language',
          count: 3,
          lastUsedAt: '2026-03-16T10:00:00Z',
          values: [
            {
              id: 'value-1',
              value: 'English',
              count: 2,
              lastUsedAt: '2026-03-16T10:00:00Z'
            },
            {
              id: 'value-2',
              value: 'German',
              count: 1,
              lastUsedAt: null
            }
          ]
        }
      ]
    });

    await loadPromise;

    expect(service.categories()).toHaveLength(1);
    expect(service.categories()[0].name).toBe('Language');
    expect(service.categories()[0].values.map(value => value.value)).toEqual(['English', 'German']);
  });

  it('creates a category and refreshes the catalog', async () => {
    const createPromise = service.createCategory('  Source  ');

    const createRequest = http.expectOne('http://localhost:5000/api/v1/labels/categories');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual({ name: 'Source' });
    createRequest.flush({ id: 'category-2' });

    await Promise.resolve();

    const refreshRequest = http.expectOne('http://localhost:5000/api/v1/labels');
    refreshRequest.flush({
      categories: [
        {
          id: 'category-2',
          name: 'Source',
          count: 0,
          lastUsedAt: null,
          values: []
        }
      ]
    });

    await expect(createPromise).resolves.toBe(true);
    expect(service.categories()[0].name).toBe('Source');
  });

  it('searches exact label pairs and trims blank rows', async () => {
    const searchPromise = service.searchLabels([
      { category: ' Language ', value: ' English ' },
      { category: ' ', value: 'ignored' }
    ], true);

    const request = http.expectOne('http://localhost:5000/api/v1/search/labels');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      labels: [
        {
          category: 'Language',
          value: 'English'
        }
      ],
      matchAll: true
    });

    request.flush([
      {
        id: 'result-1',
        title: 'Processed insight',
        summary: 'Summary text',
        sourceUrl: 'https://example.com/item',
        processedAt: '2026-03-16T10:10:00Z',
        tags: ['research'],
        labels: [
          {
            category: 'Language',
            value: 'English'
          }
        ]
      }
    ]);

    await expect(searchPromise).resolves.toBe(true);
    expect(service.searchResults()).toHaveLength(1);
    expect(service.searchResults()[0].labels[0]).toEqual({
      category: 'Language',
      value: 'English'
    });
  });

  it('rejects empty label values client-side', async () => {
    await expect(service.createValue('category-1', '   ')).resolves.toBe(false);
    expect(service.mutationError()).toBe('Label value is required.');
  });
});
