import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  TopicClusterDetail,
  TopicClusterListCriteria,
  TopicClusterListPage,
  TopicClusterSortDirection,
  TopicClusterSortField,
  TopicClusterSummary
} from '../../shared/models/knowledge.model';

const DEFAULT_PAGE_SIZE = 12;
const DEFAULT_SORT_FIELD: TopicClusterSortField = 'memberCount';
const DEFAULT_SORT_DIRECTION: TopicClusterSortDirection = 'desc';

@Injectable({
  providedIn: 'root'
})
export class TopicsStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/clusters`;
  currentCriteria = signal<TopicClusterListCriteria>(this.createDefaultCriteria());

  topicsPage = signal<TopicClusterListPage>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE
  });
  topicDetail = signal<TopicClusterDetail | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  notFound = signal(false);

  createDefaultCriteria(): TopicClusterListCriteria {
    return {
      query: '',
      sortField: DEFAULT_SORT_FIELD,
      sortDirection: DEFAULT_SORT_DIRECTION,
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE
    };
  }

  parseQueryParams(paramMap: ParamMap): TopicClusterListCriteria {
    const criteria = this.createDefaultCriteria();
    const rawPage = Number(paramMap.get('page'));
    const rawSortField = paramMap.get('sortField');
    const rawSortDirection = paramMap.get('sortDirection');

    criteria.query = paramMap.get('q')?.trim() ?? '';
    criteria.page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1;
    criteria.sortField = this.normalizeSortField(rawSortField);
    criteria.sortDirection = this.normalizeSortDirection(rawSortDirection);
    return criteria;
  }

  buildQueryParams(criteria: TopicClusterListCriteria): Record<string, string | null> {
    const normalized = this.normalizeCriteria(criteria);
    const defaultCriteria = this.createDefaultCriteria();
    const sortChanged =
      normalized.sortField !== defaultCriteria.sortField ||
      normalized.sortDirection !== defaultCriteria.sortDirection;

    return {
      q: normalized.query || null,
      sortField: sortChanged ? normalized.sortField : null,
      sortDirection: sortChanged ? normalized.sortDirection : null,
      page: normalized.page > 1 ? String(normalized.page) : null
    };
  }

  hasCanonicalQueryParams(paramMap: ParamMap, criteria: TopicClusterListCriteria): boolean {
    const expected = this.buildQueryParams(criteria);
    return (paramMap.get('q') ?? null) === expected['q'] &&
      (paramMap.get('sortField') ?? null) === expected['sortField'] &&
      (paramMap.get('sortDirection') ?? null) === expected['sortDirection'] &&
      (paramMap.get('page') ?? null) === expected['page'];
  }

  async syncUrl(router: Router, route: ActivatedRoute, criteria: TopicClusterListCriteria): Promise<void> {
    await router.navigate([], {
      relativeTo: route,
      queryParams: this.buildQueryParams(criteria),
      replaceUrl: true
    });
  }

  async loadTopicsPage(criteria: TopicClusterListCriteria = this.createDefaultCriteria()): Promise<void> {
    const normalized = this.normalizeCriteria(criteria);
    this.loading.set(true);
    this.error.set(null);
    this.notFound.set(false);
    this.currentCriteria.set(normalized);

    try {
      const response = await firstValueFrom(
        this.http.get<TopicClusterListPage>(`${this.apiUrl}/list`, {
          params: {
            page: String(normalized.page),
            pageSize: String(normalized.pageSize),
            sortField: normalized.sortField,
            sortDirection: normalized.sortDirection,
            ...(normalized.query ? { query: normalized.query } : {})
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
        page: normalized.page,
        pageSize: normalized.pageSize
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
    this.currentCriteria.set(this.createDefaultCriteria());
    this.topicsPage.set({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE
    });
    this.topicDetail.set(null);
    this.loading.set(false);
    this.error.set(null);
    this.notFound.set(false);
  }

  private normalizeCriteria(criteria: TopicClusterListCriteria): TopicClusterListCriteria {
    return {
      query: criteria.query.trim(),
      sortField: this.normalizeSortField(criteria.sortField),
      sortDirection: this.normalizeSortDirection(criteria.sortDirection),
      page: Number.isInteger(criteria.page) && criteria.page > 0 ? criteria.page : 1,
      pageSize: DEFAULT_PAGE_SIZE
    };
  }

  private normalizeSortField(sortField: string | null | undefined): TopicClusterSortField {
    switch (sortField) {
      case 'updatedAt':
      case 'title':
      case 'memberCount':
        return sortField;
      default:
        return DEFAULT_SORT_FIELD;
    }
  }

  private normalizeSortDirection(sortDirection: string | null | undefined): TopicClusterSortDirection {
    switch (sortDirection) {
      case 'asc':
      case 'desc':
        return sortDirection;
      default:
        return DEFAULT_SORT_DIRECTION;
    }
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
