import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CapturesComponent } from './captures.component';
import { CaptureStateService } from '../../core/services/capture-state.service';

describe('CapturesComponent', () => {
  it('selects failed captures on the current page and retries only those', async () => {
    const captureStateStub = createCaptureStateStub({
      captures: [
        createCaptureListItem('capture-1', 'Failed'),
        createCaptureListItem('capture-2', 'Completed'),
        createCaptureListItem('capture-3', 'Failed')
      ],
      totalFilteredCount: 3
    });

    await TestBed.configureTestingModule({
      imports: [CapturesComponent],
      providers: [
        provideRouter([]),
        { provide: CaptureStateService, useValue: captureStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CapturesComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    clickButton(fixture.nativeElement as HTMLElement, 'Select failed');
    fixture.detectChanges();

    clickButton(fixture.nativeElement as HTMLElement, 'Retry selected failed');
    await fixture.whenStable();

    expect(captureStateStub.retryFailedCaptures).toHaveBeenCalledWith(['capture-1', 'capture-3']);
  });

  it('retries all failed captures in the current filter scope', async () => {
    const captureStateStub = createCaptureStateStub({
      captures: [
        createCaptureListItem('capture-1', 'Failed'),
        createCaptureListItem('capture-2', 'Failed')
      ],
      totalFilteredCount: 6,
      filter: { contentType: 'Article', status: 'Failed' }
    });

    await TestBed.configureTestingModule({
      imports: [CapturesComponent],
      providers: [
        provideRouter([]),
        { provide: CaptureStateService, useValue: captureStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CapturesComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    clickButton(fixture.nativeElement as HTMLElement, 'Retry all failed');
    await fixture.whenStable();

    expect(captureStateStub.retryAllFailedCaptures).toHaveBeenCalledWith({
      contentType: 'Article',
      status: 'Failed'
    });
  });

  it('toggles page selection from the master checkbox', async () => {
    const captureStateStub = createCaptureStateStub({
      captures: [
        createCaptureListItem('capture-1', 'Failed'),
        createCaptureListItem('capture-2', 'Completed')
      ],
      totalFilteredCount: 2
    });

    await TestBed.configureTestingModule({
      imports: [CapturesComponent],
      providers: [
        provideRouter([]),
        { provide: CaptureStateService, useValue: captureStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CapturesComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const masterCheckbox = (fixture.nativeElement as HTMLElement)
      .querySelector('thead input[type="checkbox"]') as HTMLInputElement | null;
    expect(masterCheckbox).not.toBeNull();

    masterCheckbox!.click();
    masterCheckbox!.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('2 selected');
  });
});

function createCaptureStateStub(options: {
  captures: Array<{
    id: string;
    sourceUrl: string;
    contentType: string;
    status: string;
    createdAt: string;
    processedAt: string | null;
    failureReason: string | null;
  }>;
  totalFilteredCount: number;
  filter?: { contentType: string | null; status: string | null };
}) {
  return {
    loadingList: signal(false),
    listError: signal<string | null>(null),
    captures: signal(options.captures),
    totalFilteredCount: signal(options.totalFilteredCount),
    totalPages: signal(1),
    availableContentTypes: signal(['Article', 'Note']),
    availableStatuses: signal(['Completed', 'Failed']),
    currentSort: signal({ field: 'createdAt', direction: 'desc' as const }),
    currentFilter: signal(options.filter ?? { contentType: null, status: null }),
    currentPagination: signal({ page: 1, pageSize: 10 }),
    loadCaptures: vi.fn().mockResolvedValue(undefined),
    clearFilters: vi.fn(),
    setSort: vi.fn(),
    setFilter: vi.fn(),
    setPage: vi.fn(),
    setPageSize: vi.fn(),
    retryFailedCaptures: vi.fn().mockResolvedValue({
      retriedCount: 2,
      enqueuedCount: 2,
      message: 'accepted'
    }),
    retryAllFailedCaptures: vi.fn().mockResolvedValue({
      retriedCount: 6,
      enqueuedCount: 6,
      message: 'accepted'
    })
  };
}

function createCaptureListItem(id: string, status: string) {
  return {
    id,
    sourceUrl: `https://example.com/${id}`,
    contentType: 'Article',
    status,
    createdAt: '2026-04-11T10:00:00Z',
    processedAt: null,
    failureReason: status === 'Failed' ? 'Processing failed.' : null
  };
}

function clickButton(root: HTMLElement, label: string): void {
  const button = [...root.querySelectorAll('button')]
    .find(candidate => (candidate.textContent ?? '').includes(label)) as HTMLButtonElement | undefined;

  expect(button).toBeDefined();
  button!.click();
}
