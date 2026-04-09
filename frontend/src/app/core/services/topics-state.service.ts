import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TopicClusterDetail } from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class TopicsStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/clusters`;

  topicDetail = signal<TopicClusterDetail | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  notFound = signal(false);

  async loadTopic(id: string): Promise<void> {
    if (!id) {
      this.topicDetail.set(null);
      this.notFound.set(true);
      this.error.set(null);
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.notFound.set(false);

    try {
      const topic = await firstValueFrom(this.http.get<TopicClusterDetail>(`${this.apiUrl}/${id}`));
      this.topicDetail.set(this.normalizeTopicDetail(topic));
    } catch (error: unknown) {
      this.topicDetail.set(null);
      const status = typeof error === 'object' && error !== null && 'status' in error
        ? Number((error as { status?: unknown }).status)
        : undefined;

      if (status === 404) {
        this.notFound.set(true);
      } else {
        this.error.set('Topic detail could not be loaded.');
      }
    } finally {
      this.loading.set(false);
    }
  }

  clear(): void {
    this.topicDetail.set(null);
    this.loading.set(false);
    this.error.set(null);
    this.notFound.set(false);
  }

  private normalizeTopicDetail(topic: TopicClusterDetail): TopicClusterDetail {
    return {
      ...topic,
      title: topic.title.trim(),
      description: topic.description?.trim() ?? null,
      keywords: (topic.keywords ?? []).map(keyword => keyword.trim()).filter(keyword => keyword.length > 0),
      suggestedLabel: {
        category: topic.suggestedLabel.category.trim(),
        value: topic.suggestedLabel.value.trim()
      },
      members: (topic.members ?? []).map(member => ({
        ...member,
        title: member.title.trim(),
        summary: member.summary.trim(),
        sourceUrl: member.sourceUrl.trim(),
        tags: (member.tags ?? []).map(tag => tag.trim()).filter(tag => tag.length > 0),
        labels: (member.labels ?? []).map(label => ({
          category: label.category.trim(),
          value: label.value.trim()
        })).filter(label => label.category.length > 0 && label.value.length > 0)
      }))
    };
  }
}
