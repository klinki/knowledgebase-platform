import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AssistantChatStateService } from '../../core/services/assistant-chat-state.service';

@Component({
  selector: 'app-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assistant.component.html',
  styleUrl: './assistant.component.scss'
})
export class AssistantComponent implements OnInit {
  assistantState = inject(AssistantChatStateService);
  messageText = '';

  messages = computed(() => this.assistantState.messages());
  pendingDeleteAction = computed(() => this.assistantState.pendingDeleteAction());

  async ngOnInit(): Promise<void> {
    await this.assistantState.load();
  }

  async sendMessage(): Promise<void> {
    const message = this.messageText.trim();
    if (!message) {
      return;
    }

    this.messageText = '';
    await this.assistantState.sendMessage(message);
  }

  async confirmPendingDelete(): Promise<void> {
    const action = this.pendingDeleteAction();
    if (!action) {
      return;
    }

    await this.assistantState.confirmAction(action.id);
  }

  async cancelPendingDelete(): Promise<void> {
    const action = this.pendingDeleteAction();
    if (!action) {
      return;
    }

    await this.assistantState.cancelAction(action.id);
  }
}
