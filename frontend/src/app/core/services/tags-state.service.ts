import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TagSummary } from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class TagsStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/tags`;

  tags = signal<TagSummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  async loadTags(force = false): Promise<void> {
    if (this.loading()) {
      return;
    }

    if (!force && this.tags().length > 0) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const tags = await firstValueFrom(this.http.get<TagSummary[]>(this.apiUrl));
      this.tags.set(tags);
    } catch {
      this.tags.set([]);
      this.error.set('Tag summaries could not be loaded.');
    } finally {
      this.loading.set(false);
    }
  }
}
