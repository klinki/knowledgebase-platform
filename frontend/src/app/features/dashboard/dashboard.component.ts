import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { DashboardStateService } from '../../core/services/dashboard-state.service';
import { AdminProcessingStateService } from '../../core/services/admin-processing-state.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="dashboard">
      <header class="dashboard-header">
        <div class="header-content">
          <h1>Sentinel Vault</h1>
          <p>Browse recent captures, monitor activity, and jump into deeper search from the dedicated search page.</p>
        </div>
      </header>

      @if (isAdmin()) {
        <section class="glass-card ops-panel">
          <div class="ops-header">
            <div>
              <h2>Processing Control</h2>
              <p>Global capture processing state across all users and queued jobs.</p>
            </div>

            <div class="ops-actions">
              <span class="ops-state" [class.paused]="adminProcessingState.isPaused()">
                {{ adminProcessingState.isPaused() ? 'Paused' : 'Running' }}
              </span>
              <button
                type="button"
                class="premium-btn"
                (click)="toggleProcessing()"
                [disabled]="adminProcessingState.submitting() || adminProcessingState.loading()"
              >
                {{ adminProcessingState.submitting()
                  ? (adminProcessingState.isPaused() ? 'Resuming...' : 'Pausing...')
                  : (adminProcessingState.isPaused() ? 'Resume Processing' : 'Pause Processing') }}
              </button>
            </div>
          </div>

          @if (adminProcessingState.error()) {
            <div class="banner-error">{{ adminProcessingState.error() }}</div>
          }

          <div class="ops-meta">
            @if (adminProcessingState.changedAt()) {
              Last changed
              @if (adminProcessingState.changedByDisplayName()) {
                by {{ adminProcessingState.changedByDisplayName() }}
              }
              on {{ adminProcessingState.changedAt() | date:'medium' }}
            } @else {
              Processing has not been manually changed yet.
            }
          </div>

          <div class="ops-grid">
            <div class="ops-card">
              <h3>Capture Status</h3>
              <div class="ops-stats">
                <div class="ops-stat"><span>Pending</span><strong>{{ adminProcessingState.captureCounts().pending }}</strong></div>
                <div class="ops-stat"><span>Processing</span><strong>{{ adminProcessingState.captureCounts().processing }}</strong></div>
                <div class="ops-stat"><span>Completed</span><strong>{{ adminProcessingState.captureCounts().completed }}</strong></div>
                <div class="ops-stat"><span>Failed</span><strong>{{ adminProcessingState.captureCounts().failed }}</strong></div>
              </div>
            </div>

            <div class="ops-card">
              <h3>Hangfire Jobs</h3>
              <div class="ops-stats">
                <div class="ops-stat"><span>Enqueued</span><strong>{{ adminProcessingState.jobCounts().enqueued }}</strong></div>
                <div class="ops-stat"><span>Scheduled</span><strong>{{ adminProcessingState.jobCounts().scheduled }}</strong></div>
                <div class="ops-stat"><span>Processing</span><strong>{{ adminProcessingState.jobCounts().processing }}</strong></div>
                <div class="ops-stat"><span>Failed</span><strong>{{ adminProcessingState.jobCounts().failed }}</strong></div>
              </div>
            </div>

            <div class="ops-card recent-system-captures">
              <h3>Recent System Captures</h3>
              @if (adminProcessingState.loading() && adminProcessingState.recentCaptures().length === 0) {
                <div class="empty-state compact">
                  <p>Loading processing controls...</p>
                </div>
              } @else if (adminProcessingState.recentCaptures().length === 0) {
                <div class="empty-state compact">
                  <p>No captures available yet.</p>
                </div>
              } @else {
                <div class="ops-capture-list">
                  @for (capture of adminProcessingState.recentCaptures(); track capture.id) {
                    <div class="ops-capture-item">
                      <div class="ops-capture-head">
                        <span class="ops-capture-title">{{ capture.title || capture.sourceUrl || 'Untitled capture' }}</span>
                        <span class="status-chip" [class.paused]="capture.status === 'Pending'">{{ capture.status }}</span>
                      </div>
                      <div class="ops-capture-meta">
                        {{ capture.capturedAt | date:'medium' }} • {{ capture.sourceUrl || 'No source URL' }}
                      </div>
                    </div>
                  }
                </div>
              }
            </div>
          </div>
        </section>
      }

      <div class="main-grid">
        <section class="glass-card list-section">
          <div class="section-head">
            <h2>Recent Captures</h2>
            <a routerLink="/search" class="secondary-link">Open search</a>
          </div>

          <div class="items-list">
            @if (error()) {
              <div class="empty-state">
                <p>{{ error() }}</p>
              </div>
            } @else if (loading() && items().length === 0) {
              <div class="empty-state">
                <p>Loading dashboard data...</p>
              </div>
            } @else {
              @for (item of items(); track item.id) {
                <a class="knowledge-item" [routerLink]="['/captures', item.id]">
                  <span class="status-dot"></span>
                  <div class="item-info">
                    <span class="title">{{ item.title }}</span>
                    <span class="meta">
                      @if (item.capturedAt) {
                        {{ item.capturedAt | date:'mediumDate' }} •
                      }
                      {{ item.sourceUrl }}
                    </span>
                    @if (item.summary) {
                      <span class="summary">{{ item.summary }}</span>
                    }
                    @if (item.tags.length > 0) {
                      <div class="item-tags">
                        @for (tag of item.tags; track tag) {
                          <span class="tag-chip">{{ tag }}</span>
                        }
                      </div>
                    }
                    @if (item.labels.length > 0) {
                      <div class="item-labels">
                        @for (label of item.labels; track label.category + ':' + label.value) {
                          <span class="label-chip">{{ label.category }}: {{ label.value }}</span>
                        }
                      </div>
                    }
                  </div>
                </a>
              } @empty {
                @if (!loading()) {
                  <div class="empty-state">
                    <p>No captures have been saved yet.</p>
                    <div class="empty-actions">
                      <a routerLink="/captures/new" class="premium-btn">Create Capture</a>
                      <a routerLink="/captures" class="secondary-link">Connect browser extension</a>
                      <a routerLink="/admin/invitations" class="secondary-link">Invite teammates</a>
                    </div>
                  </div>
                }
              }
            }
          </div>
        </section>

        <section class="secondary-column">
          <div class="glass-card topics-section">
            <div class="section-head">
              <h2>Topics</h2>
              <a routerLink="/topics" class="secondary-link">View all</a>
            </div>
            @if (dashboardState.topicClusters().length > 0) {
              <div class="topics-list">
                @for (cluster of dashboardState.topicClusters(); track cluster.id) {
                  <article class="topic-card">
                    <div class="topic-head">
                      <a class="topic-title-link" [routerLink]="['/topics', cluster.id]">{{ cluster.title }}</a>
                      <span class="topic-count">{{ cluster.memberCount }}</span>
                    </div>
                    @if (cluster.description) {
                      <span class="topic-description">{{ cluster.description }}</span>
                    }
                    @if (cluster.representativeInsights.length > 0) {
                      <div class="topic-links">
                        @for (insight of cluster.representativeInsights; track insight.processedInsightId) {
                          <a class="topic-linkish" [routerLink]="['/captures', insight.captureId]">{{ insight.title }}</a>
                        }
                      </div>
                    }
                    <div class="topic-label">{{ cluster.suggestedLabel.category }}: {{ cluster.suggestedLabel.value }}</div>
                    <a class="topic-link" [routerLink]="['/topics', cluster.id]">View topic</a>
                  </article>
                }
              </div>
            } @else {
              <div class="empty-state compact">
                <p>No topic groups available yet.</p>
              </div>
            }
          </div>

          <div class="glass-card tags-section">
            <h2>Trending Tags</h2>
            @if (dashboardState.topTags().length > 0) {
              <div class="tags-cloud">
              @for (tag of dashboardState.topTags(); track tag.id) {
                <span class="tag-badge">
                  {{ tag.name }} <span class="count">{{ tag.count }}</span>
                </span>
              }
              </div>
            } @else {
              <div class="empty-state compact">
                <p>No tag data available yet.</p>
              </div>
            }
          </div>

          <div class="glass-card stats-card">
            <h2>Quick Stats</h2>
            <div class="stat">
              <span class="stat-label">Total Captures</span>
              <span class="stat-value">{{ dashboardState.stats().totalCaptures }}</span>
            </div>
            <div class="stat">
              <span class="stat-label">Active Tags</span>
              <span class="stat-value">{{ dashboardState.stats().activeTags }}</span>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { animation: fadeIn 0.4s ease-out; }

    .dashboard-header {
      padding: 3rem 0 4rem;
      text-align: center;

      h1 {
        font-size: 3.5rem;
        margin-bottom: 0.5rem;
        background: linear-gradient(to bottom, #fff, #94a3b8);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }

      p { color: #94a3b8; font-size: 1.1rem; margin-bottom: 0; }
    }

    .banner-error {
      margin-bottom: 1rem;
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.18);
      color: #fecaca;
      border-radius: 12px;
      padding: 0.8rem 1rem;
    }

    .ops-panel {
      margin-bottom: 2rem;
    }

    .ops-header {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: flex-start;
      margin-bottom: 1rem;

      h2 {
        margin-bottom: 0.35rem;
      }

      p {
        color: #94a3b8;
        margin: 0;
      }
    }

    .ops-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
      justify-content: flex-end;
    }

    .ops-state {
      padding: 0.4rem 0.8rem;
      border-radius: 999px;
      background: rgba(34, 197, 94, 0.14);
      color: #86efac;
      border: 1px solid rgba(34, 197, 94, 0.25);

      &.paused {
        background: rgba(245, 158, 11, 0.14);
        color: #fcd34d;
        border-color: rgba(245, 158, 11, 0.25);
      }
    }

    .ops-meta {
      color: #94a3b8;
      margin-bottom: 1.25rem;
      font-size: 0.9rem;
    }

    .ops-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 1rem;
    }

    .ops-card {
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid rgba(255, 255, 255, 0.05);
      border-radius: 16px;
      padding: 1.25rem;

      h3 {
        margin: 0 0 1rem;
        font-size: 1rem;
        color: #f8fafc;
      }
    }

    .ops-stats {
      display: grid;
      gap: 0.75rem;
    }

    .ops-stat {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      color: #cbd5e1;

      strong {
        color: #f8fafc;
      }
    }

    .recent-system-captures {
      grid-column: span 1;
    }

    .ops-capture-list {
      display: grid;
      gap: 0.75rem;
    }

    .ops-capture-item {
      padding: 0.9rem 1rem;
      border-radius: 12px;
      background: rgba(15, 23, 42, 0.55);
      border: 1px solid rgba(255, 255, 255, 0.05);
    }

    .ops-capture-head {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: center;
      margin-bottom: 0.35rem;
    }

    .ops-capture-title {
      color: #f8fafc;
      font-weight: 500;
      overflow-wrap: anywhere;
    }

    .ops-capture-meta {
      color: #94a3b8;
      font-size: 0.85rem;
      overflow-wrap: anywhere;
    }

    .status-chip {
      flex-shrink: 0;
      border-radius: 999px;
      padding: 0.2rem 0.65rem;
      background: rgba(99, 102, 241, 0.12);
      color: #c7d2fe;
      border: 1px solid rgba(129, 140, 248, 0.14);

      &.paused {
        background: rgba(245, 158, 11, 0.12);
        color: #fde68a;
        border-color: rgba(245, 158, 11, 0.18);
      }
    }

    .main-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      padding-bottom: 4rem;
    }

    .glass-card {
      padding: 2rem;
      h2 {
        font-size: 1.25rem;
        margin-bottom: 2rem;
        color: #f8fafc;
        display: flex;
        align-items: center;
        gap: 12px;
      }
    }

    .section-head {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: center;
      margin-bottom: 2rem;

      h2 {
        margin: 0;
      }
    }

    .knowledge-item {
      text-decoration: none;
      display: flex;
      align-items: center;
      gap: 1.5rem;
      padding: 1.25rem;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.02);
      margin-bottom: 1rem;
      transition: background 0.2s;

      &:hover { background: rgba(255, 255, 255, 0.05); }

      .status-dot { width: 10px; height: 10px; border-radius: 50%; background: #6366f1; flex-shrink: 0; }
      .item-info { display: flex; flex-direction: column; }
      .title { color: #f8fafc; font-weight: 500; font-size: 1.1rem; margin-bottom: 4px; }
      .meta { font-size: 0.85rem; color: #64748b; }
      .summary { margin-top: 8px; color: #cbd5e1; font-size: 0.9rem; }
      .item-tags { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }
      .item-labels { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }
      .tag-chip {
        background: rgba(99, 102, 241, 0.12);
        border: 1px solid rgba(129, 140, 248, 0.16);
        border-radius: 999px;
        color: #c7d2fe;
        font-size: 0.75rem;
        padding: 4px 10px;
      }
      .label-chip {
        background: rgba(245, 158, 11, 0.12);
        border: 1px solid rgba(245, 158, 11, 0.18);
        border-radius: 999px;
        color: #fde68a;
        font-size: 0.75rem;
        padding: 4px 10px;
      }
    }

    .secondary-column { display: flex; flex-direction: column; gap: 2rem; }
    .topics-list { display: grid; gap: 0.9rem; }
    .topic-card {
      display: grid;
      gap: 0.55rem;
      padding: 1rem;
      border-radius: 14px;
      background: rgba(255, 255, 255, 0.03);
      border: 1px solid rgba(255, 255, 255, 0.06);
    }
    .topic-card:hover { background: rgba(255, 255, 255, 0.05); }
    .topic-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }
    .topic-title-link {
      color: #f8fafc;
      font-weight: 600;
      text-decoration: none;
    }
    .topic-count {
      color: #7dd3fc;
      font-size: 0.8rem;
      padding: 0.2rem 0.55rem;
      border-radius: 999px;
      background: rgba(14, 165, 233, 0.12);
      border: 1px solid rgba(14, 165, 233, 0.18);
    }
    .topic-description { color: #cbd5e1; font-size: 0.9rem; }
    .topic-links { display: flex; flex-wrap: wrap; gap: 0.4rem; }
    .topic-linkish {
      color: #c7d2fe;
      font-size: 0.8rem;
      padding: 0.25rem 0.55rem;
      border-radius: 999px;
      background: rgba(99, 102, 241, 0.1);
      text-decoration: none;
    }
    .topic-label {
      color: #fde68a;
      font-size: 0.78rem;
      padding: 0.3rem 0.6rem;
      border-radius: 999px;
      background: rgba(245, 158, 11, 0.12);
      border: 1px solid rgba(245, 158, 11, 0.18);
      width: fit-content;
    }
    .topic-link {
      color: #7dd3fc;
      font-size: 0.85rem;
      text-decoration: none;
      width: fit-content;
    }
    .tags-cloud { display: flex; flex-wrap: wrap; gap: 10px; }
    .tag-badge {
      background: rgba(99, 102, 241, 0.1);
      color: #818cf8;
      padding: 8px 16px;
      border-radius: 10px;
      font-size: 0.9rem;
      border: 1px solid rgba(129, 140, 248, 0.1);
      .count { margin-left: 6px; color: #6366f1; opacity: 0.8; font-size: 0.8rem; }
    }

    .stats-card .stat {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
      &:last-child { border: none; margin: 0; padding: 0; }
      .stat-label { color: #94a3b8; font-size: 0.9rem; }
      .stat-value { color: #f8fafc; font-weight: 600; font-size: 1.25rem; }
    }

    .empty-state { text-align: center; padding: 4rem 0; color: #64748b; }
    .empty-actions { display: flex; justify-content: center; gap: 0.75rem; margin-top: 1rem; flex-wrap: wrap; }
    .secondary-link { color: #c7d2fe; text-decoration: none; border: 1px solid rgba(255, 255, 255, 0.12); border-radius: 8px; padding: 0.75rem 1rem; }
    .empty-state.compact { padding: 1rem 0 0; }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }

    @media (max-width: 1100px) {
      .ops-grid,
      .main-grid {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 720px) {
      .ops-header,
      .section-head {
        flex-direction: column;
      }

      .ops-actions {
        width: 100%;
        justify-content: flex-start;
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  dashboardState = inject(DashboardStateService);
  adminProcessingState = inject(AdminProcessingStateService);
  authService = inject(AuthService);

  isAdmin = computed(() => this.authService.currentUser()?.role === 'admin');
  items = computed(() => this.dashboardState.recentCaptures());
  loading = computed(() => this.dashboardState.loading());
  error = computed(() => this.dashboardState.error());

  async ngOnInit(): Promise<void> {
    await this.dashboardState.loadOverview();
    if (this.isAdmin()) {
      await this.adminProcessingState.loadOverview();
    }
  }

  async toggleProcessing(): Promise<void> {
    if (!this.isAdmin()) {
      return;
    }

    if (this.adminProcessingState.isPaused()) {
      await this.adminProcessingState.resumeProcessing();
      return;
    }

    await this.adminProcessingState.pauseProcessing();
  }
}
