import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { SearchComponent } from './search.component';
import { SearchStateService } from '../../core/services/search-state.service';
import { TagsStateService } from '../../core/services/tags-state.service';
import { LabelsStateService } from '../../core/services/labels-state.service';

describe('SearchComponent', () => {
  it('hydrates from URL state and renders results', async () => {
    const searchStateStub = {
      results: signal([
        {
          id: 'insight-1',
          captureId: 'capture-1',
          title: 'Angular result',
          summary: 'Summary',
          sourceUrl: 'https://example.com/angular',
          processedAt: '2026-04-09T09:00:00Z',
          tags: ['frontend'],
          labels: [{ category: 'Language', value: 'English' }],
          similarity: 0.88
        }
      ]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockReturnValue(true),
      parseQueryParams: vi.fn().mockReturnValue({
        query: 'angular',
        topicId: '',
        tags: ['frontend'],
        tagMatchMode: 'any',
        labels: [{ category: 'Language', value: 'English' }],
        labelMatchMode: 'all',
        page: 2,
        pageSize: 50,
        threshold: 0.3,
        sortField: 'relevance',
        sortDirection: 'desc'
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined),
      totalCount: signal(71),
      totalPages: vi.fn().mockReturnValue(4),
      currentPagination: signal({ page: 2, pageSize: 50 })
    };

    const tagsStateStub = {
      tags: signal([{ id: 'tag-1', name: 'frontend', count: 1, lastUsedAt: null }]),
      loadTags: vi.fn().mockResolvedValue(undefined)
    };

    const labelsStateStub = {
      categories: signal([
        {
          id: 'category-1',
          name: 'Language',
          count: 1,
          lastUsedAt: null,
          values: [
            { id: 'value-1', value: 'English', count: 1, lastUsedAt: null }
          ]
        }
      ]),
      loadLabels: vi.fn().mockResolvedValue(undefined)
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ q: 'angular' })
            }
          }
        },
        { provide: SearchStateService, useValue: searchStateStub },
        { provide: TagsStateService, useValue: tagsStateStub },
        { provide: LabelsStateService, useValue: labelsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Search the knowledgebase');
    expect(compiled.textContent).toContain('Angular result');
    expect(compiled.textContent).toContain('Language: English');
    expect(compiled.textContent).toContain('71 total results');
    expect(compiled.textContent).toContain('Page 2 of 4');
    expect(compiled.textContent).toContain('Hide filters');

    const resultLink = compiled.querySelector('.result-card') as HTMLAnchorElement | null;
    expect(resultLink?.getAttribute('href')).toContain('/captures/capture-1');
  });

  it('prefills topic cluster filter from URL and searches immediately', async () => {
    const searchStateStub = {
      results: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockReturnValue(true),
      parseQueryParams: vi.fn().mockReturnValue({
        query: '',
        topicId: '048fda43-2e0b-4392-aa06-99833f5eaf80',
        tags: [],
        tagMatchMode: 'any',
        labels: [],
        labelMatchMode: 'all',
        page: 1,
        pageSize: 20,
        threshold: 0.3,
        sortField: 'processedAt',
        sortDirection: 'desc'
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined),
      totalCount: signal(0),
      totalPages: vi.fn().mockReturnValue(1),
      currentPagination: signal({ page: 1, pageSize: 20 })
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ topicId: '048fda43-2e0b-4392-aa06-99833f5eaf80' })
            }
          }
        },
        { provide: SearchStateService, useValue: searchStateStub },
        { provide: TagsStateService, useValue: { tags: signal([]), loadTags: vi.fn().mockResolvedValue(undefined) } },
        { provide: LabelsStateService, useValue: { categories: signal([]), loadLabels: vi.fn().mockResolvedValue(undefined) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(searchStateStub.search).toHaveBeenCalledWith(
      expect.objectContaining({
        topicId: '048fda43-2e0b-4392-aa06-99833f5eaf80'
      })
    );

    const topicInput = (fixture.nativeElement as HTMLElement).querySelector('#search-topic-id') as HTMLInputElement | null;
    expect(topicInput?.value).toBe('048fda43-2e0b-4392-aa06-99833f5eaf80');
  });

  it('disables submit until at least one search criterion is present', async () => {
    const searchStateStub = {
      results: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockImplementation(criteria =>
        Boolean(criteria.query?.trim()) || Boolean(criteria.topicId?.trim()) || criteria.tags.length > 0 || criteria.labels.some((label: { category: string; value: string }) => label.category.trim() && label.value.trim())),
      parseQueryParams: vi.fn().mockReturnValue({
        query: '',
        topicId: '',
        tags: [],
        tagMatchMode: 'any',
        labels: [],
        labelMatchMode: 'all',
        page: 1,
        pageSize: 20,
        threshold: 0.3,
        sortField: 'processedAt',
        sortDirection: 'desc'
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined),
      totalCount: signal(0),
      totalPages: vi.fn().mockReturnValue(1),
      currentPagination: signal({ page: 1, pageSize: 20 })
    };

    const tagsStateStub = {
      tags: signal([]),
      loadTags: vi.fn().mockResolvedValue(undefined)
    };

    const labelsStateStub = {
      categories: signal([]),
      loadLabels: vi.fn().mockResolvedValue(undefined)
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({})
            }
          }
        },
        { provide: SearchStateService, useValue: searchStateStub },
        { provide: TagsStateService, useValue: tagsStateStub },
        { provide: LabelsStateService, useValue: labelsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const submitButton = (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]') as HTMLButtonElement | null;
    expect(submitButton).not.toBeNull();
    expect(submitButton?.disabled).toBe(true);
  });

  it('keeps advanced filters collapsed when only semantic query is present', async () => {
    const searchStateStub = {
      results: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockReturnValue(true),
      parseQueryParams: vi.fn().mockReturnValue({
        query: 'angular',
        topicId: '',
        tags: [],
        tagMatchMode: 'any',
        labels: [],
        labelMatchMode: 'all',
        page: 1,
        pageSize: 20,
        threshold: 0.3,
        sortField: 'relevance',
        sortDirection: 'desc'
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined),
      totalCount: signal(0),
      totalPages: vi.fn().mockReturnValue(1),
      currentPagination: signal({ page: 1, pageSize: 20 })
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ q: 'angular' })
            }
          }
        },
        { provide: SearchStateService, useValue: searchStateStub },
        {
          provide: TagsStateService,
          useValue: {
            tags: signal([]),
            loadTags: vi.fn().mockResolvedValue(undefined)
          }
        },
        {
          provide: LabelsStateService,
          useValue: {
            categories: signal([]),
            loadLabels: vi.fn().mockResolvedValue(undefined)
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Show filters');
    expect(compiled.textContent).not.toContain('Hide filters');
    expect(compiled.querySelector('#search-tag-input')).toBeNull();
  });

  it('resets to page 1 and keeps filters when page size changes', async () => {
    const searchStateStub = {
      results: signal([
        {
          id: 'insight-1',
          captureId: 'capture-1',
          title: 'Angular result',
          summary: 'Summary',
          sourceUrl: 'https://example.com/angular',
          processedAt: '2026-04-09T09:00:00Z',
          tags: ['frontend'],
          labels: [{ category: 'Language', value: 'English' }],
          similarity: 0.88
        }
      ]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockReturnValue(true),
      parseQueryParams: vi.fn().mockReturnValue({
        query: 'angular',
        topicId: '',
        tags: ['frontend'],
        tagMatchMode: 'any',
        labels: [{ category: 'Language', value: 'English' }],
        labelMatchMode: 'all',
        page: 2,
        pageSize: 50,
        threshold: 0.3,
        sortField: 'title',
        sortDirection: 'asc'
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined),
      totalCount: signal(71),
      totalPages: vi.fn().mockReturnValue(4),
      currentPagination: signal({ page: 2, pageSize: 50 })
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({
                q: 'angular',
                page: '2',
                pageSize: '50',
                sortField: 'title',
                sortDirection: 'asc'
              })
            }
          }
        },
        { provide: SearchStateService, useValue: searchStateStub },
        {
          provide: TagsStateService,
          useValue: {
            tags: signal([{ id: 'tag-1', name: 'frontend', count: 1, lastUsedAt: null }]),
            loadTags: vi.fn().mockResolvedValue(undefined)
          }
        },
        {
          provide: LabelsStateService,
          useValue: {
            categories: signal([
              {
                id: 'category-1',
                name: 'Language',
                count: 1,
                lastUsedAt: null,
                values: [{ id: 'value-1', value: 'English', count: 1, lastUsedAt: null }]
              }
            ]),
            loadLabels: vi.fn().mockResolvedValue(undefined)
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.searchQuery = 'angular';
    component.selectedTags = ['frontend'];
    component.labelRows = [{ id: 'label-row-1', category: 'Language', value: 'English' }];

    await component.onPageSizeChange(100);

    expect(searchStateStub.syncUrl).toHaveBeenLastCalledWith(
      expect.anything(),
      expect.anything(),
      expect.objectContaining({
        query: 'angular',
        tags: ['frontend'],
        labels: [{ category: 'Language', value: 'English' }],
        page: 1,
        pageSize: 100,
        sortField: 'title',
        sortDirection: 'asc'
      })
    );
    expect(searchStateStub.search).toHaveBeenLastCalledWith(
      expect.objectContaining({
        query: 'angular',
        tags: ['frontend'],
        page: 1,
        pageSize: 100,
        sortField: 'title',
        sortDirection: 'asc'
      })
    );
  });

  it('resets to page 1 when sort changes and keeps active filters', async () => {
    const searchStateStub = {
      results: signal([
        {
          id: 'insight-1',
          captureId: 'capture-1',
          title: 'Angular result',
          summary: 'Summary',
          sourceUrl: 'https://example.com/angular',
          processedAt: '2026-04-09T09:00:00Z',
          tags: ['frontend'],
          labels: [{ category: 'Language', value: 'English' }],
          similarity: 0.88
        }
      ]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockReturnValue(true),
      parseQueryParams: vi.fn().mockReturnValue({
        query: 'angular',
        topicId: '',
        tags: ['frontend'],
        tagMatchMode: 'any',
        labels: [{ category: 'Language', value: 'English' }],
        labelMatchMode: 'all',
        page: 3,
        pageSize: 50,
        threshold: 0.3,
        sortField: 'title',
        sortDirection: 'asc'
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined),
      totalCount: signal(71),
      totalPages: vi.fn().mockReturnValue(4),
      currentPagination: signal({ page: 3, pageSize: 50 })
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ q: 'angular', page: '3', pageSize: '50' })
            }
          }
        },
        { provide: SearchStateService, useValue: searchStateStub },
        {
          provide: TagsStateService,
          useValue: {
            tags: signal([{ id: 'tag-1', name: 'frontend', count: 1, lastUsedAt: null }]),
            loadTags: vi.fn().mockResolvedValue(undefined)
          }
        },
        {
          provide: LabelsStateService,
          useValue: {
            categories: signal([
              {
                id: 'category-1',
                name: 'Language',
                count: 1,
                lastUsedAt: null,
                values: [{ id: 'value-1', value: 'English', count: 1, lastUsedAt: null }]
              }
            ]),
            loadLabels: vi.fn().mockResolvedValue(undefined)
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(SearchComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.searchQuery = 'angular';
    component.selectedTags = ['frontend'];
    component.labelRows = [{ id: 'label-row-1', category: 'Language', value: 'English' }];

    await component.onSortChange('sourceUrl:desc');

    expect(searchStateStub.syncUrl).toHaveBeenLastCalledWith(
      expect.anything(),
      expect.anything(),
      expect.objectContaining({
        query: 'angular',
        tags: ['frontend'],
        page: 1,
        pageSize: 50,
        sortField: 'sourceUrl',
        sortDirection: 'desc'
      })
    );
    expect(searchStateStub.search).toHaveBeenLastCalledWith(
      expect.objectContaining({
        query: 'angular',
        page: 1,
        sortField: 'sourceUrl',
        sortDirection: 'desc'
      })
    );
  });
});
