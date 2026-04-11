import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CaptureAccepted,
  CaptureBulkRetryAccepted,
  CaptureCreateRequest,
  CaptureDetail,
  CaptureListItem,
  CaptureListPage,
  CaptureProcessedInsight,
  TopicClusterLink
} from '../../shared/models/knowledge.model';

interface CaptureLabelDto {
  category: string;
  value: string;
}

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

export interface CaptureFilterState {
  contentType: string | null;
  status: string | null;
}

export interface CapturePaginationState {
  page: number;
  pageSize: number;
}

export const PAGE_SIZE_OPTIONS = [10, 50, 100, 200] as const;
const AVAILABLE_CONTENT_TYPES = ['Article', 'Code', 'Note', 'Other', 'Tweet'] as const;
const AVAILABLE_STATUSES = ['Completed', 'Failed', 'Pending', 'Processing'] as const;

@Injectable({
  providedIn: 'root'
})
export class CaptureStateService {
  private static readonly maxRawContentLength = 10000;
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/capture`;

  private capturesState = signal<CaptureListItem[]>([]);
  private captureDetailState = signal<CaptureDetailWithLabels | null>(null);
  private totalCountState = signal(0);
  private sortState = signal<CaptureSortState>({ field: 'createdAt', direction: 'desc' });
  private filterState = signal<CaptureFilterState>({ contentType: null, status: null });
  private paginationState = signal<CapturePaginationState>({ page: 1, pageSize: 10 });

  loadingList = signal(false);
  loadingDetail = signal(false);
  creating = signal(false);
  listError = signal<string | null>(null);
  detailError = signal<string | null>(null);
  createError = signal<string | null>(null);
  detailNotFound = signal(false);

  totalFilteredCount = computed(() => this.totalCountState());
  captures = computed(() => this.capturesState());
  totalPages = computed(() => {
    const total = this.totalFilteredCount();
    const { pageSize } = this.paginationState();
    return Math.max(1, Math.ceil(total / pageSize));
  });
  availableContentTypes = computed(() => [...AVAILABLE_CONTENT_TYPES]);
  availableStatuses = computed(() => [...AVAILABLE_STATUSES]);

  captureDetail = computed(() => this.captureDetailState());
  currentSort = computed(() => this.sortState());
  currentFilter = computed(() => this.filterState());
  currentPagination = computed(() => this.paginationState());

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
      const { page, pageSize } = this.paginationState();
      const sort = this.sortState();
      const filter = this.filterState();

      const response = await firstValueFrom(
        this.http.get<CaptureListPage>(`${this.apiUrl}/list`, {
          params: {
            page,
            pageSize,
            sortField: sort.field,
            sortDirection: sort.direction,
            ...(filter.contentType ? { contentType: filter.contentType } : {}),
            ...(filter.status ? { status: filter.status } : {})
          }
        })
      );

      this.capturesState.set(response.items);
      this.totalCountState.set(response.totalCount);
    } catch {
      this.capturesState.set([]);
      this.totalCountState.set(0);
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
      this.totalCountState.set(0);
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

  async retryFailedCaptures(ids: string[]): Promise<CaptureBulkRetryAccepted> {
    const normalizedIds = ids
      .map(id => id.trim())
      .filter(id => id.length > 0);

    const response = await firstValueFrom(
      this.http.post<CaptureBulkRetryAccepted>(`${this.apiUrl}/retry-failed`, {
        captureIds: normalizedIds,
        retryAllMatching: false
      })
    );

    await this.loadCaptures(true);
    return response;
  }

  async retryAllFailedCaptures(filter: CaptureFilterState): Promise<CaptureBulkRetryAccepted> {
    const response = await firstValueFrom(
      this.http.post<CaptureBulkRetryAccepted>(`${this.apiUrl}/retry-failed`, {
        retryAllMatching: true,
        contentType: filter.contentType,
        status: filter.status
      })
    );

    await this.loadCaptures(true);
    return response;
  }

  setSort(field: CaptureSortField): void {
    const currentSort = this.sortState();
    if (currentSort.field === field) {
      this.sortState.set({
        field,
        direction: currentSort.direction === 'asc' ? 'desc' : 'asc'
      });
    } else {
      this.sortState.set({
        field,
        direction: field === 'createdAt' ? 'desc' : 'asc'
      });
    }

    this.paginationState.update(pagination => ({ ...pagination, page: 1 }));
    void this.loadCaptures(true);
  }

  setFilter(filter: Partial<CaptureFilterState>): void {
    this.filterState.update(current => ({ ...current, ...filter }));
    this.paginationState.update(pagination => ({ ...pagination, page: 1 }));
    void this.loadCaptures(true);
  }

  clearFilters(): void {
    this.filterState.set({ contentType: null, status: null });
    this.paginationState.update(pagination => ({ ...pagination, page: 1 }));
    void this.loadCaptures(true);
  }

  setPage(page: number): void {
    const clamped = Math.max(1, Math.min(page, this.totalPages()));
    this.paginationState.update(pagination => ({ ...pagination, page: clamped }));
    void this.loadCaptures(true);
  }

  setPageSize(pageSize: number): void {
    this.paginationState.set({ page: 1, pageSize });
    void this.loadCaptures(true);
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

  private normalizeCaptureDetail(capture: CaptureDetailWithLabels): CaptureDetailWithLabels {
    return {
      ...capture,
      labels: this.normalizeLabels(capture.labels),
      processedInsight: capture.processedInsight
        ? {
            ...capture.processedInsight,
            labels: this.normalizeLabels(capture.processedInsight.labels),
            cluster: this.normalizeCluster(capture.processedInsight.cluster)
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

  private normalizeCluster(cluster: TopicClusterLink | null | undefined): TopicClusterLink | null {
    if (!cluster) {
      return null;
    }

    return {
      ...cluster,
      title: cluster.title.trim(),
      description: cluster.description?.trim() ?? null,
      suggestedLabel: {
        category: cluster.suggestedLabel.category.trim(),
        value: cluster.suggestedLabel.value.trim()
      }
    };
  }
}
