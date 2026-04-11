import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { TopicsStateService } from '../../core/services/topics-state.service';

@Component({
  selector: 'app-topic-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="topic-page">
      <header class="page-header">
        <div>
          <h1>Topic</h1>
          <p>Explore a group of related processed insights discovered from embeddings.</p>
        </div>
        <a routerLink="/topics" class="back-link">Back to topics</a>
      </header>

      @if (topicsState.loading()) {
        <div class="glass-card empty-state">
          <p>Loading topic...</p>
        </div>
      } @else if (topicsState.error()) {
        <div class="glass-card empty-state error">
          <p>{{ topicsState.error() }}</p>
        </div>
      } @else if (topicsState.notFound()) {
        <div class="glass-card empty-state">
          <h2>Topic not found</h2>
          <p>The requested topic does not exist or is not visible to your account.</p>
        </div>
      } @else if (topic()) {
        <section class="glass-card hero-card">
          <div class="hero-head">
            <div>
              <h2>{{ topic()!.title }}</h2>
              @if (topic()!.description) {
                <p>{{ topic()!.description }}</p>
              }
            </div>
            <div class="meta-stack">
              <span class="meta-chip">{{ topic()!.memberCount }} insights</span>
              <span class="label-chip">{{ topic()!.suggestedLabel.category }}: {{ topic()!.suggestedLabel.value }}</span>
            </div>
          </div>

          @if (topic()!.keywords.length > 0) {
            <div class="keyword-row">
              @for (keyword of topic()!.keywords; track keyword) {
                <span class="keyword-chip">{{ keyword }}</span>
              }
            </div>
          }
        </section>

        <section class="glass-card">
          <h2>Insights</h2>
          <div class="member-list">
            @for (member of topic()!.members; track member.processedInsightId) {
              <a class="member-card" [routerLink]="['/captures', member.captureId]">
                <div class="member-head">
                  <span class="member-rank">#{{ member.rank }}</span>
                  <span class="member-score">{{ member.similarityToCentroid | number:'1.2-2' }}</span>
                </div>
                <div class="member-title">{{ member.title }}</div>
                <div class="member-summary">{{ member.summary }}</div>
                <div class="member-meta">{{ member.sourceUrl }}</div>
                @if (member.tags.length > 0) {
                  <div class="tag-row">
                    @for (tag of member.tags; track tag) {
                      <span class="tag-chip">{{ tag }}</span>
                    }
                  </div>
                }
                @if (member.labels.length > 0) {
                  <div class="label-row">
                    @for (label of member.labels; track label.category + ':' + label.value) {
                      <span class="label-chip">{{ label.category }}: {{ label.value }}</span>
                    }
                  </div>
                }
              </a>
            }
          </div>
        </section>
      }
    </div>
  `,
  styles: [`
    .topic-page { display: grid; gap: 1.5rem; }
    .page-header { display: flex; justify-content: space-between; gap: 1rem; align-items: flex-start; }
    h1 { font-size: 3rem; margin: 0 0 0.5rem; }
    .page-header p { margin: 0; color: #94a3b8; }
    .back-link { color: #c7d2fe; text-decoration: none; padding-top: 0.8rem; }
    .hero-card, .glass-card { display: grid; gap: 1rem; }
    .hero-head { display: flex; justify-content: space-between; gap: 1rem; align-items: flex-start; }
    .hero-head h2 { margin: 0 0 0.5rem; }
    .hero-head p { margin: 0; color: #cbd5e1; }
    .meta-stack { display: grid; gap: 0.5rem; justify-items: end; }
    .meta-chip, .keyword-chip, .tag-chip, .label-chip { border-radius: 999px; padding: 0.35rem 0.75rem; font-size: 0.75rem; }
    .meta-chip { background: rgba(14, 165, 233, 0.12); color: #7dd3fc; border: 1px solid rgba(14, 165, 233, 0.18); }
    .keyword-row, .tag-row, .label-row { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .keyword-chip, .tag-chip { background: rgba(99, 102, 241, 0.12); color: #c7d2fe; border: 1px solid rgba(129, 140, 248, 0.18); }
    .label-chip { background: rgba(245, 158, 11, 0.12); color: #fde68a; border: 1px solid rgba(245, 158, 11, 0.18); }
    .member-list { display: grid; gap: 1rem; }
    .member-card { text-decoration: none; color: inherit; padding: 1rem; border-radius: 14px; background: rgba(255, 255, 255, 0.03); border: 1px solid rgba(255, 255, 255, 0.06); display: grid; gap: 0.65rem; }
    .member-card:hover { background: rgba(255, 255, 255, 0.05); }
    .member-head { display: flex; justify-content: space-between; gap: 1rem; color: #94a3b8; font-size: 0.85rem; }
    .member-title { color: #f8fafc; font-size: 1.05rem; font-weight: 600; }
    .member-summary { color: #cbd5e1; }
    .member-meta { color: #64748b; font-size: 0.85rem; overflow-wrap: anywhere; }
    .empty-state { text-align: center; color: #94a3b8; padding: 3rem 1rem; }
    .empty-state.error { color: #fecaca; }
    @media (max-width: 720px) {
      .page-header, .hero-head { flex-direction: column; }
      .meta-stack { justify-items: start; }
    }
  `]
})
export class TopicDetailComponent implements OnInit, OnDestroy {
  topicsState = inject(TopicsStateService);
  private route = inject(ActivatedRoute);

  topic = computed(() => this.topicsState.topicDetail());

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    await this.topicsState.loadTopic(id);
  }

  ngOnDestroy(): void {
    this.topicsState.clear();
  }
}
