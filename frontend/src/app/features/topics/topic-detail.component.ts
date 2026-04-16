import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';

import { TopicClusterDetailCriteria, TopicsStateService } from '../../core/services/topics-state.service';
import { TopicClusterDetailSortDirection, TopicClusterDetailSortField } from '../../shared/models/knowledge.model';

type TopicDetailSortOptionValue =
  | 'rank-asc'
  | 'rank-desc'
  | 'similarity-desc'
  | 'similarity-asc'
  | 'title-asc'
  | 'title-desc'
  | 'sourceUrl-asc'
  | 'sourceUrl-desc';

interface TopicDetailSortOption {
  value: TopicDetailSortOptionValue;
  label: string;
  sortField: TopicClusterDetailSortField;
  sortDirection: TopicClusterDetailSortDirection;
}

const DEFAULT_TOPIC_DETAIL_SORT: TopicDetailSortOptionValue = 'rank-asc';
const TOPIC_DETAIL_SORT_OPTIONS: TopicDetailSortOption[] = [
  { value: 'rank-asc', label: 'Rank (best first)', sortField: 'rank', sortDirection: 'asc' },
  { value: 'rank-desc', label: 'Rank (reverse)', sortField: 'rank', sortDirection: 'desc' },
  { value: 'similarity-desc', label: 'Highest similarity', sortField: 'similarity', sortDirection: 'desc' },
  { value: 'similarity-asc', label: 'Lowest similarity', sortField: 'similarity', sortDirection: 'asc' },
  { value: 'title-asc', label: 'Title A-Z', sortField: 'title', sortDirection: 'asc' },
  { value: 'title-desc', label: 'Title Z-A', sortField: 'title', sortDirection: 'desc' },
  { value: 'sourceUrl-asc', label: 'Source A-Z', sortField: 'sourceUrl', sortDirection: 'asc' },
  { value: 'sourceUrl-desc', label: 'Source Z-A', sortField: 'sourceUrl', sortDirection: 'desc' }
];

@Component({
  selector: 'app-topic-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './topic-detail.component.html',
  styleUrl: './topic-detail.component.scss'
})
export class TopicDetailComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  topicsState = inject(TopicsStateService);
  topic = computed(() => this.topicsState.topicDetail());
  detailCriteria = computed(() => this.topicsState.topicDetailCriteria());
  totalMembers = computed(() => this.topic()?.membersTotalCount ?? this.topic()?.memberCount ?? 0);
  currentPage = computed(() => this.topic()?.membersPage ?? this.detailCriteria().page);
  pageSize = computed(() => this.topic()?.membersPageSize ?? this.detailCriteria().pageSize);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalMembers() / this.pageSize())));

  readonly sortOptions = TOPIC_DETAIL_SORT_OPTIONS;
  readonly pageSizeOptions = [20, 50, 100] as const;
  selectedSort: TopicDetailSortOptionValue = DEFAULT_TOPIC_DETAIL_SORT;

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(paramMap => {
        void this.handleQueryParamChange(paramMap);
      });
  }

  ngOnDestroy(): void {
    this.topicsState.clear();
  }

  async onSortChange(sortValue: string): Promise<void> {
    const option = this.sortOptions.find(item => item.value === sortValue) ?? this.sortOptions[0];
    this.selectedSort = option.value;

    await this.topicsState.syncTopicDetailUrl(this.router, this.route, {
      ...this.detailCriteria(),
      sortField: option.sortField,
      sortDirection: option.sortDirection,
      page: 1
    });
  }

  async onPageSizeChange(value: number): Promise<void> {
    await this.topicsState.syncTopicDetailUrl(this.router, this.route, {
      ...this.detailCriteria(),
      pageSize: value,
      page: 1
    });
  }

  async goToPage(page: number): Promise<void> {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) {
      return;
    }

    await this.topicsState.syncTopicDetailUrl(this.router, this.route, {
      ...this.detailCriteria(),
      page
    });
  }

  private async handleQueryParamChange(paramMap: ParamMap): Promise<void> {
    const criteria = this.topicsState.parseTopicDetailQueryParams(paramMap);
    this.applyCriteria(criteria);

    if (!this.topicsState.hasCanonicalTopicDetailQueryParams(paramMap, criteria)) {
      await this.topicsState.syncTopicDetailUrl(this.router, this.route, criteria);
      return;
    }

    const id = this.route.snapshot.paramMap.get('id') ?? '';
    await this.topicsState.loadTopic(id, criteria);
  }

  private applyCriteria(criteria: TopicClusterDetailCriteria): void {
    this.selectedSort = this.toSortValue(criteria.sortField, criteria.sortDirection);
  }

  private toSortValue(
    sortField: TopicClusterDetailSortField,
    sortDirection: TopicClusterDetailSortDirection
  ): TopicDetailSortOptionValue {
    return this.sortOptions.find(item => item.sortField === sortField && item.sortDirection === sortDirection)?.value
      ?? DEFAULT_TOPIC_DETAIL_SORT;
  }
}
