import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TopicClusterDetail, TopicClusterListPage, TopicClusterSummary } from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class TopicsStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/clusters`;

  topicsPage = signal<TopicClusterListPage>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 12
  });
  topicDetail = signal<TopicClusterDetail | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  notFound = signal(false);

  async loadTopicsPage(page = 1, pageSize = 12): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    this.notFound.set(false);

    try {
      const response = await firstValueFrom(
        this.http.get<TopicClusterListPage>(`${this.apiUrl}/list`, {
          params: {
            page: String(page),
            pageSize: String(pageSize)
          }
        })
      );
      this.topicsPage.set({
        ...response,
        items: (response.items ?? []).map(topic => this.normalizeTopicSummary(topic))
      });
    } catch {
      this.error.set('Topics could not be loaded.');
      this.topicsPage.set({
        items: [],
        totalCount: 0,
        page,
        pageSize
      });
    } finally {
      this.loading.set(false);
    }
  }

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
    this.topicsPage.set({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 12
    });
    this.topicDetail.set(null);
    this.loading.set(false);
    this.error.set(null);
    this.notFound.set(false);
  }

  private normalizeTopicSummary(topic: TopicClusterSummary): TopicClusterSummary {
    return {
      ...topic,
      title: topic.title.trim(),
      description: topic.description?.trim() ?? null,
      keywords: (topic.keywords ?? []).map(keyword => keyword.trim()).filter(keyword => keyword.length > 0),
      representativeInsights: (topic.representativeInsights ?? []).map(insight => ({
        ...insight,
        title: insight.title.trim(),
        summary: insight.summary.trim(),
        sourceUrl: insight.sourceUrl.trim()
      })),
      suggestedLabel: {
        category: topic.suggestedLabel.category.trim(),
        value: topic.suggestedLabel.value.trim()
      }
    };
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
