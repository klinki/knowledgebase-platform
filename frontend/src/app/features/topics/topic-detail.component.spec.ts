import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap, provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';

import { TopicDetailComponent } from './topic-detail.component';
import { TopicsStateService } from '../../core/services/topics-state.service';

describe('TopicDetailComponent', () => {
  it('renders topic detail and member cards', async () => {
    const topicsStateStub = {
      loading: signal(false),
      error: signal<string | null>(null),
      notFound: signal(false),
      topicDetail: signal({
        id: 'topic-1',
        title: 'AI Infrastructure',
        description: 'Serving and deployment notes.',
        keywords: ['gpu', 'serving', 'ops'],
        memberCount: 2,
        updatedAt: '2026-03-16T10:00:00Z',
        suggestedLabel: { category: 'Topic', value: 'AI Infrastructure' },
        members: [
          {
            captureId: 'capture-1',
            processedInsightId: 'insight-1',
            title: 'GPU scheduling note',
            summary: 'Summary',
            sourceUrl: 'https://example.com/gpu',
            rank: 1,
            similarityToCentroid: 0.99,
            tags: ['ops'],
            labels: [{ category: 'Source', value: 'Web' }]
          }
        ]
      }),
      loadTopic: vi.fn().mockResolvedValue(undefined),
      clear: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'topic-1' })
            }
          }
        },
        {
          provide: TopicsStateService,
          useValue: topicsStateStub
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AI Infrastructure');
    expect(compiled.textContent).toContain('GPU scheduling note');

    const memberLink = compiled.querySelector('.member-card') as HTMLAnchorElement | null;
    expect(memberLink?.getAttribute('href')).toContain('/captures/capture-1');
  });

  it('renders the not found state', async () => {
    const topicsStateStub = {
      loading: signal(false),
      error: signal<string | null>(null),
      notFound: signal(true),
      topicDetail: signal(null),
      loadTopic: vi.fn().mockResolvedValue(undefined),
      clear: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'missing-topic' })
            }
          }
        },
        {
          provide: TopicsStateService,
          useValue: topicsStateStub
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Topic not found');
  });
});
