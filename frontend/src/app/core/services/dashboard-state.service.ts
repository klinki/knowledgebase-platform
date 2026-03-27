import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DashboardItem, DashboardOverview } from '../../shared/models/knowledge.model';

interface LabelDto {
  category: string;
  value: string;
}

type DashboardItemWithLabels = DashboardItem & {
  labels: LabelDto[];
};

type DashboardOverviewWithLabels = Omit<DashboardOverview, 'recentCaptures'> & {
  recentCaptures: DashboardItemWithLabels[];
};

@Injectable({
  providedIn: 'root'
})
export class DashboardStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/dashboard`;

  private overviewState = signal<DashboardOverviewWithLabels | null>(null);
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
        this.http.get<DashboardOverviewWithLabels>(`${this.apiUrl}/overview`)
      );

      this.overviewState.set(this.normalizeOverview(overview));
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

  private normalizeOverview(overview: DashboardOverviewWithLabels): DashboardOverviewWithLabels {
    return {
      ...overview,
      recentCaptures: overview.recentCaptures.map(capture => this.normalizeDashboardItem(capture))
    };
  }

  private normalizeDashboardItem(item: DashboardItemWithLabels): DashboardItemWithLabels {
    return {
      ...item,
      labels: this.normalizeLabels(item.labels)
    };
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
