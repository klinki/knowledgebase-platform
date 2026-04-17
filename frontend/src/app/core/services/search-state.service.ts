import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LabelAssignment, SearchResult, SearchResultPage } from '../../shared/models/knowledge.model';

export type SearchMatchMode = 'any' | 'all';
export type SearchSortField = 'relevance' | 'processedAt' | 'title' | 'sourceUrl';
export type SearchSortDirection = 'asc' | 'desc';
export const SEARCH_PAGE_SIZE_OPTIONS = [20, 50, 100] as const;
export const SEARCH_SORT_FIELDS: readonly SearchSortField[] = ['relevance', 'processedAt', 'title', 'sourceUrl'] as const;
export const SEARCH_SORT_DIRECTIONS: readonly SearchSortDirection[] = ['asc', 'desc'] as const;
export const DEFAULT_SEARCH_THRESHOLD = 0.6;

interface SearchPaginationState {
  page: number;
  pageSize: number;
}

export interface SearchCriteria {
  query: string;
  topicId: string;
  tags: string[];
  tagMatchMode: SearchMatchMode;
  labels: LabelAssignment[];
  labelMatchMode: SearchMatchMode;
  page: number;
  pageSize: number;
  threshold: number;
  sortField: SearchSortField;
  sortDirection: SearchSortDirection;
}

@Injectable({
  providedIn: 'root'
})
export class SearchStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/search`;
  private totalCountState = signal(0);
  private paginationState = signal<SearchPaginationState>({ page: 1, pageSize: 20 });

  results = signal<SearchResult[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  totalCount = computed(() => this.totalCountState());
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCountState() / this.paginationState().pageSize)));
  currentPagination = computed(() => this.paginationState());

  clear(): void {
    this.results.set([]);
    this.totalCountState.set(0);
    this.paginationState.set({ page: 1, pageSize: 20 });
    this.error.set(null);
    this.loading.set(false);
  }

  createEmptyCriteria(): SearchCriteria {
    return {
      query: '',
      topicId: '',
      tags: [],
      tagMatchMode: 'any',
      labels: [],
      labelMatchMode: 'all',
      page: 1,
      pageSize: 20,
      threshold: DEFAULT_SEARCH_THRESHOLD,
      sortField: 'processedAt',
      sortDirection: 'desc'
    };
  }

  hasCriteria(criteria: SearchCriteria): boolean {
    const normalized = this.normalizeCriteria(criteria);
    return normalized.query.length > 0 || normalized.topicId.length > 0 || normalized.tags.length > 0 || normalized.labels.length > 0;
  }

  parseQueryParams(paramMap: ParamMap): SearchCriteria {
    const criteria = this.createEmptyCriteria();
    criteria.query = paramMap.get('q')?.trim() ?? '';
    criteria.topicId = this.normalizeTopicId(paramMap.get('topicId'));
    criteria.tags = this.normalizeTags(paramMap.getAll('tag'));
    criteria.labels = paramMap.getAll('label')
      .map(value => this.parseLabelParam(value))
      .filter((label): label is LabelAssignment => label !== null);
    criteria.tagMatchMode = this.normalizeMatchMode(paramMap.get('tagMode'), 'any');
    criteria.labelMatchMode = this.normalizeMatchMode(paramMap.get('labelMode'), 'all');
    criteria.page = this.parsePositiveInt(paramMap.get('page'), 1);
    criteria.pageSize = this.normalizePageSize(this.parsePositiveInt(paramMap.get('pageSize'), 20));
    criteria.sortField = paramMap.has('sortField')
      ? this.normalizeSortField(paramMap.get('sortField'))
      : (criteria.query.length > 0 ? 'relevance' : 'processedAt');
    criteria.sortDirection = paramMap.has('sortDirection')
      ? this.normalizeSortDirection(paramMap.get('sortDirection'))
      : 'desc';
    return criteria;
  }

  buildQueryParams(criteria: SearchCriteria): Record<string, string | string[] | null> {
    const normalized = this.normalizeCriteria(criteria);
    return {
      q: normalized.query || null,
      topicId: normalized.topicId || null,
      tag: normalized.tags.length > 0 ? normalized.tags : null,
      label: normalized.labels.length > 0
        ? normalized.labels.map(label => `${label.category}::${label.value}`)
        : null,
      tagMode: normalized.tagMatchMode !== 'any' ? normalized.tagMatchMode : null,
      labelMode: normalized.labelMatchMode !== 'all' ? normalized.labelMatchMode : null,
      page: normalized.page > 1 ? String(normalized.page) : null,
      pageSize: normalized.pageSize !== 20 ? String(normalized.pageSize) : null,
      sortField: normalized.sortField,
      sortDirection: normalized.sortDirection
    };
  }

  async syncUrl(router: Router, route: ActivatedRoute, criteria: SearchCriteria): Promise<void> {
    await router.navigate([], {
      relativeTo: route,
      queryParams: this.buildQueryParams(criteria),
      replaceUrl: true
    });
  }

  async search(criteria: SearchCriteria): Promise<void> {
    const normalized = this.normalizeCriteria(criteria);
    if (!this.hasCriteria(normalized)) {
      this.clear();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.paginationState.set({ page: normalized.page, pageSize: normalized.pageSize });

    try {
      const results = await firstValueFrom(
        this.http.post<SearchResultPage>(this.apiUrl, {
          query: normalized.query || null,
          topicClusterId: normalized.topicId || null,
          tags: normalized.tags,
          tagMatchMode: normalized.tagMatchMode,
          labels: normalized.labels,
          labelMatchMode: normalized.labelMatchMode,
          page: normalized.page,
          pageSize: normalized.pageSize,
          threshold: normalized.threshold,
          sortField: normalized.sortField,
          sortDirection: normalized.sortDirection
        })
      );

      this.results.set(results.items.map(result => this.normalizeResult(result)));
      this.totalCountState.set(results.totalCount);
      this.paginationState.set({
        page: results.page,
        pageSize: this.normalizePageSize(results.pageSize)
      });
    } catch {
      this.results.set([]);
      this.totalCountState.set(0);
      this.error.set('Search failed. Try again in a moment.');
    } finally {
      this.loading.set(false);
    }
  }

  private normalizeCriteria(criteria: SearchCriteria): SearchCriteria {
    return {
      ...criteria,
      query: criteria.query.trim(),
      topicId: this.normalizeTopicId(criteria.topicId),
      tags: this.normalizeTags(criteria.tags),
      labels: criteria.labels
        .map(label => ({
          category: label.category.trim(),
          value: label.value.trim()
        }))
        .filter(label => label.category.length > 0 && label.value.length > 0)
        .filter((label, index, items) =>
          items.findIndex(item =>
            item.category.toLowerCase() === label.category.toLowerCase() &&
            item.value.toLowerCase() === label.value.toLowerCase()) === index),
      tagMatchMode: this.normalizeMatchMode(criteria.tagMatchMode, 'any'),
      labelMatchMode: this.normalizeMatchMode(criteria.labelMatchMode, 'all'),
      page: criteria.page > 0 ? Math.floor(criteria.page) : 1,
      pageSize: this.normalizePageSize(criteria.pageSize),
      threshold: criteria.threshold >= 0 ? criteria.threshold : DEFAULT_SEARCH_THRESHOLD,
      ...this.normalizeSort(criteria.query.trim(), criteria.sortField, criteria.sortDirection)
    };
  }

  private normalizeTags(tags: readonly string[]): string[] {
    const seen = new Set<string>();
    const normalized: string[] = [];

    for (const tag of tags) {
      const trimmed = tag.trim();
      const key = trimmed.toLocaleLowerCase();
      if (trimmed.length === 0 || seen.has(key)) {
        continue;
      }

      seen.add(key);
      normalized.push(trimmed);
    }

    return normalized;
  }

  private normalizeResult(result: SearchResult): SearchResult {
    return {
      ...result,
      captureId: result.captureId,
      summary: result.summary?.trim() || null,
      processedAt: result.processedAt ?? null,
      tags: this.normalizeTags(result.tags),
      labels: result.labels
        .map(label => ({
          category: label.category.trim(),
          value: label.value.trim()
        }))
        .filter(label => label.category.length > 0 && label.value.length > 0)
    };
  }

  private normalizeMatchMode(value: string | null | undefined, fallback: SearchMatchMode): SearchMatchMode {
    if (value?.toLowerCase() === 'all') {
      return 'all';
    }

    if (value?.toLowerCase() === 'any') {
      return 'any';
    }

    return fallback;
  }

  private normalizeTopicId(value: string | null | undefined): string {
    const trimmed = value?.trim() ?? '';
    if (!trimmed) {
      return '';
    }

    return this.isGuid(trimmed) ? trimmed : '';
  }

  private normalizePageSize(value: number): number {
    return SEARCH_PAGE_SIZE_OPTIONS.includes(value as 20 | 50 | 100) ? value : 20;
  }

  private normalizeSort(
    query: string,
    sortField: SearchSortField | string | null | undefined,
    sortDirection: SearchSortDirection | string | null | undefined
  ): Pick<SearchCriteria, 'sortField' | 'sortDirection'> {
    const hasQuery = query.length > 0;
    let normalizedSortField = this.normalizeSortField(sortField);
    if (!normalizedSortField) {
      normalizedSortField = hasQuery ? 'relevance' : 'processedAt';
    }

    if (!hasQuery && normalizedSortField === 'relevance') {
      normalizedSortField = 'processedAt';
    }

    const normalizedSortDirection = this.normalizeSortDirection(sortDirection);
    return {
      sortField: normalizedSortField,
      sortDirection: normalizedSortDirection
    };
  }

  private normalizeSortField(value: string | null | undefined): SearchSortField {
    const normalized = value?.trim();
    if (normalized && SEARCH_SORT_FIELDS.includes(normalized as SearchSortField)) {
      return normalized as SearchSortField;
    }

    return 'processedAt';
  }

  private normalizeSortDirection(value: string | null | undefined): SearchSortDirection {
    if (value?.toLowerCase() === 'asc') {
      return 'asc';
    }

    return 'desc';
  }

  private parsePositiveInt(value: string | null, fallback: number): number {
    if (!value) {
      return fallback;
    }

    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
  }

  private parseLabelParam(value: string): LabelAssignment | null {
    const separatorIndex = value.indexOf('::');
    if (separatorIndex <= 0 || separatorIndex === value.length - 2) {
      return null;
    }

    const category = value.slice(0, separatorIndex).trim();
    const labelValue = value.slice(separatorIndex + 2).trim();
    if (!category || !labelValue) {
      return null;
    }

    return {
      category,
      value: labelValue
    };
  }

  private isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }
}
