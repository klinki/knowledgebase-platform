import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DashboardItem, SemanticSearchResult } from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class SearchStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/search`;

  results = signal<DashboardItem[]>([]);
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
        this.http.post<SemanticSearchResult[]>(`${this.apiUrl}/semantic`, {
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
        similarity: result.similarity
      })));
    } catch {
      this.results.set([]);
      this.error.set('Search failed. Try again in a moment.');
    } finally {
      this.loading.set(false);
    }
  }
}
