import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CaptureAccepted, CaptureCreateRequest, CaptureDetail, CaptureListItem, CaptureProcessedInsight } from '../../shared/models/knowledge.model';

interface CaptureLabelDto {
  category: string;
  value: string;
}

type CaptureListItemWithLabels = CaptureListItem & {
  labels: CaptureLabelDto[];
};

type CaptureProcessedInsightWithLabels = CaptureProcessedInsight & {
  labels: CaptureLabelDto[];
};

type CaptureDetailWithLabels = Omit<CaptureDetail, 'processedInsight'> & {
  labels: CaptureLabelDto[];
  processedInsight: CaptureProcessedInsightWithLabels | null;
};

type CaptureCreateRequestWithLabels = CaptureCreateRequest & {
  labels?: CaptureLabelDto[];
};

export type CaptureSortField = 'contentType' | 'createdAt' | 'status' | 'sourceUrl';
export type CaptureSortDirection = 'asc' | 'desc';

interface CaptureSortState {
  field: CaptureSortField;
  direction: CaptureSortDirection;
}

@Injectable({
  providedIn: 'root'
})
export class CaptureStateService {
  private static readonly maxRawContentLength = 10000;
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/capture`;

  private capturesState = signal<CaptureListItemWithLabels[]>([]);
  private captureDetailState = signal<CaptureDetailWithLabels | null>(null);
  private sortState = signal<CaptureSortState>({ field: 'createdAt', direction: 'desc' });

  loadingList = signal(false);
  loadingDetail = signal(false);
  creating = signal(false);
  listError = signal<string | null>(null);
  detailError = signal<string | null>(null);
  createError = signal<string | null>(null);
  detailNotFound = signal(false);

  captures = computed(() => this.sortCaptures(this.capturesState(), this.sortState()));
  captureDetail = computed(() => this.captureDetailState());
  currentSort = computed(() => this.sortState());

  async loadCaptures(force = false): Promise<void> {
    if (this.loadingList()) {
      return;
    }

    if (!force && this.capturesState().length > 0) {
      return;
    }

    this.loadingList.set(true);
    this.listError.set(null);

    try {
      const captures = await firstValueFrom(
        this.http.get<CaptureDetailWithLabels[]>(this.apiUrl)
      );

      this.capturesState.set(captures.map(capture => this.mapCaptureListItem(capture)));
    } catch {
      this.capturesState.set([]);
      this.listError.set('Captures could not be loaded.');
    } finally {
      this.loadingList.set(false);
    }
  }

  async loadCaptureDetail(id: string): Promise<void> {
    if (!id) {
      this.captureDetailState.set(null);
      this.detailNotFound.set(true);
      this.detailError.set(null);
      return;
    }

    this.loadingDetail.set(true);
    this.detailError.set(null);
    this.detailNotFound.set(false);

    try {
      const capture = await firstValueFrom(
        this.http.get<CaptureDetailWithLabels>(`${this.apiUrl}/${id}`)
      );

      this.captureDetailState.set(this.normalizeCaptureDetail(capture));
    } catch (error: unknown) {
      this.captureDetailState.set(null);

      const status = typeof error === 'object' && error !== null && 'status' in error
        ? Number((error as { status?: unknown }).status)
        : undefined;

      if (status === 404) {
        this.detailNotFound.set(true);
      } else {
        this.detailError.set('Capture detail could not be loaded.');
      }
    } finally {
      this.loadingDetail.set(false);
    }
  }

  clearDetail(): void {
    this.captureDetailState.set(null);
    this.detailError.set(null);
    this.detailNotFound.set(false);
    this.loadingDetail.set(false);
  }

  async createCapture(request: CaptureCreateRequestWithLabels): Promise<CaptureAccepted> {
    this.creating.set(true);
    this.createError.set(null);

    try {
      const payload = this.mapCreateRequest(request);
      const accepted = await firstValueFrom(
        this.http.post<CaptureAccepted>(this.apiUrl, payload)
      );

      this.capturesState.set([]);
      return accepted;
    } catch {
      this.createError.set('Capture could not be created.');
      throw new Error('Capture could not be created.');
    } finally {
      this.creating.set(false);
    }
  }


  async retryCapture(id: string): Promise<void> {
    if (!id) {
      return;
    }

    await firstValueFrom(
      this.http.post(`${this.apiUrl}/${id}/retry`, {})
    );

    this.captureDetailState.set(null);
    await this.loadCaptures(true);
  }

  setSort(field: CaptureSortField): void {
    const currentSort = this.sortState();
    if (currentSort.field === field) {
      this.sortState.set({
        field,
        direction: currentSort.direction === 'asc' ? 'desc' : 'asc'
      });
      return;
    }

    this.sortState.set({
      field,
      direction: field === 'createdAt' ? 'desc' : 'asc'
    });
  }

  private sortCaptures(captures: CaptureListItemWithLabels[], sort: CaptureSortState): CaptureListItemWithLabels[] {
    return [...captures].sort((left, right) => {
      const leftValue = this.getSortValue(left, sort.field);
      const rightValue = this.getSortValue(right, sort.field);

      const comparison = leftValue.localeCompare(rightValue, undefined, {
        numeric: true,
        sensitivity: 'base'
      });

      return sort.direction === 'asc' ? comparison : -comparison;
    });
  }

  private getSortValue(capture: CaptureListItem, field: CaptureSortField): string {
    switch (field) {
      case 'createdAt':
        return capture.createdAt;
      case 'contentType':
        return capture.contentType;
      case 'status':
        return capture.status;
      case 'sourceUrl':
        return capture.sourceUrl;
    }
  }

  private mapCreateRequest(request: CaptureCreateRequestWithLabels): {
    sourceUrl: string;
    contentType: string;
    rawContent: string;
    metadata: string;
    tags: string[];
    labels: CaptureLabelDto[];
  } {
    const normalizedUrl = request.sourceUrl.trim();
    const normalizedContent = request.rawContent.trim();
    const tags = request.tags
      .map(tag => tag.trim())
      .filter(tag => tag.length > 0);
    const labels = this.normalizeLabels(request.labels);

    if (normalizedUrl && !normalizedContent) {
      return {
        sourceUrl: normalizedUrl,
        contentType: 'Article',
        rawContent: this.normalizeRawContent(normalizedUrl),
        metadata: JSON.stringify({
          source: 'frontend_url_input',
          capturedAt: new Date().toISOString()
        }),
        tags,
        labels
      };
    }

    return {
      sourceUrl: normalizedUrl,
      contentType: request.contentType,
      rawContent: this.normalizeRawContent(normalizedContent),
      metadata: JSON.stringify({
        source: 'frontend_manual_input',
        capturedAt: new Date().toISOString()
      }),
      tags,
      labels
    };
  }

  private normalizeRawContent(value: string): string {
    return value.length <= CaptureStateService.maxRawContentLength
      ? value
      : value.slice(0, CaptureStateService.maxRawContentLength);
  }

  private mapCaptureListItem(capture: CaptureDetailWithLabels): CaptureListItemWithLabels {
    return {
      id: capture.id,
      sourceUrl: capture.sourceUrl,
      contentType: capture.contentType,
      status: capture.status,
      createdAt: capture.createdAt,
      processedAt: capture.processedAt,
      failureReason: capture.failureReason,
      labels: this.normalizeLabels(capture.labels ?? capture.processedInsight?.labels)
    };
  }

  private normalizeCaptureDetail(capture: CaptureDetailWithLabels): CaptureDetailWithLabels {
    return {
      ...capture,
      labels: this.normalizeLabels(capture.labels),
      processedInsight: capture.processedInsight
        ? {
            ...capture.processedInsight,
            labels: this.normalizeLabels(capture.processedInsight.labels)
          }
        : null
    };
  }

  private normalizeLabels(labels: CaptureLabelDto[] | null | undefined): CaptureLabelDto[] {
    return (labels ?? [])
      .map(label => ({
        category: label.category.trim(),
        value: label.value.trim()
      }))
      .filter(label => label.category.length > 0 && label.value.length > 0);
  }
}
