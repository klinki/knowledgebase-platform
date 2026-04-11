import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CaptureSortField, CaptureStateService, PAGE_SIZE_OPTIONS } from '../../core/services/capture-state.service';

@Component({
  selector: 'app-captures',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './captures.component.html',
  styleUrl: './captures.component.scss'
})
export class CapturesComponent implements OnInit {
  captureState = inject(CaptureStateService);
  private router = inject(Router);

  readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  selectedCaptureIds = signal<string[]>([]);
  bulkActionPending = signal(false);
  bulkActionMessage = signal<string | null>(null);
  bulkActionError = signal<string | null>(null);
  currentSort = computed(() => this.captureState.currentSort());
  currentFilter = computed(() => this.captureState.currentFilter());
  currentPagination = computed(() => this.captureState.currentPagination());
  selectedCount = computed(() => this.selectedCaptureIds().length);
  selectedFailedIds = computed(() => {
    const selected = new Set(this.selectedCaptureIds());
    return this.captureState.captures()
      .filter(capture => selected.has(capture.id) && capture.status.toLowerCase() === 'failed')
      .map(capture => capture.id);
  });
  selectedFailedCount = computed(() => this.selectedFailedIds().length);
  failedCountOnPage = computed(() =>
    this.captureState.captures().filter(capture => capture.status.toLowerCase() === 'failed').length
  );
  allPageSelected = computed(() => {
    const captures = this.captureState.captures();
    if (captures.length === 0) {
      return false;
    }

    const selected = new Set(this.selectedCaptureIds());
    return captures.every(capture => selected.has(capture.id));
  });
  canRetryAllFailed = computed(() => {
    if (this.captureState.totalFilteredCount() === 0) {
      return false;
    }

    const statusFilter = this.currentFilter().status;
    return statusFilter === null || statusFilter === 'Failed';
  });
  hasActiveFilters = computed(() => {
    const f = this.currentFilter();
    return f.contentType !== null || f.status !== null;
  });

  /** Compute a window of page numbers to show, with ellipsis markers (-1). */
  visiblePages = computed(() => {
    const total = this.captureState.totalPages();
    const current = this.currentPagination().page;
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }

    // Always show first page
    pages.push(1);

    if (current > 3) pages.push(-1); // ellipsis

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);

    for (let i = start; i <= end; i++) pages.push(i);

    if (current < total - 2) pages.push(-1); // ellipsis

    // Always show last page
    pages.push(total);

    return pages;
  });

  constructor() {
    effect(() => {
      const captureIds = new Set(this.captureState.captures().map(capture => capture.id));
      this.selectedCaptureIds.update(selectedIds => selectedIds.filter(id => captureIds.has(id)));
    });
  }

  async ngOnInit(): Promise<void> {
    await this.captureState.loadCaptures();
  }

  sortBy(field: CaptureSortField): void {
    this.captureState.setSort(field);
  }

  sortIndicator(field: CaptureSortField): string {
    const currentSort = this.currentSort();
    if (currentSort.field !== field) {
      return '';
    }

    return currentSort.direction === 'asc' ? '↑' : '↓';
  }

  statusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  onTypeFilterChange(value: string): void {
    this.captureState.setFilter({ contentType: value || null });
  }

  onStatusFilterChange(value: string): void {
    this.captureState.setFilter({ status: value || null });
  }

  onPageSizeChange(size: number): void {
    this.captureState.setPageSize(size);
  }

  isSelected(id: string): boolean {
    return this.selectedCaptureIds().includes(id);
  }

  toggleSelection(id: string, checked: boolean): void {
    this.bulkActionMessage.set(null);
    this.bulkActionError.set(null);
    this.selectedCaptureIds.update(selectedIds => {
      const next = new Set(selectedIds);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }

      return [...next];
    });
  }

  toggleSelectPage(checked: boolean): void {
    this.bulkActionMessage.set(null);
    this.bulkActionError.set(null);
    if (!checked) {
      this.clearSelection();
      return;
    }

    this.selectedCaptureIds.set(this.captureState.captures().map(capture => capture.id));
  }

  selectFailedOnPage(): void {
    this.bulkActionMessage.set(null);
    this.bulkActionError.set(null);
    this.selectedCaptureIds.set(
      this.captureState.captures()
        .filter(capture => capture.status.toLowerCase() === 'failed')
        .map(capture => capture.id)
    );
  }

  clearSelection(): void {
    this.selectedCaptureIds.set([]);
  }

  onCheckboxClick(event: Event): void {
    event.stopPropagation();
  }

  async retrySelectedFailed(): Promise<void> {
    const ids = this.selectedFailedIds();
    if (ids.length === 0 || this.bulkActionPending()) {
      return;
    }

    this.bulkActionPending.set(true);
    this.bulkActionMessage.set(null);
    this.bulkActionError.set(null);

    try {
      const response = await this.captureState.retryFailedCaptures(ids);
      this.selectedCaptureIds.set([]);
      this.bulkActionMessage.set(
        response.retriedCount === 0
          ? 'No failed captures were eligible for retry.'
          : `Retried ${response.retriedCount} failed capture${response.retriedCount === 1 ? '' : 's'}.`
      );
    } catch {
      this.bulkActionError.set('Failed captures could not be retried.');
    } finally {
      this.bulkActionPending.set(false);
    }
  }

  async retryAllFailed(): Promise<void> {
    if (!this.canRetryAllFailed() || this.bulkActionPending()) {
      return;
    }

    this.bulkActionPending.set(true);
    this.bulkActionMessage.set(null);
    this.bulkActionError.set(null);

    try {
      const response = await this.captureState.retryAllFailedCaptures(this.currentFilter());
      this.selectedCaptureIds.set([]);
      this.bulkActionMessage.set(
        response.retriedCount === 0
          ? 'No failed captures matched the current retry scope.'
          : `Retried ${response.retriedCount} failed capture${response.retriedCount === 1 ? '' : 's'} across the current scope.`
      );
    } catch {
      this.bulkActionError.set('Failed captures could not be retried.');
    } finally {
      this.bulkActionPending.set(false);
    }
  }

  async openCapture(id: string): Promise<void> {
    await this.router.navigate(['/captures', id]);
  }
}
