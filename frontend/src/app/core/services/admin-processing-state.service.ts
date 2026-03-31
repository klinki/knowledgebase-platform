import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CaptureProcessingAdminOverview, DashboardItem, LabelAssignment } from '../../shared/models/knowledge.model';

type AdminOverviewWithNormalizedCaptures = Omit<CaptureProcessingAdminOverview, 'recentCaptures'> & {
  recentCaptures: DashboardItem[];
};

@Injectable({
  providedIn: 'root'
})
export class AdminProcessingStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/admin/processing`;

  private overviewState = signal<AdminOverviewWithNormalizedCaptures | null>(null);
  loading = signal(false);
  submitting = signal(false);
  error = signal<string | null>(null);

  overview = computed(() => this.overviewState());
  isPaused = computed(() => this.overviewState()?.isPaused ?? false);
  changedAt = computed(() => this.overviewState()?.changedAt ?? null);
  changedByDisplayName = computed(() => this.overviewState()?.changedByDisplayName ?? null);
  captureCounts = computed(() => this.overviewState()?.captureCounts ?? {
    pending: 0,
    processing: 0,
    completed: 0,
    failed: 0
  });
  jobCounts = computed(() => this.overviewState()?.jobCounts ?? {
    enqueued: 0,
    scheduled: 0,
    processing: 0,
    failed: 0
  });
  recentCaptures = computed(() => this.overviewState()?.recentCaptures ?? []);

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
        this.http.get<CaptureProcessingAdminOverview>(this.apiUrl)
      );

      this.overviewState.set(this.normalizeOverview(overview));
    } catch {
      this.error.set('Processing controls could not be loaded.');
    } finally {
      this.loading.set(false);
    }
  }

  async pauseProcessing(): Promise<void> {
    await this.submitAction('pause');
  }

  async resumeProcessing(): Promise<void> {
    await this.submitAction('resume');
  }

  private async submitAction(action: 'pause' | 'resume'): Promise<void> {
    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    try {
      const overview = await firstValueFrom(
        this.http.post<CaptureProcessingAdminOverview>(`${this.apiUrl}/${action}`, {})
      );

      this.overviewState.set(this.normalizeOverview(overview));
    } catch {
      this.error.set(`Processing could not be ${action}d.`);
    } finally {
      this.submitting.set(false);
    }
  }

  private normalizeOverview(overview: CaptureProcessingAdminOverview): AdminOverviewWithNormalizedCaptures {
    return {
      ...overview,
      recentCaptures: overview.recentCaptures.map(capture => this.normalizeCapture(capture))
    };
  }

  private normalizeCapture(item: DashboardItem): DashboardItem {
    return {
      ...item,
      labels: this.normalizeLabels(item.labels)
    };
  }

  private normalizeLabels(labels: LabelAssignment[] | null | undefined): LabelAssignment[] {
    return (labels ?? [])
      .map(label => ({
        category: label.category.trim(),
        value: label.value.trim()
      }))
      .filter(label => label.category.length > 0 && label.value.length > 0);
  }
}
