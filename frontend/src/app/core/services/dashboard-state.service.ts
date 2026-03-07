import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DashboardOverview } from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/dashboard`;

  private overviewState = signal<DashboardOverview | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  overview = computed(() => this.overviewState());
  recentCaptures = computed(() => this.overviewState()?.recentCaptures ?? []);
  topTags = computed(() => this.overviewState()?.topTags ?? []);
  stats = computed(() => this.overviewState()?.stats ?? {
    totalCaptures: 0,
    activeTags: 0
  });

  async loadOverview(force = false): Promise<void> {
    if (this.loading()) {
      return;
    }

    if (!force && this.overviewState()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const overview = await firstValueFrom(
        this.http.get<DashboardOverview>(`${this.apiUrl}/overview`)
      );

      this.overviewState.set(overview);
    } catch {
      this.error.set('Dashboard data could not be loaded.');
      this.overviewState.set({
        recentCaptures: [],
        topTags: [],
        stats: {
          totalCaptures: 0,
          activeTags: 0
        }
      });
    } finally {
      this.loading.set(false);
    }
  }
}
