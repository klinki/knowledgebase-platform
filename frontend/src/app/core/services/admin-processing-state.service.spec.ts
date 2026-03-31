import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminProcessingStateService } from './admin-processing-state.service';

describe('AdminProcessingStateService', () => {
  let service: AdminProcessingStateService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminProcessingStateService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('loads admin processing overview', async () => {
    const loadPromise = service.loadOverview(true);

    const request = http.expectOne('http://localhost:5000/api/v1/admin/processing');
    expect(request.request.method).toBe('GET');
    request.flush({
      isPaused: true,
      changedAt: '2026-03-31T08:00:00Z',
      changedByDisplayName: 'Integration Admin',
      captureCounts: { pending: 4, processing: 1, completed: 7, failed: 2 },
      jobCounts: { enqueued: 5, scheduled: 2, processing: 1, failed: 0 },
      recentCaptures: [
        {
          id: 'capture-1',
          title: 'Capture title',
          sourceUrl: 'https://example.com/item',
          capturedAt: '2026-03-16T10:00:00Z',
          status: 'Pending',
          tags: [],
          labels: [{ category: 'Source', value: ' Web ' }]
        }
      ]
    });

    await loadPromise;

    expect(service.isPaused()).toBe(true);
    expect(service.captureCounts().pending).toBe(4);
    expect(service.recentCaptures()[0].labels).toEqual([{ category: 'Source', value: 'Web' }]);
  });

  it('submits pause action and updates overview', async () => {
    const pausePromise = service.pauseProcessing();

    const request = http.expectOne('http://localhost:5000/api/v1/admin/processing/pause');
    expect(request.request.method).toBe('POST');
    request.flush({
      isPaused: true,
      changedAt: '2026-03-31T08:00:00Z',
      changedByDisplayName: 'Integration Admin',
      captureCounts: { pending: 1, processing: 0, completed: 0, failed: 0 },
      jobCounts: { enqueued: 1, scheduled: 0, processing: 0, failed: 0 },
      recentCaptures: []
    });

    await pausePromise;

    expect(service.isPaused()).toBe(true);
    expect(service.submitting()).toBe(false);
  });
});
