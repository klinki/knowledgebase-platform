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
  mutationError = signal<string | null>(null);

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

  async createTag(name: string): Promise<boolean> {
    this.mutationError.set(null);
    try {
      const created = await firstValueFrom(
        this.http.post<TagSummary>(this.apiUrl, { name })
      );
      this.tags.update(tags => [created, ...tags]);
      return true;
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status;
      this.mutationError.set(
        status === 409 ? `A tag named "${name}" already exists.` : 'Failed to create tag.'
      );
      return false;
    }
  }

  async renameTag(id: string, name: string): Promise<boolean> {
    this.mutationError.set(null);
    try {
      const updated = await firstValueFrom(
        this.http.patch<TagSummary>(`${this.apiUrl}/${id}`, { name })
      );
      this.tags.update(tags => tags.map(t => t.id === id ? updated : t));
      return true;
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status;
      this.mutationError.set(
        status === 409 ? `A tag named "${name}" already exists.` : 'Failed to rename tag.'
      );
      return false;
    }
  }

  async deleteTag(id: string): Promise<boolean> {
    this.mutationError.set(null);
    try {
      await firstValueFrom(this.http.delete(`${this.apiUrl}/${id}`));
      this.tags.update(tags => tags.filter(t => t.id !== id));
      return true;
    } catch {
      this.mutationError.set('Failed to delete tag.');
      return false;
    }
  }
}
