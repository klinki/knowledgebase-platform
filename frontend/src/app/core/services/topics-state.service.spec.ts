import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

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

    const request = http.expectOne('http://localhost:5000/api/v1/clusters/topic-1');
    expect(request.request.method).toBe('GET');
    request.flush({
      id: 'topic-1',
      title: ' Topic Alpha ',
      description: ' Cluster description ',
      keywords: [' alpha ', ' beta '],
      memberCount: 3,
      updatedAt: '2026-03-16T10:00:00Z',
      suggestedLabel: { category: ' Topic ', value: ' Topic Alpha ' },
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

  it('sets notFound for missing topics', async () => {
    const loadPromise = service.loadTopic('missing-topic');

    const request = http.expectOne('http://localhost:5000/api/v1/clusters/missing-topic');
    request.flush({}, { status: 404, statusText: 'Not Found' });

    await loadPromise;

    expect(service.notFound()).toBe(true);
    expect(service.error()).toBeNull();
    expect(service.topicDetail()).toBeNull();
  });
});
