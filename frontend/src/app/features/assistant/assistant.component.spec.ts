import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AssistantComponent } from './assistant.component';
import { AssistantChatStateService } from '../../core/services/assistant-chat-state.service';

describe('AssistantComponent', () => {
  it('loads persisted history on init', async () => {
    const state = createAssistantStateStub();

    await TestBed.configureTestingModule({
      imports: [AssistantComponent],
      providers: [{ provide: AssistantChatStateService, useValue: state }]
    }).compileComponents();

    const fixture = TestBed.createComponent(AssistantComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(state.load).toHaveBeenCalledTimes(1);
  });

  it('shows pending delete confirmation and confirms on button click', async () => {
    const pendingActionId = 'pending-action';
    const state = createAssistantStateStub({
      pendingActionId
    });

    await TestBed.configureTestingModule({
      imports: [AssistantComponent],
      providers: [{ provide: AssistantChatStateService, useValue: state }]
    }).compileComponents();

    const fixture = TestBed.createComponent(AssistantComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    clickButton(fixture.nativeElement as HTMLElement, 'Confirm Delete');
    await fixture.whenStable();

    expect(state.confirmAction).toHaveBeenCalledWith(pendingActionId);
  });
});

function createAssistantStateStub(options?: { pendingActionId?: string }) {
  const pendingAction = options?.pendingActionId
    ? {
        id: options.pendingActionId,
        actionType: 'DeleteCaptures' as const,
        status: 'PendingConfirmation' as const,
        targetResultSetId: 'result-set-1',
        captureCount: 3,
        executedCount: null,
        createdAt: '2026-04-14T10:00:00Z',
        confirmedAt: null,
        cancelledAt: null,
        executedAt: null
      }
    : null;

  return {
    load: vi.fn().mockResolvedValue(undefined),
    sendMessage: vi.fn().mockResolvedValue(undefined),
    confirmAction: vi.fn().mockResolvedValue(undefined),
    cancelAction: vi.fn().mockResolvedValue(undefined),
    loading: signal(false),
    sending: signal(false),
    acting: signal(false),
    error: signal<string | null>(null),
    messages: signal([]),
    pendingDeleteAction: signal(pendingAction)
  };
}

function clickButton(root: HTMLElement, label: string): void {
  const button = [...root.querySelectorAll('button')]
    .find(candidate => (candidate.textContent ?? '').includes(label)) as HTMLButtonElement | undefined;

  expect(button).toBeDefined();
  button!.click();
}
