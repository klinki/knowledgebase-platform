import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CaptureStateService } from '../../core/services/capture-state.service';

interface RenderNode {
  type: 'text' | 'list' | 'json';
  value: string;
  items: string[];
}

@Component({
  selector: 'app-capture-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="capture-detail-page">
      <header class="page-header">
        <div>
          <h1>Capture Detail</h1>
          <p>Inspect the stored payload and processed insight for a single capture.</p>
        </div>
        <a routerLink="/captures" class="back-link">Back to captures</a>
      </header>

      @if (captureState.loadingDetail()) {
        <div class="glass-card empty-state">
          <p>Loading capture detail...</p>
        </div>
      } @else if (captureState.detailError()) {
        <div class="glass-card empty-state error">
          <p>{{ captureState.detailError() }}</p>
        </div>
      } @else if (captureState.detailNotFound()) {
        <div class="glass-card empty-state">
          <h2>Capture not found</h2>
          <p>The requested capture does not exist or is not visible to your account.</p>
          <div class="actions">
            <a routerLink="/dashboard" class="premium-btn">Go to dashboard</a>
            <a routerLink="/captures" class="secondary-btn">Browse captures</a>
          </div>
        </div>
      } @else if (capture()) {
        <div class="detail-grid">
          <section class="glass-card summary-card">
            <h2>Overview</h2>
            <dl class="overview-grid">
              <div>
                <dt>Source</dt>
                <dd>
                  @if (capture()!.sourceUrl) {
                    <a [href]="capture()!.sourceUrl" target="_blank" rel="noreferrer">{{ capture()!.sourceUrl }}</a>
                  } @else {
                    Manual capture (no source URL)
                  }
                </dd>
              </div>
              <div>
                <dt>Type</dt>
                <dd>{{ capture()!.contentType }}</dd>
              </div>
              <div>
                <dt>Status</dt>
                <dd>{{ capture()!.status }}</dd>
              </div>
              <div>
                <dt>Created</dt>
                <dd>{{ capture()!.createdAt | date:'medium' }}</dd>
              </div>
              <div>
                <dt>Processed</dt>
                <dd>{{ capture()!.processedAt ? (capture()!.processedAt | date:'medium') : 'Not processed yet' }}</dd>
              </div>
              @if (capture()!.status.toLowerCase() === 'failed') {
                <div>
                  <dt>Failure reason</dt>
                  <dd>{{ capture()!.failureReason || 'Capture processing failed. Retry from this page.' }}</dd>
                </div>
              }
            </dl>
            @if (capture()!.status.toLowerCase() === 'failed') {
              <div class="retry-row">
                <button type="button" class="premium-btn" (click)="retryCapture()" [disabled]="retrying()">
                  {{ retrying() ? 'Retrying...' : 'Retry Capture' }}
                </button>
              </div>
            }

            @if (capture()!.tags.length > 0) {
              <div class="tag-list">
                @for (tag of capture()!.tags; track tag) {
                  <span class="tag-chip">{{ tag }}</span>
                }
              </div>
            }

            @if (capture()!.labels.length > 0) {
              <div class="label-list">
                @for (label of capture()!.labels; track label.category + ':' + label.value) {
                  <span class="label-chip">{{ label.category }}: {{ label.value }}</span>
                }
              </div>
            }
          </section>

          <section class="glass-card">
            <h2>Raw Content</h2>
            <pre class="content-block">{{ capture()!.rawContent }}</pre>
          </section>

          <section class="glass-card">
            <h2>Metadata</h2>
            @if (metadataNode().type === 'list') {
              <ul class="value-list">
                @for (item of metadataNode().items; track item) {
                  <li>{{ item }}</li>
                }
              </ul>
            } @else {
              <pre class="content-block">{{ metadataNode().value }}</pre>
            }
          </section>

          <section class="glass-card" *ngIf="capture()!.processedInsight as insight; else noInsight">
            <h2>Processed Insight</h2>
            <dl class="overview-grid">
              <div>
                <dt>Title</dt>
                <dd>{{ insight.title || '—' }}</dd>
              </div>
              <div>
                <dt>Summary</dt>
                <dd>{{ insight.summary || '—' }}</dd>
              </div>
              <div>
                <dt>Source Title</dt>
                <dd>{{ insight.sourceTitle || '—' }}</dd>
              </div>
              <div>
                <dt>Author</dt>
                <dd>{{ insight.author || '—' }}</dd>
              </div>
              <div>
                <dt>Processed At</dt>
                <dd>{{ insight.processedAt | date:'medium' }}</dd>
              </div>
            </dl>

            <div class="insight-block">
              <h3>Key Insights</h3>
              @if (keyInsightsNode().type === 'list') {
                <ul class="value-list">
                  @for (item of keyInsightsNode().items; track item) {
                    <li>{{ item }}</li>
                  }
                </ul>
              } @else {
                <pre class="content-block">{{ keyInsightsNode().value }}</pre>
              }
            </div>

            <div class="insight-block">
              <h3>Action Items</h3>
              @if (actionItemsNode().type === 'list') {
                <ul class="value-list">
                  @for (item of actionItemsNode().items; track item) {
                    <li>{{ item }}</li>
                  }
                </ul>
              } @else {
                <pre class="content-block">{{ actionItemsNode().value }}</pre>
              }
            </div>

            @if (insight.tags.length > 0) {
              <div class="tag-list">
                @for (tag of insight.tags; track tag) {
                  <span class="tag-chip">{{ tag }}</span>
                }
              </div>
            }

            @if (insight.labels.length > 0) {
              <div class="label-list">
                @for (label of insight.labels; track label.category + ':' + label.value) {
                  <span class="label-chip">{{ label.category }}: {{ label.value }}</span>
                }
              </div>
            }
          </section>

          <ng-template #noInsight>
            <section class="glass-card empty-state">
              <h2>Processed Insight</h2>
              <p>This capture does not have a processed insight yet.</p>
            </section>
          </ng-template>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 2rem;
    }

    h1 {
      font-size: 3rem;
      margin-bottom: 0.5rem;
      letter-spacing: -1px;
    }

    .page-header p {
      color: #94a3b8;
      margin: 0;
    }

    .back-link {
      color: #c7d2fe;
      text-decoration: none;
      padding-top: 0.8rem;
    }

    .detail-grid {
      display: grid;
      gap: 1.5rem;
    }

    .summary-card {
      border: 1px solid rgba(99, 102, 241, 0.14);
    }

    h2 {
      margin-top: 0;
      margin-bottom: 1.2rem;
      color: #f8fafc;
    }

    h3 {
      color: #e2e8f0;
      margin-bottom: 0.8rem;
    }

    .overview-grid {
      display: grid;
      gap: 1rem;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      margin: 0;
    }

    dt {
      color: #94a3b8;
      font-size: 0.82rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      margin-bottom: 0.35rem;
    }

    dd {
      margin: 0;
      color: #e2e8f0;
      line-height: 1.5;
      word-break: break-word;
    }

    dd a {
      color: #c7d2fe;
    }

    .content-block {
      margin: 0;
      padding: 1rem;
      white-space: pre-wrap;
      word-break: break-word;
      border-radius: 12px;
      background: rgba(15, 23, 42, 0.55);
      color: #dbeafe;
      font-family: Consolas, 'Courier New', monospace;
      font-size: 0.92rem;
    }

    .retry-row {
      margin-top: 1rem;
    }

    .retry-row button {
      border: none;
      cursor: pointer;
    }

    .tag-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.6rem;
      margin-top: 1.2rem;
    }

    .label-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.6rem;
      margin-top: 0.8rem;
    }

    .tag-chip {
      display: inline-flex;
      padding: 0.35rem 0.7rem;
      border-radius: 999px;
      background: rgba(99, 102, 241, 0.14);
      color: #c7d2fe;
      border: 1px solid rgba(129, 140, 248, 0.16);
    }

    .label-chip {
      display: inline-flex;
      padding: 0.35rem 0.7rem;
      border-radius: 999px;
      background: rgba(245, 158, 11, 0.12);
      color: #fde68a;
      border: 1px solid rgba(245, 158, 11, 0.18);
    }

    .value-list {
      margin: 0;
      padding-left: 1.2rem;
      color: #e2e8f0;
    }

    .value-list li + li {
      margin-top: 0.4rem;
    }

    .insight-block + .insight-block {
      margin-top: 1.5rem;
    }

    .empty-state {
      text-align: center;
      color: #94a3b8;
    }

    .empty-state.error {
      color: #fecaca;
    }

    .actions {
      display: flex;
      justify-content: center;
      gap: 1rem;
      margin-top: 1rem;
    }

    a.premium-btn,
    .secondary-btn {
      text-decoration: none;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }

    .secondary-btn {
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 8px;
      padding: 0.75rem 1.5rem;
      color: #cbd5e1;
    }
  `]
})
export class CaptureDetailComponent implements OnInit, OnDestroy {
  captureState = inject(CaptureStateService);
  private route = inject(ActivatedRoute);

  capture = computed(() => this.captureState.captureDetail());
  retrying = computed(() => this.captureState.loadingDetail());
  metadataNode = computed(() => this.parseContent(this.capture()?.metadata));
  keyInsightsNode = computed(() => this.parseContent(this.capture()?.processedInsight?.keyInsights));
  actionItemsNode = computed(() => this.parseContent(this.capture()?.processedInsight?.actionItems));

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    await this.captureState.loadCaptureDetail(id);
  }

  async retryCapture(): Promise<void> {
    const id = this.capture()?.id;
    if (!id) {
      return;
    }

    this.captureState.loadingDetail.set(true);
    this.captureState.detailError.set(null);

    try {
      await this.captureState.retryCapture(id);
      await this.captureState.loadCaptureDetail(id);
    } catch {
      this.captureState.detailError.set('Capture retry could not be started.');
    } finally {
      this.captureState.loadingDetail.set(false);
    }
  }

  ngOnDestroy(): void {
    this.captureState.clearDetail();
  }

  private parseContent(value: string | null | undefined): RenderNode {
    if (!value) {
      return { type: 'text', value: '—', items: [] };
    }

    try {
      const parsed = JSON.parse(value) as unknown;
      if (Array.isArray(parsed)) {
        return {
          type: 'list',
          value: '',
          items: parsed.map(item => String(item))
        };
      }

      if (parsed && typeof parsed === 'object') {
        return {
          type: 'json',
          value: JSON.stringify(parsed, null, 2),
          items: []
        };
      }
    } catch {
      return { type: 'text', value, items: [] };
    }

    return { type: 'text', value, items: [] };
  }
}
