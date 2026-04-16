import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { TopicClusterDetailCriteria } from '../../core/services/topics-state.service';
import { TopicsStateService } from '../../core/services/topics-state.service';
import { TopicDetailComponent } from './topic-detail.component';

describe('TopicDetailComponent', () => {
  it('hydrates from URL params, loads detail, and renders member cards', async () => {
    const detailCriteria: TopicClusterDetailCriteria = {
      page: 2,
      pageSize: 50,
      sortField: 'title',
      sortDirection: 'asc'
    };
    const routeState = createRouteStub({ id: 'topic-1' }, {
      page: '2',
      pageSize: '50',
      sortField: 'title',
      sortDirection: 'asc'
    });
    const topicsStateStub = createTopicsStateStub(detailCriteria);

    await TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(topicsStateStub.loadTopic).toHaveBeenCalledWith('topic-1', detailCriteria);
    expect(compiled.textContent).toContain('AI Infrastructure');
    expect(compiled.textContent).toContain('GPU scheduling note');
    expect(compiled.textContent).toContain('2 total');

    const memberLink = compiled.querySelector('.member-card') as HTMLAnchorElement | null;
    expect(memberLink?.getAttribute('href')).toContain('/captures/capture-1');
  });

  it('changing sort syncs URL and resets to page 1', async () => {
    const detailCriteria: TopicClusterDetailCriteria = {
      page: 3,
      pageSize: 20,
      sortField: 'rank',
      sortDirection: 'asc'
    };
    const routeState = createRouteStub({ id: 'topic-1' }, {});
    const topicsStateStub = createTopicsStateStub(detailCriteria);

    await TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    await component.onSortChange('title-desc');

    expect(topicsStateStub.syncTopicDetailUrl).toHaveBeenCalledWith(
      expect.anything(),
      expect.anything(),
      {
        ...detailCriteria,
        sortField: 'title',
        sortDirection: 'desc',
        page: 1
      }
    );
  });

  it('renders the not found state', async () => {
    const detailCriteria: TopicClusterDetailCriteria = {
      page: 1,
      pageSize: 20,
      sortField: 'rank',
      sortDirection: 'asc'
    };
    const routeState = createRouteStub({ id: 'missing-topic' }, {});
    const topicsStateStub = createTopicsStateStub(detailCriteria, null, true);

    await TestBed.configureTestingModule({
      imports: [TopicDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Topic not found');
  });
});

function createRouteStub(pathParams: Record<string, string>, queryParams: Record<string, string>) {
  const subject = new BehaviorSubject<ParamMap>(convertToParamMap(queryParams));
  return {
    route: {
      queryParamMap: subject.asObservable(),
      snapshot: {
        paramMap: convertToParamMap(pathParams),
        queryParamMap: subject.value
      }
    },
    subject
  };
}

function createTopicsStateStub(
  criteria: TopicClusterDetailCriteria,
  detail: {
    id: string;
    title: string;
    description: string;
    keywords: string[];
    memberCount: number;
    updatedAt: string;
    suggestedLabel: { category: string; value: string };
    membersPage: number;
    membersPageSize: number;
    membersTotalCount: number;
    membersSortField: TopicClusterDetailCriteria['sortField'];
    membersSortDirection: TopicClusterDetailCriteria['sortDirection'];
    members: Array<{
      captureId: string;
      processedInsightId: string;
      title: string;
      summary: string;
      sourceUrl: string;
      rank: number;
      similarityToCentroid: number;
      tags: string[];
      labels: Array<{ category: string; value: string }>;
    }>;
  } | null = {
    id: 'topic-1',
    title: 'AI Infrastructure',
    description: 'Serving and deployment notes.',
    keywords: ['gpu', 'serving', 'ops'],
    memberCount: 2,
    updatedAt: '2026-03-16T10:00:00Z',
    suggestedLabel: { category: 'Topic', value: 'AI Infrastructure' },
    membersPage: criteria.page,
    membersPageSize: criteria.pageSize,
    membersTotalCount: 2,
    membersSortField: criteria.sortField,
    membersSortDirection: criteria.sortDirection,
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
  },
  notFound = false
) {
  const topicDetailCriteria = signal(criteria);

  return {
    loading: signal(false),
    error: signal<string | null>(null),
    notFound: signal(notFound),
    topicDetail: signal(detail),
    topicDetailCriteria,
    parseTopicDetailQueryParams: vi.fn().mockReturnValue(criteria),
    hasCanonicalTopicDetailQueryParams: vi.fn().mockReturnValue(true),
    syncTopicDetailUrl: vi.fn().mockResolvedValue(undefined),
    loadTopic: vi.fn().mockImplementation(async (_id: string, next: TopicClusterDetailCriteria) => {
      topicDetailCriteria.set(next);
    }),
    clear: vi.fn()
  };
}
