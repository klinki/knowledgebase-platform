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
    fixture.componentInstance.labelRows[0].category = 'Language';
    fixture.componentInstance.labelRows[0].value = 'English';

    await fixture.componentInstance.submit();

    expect(captureStateStub.createCapture).toHaveBeenCalledWith({
      sourceUrl: '',
      contentType: 'Note',
      rawContent: 'Manual content',
      tags: [' alpha', ' beta '],
      labels: [
        { category: 'Language', value: 'English' }
      ]
    });
    expect(navigateSpy).toHaveBeenCalledWith(['/captures', 'capture-1']);
  });

  it('blocks duplicate label categories client-side', async () => {
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

    fixture.componentInstance.sourceUrl = 'https://example.com/item';
    fixture.componentInstance.labelRows[0].category = 'Source';
    fixture.componentInstance.labelRows[0].value = 'Twitter';
    fixture.componentInstance.addLabelRow();
    fixture.componentInstance.labelRows[1].category = 'source';
    fixture.componentInstance.labelRows[1].value = 'Web';

    expect(fixture.componentInstance.hasInvalidLabelRows()).toBe(true);

    await fixture.componentInstance.submit();
    fixture.detectChanges();

    expect(captureStateStub.createCapture).not.toHaveBeenCalled();
    expect(fixture.componentInstance.hasInvalidLabelRows()).toBe(true);
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement | null;
    expect(submitButton?.disabled).toBe(true);
  });
});
