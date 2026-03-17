import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CaptureDetail, CaptureListItem } from '../../shared/models/knowledge.model';

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
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/capture`;

  private capturesState = signal<CaptureListItem[]>([]);
  private captureDetailState = signal<CaptureDetail | null>(null);
  private sortState = signal<CaptureSortState>({ field: 'createdAt', direction: 'desc' });

  loadingList = signal(false);
  loadingDetail = signal(false);
  listError = signal<string | null>(null);
  detailError = signal<string | null>(null);
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
        this.http.get<CaptureDetail[]>(this.apiUrl)
      );

      this.capturesState.set(captures.map(capture => ({
        id: capture.id,
        sourceUrl: capture.sourceUrl,
        contentType: capture.contentType,
        status: capture.status,
        createdAt: capture.createdAt,
        processedAt: capture.processedAt
      })));
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
        this.http.get<CaptureDetail>(`${this.apiUrl}/${id}`)
      );

      this.captureDetailState.set(capture);
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

  private sortCaptures(captures: CaptureListItem[], sort: CaptureSortState): CaptureListItem[] {
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
}
