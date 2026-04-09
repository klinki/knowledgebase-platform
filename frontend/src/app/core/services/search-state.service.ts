import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LabelAssignment, SearchResult } from '../../shared/models/knowledge.model';

export type SearchMatchMode = 'any' | 'all';

export interface SearchCriteria {
  query: string;
  tags: string[];
  tagMatchMode: SearchMatchMode;
  labels: LabelAssignment[];
  labelMatchMode: SearchMatchMode;
  limit: number;
  threshold: number;
}

@Injectable({
  providedIn: 'root'
})
export class SearchStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/search`;

  results = signal<SearchResult[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  clear(): void {
    this.results.set([]);
    this.error.set(null);
    this.loading.set(false);
  }

  createEmptyCriteria(): SearchCriteria {
    return {
      query: '',
      tags: [],
      tagMatchMode: 'any',
      labels: [],
      labelMatchMode: 'all',
      limit: 20,
      threshold: 0.3
    };
  }

  hasCriteria(criteria: SearchCriteria): boolean {
    const normalized = this.normalizeCriteria(criteria);
    return normalized.query.length > 0 || normalized.tags.length > 0 || normalized.labels.length > 0;
  }

  parseQueryParams(paramMap: ParamMap): SearchCriteria {
    const criteria = this.createEmptyCriteria();
    criteria.query = paramMap.get('q')?.trim() ?? '';
    criteria.tags = this.normalizeTags(paramMap.getAll('tag'));
    criteria.labels = paramMap.getAll('label')
      .map(value => this.parseLabelParam(value))
      .filter((label): label is LabelAssignment => label !== null);
    criteria.tagMatchMode = this.normalizeMatchMode(paramMap.get('tagMode'), 'any');
    criteria.labelMatchMode = this.normalizeMatchMode(paramMap.get('labelMode'), 'all');
    return criteria;
  }

  buildQueryParams(criteria: SearchCriteria): Record<string, string | string[] | null> {
    const normalized = this.normalizeCriteria(criteria);
    return {
      q: normalized.query || null,
      tag: normalized.tags.length > 0 ? normalized.tags : null,
      label: normalized.labels.length > 0
        ? normalized.labels.map(label => `${label.category}::${label.value}`)
        : null,
      tagMode: normalized.tagMatchMode !== 'any' ? normalized.tagMatchMode : null,
      labelMode: normalized.labelMatchMode !== 'all' ? normalized.labelMatchMode : null
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

    try {
      const results = await firstValueFrom(
        this.http.post<SearchResult[]>(this.apiUrl, {
          query: normalized.query || null,
          tags: normalized.tags,
          tagMatchMode: normalized.tagMatchMode,
          labels: normalized.labels,
          labelMatchMode: normalized.labelMatchMode,
          limit: normalized.limit,
          threshold: normalized.threshold
        })
      );

      this.results.set(results.map(result => this.normalizeResult(result)));
    } catch {
      this.results.set([]);
      this.error.set('Search failed. Try again in a moment.');
    } finally {
      this.loading.set(false);
    }
  }

  private normalizeCriteria(criteria: SearchCriteria): SearchCriteria {
    return {
      ...criteria,
      query: criteria.query.trim(),
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
      limit: criteria.limit > 0 ? criteria.limit : 20,
      threshold: criteria.threshold >= 0 ? criteria.threshold : 0.3
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
}
