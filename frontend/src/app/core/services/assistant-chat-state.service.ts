import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AssistantChatAction,
  AssistantChatMessage,
  AssistantChatSendResponse,
  AssistantChatSession
} from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class AssistantChatStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/chat`;
  private sessionState = signal<AssistantChatSession | null>(null);
  private messagesState = signal<AssistantChatMessage[]>([]);

  loading = signal(false);
  sending = signal(false);
  acting = signal(false);
  error = signal<string | null>(null);
  session = computed(() => this.sessionState());
  messages = computed(() => this.messagesState());
  pendingDeleteAction = computed(() => {
    const items = this.messagesState();
    for (let index = items.length - 1; index >= 0; index -= 1) {
      const action = items[index].action;
      if (action?.actionType === 'DeleteCaptures' && action.status === 'PendingConfirmation') {
        return action;
      }
    }

    return null;
  });

  async load(): Promise<void> {
    if (this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const session = await firstValueFrom(
        this.http.get<AssistantChatSession>(`${this.apiUrl}/session`)
      );
      const messages = await firstValueFrom(
        this.http.get<AssistantChatMessage[]>(`${this.apiUrl}/session/messages`)
      );

      this.sessionState.set(session);
      this.messagesState.set(messages);
    } catch {
      this.error.set('Assistant chat could not be loaded.');
    } finally {
      this.loading.set(false);
    }
  }

  async sendMessage(message: string): Promise<void> {
    const trimmed = message.trim();
    if (!trimmed || this.sending()) {
      return;
    }

    this.sending.set(true);
    this.error.set(null);

    try {
      const response = await firstValueFrom(
        this.http.post<AssistantChatSendResponse>(`${this.apiUrl}/session/messages`, { message: trimmed })
      );

      this.messagesState.update(messages => [...messages, response.userMessage, response.assistantMessage]);
    } catch {
      this.error.set('Message could not be sent.');
    } finally {
      this.sending.set(false);
    }
  }

  async confirmAction(actionId: string): Promise<void> {
    await this.executeAction(`${this.apiUrl}/actions/${actionId}/confirm`);
  }

  async cancelAction(actionId: string): Promise<void> {
    await this.executeAction(`${this.apiUrl}/actions/${actionId}/cancel`);
  }

  private async executeAction(url: string): Promise<void> {
    if (this.acting()) {
      return;
    }

    this.acting.set(true);
    this.error.set(null);

    try {
      await firstValueFrom(this.http.post(url, {}));
      const messages = await firstValueFrom(
        this.http.get<AssistantChatMessage[]>(`${this.apiUrl}/session/messages`)
      );
      this.messagesState.set(messages);
    } catch {
      this.error.set('Assistant action could not be completed.');
    } finally {
      this.acting.set(false);
    }
  }
}
