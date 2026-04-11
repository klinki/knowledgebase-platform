import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CaptureSortField, CaptureStateService, PAGE_SIZE_OPTIONS } from '../../core/services/capture-state.service';

@Component({
  selector: 'app-captures',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="captures-page">
      <header>
        <div>
          <h1>Captures</h1>
          <p>Browse all captured items across their processing lifecycle.</p>
        </div>
        <a routerLink="/captures/new" class="premium-btn create-btn">Create Capture</a>
      </header>

      <!-- Filter bar -->
      <div class="filter-bar glass-card">
        <div class="filter-group">
          <label for="filter-type">Type</label>
          <select
            id="filter-type"
            [ngModel]="currentFilter().contentType ?? ''"
            (ngModelChange)="onTypeFilterChange($event)"
          >
            <option value="">All types</option>
            @for (type of captureState.availableContentTypes(); track type) {
              <option [value]="type">{{ type }}</option>
            }
          </select>
        </div>

        <div class="filter-group">
          <label for="filter-status">Status</label>
          <select
            id="filter-status"
            [ngModel]="currentFilter().status ?? ''"
            (ngModelChange)="onStatusFilterChange($event)"
          >
            <option value="">All statuses</option>
            @for (status of captureState.availableStatuses(); track status) {
              <option [value]="status">{{ status }}</option>
            }
          </select>
        </div>

        @if (hasActiveFilters()) {
          <button type="button" class="clear-filters-btn" (click)="captureState.clearFilters()">
            ✕ Clear filters
          </button>
        }

        <div class="filter-spacer"></div>

        <div class="filter-group page-size-group">
          <label for="page-size">Show</label>
          <select
            id="page-size"
            [ngModel]="currentPagination().pageSize"
            (ngModelChange)="onPageSizeChange($event)"
          >
            @for (size of pageSizeOptions; track size) {
              <option [ngValue]="size">{{ size }}</option>
            }
          </select>
        </div>

        <span class="results-count">{{ captureState.totalFilteredCount() }} result{{ captureState.totalFilteredCount() === 1 ? '' : 's' }}</span>
      </div>

      <!-- Table -->
      <div class="glass-card table-card">
        @if (captureState.loadingList()) {
          <div class="empty-state">
            <p>Loading captures...</p>
          </div>
        } @else if (captureState.listError()) {
          <div class="empty-state error">
            <p>{{ captureState.listError() }}</p>
          </div>
        } @else if (captureState.totalFilteredCount() === 0) {
          <div class="empty-state">
            @if (hasActiveFilters()) {
              <p>No captures match the current filters.</p>
              <button type="button" class="premium-btn clear-btn" (click)="captureState.clearFilters()">Clear filters</button>
            } @else {
              <p>No captures have been saved yet.</p>
            }
          </div>
        } @else {
          <div class="table-actions">
            <div class="selection-group">
              <span class="selection-summary">
                {{ selectedCount() }} selected
                @if (selectedFailedCount() > 0) {
                  <span> • {{ selectedFailedCount() }} failed</span>
                }
              </span>
              <button type="button" class="secondary-action-btn" (click)="toggleSelectPage(!allPageSelected())">
                {{ allPageSelected() ? 'Clear page' : 'Select page' }}
              </button>
              <button
                type="button"
                class="secondary-action-btn"
                (click)="selectFailedOnPage()"
                [disabled]="failedCountOnPage() === 0"
              >
                Select failed
              </button>
              <button
                type="button"
                class="secondary-action-btn"
                (click)="clearSelection()"
                [disabled]="selectedCount() === 0"
              >
                Clear selection
              </button>
            </div>

            <div class="retry-group">
              <button
                type="button"
                class="secondary-action-btn retry-btn"
                (click)="retrySelectedFailed()"
                [disabled]="selectedFailedCount() === 0 || bulkActionPending()"
              >
                {{ bulkActionPending() ? 'Retrying...' : 'Retry selected failed' }}
              </button>
              <button
                type="button"
                class="premium-btn retry-btn"
                (click)="retryAllFailed()"
                [disabled]="!canRetryAllFailed() || bulkActionPending()"
              >
                {{ bulkActionPending() ? 'Retrying...' : 'Retry all failed' }}
              </button>
            </div>
          </div>

          @if (bulkActionMessage()) {
            <div class="bulk-banner success">{{ bulkActionMessage() }}</div>
          }

          @if (bulkActionError()) {
            <div class="bulk-banner error">{{ bulkActionError() }}</div>
          }

          <table class="captures-table">
            <thead>
              <tr>
                <th class="checkbox-column">
                  <input
                    type="checkbox"
                    [checked]="allPageSelected()"
                    [disabled]="captureState.captures().length === 0"
                    aria-label="Select all captures on this page"
                    (click)="onCheckboxClick($event)"
                    (change)="toggleSelectPage(($any($event.target)).checked)"
                  />
                </th>
                <th>
                  <button type="button" (click)="sortBy('contentType')">
                    Type {{ sortIndicator('contentType') }}
                  </button>
                </th>
                <th>
                  <button type="button" (click)="sortBy('createdAt')">
                    Created {{ sortIndicator('createdAt') }}
                  </button>
                </th>
                <th>
                  <button type="button" (click)="sortBy('status')">
                    Status {{ sortIndicator('status') }}
                  </button>
                </th>
                <th>
                  <button type="button" (click)="sortBy('sourceUrl')">
                    Source {{ sortIndicator('sourceUrl') }}
                  </button>
                </th>
              </tr>
            </thead>
            <tbody>
              @for (capture of captureState.captures(); track capture.id) {
                <tr (click)="openCapture(capture.id)" tabindex="0" (keyup.enter)="openCapture(capture.id)">
                  <td class="checkbox-column">
                    <input
                      type="checkbox"
                      [checked]="isSelected(capture.id)"
                      [attr.aria-label]="'Select capture ' + capture.id"
                      (click)="onCheckboxClick($event)"
                      (change)="toggleSelection(capture.id, ($any($event.target)).checked)"
                    />
                  </td>
                  <td><span class="type-pill">{{ capture.contentType }}</span></td>
                  <td>{{ capture.createdAt | date:'medium' }}</td>
                  <td>
                    <span class="status-pill" [class]="statusClass(capture.status)">{{ capture.status }}</span>
                    @if (capture.status.toLowerCase() === 'failed') {
                      <div class="failure-reason">{{ capture.failureReason || 'Processing failed. Open detail to retry.' }}</div>
                    }
                  </td>
                  <td class="source-cell">{{ capture.sourceUrl }}</td>
                </tr>
              }
            </tbody>
          </table>

          <!-- Pagination -->
          @if (captureState.totalPages() > 1) {
            <div class="pagination-bar">
              <button
                type="button"
                class="page-btn"
                [disabled]="currentPagination().page === 1"
                (click)="captureState.setPage(1)"
                aria-label="First page"
              >⟨⟨</button>
              <button
                type="button"
                class="page-btn"
                [disabled]="currentPagination().page === 1"
                (click)="captureState.setPage(currentPagination().page - 1)"
                aria-label="Previous page"
              >⟨</button>

              @for (p of visiblePages(); track p) {
                @if (p === -1) {
                  <span class="page-ellipsis">…</span>
                } @else {
                  <button
                    type="button"
                    class="page-btn"
                    [class.active]="p === currentPagination().page"
                    (click)="captureState.setPage(p)"
                  >{{ p }}</button>
                }
              }

              <button
                type="button"
                class="page-btn"
                [disabled]="currentPagination().page === captureState.totalPages()"
                (click)="captureState.setPage(currentPagination().page + 1)"
                aria-label="Next page"
              >⟩</button>
              <button
                type="button"
                class="page-btn"
                [disabled]="currentPagination().page === captureState.totalPages()"
                (click)="captureState.setPage(captureState.totalPages())"
                aria-label="Last page"
              >⟩⟩</button>

              <span class="page-info">
                Page {{ currentPagination().page }} of {{ captureState.totalPages() }}
              </span>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 2rem;
    }

    h1 { font-size: 3rem; margin-bottom: 0.5rem; letter-spacing: -1px; }
    header p { color: #94a3b8; margin: 0; font-size: 1.05rem; }
    .create-btn { text-decoration: none; display: inline-flex; align-items: center; }

    /* ---- Filter bar ---- */
    .filter-bar {
      display: flex;
      align-items: center;
      gap: 1.25rem;
      padding: 1rem 1.5rem;
      margin-bottom: 1rem;
      flex-wrap: wrap;
    }

    .filter-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .filter-group label {
      color: #94a3b8;
      font-size: 0.82rem;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      white-space: nowrap;
    }

    .filter-group select {
      appearance: none;
      background: rgba(255, 255, 255, 0.06);
      color: #e2e8f0;
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      padding: 0.5rem 2rem 0.5rem 0.75rem;
      font-family: inherit;
      font-size: 0.88rem;
      cursor: pointer;
      transition: border-color 0.2s ease, background 0.2s ease;
      background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' fill='%2394a3b8' viewBox='0 0 16 16'%3E%3Cpath d='M8 11L3 6h10z'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 0.6rem center;
    }

    .filter-group select:hover,
    .filter-group select:focus-visible {
      border-color: rgba(99, 102, 241, 0.5);
      background-color: rgba(255, 255, 255, 0.09);
      outline: none;
    }

    .filter-group select option {
      background: #1e293b;
      color: #e2e8f0;
    }

    .clear-filters-btn {
      appearance: none;
      background: rgba(239, 68, 68, 0.12);
      color: #fca5a5;
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 8px;
      padding: 0.5rem 0.85rem;
      font-size: 0.82rem;
      cursor: pointer;
      transition: background 0.2s ease, border-color 0.2s ease;
    }

    .clear-filters-btn:hover {
      background: rgba(239, 68, 68, 0.22);
      border-color: rgba(239, 68, 68, 0.4);
    }

    .filter-spacer { flex: 1; }

    .results-count {
      color: #64748b;
      font-size: 0.82rem;
      white-space: nowrap;
    }

    /* ---- Table card ---- */
    .table-card {
      padding: 0;
      overflow: hidden;
    }

    .table-actions {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 1rem 1.2rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.06);
      flex-wrap: wrap;
    }

    .selection-group,
    .retry-group {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .selection-summary {
      color: #94a3b8;
      font-size: 0.85rem;
    }

    .secondary-action-btn {
      appearance: none;
      background: rgba(255, 255, 255, 0.06);
      color: #e2e8f0;
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
      padding: 0.55rem 0.9rem;
      font-size: 0.84rem;
      cursor: pointer;
      transition: background 0.2s ease, border-color 0.2s ease;
    }

    .secondary-action-btn:hover:not(:disabled) {
      background: rgba(255, 255, 255, 0.1);
      border-color: rgba(129, 140, 248, 0.24);
    }

    .secondary-action-btn:disabled,
    .retry-btn:disabled {
      opacity: 0.45;
      cursor: default;
    }

    .retry-btn {
      white-space: nowrap;
    }

    .bulk-banner {
      margin: 0 1.2rem 1rem;
      border-radius: 10px;
      padding: 0.85rem 1rem;
      font-size: 0.9rem;
    }

    .bulk-banner.success {
      background: rgba(34, 197, 94, 0.12);
      border: 1px solid rgba(34, 197, 94, 0.18);
      color: #bbf7d0;
    }

    .bulk-banner.error {
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.18);
      color: #fecaca;
    }

    .captures-table {
      width: 100%;
      border-collapse: collapse;
    }

    th, td {
      padding: 1rem 1.2rem;
      text-align: left;
    }

    thead th {
      color: #94a3b8;
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    }

    th button {
      appearance: none;
      background: transparent;
      border: none;
      color: inherit;
      font: inherit;
      cursor: pointer;
      padding: 0;
      text-transform: inherit;
      letter-spacing: inherit;
    }

    tbody tr {
      cursor: pointer;
      transition: background 0.2s ease;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04);
    }

    tbody tr:hover,
    tbody tr:focus-visible {
      background: rgba(255, 255, 255, 0.04);
      outline: none;
    }

    .checkbox-column {
      width: 3rem;
      padding-right: 0.2rem;
    }

    .checkbox-column input {
      width: 1rem;
      height: 1rem;
      cursor: pointer;
      accent-color: #6366f1;
    }

    td {
      color: #e2e8f0;
      vertical-align: middle;
    }

    .type-pill,
    .status-pill {
      display: inline-flex;
      align-items: center;
      padding: 0.35rem 0.7rem;
      border-radius: 999px;
      font-size: 0.82rem;
      font-weight: 600;
      border: 1px solid rgba(255, 255, 255, 0.08);
    }

    .type-pill {
      color: #c7d2fe;
      background: rgba(99, 102, 241, 0.14);
    }

    .status-completed {
      color: #bbf7d0;
      background: rgba(34, 197, 94, 0.16);
    }

    .status-processing,
    .status-pending {
      color: #fde68a;
      background: rgba(245, 158, 11, 0.16);
    }

    .status-failed {
      color: #fecaca;
      background: rgba(239, 68, 68, 0.16);
    }

    .failure-reason {
      margin-top: 0.45rem;
      color: #fecaca;
      font-size: 0.78rem;
      max-width: 20rem;
    }

    .source-cell {
      color: #cbd5e1;
      max-width: 28rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .empty-state {
      padding: 3rem 1rem;
      text-align: center;
      color: #94a3b8;
    }

    .empty-state.error {
      color: #fecaca;
    }

    .clear-btn {
      margin-top: 1rem;
      font-size: 0.88rem;
    }

    /* ---- Pagination bar ---- */
    .pagination-bar {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.85rem 1.2rem;
      border-top: 1px solid rgba(255, 255, 255, 0.06);
      flex-wrap: wrap;
    }

    .page-btn {
      appearance: none;
      background: transparent;
      color: #94a3b8;
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 6px;
      min-width: 2.2rem;
      height: 2.2rem;
      font-family: inherit;
      font-size: 0.82rem;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: background 0.2s ease, color 0.2s ease, border-color 0.2s ease;
    }

    .page-btn:hover:not(:disabled) {
      background: rgba(99, 102, 241, 0.14);
      color: #c7d2fe;
      border-color: rgba(99, 102, 241, 0.3);
    }

    .page-btn:disabled {
      opacity: 0.35;
      cursor: default;
    }

    .page-btn.active {
      background: rgba(99, 102, 241, 0.22);
      color: #e0e7ff;
      border-color: rgba(99, 102, 241, 0.4);
      font-weight: 600;
    }

    .page-ellipsis {
      color: #475569;
      font-size: 0.9rem;
      padding: 0 0.25rem;
      user-select: none;
    }

    .page-info {
      margin-left: auto;
      color: #64748b;
      font-size: 0.82rem;
      white-space: nowrap;
    }

    @media (max-width: 900px) {
      .table-actions {
        align-items: flex-start;
      }

      .retry-group {
        width: 100%;
      }
    }
  `]
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
