import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DashboardItem, SemanticSearchResult } from '../../shared/models/knowledge.model';

interface LabelDto {
  category: string;
  value: string;
}

type DashboardItemWithLabels = DashboardItem & {
  labels: LabelDto[];
};

type SemanticSearchResultWithLabels = SemanticSearchResult & {
  labels: LabelDto[];
};

@Injectable({
  providedIn: 'root'
})
export class SearchStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/search`;

  results = signal<DashboardItemWithLabels[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  clear(): void {
    this.results.set([]);
    this.error.set(null);
    this.loading.set(false);
  }

  async search(query: string): Promise<void> {
    const normalizedQuery = query.trim();
    if (!normalizedQuery) {
      this.clear();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const results = await firstValueFrom(
        this.http.post<SemanticSearchResultWithLabels[]>(`${this.apiUrl}/semantic`, {
          query: normalizedQuery,
          topK: 10,
          threshold: 0.3
        })
      );

      this.results.set(results.map(result => ({
        id: result.id,
        title: result.title,
        sourceUrl: result.sourceUrl,
        capturedAt: null,
        status: null,
        tags: result.tags,
        summary: result.summary,
        similarity: result.similarity,
        labels: this.normalizeLabels(result.labels)
      })));
    } catch {
      this.results.set([]);
      this.error.set('Search failed. Try again in a moment.');
    } finally {
      this.loading.set(false);
    }
  }

  private normalizeLabels(labels: LabelDto[] | null | undefined): LabelDto[] {
    return (labels ?? [])
      .map(label => ({
        category: label.category.trim(),
        value: label.value.trim()
      }))
      .filter(label => label.category.length > 0 && label.value.length > 0);
  }
}
