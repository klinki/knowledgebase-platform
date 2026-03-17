import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CaptureSortField, CaptureStateService } from '../../core/services/capture-state.service';

@Component({
  selector: 'app-captures',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="captures-page">
      <header>
        <h1>Captures</h1>
        <p>Browse all captured items across their processing lifecycle.</p>
      </header>

      <div class="glass-card">
        @if (captureState.loadingList()) {
          <div class="empty-state">
            <p>Loading captures...</p>
          </div>
        } @else if (captureState.listError()) {
          <div class="empty-state error">
            <p>{{ captureState.listError() }}</p>
          </div>
        } @else if (captureState.captures().length === 0) {
          <div class="empty-state">
            <p>No captures have been saved yet.</p>
          </div>
        } @else {
          <table class="captures-table">
            <thead>
              <tr>
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
                  <td><span class="type-pill">{{ capture.contentType }}</span></td>
                  <td>{{ capture.createdAt | date:'medium' }}</td>
                  <td><span class="status-pill" [class]="statusClass(capture.status)">{{ capture.status }}</span></td>
                  <td class="source-cell">{{ capture.sourceUrl }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </div>
  `,
  styles: [`
    h1 { font-size: 3rem; margin-bottom: 0.5rem; letter-spacing: -1px; }
    header p { color: #94a3b8; margin-bottom: 2rem; font-size: 1.05rem; }

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
  `]
})
export class CapturesComponent implements OnInit {
  captureState = inject(CaptureStateService);
  private router = inject(Router);

  currentSort = computed(() => this.captureState.currentSort());

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

  async openCapture(id: string): Promise<void> {
    await this.router.navigate(['/captures', id]);
  }
}
