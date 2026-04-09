import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { DashboardStateService } from './dashboard-state.service';

describe('DashboardStateService', () => {
  let service: DashboardStateService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(DashboardStateService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('normalizes labels in recent captures', async () => {
    const loadPromise = service.loadOverview(true);

    const request = http.expectOne('http://localhost:5000/api/v1/dashboard/overview');
    expect(request.request.method).toBe('GET');
    request.flush({
      recentCaptures: [
        {
          id: 'capture-1',
          title: 'Capture title',
          sourceUrl: 'https://example.com/item',
          capturedAt: '2026-03-16T10:00:00Z',
          status: 'Completed',
          tags: ['alpha'],
          summary: 'Summary',
          similarity: null,
          labels: [{ category: 'Language', value: ' English ' }]
        }
      ],
      topicClusters: [
        {
          id: 'topic-1',
          title: ' Topic Alpha ',
          description: ' Cluster description ',
          keywords: [' alpha ', ' beta '],
          memberCount: 3,
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
      topTags: [],
      stats: {
        totalCaptures: 1,
        activeTags: 0
      }
    });

    await loadPromise;

    expect(service.recentCaptures()).toEqual([
      {
        id: 'capture-1',
        title: 'Capture title',
        sourceUrl: 'https://example.com/item',
        capturedAt: '2026-03-16T10:00:00Z',
        status: 'Completed',
        tags: ['alpha'],
        summary: 'Summary',
        similarity: null,
        labels: [{ category: 'Language', value: 'English' }]
      }
    ]);

    expect(service.topicClusters()).toEqual([
      {
        id: 'topic-1',
        title: ' Topic Alpha ',
        description: 'Cluster description',
        keywords: ['alpha', 'beta'],
        memberCount: 3,
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
    ]);
  });
});
