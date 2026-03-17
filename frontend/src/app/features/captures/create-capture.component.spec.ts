import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { CreateCaptureComponent } from './create-capture.component';
import { CaptureStateService } from '../../core/services/capture-state.service';

describe('CreateCaptureComponent', () => {
  it('rejects an empty form client-side', async () => {
    const captureStateStub = {
      creating: signal(false),
      createError: signal<string | null>(null),
      createCapture: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CreateCaptureComponent],
      providers: [
        provideRouter([]),
        { provide: CaptureStateService, useValue: captureStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CreateCaptureComponent);
    fixture.detectChanges();

    await fixture.componentInstance.submit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Provide a URL or direct content.');
    expect(captureStateStub.createCapture).not.toHaveBeenCalled();
  });

  it('submits direct content and navigates to capture detail', async () => {
    const captureStateStub = {
      creating: signal(false),
      createError: signal<string | null>(null),
      createCapture: vi.fn().mockResolvedValue({ id: 'capture-1', message: 'accepted' })
    };

    await TestBed.configureTestingModule({
      imports: [CreateCaptureComponent],
      providers: [
        provideRouter([]),
        { provide: CaptureStateService, useValue: captureStateStub }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CreateCaptureComponent);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.componentInstance.rawContent = 'Manual content';
    fixture.componentInstance.selectedContentType.set('Note');
    fixture.componentInstance.tags = ' alpha, beta ';

    await fixture.componentInstance.submit();

    expect(captureStateStub.createCapture).toHaveBeenCalledWith({
      sourceUrl: '',
      contentType: 'Note',
      rawContent: 'Manual content',
      tags: [' alpha', ' beta ']
    });
    expect(navigateSpy).toHaveBeenCalledWith(['/captures', 'capture-1']);
  });
});
