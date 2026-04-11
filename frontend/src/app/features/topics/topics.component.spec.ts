import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { TopicsStateService } from '../../core/services/topics-state.service';
import { TopicsComponent } from './topics.component';

describe('TopicsComponent', () => {
  it('renders topic cards, pagination, and view links', async () => {
    const topicsStateStub = {
      topicsPage: signal({
        items: [
          {
            id: 'topic-1',
            title: 'AI Infrastructure',
            description: 'Serving, orchestration, and runtime concerns.',
            keywords: ['gpu', 'serving', 'latency'],
            memberCount: 12,
            updatedAt: '2026-03-16T10:00:00Z',
            representativeInsights: [
              {
                captureId: 'capture-1',
                processedInsightId: 'insight-1',
                title: 'GPU scheduling note',
                summary: 'Summary',
                sourceUrl: 'https://example.com/gpu'
              }
            ],
            suggestedLabel: {
              category: 'Topic',
              value: 'AI Infrastructure'
            }
          }
        ],
        totalCount: 24,
        page: 2,
        pageSize: 12
      }),
      topicDetail: signal(null),
      loading: signal(false),
      error: signal<string | null>(null),
      notFound: signal(false),
      loadTopicsPage: vi.fn().mockResolvedValue(undefined),
      loadTopic: vi.fn().mockResolvedValue(undefined),
      clear: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [TopicsComponent],
      providers: [
        provideRouter([]),
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AI Infrastructure');
    expect(compiled.textContent).toContain('Page 2 of 2');
    expect(compiled.textContent).toContain('Open topic');

    const topicLink = compiled.querySelector('.topic-title-link') as HTMLAnchorElement | null;
    const captureLink = compiled.querySelector('.insight-preview') as HTMLAnchorElement | null;
    expect(topicLink?.getAttribute('href')).toContain('/topics/topic-1');
    expect(captureLink?.getAttribute('href')).toContain('/captures/capture-1');
  });
});
