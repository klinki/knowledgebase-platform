import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { DashboardComponent } from './dashboard.component';
import { AdminProcessingStateService } from '../../core/services/admin-processing-state.service';
import { AuthService } from '../../core/services/auth.service';
import { DashboardStateService } from '../../core/services/dashboard-state.service';

describe('DashboardComponent', () => {
  it('renders label chips on recent captures and no inline search box', async () => {
    const dashboardStateStub = {
      loading: signal(false),
      error: signal<string | null>(null),
      recentCaptures: signal([
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
      ]),
      topicClusters: signal([]),
      topTags: signal([]),
      stats: signal({
        totalCaptures: 1,
        activeTags: 0
      }),
      loadOverview: vi.fn().mockResolvedValue(undefined)
    };

    const adminProcessingStateStub = {
      loading: signal(false),
      submitting: signal(false),
      error: signal<string | null>(null),
      isPaused: signal(false),
      changedAt: signal<string | null>(null),
      changedByDisplayName: signal<string | null>(null),
      captureCounts: signal({ pending: 0, processing: 0, completed: 0, failed: 0 }),
      jobCounts: signal({ enqueued: 0, scheduled: 0, processing: 0, failed: 0 }),
      recentCaptures: signal([]),
      loadOverview: vi.fn().mockResolvedValue(undefined),
      pauseProcessing: vi.fn().mockResolvedValue(undefined),
      resumeProcessing: vi.fn().mockResolvedValue(undefined)
    };

    const authServiceStub = {
      currentUser: signal({
        id: 'user-1',
        email: 'member@example.com',
        displayName: 'Member',
        role: 'member',
        defaultLanguageCode: 'en',
        preservedLanguageCodes: []
      })
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
        { provide: AdminProcessingStateService, useValue: adminProcessingStateStub },
        { provide: DashboardStateService, useValue: dashboardStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Language: English');
    expect(compiled.textContent).toContain('Open search');
    expect(compiled.textContent).not.toContain('Search across your knowledge');
  });

  it('renders the admin processing panel for admins', async () => {
    const dashboardStateStub = {
      loading: signal(false),
      error: signal<string | null>(null),
      recentCaptures: signal([]),
      topicClusters: signal([]),
      topTags: signal([]),
      stats: signal({ totalCaptures: 0, activeTags: 0 }),
      loadOverview: vi.fn().mockResolvedValue(undefined)
    };
    const adminProcessingStateStub = {
      loading: signal(false),
      submitting: signal(false),
      error: signal<string | null>(null),
      isPaused: signal(true),
      changedAt: signal('2026-03-31T08:00:00Z'),
      changedByDisplayName: signal('Integration Admin'),
      captureCounts: signal({ pending: 4, processing: 1, completed: 8, failed: 2 }),
      jobCounts: signal({ enqueued: 5, scheduled: 2, processing: 1, failed: 0 }),
      recentCaptures: signal([
        {
          id: 'capture-1',
          title: 'Capture title',
          sourceUrl: 'https://example.com/item',
          capturedAt: '2026-03-16T10:00:00Z',
          status: 'Pending',
          tags: [],
          summary: null,
          similarity: null,
          labels: []
        }
      ]),
      loadOverview: vi.fn().mockResolvedValue(undefined),
      pauseProcessing: vi.fn().mockResolvedValue(undefined),
      resumeProcessing: vi.fn().mockResolvedValue(undefined)
    };
    const authServiceStub = {
      currentUser: signal({
        id: 'admin-1',
        email: 'admin@example.com',
        displayName: 'Admin',
        role: 'admin',
        defaultLanguageCode: 'en',
        preservedLanguageCodes: []
      })
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
        { provide: AdminProcessingStateService, useValue: adminProcessingStateStub },
        { provide: DashboardStateService, useValue: dashboardStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Processing Control');
    expect(text).toContain('Paused');
    expect(text).toContain('Recent System Captures');
  });

  it('disables the processing toggle while an admin action is pending', async () => {
    const dashboardStateStub = {
      loading: signal(false),
      error: signal<string | null>(null),
      recentCaptures: signal([]),
      topicClusters: signal([]),
      topTags: signal([]),
      stats: signal({ totalCaptures: 0, activeTags: 0 }),
      loadOverview: vi.fn().mockResolvedValue(undefined)
    };
    const adminProcessingStateStub = {
      loading: signal(false),
      submitting: signal(true),
      error: signal<string | null>(null),
      isPaused: signal(false),
      changedAt: signal<string | null>(null),
      changedByDisplayName: signal<string | null>(null),
      captureCounts: signal({ pending: 0, processing: 0, completed: 0, failed: 0 }),
      jobCounts: signal({ enqueued: 0, scheduled: 0, processing: 0, failed: 0 }),
      recentCaptures: signal([]),
      loadOverview: vi.fn().mockResolvedValue(undefined),
      pauseProcessing: vi.fn().mockResolvedValue(undefined),
      resumeProcessing: vi.fn().mockResolvedValue(undefined)
    };
    const authServiceStub = {
      currentUser: signal({
        id: 'admin-1',
        email: 'admin@example.com',
        displayName: 'Admin',
        role: 'admin',
        defaultLanguageCode: 'en',
        preservedLanguageCodes: []
      })
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
        { provide: AdminProcessingStateService, useValue: adminProcessingStateStub },
        { provide: DashboardStateService, useValue: dashboardStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const button = (fixture.nativeElement as HTMLElement).querySelector('.ops-actions button') as HTMLButtonElement | null;
    expect(button).not.toBeNull();
    expect(button?.disabled).toBe(true);
  });

  it('renders topic cards with topic and capture links', async () => {
    const dashboardStateStub = {
      loading: signal(false),
      error: signal<string | null>(null),
      recentCaptures: signal([]),
      topicClusters: signal([
        {
          id: 'topic-1',
          title: 'AI Infrastructure',
          description: 'Cluster about serving and deployment.',
          keywords: ['ai', 'infra', 'serving'],
          memberCount: 3,
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
          suggestedLabel: { category: 'Topic', value: 'AI Infrastructure' }
        }
      ]),
      topTags: signal([]),
      stats: signal({ totalCaptures: 0, activeTags: 0 }),
      loadOverview: vi.fn().mockResolvedValue(undefined)
    };
    const adminProcessingStateStub = {
      loading: signal(false),
      submitting: signal(false),
      error: signal<string | null>(null),
      isPaused: signal(false),
      changedAt: signal<string | null>(null),
      changedByDisplayName: signal<string | null>(null),
      captureCounts: signal({ pending: 0, processing: 0, completed: 0, failed: 0 }),
      jobCounts: signal({ enqueued: 0, scheduled: 0, processing: 0, failed: 0 }),
      recentCaptures: signal([]),
      loadOverview: vi.fn().mockResolvedValue(undefined),
      pauseProcessing: vi.fn().mockResolvedValue(undefined),
      resumeProcessing: vi.fn().mockResolvedValue(undefined)
    };
    const searchStateStub = {
      results: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      search: vi.fn()
    };
    const authServiceStub = {
      currentUser: signal({
        id: 'user-1',
        email: 'member@example.com',
        displayName: 'Member',
        role: 'member'
      })
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceStub },
        { provide: AdminProcessingStateService, useValue: adminProcessingStateStub },
        { provide: DashboardStateService, useValue: dashboardStateStub },
        { provide: SearchStateService, useValue: searchStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AI Infrastructure');
    expect(compiled.textContent).toContain('GPU scheduling note');

    const topicLink = compiled.querySelector('.topic-title-link') as HTMLAnchorElement | null;
    const captureLink = compiled.querySelector('.topic-linkish') as HTMLAnchorElement | null;
    expect(topicLink?.getAttribute('href')).toContain('/topics/topic-1');
    expect(captureLink?.getAttribute('href')).toContain('/captures/capture-1');
  });
});
