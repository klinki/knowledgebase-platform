import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  TopicClusterDetail,
  TopicClusterDetailSortDirection,
  TopicClusterDetailSortField,
  TopicClusterListCriteria,
  TopicClusterListPage,
  TopicClusterSortDirection,
  TopicClusterSortField,
  TopicClusterSummary
} from '../../shared/models/knowledge.model';

const DEFAULT_PAGE_SIZE = 12;
const DEFAULT_SORT_FIELD: TopicClusterSortField = 'memberCount';
const DEFAULT_SORT_DIRECTION: TopicClusterSortDirection = 'desc';
const DEFAULT_TOPIC_DETAIL_PAGE_SIZE = 20;
const DEFAULT_TOPIC_DETAIL_SORT_FIELD: TopicClusterDetailSortField = 'rank';
const DEFAULT_TOPIC_DETAIL_SORT_DIRECTION: TopicClusterDetailSortDirection = 'asc';

export interface TopicClusterDetailCriteria {
  page: number;
  pageSize: number;
  sortField: TopicClusterDetailSortField;
  sortDirection: TopicClusterDetailSortDirection;
}

@Injectable({
  providedIn: 'root'
})
export class TopicsStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/clusters`;
  currentCriteria = signal<TopicClusterListCriteria>(this.createDefaultCriteria());
  topicDetailCriteria = signal<TopicClusterDetailCriteria>(this.createDefaultTopicDetailCriteria());

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

  createDefaultTopicDetailCriteria(): TopicClusterDetailCriteria {
    return {
      page: 1,
      pageSize: DEFAULT_TOPIC_DETAIL_PAGE_SIZE,
      sortField: DEFAULT_TOPIC_DETAIL_SORT_FIELD,
      sortDirection: DEFAULT_TOPIC_DETAIL_SORT_DIRECTION
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

  parseTopicDetailQueryParams(paramMap: ParamMap): TopicClusterDetailCriteria {
    const criteria = this.createDefaultTopicDetailCriteria();
    const rawPage = Number(paramMap.get('page'));
    const rawPageSize = Number(paramMap.get('pageSize'));
    criteria.page = Number.isInteger(rawPage) && rawPage > 0 ? rawPage : 1;
    criteria.pageSize = this.normalizeTopicDetailPageSize(rawPageSize);
    criteria.sortField = this.normalizeTopicDetailSortField(paramMap.get('sortField'));
    criteria.sortDirection = this.normalizeTopicDetailSortDirection(paramMap.get('sortDirection'));
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

  buildTopicDetailQueryParams(criteria: TopicClusterDetailCriteria): Record<string, string | null> {
    const normalized = this.normalizeTopicDetailCriteria(criteria);
    const defaults = this.createDefaultTopicDetailCriteria();
    const sortChanged =
      normalized.sortField !== defaults.sortField ||
      normalized.sortDirection !== defaults.sortDirection;

    return {
      page: normalized.page > 1 ? String(normalized.page) : null,
      pageSize: normalized.pageSize !== defaults.pageSize ? String(normalized.pageSize) : null,
      sortField: sortChanged ? normalized.sortField : null,
      sortDirection: sortChanged ? normalized.sortDirection : null
    };
  }

  hasCanonicalQueryParams(paramMap: ParamMap, criteria: TopicClusterListCriteria): boolean {
    const expected = this.buildQueryParams(criteria);
    return (paramMap.get('q') ?? null) === expected['q'] &&
      (paramMap.get('sortField') ?? null) === expected['sortField'] &&
      (paramMap.get('sortDirection') ?? null) === expected['sortDirection'] &&
      (paramMap.get('page') ?? null) === expected['page'];
  }

  hasCanonicalTopicDetailQueryParams(paramMap: ParamMap, criteria: TopicClusterDetailCriteria): boolean {
    const expected = this.buildTopicDetailQueryParams(criteria);
    return (paramMap.get('page') ?? null) === expected['page'] &&
      (paramMap.get('pageSize') ?? null) === expected['pageSize'] &&
      (paramMap.get('sortField') ?? null) === expected['sortField'] &&
      (paramMap.get('sortDirection') ?? null) === expected['sortDirection'];
  }

  async syncUrl(router: Router, route: ActivatedRoute, criteria: TopicClusterListCriteria): Promise<void> {
    await router.navigate([], {
      relativeTo: route,
      queryParams: this.buildQueryParams(criteria),
      replaceUrl: true
    });
  }

  async syncTopicDetailUrl(router: Router, route: ActivatedRoute, criteria: TopicClusterDetailCriteria): Promise<void> {
    await router.navigate([], {
      relativeTo: route,
      queryParams: this.buildTopicDetailQueryParams(criteria),
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

  async loadTopic(id: string, criteria: TopicClusterDetailCriteria = this.createDefaultTopicDetailCriteria()): Promise<void> {
    if (!id) {
      this.topicDetail.set(null);
      this.notFound.set(true);
      this.error.set(null);
      return;
    }

    const normalizedCriteria = this.normalizeTopicDetailCriteria(criteria);
    this.topicDetailCriteria.set(normalizedCriteria);
    this.loading.set(true);
    this.error.set(null);
    this.notFound.set(false);

    try {
      const topic = await firstValueFrom(
        this.http.get<TopicClusterDetail>(`${this.apiUrl}/${id}`, {
          params: {
            page: String(normalizedCriteria.page),
            pageSize: String(normalizedCriteria.pageSize),
            sortField: normalizedCriteria.sortField,
            sortDirection: normalizedCriteria.sortDirection
          }
        })
      );
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
    this.topicDetailCriteria.set(this.createDefaultTopicDetailCriteria());
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

  private normalizeTopicDetailCriteria(criteria: TopicClusterDetailCriteria): TopicClusterDetailCriteria {
    return {
      page: Number.isInteger(criteria.page) && criteria.page > 0 ? criteria.page : 1,
      pageSize: this.normalizeTopicDetailPageSize(criteria.pageSize),
      sortField: this.normalizeTopicDetailSortField(criteria.sortField),
      sortDirection: this.normalizeTopicDetailSortDirection(criteria.sortDirection)
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

  private normalizeTopicDetailSortField(sortField: string | null | undefined): TopicClusterDetailSortField {
    switch (sortField) {
      case 'similarity':
      case 'title':
      case 'sourceUrl':
      case 'rank':
        return sortField;
      default:
        return DEFAULT_TOPIC_DETAIL_SORT_FIELD;
    }
  }

  private normalizeTopicDetailSortDirection(sortDirection: string | null | undefined): TopicClusterDetailSortDirection {
    switch (sortDirection) {
      case 'asc':
      case 'desc':
        return sortDirection;
      default:
        return DEFAULT_TOPIC_DETAIL_SORT_DIRECTION;
    }
  }

  private normalizeTopicDetailPageSize(pageSize: number | null | undefined): number {
    switch (pageSize) {
      case 20:
      case 50:
      case 100:
        return pageSize;
      default:
        return DEFAULT_TOPIC_DETAIL_PAGE_SIZE;
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
    const membersPage = Number.isInteger(topic.membersPage) && topic.membersPage > 0 ? topic.membersPage : 1;
    const membersPageSize = this.normalizeTopicDetailPageSize(topic.membersPageSize);

    return {
      ...topic,
      title: topic.title.trim(),
      description: topic.description?.trim() ?? null,
      keywords: (topic.keywords ?? []).map(keyword => keyword.trim()).filter(keyword => keyword.length > 0),
      suggestedLabel: {
        category: topic.suggestedLabel.category.trim(),
        value: topic.suggestedLabel.value.trim()
      },
      membersPage,
      membersPageSize,
      membersTotalCount: Number.isInteger(topic.membersTotalCount) && topic.membersTotalCount >= 0
        ? topic.membersTotalCount
        : topic.memberCount,
      membersSortField: this.normalizeTopicDetailSortField(topic.membersSortField),
      membersSortDirection: this.normalizeTopicDetailSortDirection(topic.membersSortDirection),
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
