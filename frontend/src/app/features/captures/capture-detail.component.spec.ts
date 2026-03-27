import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap, provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';

import { CaptureDetailComponent } from './capture-detail.component';
import { CaptureStateService } from '../../core/services/capture-state.service';

describe('CaptureDetailComponent', () => {
  it('renders capture detail fields and parsed insight data', async () => {
    const captureStateStub = {
      loadingDetail: signal(false),
      detailError: signal<string | null>(null),
      detailNotFound: signal(false),
      captureDetail: signal({
        id: 'capture-1',
        sourceUrl: 'https://example.com/item',
        contentType: 'Article',
        status: 'Completed',
        createdAt: '2026-03-16T10:00:00Z',
        processedAt: '2026-03-16T10:10:00Z',
        rawContent: 'Raw payload',
        metadata: JSON.stringify({ author: 'A. Writer' }),
        labels: [{ category: 'Language', value: 'English' }],
        tags: ['alpha'],
        processedInsight: {
          id: 'insight-1',
          title: 'Insight title',
          summary: 'Insight summary',
          keyInsights: JSON.stringify(['First', 'Second']),
          actionItems: JSON.stringify(['Act']),
          sourceTitle: 'Source title',
          author: 'Author',
          processedAt: '2026-03-16T10:10:00Z',
          labels: [{ category: 'Source', value: 'Web' }],
          tags: ['alpha']
        }
      }),
      loadCaptureDetail: vi.fn().mockResolvedValue(undefined),
      clearDetail: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CaptureDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'capture-1' })
            }
          }
        },
        {
          provide: CaptureStateService,
          useValue: captureStateStub
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CaptureDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Raw payload');
    expect(compiled.textContent).toContain('Insight title');
    expect(compiled.textContent).toContain('First');
    expect(compiled.textContent).toContain('A. Writer');
    expect(compiled.textContent).toContain('Language: English');
    expect(compiled.textContent).toContain('Source: Web');
  });

  it('renders a local not found state for missing captures', async () => {
    const captureStateStub = {
      loadingDetail: signal(false),
      detailError: signal<string | null>(null),
      detailNotFound: signal(true),
      captureDetail: signal(null),
      loadCaptureDetail: vi.fn().mockResolvedValue(undefined),
      clearDetail: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CaptureDetailComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'missing' })
            }
          }
        },
        {
          provide: CaptureStateService,
          useValue: captureStateStub
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CaptureDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Capture not found');
  });
});
