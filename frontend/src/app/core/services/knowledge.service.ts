import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { KnowledgeItem, Tag } from '../../shared/models/knowledge.model';
import { toSignal } from '@angular/core/rxjs-interop';
import { map, of, catchError, switchMap, debounceTime, distinctUntilChanged } from 'rxjs';

import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class KnowledgeService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  private itemsState = signal<KnowledgeItem[]>([
    { id: '1', title: 'DeepSeek-V3 Technical Report', url: '#', capturedAt: new Date() },
    { id: '2', title: 'Building Scalable .NET Core APIs', url: '#', capturedAt: new Date(Date.now() - 86400000) },
    { id: '3', title: 'Why Signals are the future of Angular', url: '#', capturedAt: new Date(Date.now() - 172800000) },
    { id: '4', title: 'Modern CSS Grid Layouts', url: '#', capturedAt: new Date(Date.now() - 259200000) },
    { id: '5', title: 'Refactoring to SOLID principles', url: '#', capturedAt: new Date(Date.now() - 345600000) },
  ]);

  private tagsState = signal<Tag[]>([
    { id: '1', name: 'AI', count: 42, lastUsed: new Date() },
    { id: '2', name: 'Angular', count: 28, lastUsed: new Date() },
    { id: '3', name: 'Architecture', count: 15, lastUsed: new Date(Date.now() - 86400000) },
    { id: '4', name: 'TypeScript', count: 12, lastUsed: new Date(Date.now() - 172800000) },
    { id: '5', name: 'Design', count: 8, lastUsed: new Date(Date.now() - 432000000) },
  ]);

  // Public Signals
  items = computed(() => this.itemsState());
  tags = computed(() => this.tagsState());
  
  recentItems = computed(() => this.itemsState().slice(0, 10));
  
  topTags = computed(() => [...this.tagsState()]
    .sort((a, b) => b.count - a.count)
    .slice(0, 10)
  );

  loading = signal(false);
  error = signal<string | null>(null);

  search(query: string) {
    if (!query) {
      this.error.set(null);
      return of(this.itemsState());
    }

    this.loading.set(true);
    return this.http.post<any[]>(`${this.apiUrl}/semantic`, {
      query,
      topK: 10,
      threshold: 0.3
    }).pipe(
      map(results => results.map(r => ({
        id: r.id,
        title: r.title,
        url: r.sourceUrl,
        capturedAt: new Date() // Backend DTO doesn't have capturedAt for semantic search yet
      }))),
      catchError(err => {
        console.error('Search failed', err);
        this.error.set('Search failed. Using local results.');
        return of(this.itemsState().filter(item => 
          item.title.toLowerCase().includes(query.toLowerCase())
        ));
      }),
      map(res => {
        this.loading.set(false);
        return res;
      })
    );
  }
}
