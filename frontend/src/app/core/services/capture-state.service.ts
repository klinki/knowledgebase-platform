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

export interface CaptureFilterState {
  contentType: string | null;
  status: string | null;
}

export interface CapturePaginationState {
  page: number;
  pageSize: number;
}

export const PAGE_SIZE_OPTIONS = [10, 50, 100, 200] as const;

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
  private filterState = signal<CaptureFilterState>({ contentType: null, status: null });
  private paginationState = signal<CapturePaginationState>({ page: 1, pageSize: 10 });

  loadingList = signal(false);
  loadingDetail = signal(false);
  creating = signal(false);
  listError = signal<string | null>(null);
  detailError = signal<string | null>(null);
  createError = signal<string | null>(null);
  detailNotFound = signal(false);

  /** All captures after filtering and sorting (before pagination). */
  private filteredAndSorted = computed(() => {
    const all = this.capturesState();
    const filter = this.filterState();
    const sort = this.sortState();

    let result = all;

    if (filter.contentType) {
      const target = filter.contentType.toLowerCase();
      result = result.filter(c => c.contentType.toLowerCase() === target);
    }

    if (filter.status) {
      const target = filter.status.toLowerCase();
      result = result.filter(c => c.status.toLowerCase() === target);
    }

    return this.sortCaptures(result, sort);
  });

  /** Total items after filtering (used for pagination UI). */
  totalFilteredCount = computed(() => this.filteredAndSorted().length);

  /** The current page slice of captures. */
  captures = computed(() => {
    const all = this.filteredAndSorted();
    const { page, pageSize } = this.paginationState();
    const start = (page - 1) * pageSize;
    return all.slice(start, start + pageSize);
  });

  /** Total number of pages. */
  totalPages = computed(() => {
    const total = this.totalFilteredCount();
    const { pageSize } = this.paginationState();
    return Math.max(1, Math.ceil(total / pageSize));
  });

  /** Distinct content types from all loaded captures (for filter dropdown). */
  availableContentTypes = computed(() => {
    const types = new Set(this.capturesState().map(c => c.contentType));
    return [...types].sort((a, b) => a.localeCompare(b));
  });

  /** Distinct statuses from all loaded captures (for filter dropdown). */
  availableStatuses = computed(() => {
    const statuses = new Set(this.capturesState().map(c => c.status));
    return [...statuses].sort((a, b) => a.localeCompare(b));
  });

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
    } else {
      this.sortState.set({
        field,
        direction: field === 'createdAt' ? 'desc' : 'asc'
      });
    }

    // Reset to page 1 when sort changes
    this.paginationState.update(p => ({ ...p, page: 1 }));
  }

  setFilter(filter: Partial<CaptureFilterState>): void {
    this.filterState.update(current => ({ ...current, ...filter }));
    // Reset to page 1 when filter changes
    this.paginationState.update(p => ({ ...p, page: 1 }));
  }

  clearFilters(): void {
    this.filterState.set({ contentType: null, status: null });
    this.paginationState.update(p => ({ ...p, page: 1 }));
  }

  setPage(page: number): void {
    const clamped = Math.max(1, Math.min(page, this.totalPages()));
    this.paginationState.update(p => ({ ...p, page: clamped }));
  }

  setPageSize(pageSize: number): void {
    this.paginationState.set({ page: 1, pageSize });
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
