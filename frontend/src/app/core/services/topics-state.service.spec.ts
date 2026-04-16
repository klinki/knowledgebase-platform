import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { convertToParamMap } from '@angular/router';

import { TopicsStateService } from './topics-state.service';

describe('TopicsStateService', () => {
  let service: TopicsStateService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(TopicsStateService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('normalizes topic detail payloads', async () => {
    const loadPromise = service.loadTopic('topic-1');

    const request = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/clusters/topic-1' &&
      req.params.get('page') === '1' &&
      req.params.get('pageSize') === '20' &&
      req.params.get('sortField') === 'rank' &&
      req.params.get('sortDirection') === 'asc');
    expect(request.request.method).toBe('GET');
    request.flush({
      id: 'topic-1',
      title: ' Topic Alpha ',
      description: ' Cluster description ',
      keywords: [' alpha ', ' beta '],
      memberCount: 3,
      updatedAt: '2026-03-16T10:00:00Z',
      suggestedLabel: { category: ' Topic ', value: ' Topic Alpha ' },
      membersPage: 1,
      membersPageSize: 20,
      membersTotalCount: 3,
      membersSortField: 'rank',
      membersSortDirection: 'asc',
      members: [
        {
          captureId: 'capture-1',
          processedInsightId: 'insight-1',
          title: ' Representative insight ',
          summary: ' Summary ',
          sourceUrl: ' https://example.com/topic ',
          rank: 1,
          similarityToCentroid: 0.98,
          tags: [' ai '],
          labels: [{ category: ' Source ', value: ' Web ' }]
        }
      ]
    });

    await loadPromise;

    expect(service.topicDetail()).toEqual({
      id: 'topic-1',
      title: 'Topic Alpha',
      description: 'Cluster description',
      keywords: ['alpha', 'beta'],
      memberCount: 3,
      updatedAt: '2026-03-16T10:00:00Z',
      suggestedLabel: { category: 'Topic', value: 'Topic Alpha' },
      membersPage: 1,
      membersPageSize: 20,
      membersTotalCount: 3,
      membersSortField: 'rank',
      membersSortDirection: 'asc',
      members: [
        {
          captureId: 'capture-1',
          processedInsightId: 'insight-1',
          title: 'Representative insight',
          summary: 'Summary',
          sourceUrl: 'https://example.com/topic',
          rank: 1,
          similarityToCentroid: 0.98,
          tags: ['ai'],
          labels: [{ category: 'Source', value: 'Web' }]
        }
      ]
    });
  });

  it('loads a paginated topics list', async () => {
    const loadPromise = service.loadTopicsPage({
      query: '  infra  ',
      sortField: 'title',
      sortDirection: 'asc',
      page: 2,
      pageSize: 50
    });

    const request = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/clusters/list' &&
      req.params.get('page') === '2' &&
      req.params.get('pageSize') === '12' &&
      req.params.get('query') === 'infra' &&
      req.params.get('sortField') === 'title' &&
      req.params.get('sortDirection') === 'asc');
    expect(request.request.method).toBe('GET');
    request.flush({
      items: [
        {
          id: 'topic-1',
          title: ' Topic Alpha ',
          description: ' Cluster description ',
          keywords: [' alpha ', ' beta '],
          memberCount: 8,
          updatedAt: '2026-03-16T10:00:00Z',
          representativeInsights: [
            {
              captureId: 'capture-1',
              processedInsightId: 'insight-1',
              title: ' Representative insight ',
              summary: ' Summary ',
              sourceUrl: ' https://example.com/topic '
            }
          ],
          suggestedLabel: { category: ' Topic ', value: ' Topic Alpha ' }
        }
      ],
      totalCount: 14,
      page: 2,
      pageSize: 12
    });

    await loadPromise;

    expect(service.currentCriteria()).toEqual({
      query: 'infra',
      sortField: 'title',
      sortDirection: 'asc',
      page: 2,
      pageSize: 12
    });
    expect(service.topicsPage()).toEqual({
      items: [
        {
          id: 'topic-1',
          title: 'Topic Alpha',
          description: 'Cluster description',
          keywords: ['alpha', 'beta'],
          memberCount: 8,
          updatedAt: '2026-03-16T10:00:00Z',
          representativeInsights: [
            {
              captureId: 'capture-1',
              processedInsightId: 'insight-1',
              title: 'Representative insight',
              summary: 'Summary',
              sourceUrl: 'https://example.com/topic'
            }
          ],
          suggestedLabel: { category: 'Topic', value: 'Topic Alpha' }
        }
      ],
      totalCount: 14,
      page: 2,
      pageSize: 12
    });
  });

  it('parses URL query params into normalized topic list criteria', () => {
    const criteria = service.parseQueryParams(convertToParamMap({
      q: '  infra  ',
      sortField: 'updatedAt',
      sortDirection: 'asc',
      page: '3'
    }));

    expect(criteria).toEqual({
      query: 'infra',
      sortField: 'updatedAt',
      sortDirection: 'asc',
      page: 3,
      pageSize: 12
    });
  });

  it('builds canonical query params and omits defaults', () => {
    expect(service.buildQueryParams(service.createDefaultCriteria())).toEqual({
      q: null,
      sortField: null,
      sortDirection: null,
      page: null
    });

    expect(service.buildQueryParams({
      query: '  infra  ',
      sortField: 'title',
      sortDirection: 'asc',
      page: 2,
      pageSize: 12
    })).toEqual({
      q: 'infra',
      sortField: 'title',
      sortDirection: 'asc',
      page: '2'
    });
  });

  it('parses URL query params into normalized topic detail criteria', () => {
    const criteria = service.parseTopicDetailQueryParams(convertToParamMap({
      page: '3',
      pageSize: '50',
      sortField: 'sourceUrl',
      sortDirection: 'desc'
    }));

    expect(criteria).toEqual({
      page: 3,
      pageSize: 50,
      sortField: 'sourceUrl',
      sortDirection: 'desc'
    });
  });

  it('builds canonical topic detail query params and omits defaults', () => {
    expect(service.buildTopicDetailQueryParams(service.createDefaultTopicDetailCriteria())).toEqual({
      page: null,
      pageSize: null,
      sortField: null,
      sortDirection: null
    });

    expect(service.buildTopicDetailQueryParams({
      page: 2,
      pageSize: 50,
      sortField: 'title',
      sortDirection: 'asc'
    })).toEqual({
      page: '2',
      pageSize: '50',
      sortField: 'title',
      sortDirection: 'asc'
    });
  });

  it('falls back to default criteria when URL params are invalid', () => {
    const criteria = service.parseQueryParams(convertToParamMap({
      q: '  ',
      sortField: 'unsupported',
      sortDirection: 'sideways',
      page: '-7'
    }));

    expect(criteria).toEqual(service.createDefaultCriteria());
  });

  it('sets notFound for missing topics', async () => {
    const loadPromise = service.loadTopic('missing-topic');

    const request = http.expectOne(req =>
      req.url === 'http://localhost:5000/api/v1/clusters/missing-topic' &&
      req.params.get('page') === '1' &&
      req.params.get('pageSize') === '20' &&
      req.params.get('sortField') === 'rank' &&
      req.params.get('sortDirection') === 'asc');
    request.flush({}, { status: 404, statusText: 'Not Found' });

    await loadPromise;

    expect(service.notFound()).toBe(true);
    expect(service.error()).toBeNull();
    expect(service.topicDetail()).toBeNull();
  });
});
