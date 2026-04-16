import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { TopicsStateService } from '../../core/services/topics-state.service';
import { TopicClusterListCriteria } from '../../shared/models/knowledge.model';
import { TopicsComponent } from './topics.component';

describe('TopicsComponent', () => {
  it('hydrates from URL state, renders controls, and loads topics', async () => {
    const criteria: TopicClusterListCriteria = {
      query: 'infra',
      sortField: 'title',
      sortDirection: 'asc',
      page: 2,
      pageSize: 12
    };
    const routeState = createRouteStub({ q: 'infra', sortField: 'title', sortDirection: 'asc', page: '2' });
    const topicsStateStub = createTopicsStateStub(criteria);

    await TestBed.configureTestingModule({
      imports: [TopicsComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector('#topics-search') as HTMLInputElement | null;
    const sortSelect = compiled.querySelector('#topics-sort') as HTMLSelectElement | null;

    expect(topicsStateStub.loadTopicsPage).toHaveBeenCalledWith(criteria);
    expect(compiled.textContent).toContain('AI Infrastructure');
    expect(compiled.textContent).toContain('Page 2 of 2');
    expect(searchInput?.value).toBe('infra');
    expect(sortSelect?.value).toBe('title-asc');
    const topicSearchLink = Array.from(compiled.querySelectorAll('a.topic-link'))
      .find(link => link.textContent?.includes('Search on topic')) as HTMLAnchorElement | undefined;
    expect(topicSearchLink?.getAttribute('href')).toContain('/search?topicId=topic-1');
  });

  it('submitting search syncs URL state and resets page to 1', async () => {
    const criteria: TopicClusterListCriteria = {
      query: '',
      sortField: 'memberCount',
      sortDirection: 'desc',
      page: 2,
      pageSize: 12
    };
    const routeState = createRouteStub({});
    const topicsStateStub = createTopicsStateStub(criteria);

    await TestBed.configureTestingModule({
      imports: [TopicsComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector('#topics-search') as HTMLInputElement;
    searchInput.value = 'vector';
    searchInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = compiled.querySelector('.topics-controls') as HTMLFormElement | null;
    form?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(topicsStateStub.syncUrl).toHaveBeenCalledWith(
      expect.anything(),
      expect.anything(),
      {
        ...criteria,
        query: 'vector',
        page: 1
      }
    );
  });

  it('changing sort syncs URL state and resets page to 1', async () => {
    const criteria: TopicClusterListCriteria = {
      query: 'infra',
      sortField: 'memberCount',
      sortDirection: 'desc',
      page: 3,
      pageSize: 12
    };
    const routeState = createRouteStub({ q: 'infra', page: '3' });
    const topicsStateStub = createTopicsStateStub(criteria);

    await TestBed.configureTestingModule({
      imports: [TopicsComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const sortSelect = compiled.querySelector('#topics-sort') as HTMLSelectElement;
    sortSelect.value = 'updatedAt-asc';
    sortSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(topicsStateStub.syncUrl).toHaveBeenCalledWith(
      expect.anything(),
      expect.anything(),
      {
        ...criteria,
        query: 'infra',
        sortField: 'updatedAt',
        sortDirection: 'asc',
        page: 1
      }
    );
  });

  it('renders a filtered empty state when no topics match the search', async () => {
    const criteria: TopicClusterListCriteria = {
      query: 'infra',
      sortField: 'memberCount',
      sortDirection: 'desc',
      page: 1,
      pageSize: 12
    };
    const routeState = createRouteStub({ q: 'infra' });
    const topicsStateStub = createTopicsStateStub(criteria, {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 12
    });

    await TestBed.configureTestingModule({
      imports: [TopicsComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: routeState.route },
        { provide: TopicsStateService, useValue: topicsStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(TopicsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No matching topics');
    expect(compiled.textContent).toContain('No topics matched the current search.');
  });
});

function createRouteStub(initialParams: Record<string, string>) {
  const subject = new BehaviorSubject<ParamMap>(convertToParamMap(initialParams));

  return {
    route: {
      queryParamMap: subject.asObservable(),
      snapshot: {
        queryParamMap: subject.value
      }
    },
    subject
  };
}

function createTopicsStateStub(
  criteria: TopicClusterListCriteria,
  page = {
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
  }) {
  const currentCriteria = signal(criteria);

  return {
    currentCriteria,
    topicsPage: signal(page),
    topicDetail: signal(null),
    loading: signal(false),
    error: signal<string | null>(null),
    notFound: signal(false),
    createDefaultCriteria: vi.fn().mockReturnValue({
      query: '',
      sortField: 'memberCount',
      sortDirection: 'desc',
      page: 1,
      pageSize: 12
    }),
    parseQueryParams: vi.fn().mockReturnValue(criteria),
    buildQueryParams: vi.fn().mockReturnValue({
      q: criteria.query || null,
      sortField: criteria.sortField === 'memberCount' && criteria.sortDirection === 'desc' ? null : criteria.sortField,
      sortDirection: criteria.sortField === 'memberCount' && criteria.sortDirection === 'desc' ? null : criteria.sortDirection,
      page: criteria.page > 1 ? String(criteria.page) : null
    }),
    hasCanonicalQueryParams: vi.fn().mockReturnValue(true),
    syncUrl: vi.fn().mockResolvedValue(undefined),
    loadTopicsPage: vi.fn().mockImplementation(async (nextCriteria: TopicClusterListCriteria) => {
      currentCriteria.set(nextCriteria);
    }),
    loadTopic: vi.fn().mockResolvedValue(undefined),
    clear: vi.fn()
  };
}
