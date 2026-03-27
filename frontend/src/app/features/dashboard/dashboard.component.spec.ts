import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { DashboardComponent } from './dashboard.component';
import { DashboardStateService } from '../../core/services/dashboard-state.service';
import { SearchStateService } from '../../core/services/search-state.service';

describe('DashboardComponent', () => {
  it('renders label chips on recent captures', async () => {
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
      topTags: signal([]),
      stats: signal({
        totalCaptures: 1,
        activeTags: 0
      }),
      loadOverview: vi.fn().mockResolvedValue(undefined)
    };

    const searchStateStub = {
      results: signal([]),
      loading: signal(false),
      error: signal<string | null>(null),
      clear: vi.fn(),
      search: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardStateService, useValue: dashboardStateStub },
        { provide: SearchStateService, useValue: searchStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Language: English');
  });
});
