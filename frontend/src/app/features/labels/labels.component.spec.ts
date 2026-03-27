import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { LabelsComponent } from './labels.component';
import { LabelsStateService } from '../../core/services/labels-state.service';

describe('LabelsComponent', () => {
  it('renders grouped catalog items and search results', async () => {
    const labelsStateStub = {
      categories: signal([
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
            }
          ]
        }
      ]),
      searchResults: signal([
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
      ]),
      loading: signal(false),
      error: signal<string | null>(null),
      mutating: signal(false),
      mutationError: signal<string | null>(null),
      searchLoading: signal(false),
      searchError: signal<string | null>(null),
      loadLabels: vi.fn().mockResolvedValue(undefined),
      createCategory: vi.fn().mockResolvedValue(true),
      renameCategory: vi.fn().mockResolvedValue(true),
      deleteCategory: vi.fn().mockResolvedValue(true),
      createValue: vi.fn().mockResolvedValue(true),
      renameValue: vi.fn().mockResolvedValue(true),
      deleteValue: vi.fn().mockResolvedValue(true),
      searchLabels: vi.fn().mockResolvedValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [LabelsComponent],
      providers: [
        { provide: LabelsStateService, useValue: labelsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LabelsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Language');
    expect(compiled.textContent).toContain('English');
    expect(compiled.textContent).toContain('Processed insight');
    expect(compiled.textContent).toContain('Language: English');
  });

  it('trims inputs before creating labels and searching exact pairs', async () => {
    const labelsStateStub = {
      categories: signal([]),
      searchResults: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      mutating: signal(false),
      mutationError: signal<string | null>(null),
      searchLoading: signal(false),
      searchError: signal<string | null>(null),
      loadLabels: vi.fn().mockResolvedValue(undefined),
      createCategory: vi.fn().mockResolvedValue(true),
      renameCategory: vi.fn().mockResolvedValue(true),
      deleteCategory: vi.fn().mockResolvedValue(true),
      createValue: vi.fn().mockResolvedValue(true),
      renameValue: vi.fn().mockResolvedValue(true),
      deleteValue: vi.fn().mockResolvedValue(true),
      searchLabels: vi.fn().mockResolvedValue(true)
    };

    await TestBed.configureTestingModule({
      imports: [LabelsComponent],
      providers: [
        { provide: LabelsStateService, useValue: labelsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LabelsComponent);
    fixture.componentInstance.newCategoryName = '  Topic  ';
    fixture.componentInstance.searchRows = [
      {
        id: 'search-row-1',
        category: ' Language ',
        value: ' English '
      }
    ];
    fixture.componentInstance.searchMatchAll = false;

    await fixture.componentInstance.submitCategory();
    await fixture.componentInstance.submitSearch();

    expect(labelsStateStub.createCategory).toHaveBeenCalledWith('Topic');
    expect(fixture.componentInstance.newCategoryName).toBe('');
    expect(labelsStateStub.searchLabels).toHaveBeenCalledWith([
      {
        category: 'Language',
        value: 'English'
      }
    ], false);
    expect(fixture.componentInstance.searchSubmitted).toBe(true);
  });
});
