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
          id: 'result-1',
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
        tags: ['frontend'],
        tagMatchMode: 'any',
        labels: [{ category: 'Language', value: 'English' }],
        labelMatchMode: 'all',
        limit: 20,
        threshold: 0.3
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined)
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

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Search the knowledgebase');
    expect(compiled.textContent).toContain('Angular result');
    expect(compiled.textContent).toContain('Language: English');
  });

  it('disables submit until at least one search criterion is present', async () => {
    const searchStateStub = {
      results: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      createEmptyCriteria: vi.fn(),
      hasCriteria: vi.fn().mockImplementation(criteria =>
        Boolean(criteria.query?.trim()) || criteria.tags.length > 0 || criteria.labels.some((label: { category: string; value: string }) => label.category.trim() && label.value.trim())),
      parseQueryParams: vi.fn().mockReturnValue({
        query: '',
        tags: [],
        tagMatchMode: 'any',
        labels: [],
        labelMatchMode: 'all',
        limit: 20,
        threshold: 0.3
      }),
      buildQueryParams: vi.fn(),
      syncUrl: vi.fn().mockResolvedValue(undefined),
      search: vi.fn().mockResolvedValue(undefined)
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

    const submitButton = (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]') as HTMLButtonElement | null;
    expect(submitButton).not.toBeNull();
    expect(submitButton?.disabled).toBe(true);
  });
});
