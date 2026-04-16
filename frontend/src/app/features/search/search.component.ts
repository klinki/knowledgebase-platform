import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  SEARCH_PAGE_SIZE_OPTIONS,
  SearchCriteria,
  SearchMatchMode,
  SearchSortDirection,
  SearchSortField,
  SearchStateService
} from '../../core/services/search-state.service';
import { TagsStateService } from '../../core/services/tags-state.service';
import { LabelsStateService } from '../../core/services/labels-state.service';

interface LabelRow {
  id: string;
  category: string;
  value: string;
}

interface SortOption {
  label: string;
  sortField: SearchSortField;
  sortDirection: SearchSortDirection;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss'
})
export class SearchComponent implements OnInit {
  private static rowSeed = 0;

  readonly pageSizeOptions = SEARCH_PAGE_SIZE_OPTIONS;
  readonly sortOptions: SortOption[] = [
    { label: 'Relevance (best match)', sortField: 'relevance', sortDirection: 'desc' },
    { label: 'Newest processed', sortField: 'processedAt', sortDirection: 'desc' },
    { label: 'Oldest processed', sortField: 'processedAt', sortDirection: 'asc' },
    { label: 'Title A-Z', sortField: 'title', sortDirection: 'asc' },
    { label: 'Title Z-A', sortField: 'title', sortDirection: 'desc' },
    { label: 'Source A-Z', sortField: 'sourceUrl', sortDirection: 'asc' },
    { label: 'Source Z-A', sortField: 'sourceUrl', sortDirection: 'desc' }
  ];
  searchState = inject(SearchStateService);
  tagsState = inject(TagsStateService);
  labelsState = inject(LabelsStateService);

  private route = inject(ActivatedRoute);
  private router = inject(Router);

  searchQuery = '';
  tagInput = '';
  selectedTags: string[] = [];
  tagMatchMode: SearchMatchMode = 'any';
  labelMatchMode: SearchMatchMode = 'all';
  sortField: SearchSortField = 'processedAt';
  sortDirection: SearchSortDirection = 'desc';
  sortOverridden = false;
  labelRows: LabelRow[] = [SearchComponent.createLabelRow()];
  advancedFiltersOpen = false;
  currentPagination = computed(() => this.searchState.currentPagination());
  totalCount = computed(() => this.searchState.totalCount());
  activeAdvancedFilterCount = computed(() => this.selectedTags.length + this.countActiveLabelRows());
  visiblePages = computed(() => {
    const total = this.searchState.totalPages();
    const current = this.currentPagination().page;
    const pages: number[] = [];

    if (total <= 7) {
      for (let page = 1; page <= total; page += 1) {
        pages.push(page);
      }

      return pages;
    }

    pages.push(1);

    if (current > 3) {
      pages.push(-1);
    }

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);
    for (let page = start; page <= end; page += 1) {
      pages.push(page);
    }

    if (current < total - 2) {
      pages.push(-1);
    }

    pages.push(total);
    return pages;
  });

  async ngOnInit(): Promise<void> {
    this.sortOverridden = this.route.snapshot.queryParamMap.has('sortField')
      || this.route.snapshot.queryParamMap.has('sortDirection');
    const criteria = this.searchState.parseQueryParams(this.route.snapshot.queryParamMap);
    this.applyCriteria(criteria);

    await Promise.all([
      this.tagsState.loadTags(),
      this.labelsState.loadLabels()
    ]);

    if (this.searchState.hasCriteria(criteria)) {
      await this.searchState.search(criteria);
    }
  }

  canSubmit(): boolean {
    return this.searchState.hasCriteria(this.buildCriteria());
  }

  activeLabelCount(): number {
    return this.countActiveLabelRows();
  }

  toggleAdvancedFilters(): void {
    this.advancedFiltersOpen = !this.advancedFiltersOpen;
  }

  addTagFromInput(): void {
    const normalized = this.tagInput.trim();
    if (!normalized) {
      return;
    }

    if (this.selectedTags.some(tag => tag.toLowerCase() === normalized.toLowerCase())) {
      this.tagInput = '';
      return;
    }

    this.selectedTags = [...this.selectedTags, normalized];
    this.tagInput = '';
  }

  removeTag(tag: string): void {
    this.selectedTags = this.selectedTags.filter(existing => existing !== tag);
  }

  addLabelRow(): void {
    this.labelRows = [...this.labelRows, SearchComponent.createLabelRow()];
  }

  removeLabelRow(rowId: string): void {
    if (this.labelRows.length === 1) {
      this.labelRows = [SearchComponent.createLabelRow()];
      return;
    }

    this.labelRows = this.labelRows.filter(row => row.id !== rowId);
  }

  labelCategorySuggestions(): string[] {
    return this.labelsState.categories().map(category => category.name);
  }

  labelValueSuggestions(row: LabelRow): string[] {
    const normalizedCategory = row.category.trim().toLowerCase();
    if (!normalizedCategory) {
      const values = new Set<string>();
      for (const category of this.labelsState.categories()) {
        for (const value of category.values) {
          values.add(value.value);
        }
      }

      return Array.from(values).sort((left, right) => left.localeCompare(right));
    }

    const category = this.labelsState.categories().find(item => item.name.toLowerCase() === normalizedCategory);
    return category?.values.map(value => value.value) ?? [];
  }

  suggestedTags(): string[] {
    const query = this.tagInput.trim().toLowerCase();
    return this.tagsState.tags()
      .map(tag => tag.name)
      .filter(tag => !this.selectedTags.some(selected => selected.toLowerCase() === tag.toLowerCase()))
      .filter(tag => query.length === 0 || tag.toLowerCase().includes(query))
      .slice(0, 10);
  }

  onTagInputKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter') {
      return;
    }

    event.preventDefault();
    this.addTagFromInput();
  }

  async submitSearch(): Promise<void> {
    const criteria = this.buildCriteria({ page: 1 });
    if (!this.searchState.hasCriteria(criteria)) {
      return;
    }

    await this.searchState.syncUrl(this.router, this.route, criteria);
    await this.searchState.search(criteria);
  }

  async clearSearch(): Promise<void> {
    this.searchQuery = '';
    this.tagInput = '';
    this.selectedTags = [];
    this.tagMatchMode = 'any';
    this.labelMatchMode = 'all';
    this.sortOverridden = false;
    this.labelRows = [SearchComponent.createLabelRow()];
    this.advancedFiltersOpen = false;
    this.searchState.clear();
    await this.searchState.syncUrl(this.router, this.route, this.buildCriteria());
  }

  async onPageSizeChange(size: number): Promise<void> {
    const criteria = this.buildCriteria({ page: 1, pageSize: size });
    await this.searchState.syncUrl(this.router, this.route, criteria);
    await this.searchState.search(criteria);
  }

  async onSortChange(value: string): Promise<void> {
    const option = this.sortOptions.find(candidate => this.toSortOptionValue(candidate) === value);
    if (!option) {
      return;
    }

    this.sortField = option.sortField;
    this.sortDirection = option.sortDirection;
    this.sortOverridden = true;

    const criteria = this.buildCriteria({ page: 1 });
    await this.searchState.syncUrl(this.router, this.route, criteria);
    await this.searchState.search(criteria);
  }

  async goToPage(page: number): Promise<void> {
    if (page < 1 || page > this.searchState.totalPages() || page === this.currentPagination().page) {
      return;
    }

    const criteria = this.buildCriteria({ page });
    await this.searchState.syncUrl(this.router, this.route, criteria);
    await this.searchState.search(criteria);
  }

  private applyCriteria(criteria: SearchCriteria): void {
    this.searchQuery = criteria.query;
    this.selectedTags = [...criteria.tags];
    this.tagMatchMode = criteria.tagMatchMode;
    this.labelMatchMode = criteria.labelMatchMode;
    this.sortField = criteria.sortField;
    this.sortDirection = criteria.sortDirection;
    this.labelRows = criteria.labels.length > 0
      ? criteria.labels.map(label => SearchComponent.createLabelRow(label.category, label.value))
      : [SearchComponent.createLabelRow()];
    this.advancedFiltersOpen = criteria.tags.length > 0 || criteria.labels.length > 0;
  }

  private countActiveLabelRows(): number {
    return this.labelRows.filter(row => row.category.trim().length > 0 && row.value.trim().length > 0).length;
  }

  private buildCriteria(pagination: Partial<Pick<SearchCriteria, 'page' | 'pageSize'>> = {}): SearchCriteria {
    const currentPagination = this.currentPagination();
    const hasQuery = this.searchQuery.trim().length > 0;
    const defaultSortField: SearchSortField = hasQuery ? 'relevance' : 'processedAt';
    const sortField = this.sortOverridden ? this.sortField : defaultSortField;
    const sortDirection: SearchSortDirection = this.sortOverridden ? this.sortDirection : 'desc';

    return {
      query: this.searchQuery,
      tags: this.selectedTags,
      tagMatchMode: this.tagMatchMode,
      labels: this.labelRows.map(row => ({
        category: row.category,
        value: row.value
      })),
      labelMatchMode: this.labelMatchMode,
      page: pagination.page ?? currentPagination.page,
      pageSize: pagination.pageSize ?? currentPagination.pageSize,
      threshold: 0.3,
      sortField,
      sortDirection
    };
  }

  toSortOptionValue(option: SortOption): string {
    return `${option.sortField}:${option.sortDirection}`;
  }

  selectedSortOptionValue(): string {
    return `${this.sortField}:${this.sortDirection}`;
  }

  private static createLabelRow(category = '', value = ''): LabelRow {
    SearchComponent.rowSeed += 1;
    return {
      id: `label-row-${SearchComponent.rowSeed}`,
      category,
      value
    };
  }
}
