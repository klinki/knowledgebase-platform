import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';

import { TopicsStateService } from '../../core/services/topics-state.service';
import { TopicClusterListCriteria, TopicClusterSortDirection, TopicClusterSortField } from '../../shared/models/knowledge.model';

type TopicSortOptionValue =
  | 'memberCount-desc'
  | 'memberCount-asc'
  | 'updatedAt-desc'
  | 'updatedAt-asc'
  | 'title-asc'
  | 'title-desc';

interface TopicSortOption {
  value: TopicSortOptionValue;
  label: string;
  sortField: TopicClusterSortField;
  sortDirection: TopicClusterSortDirection;
}

const DEFAULT_SORT_OPTION: TopicSortOptionValue = 'memberCount-desc';
const TOPIC_SORT_OPTIONS: TopicSortOption[] = [
  { value: 'memberCount-desc', label: 'Most insights', sortField: 'memberCount', sortDirection: 'desc' },
  { value: 'memberCount-asc', label: 'Fewest insights', sortField: 'memberCount', sortDirection: 'asc' },
  { value: 'updatedAt-desc', label: 'Recently updated', sortField: 'updatedAt', sortDirection: 'desc' },
  { value: 'updatedAt-asc', label: 'Oldest updated', sortField: 'updatedAt', sortDirection: 'asc' },
  { value: 'title-asc', label: 'Title A-Z', sortField: 'title', sortDirection: 'asc' },
  { value: 'title-desc', label: 'Title Z-A', sortField: 'title', sortDirection: 'desc' }
];

@Component({
  selector: 'app-topics',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './topics.component.html',
  styleUrl: './topics.component.scss'
})
export class TopicsComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  topicsState = inject(TopicsStateService);
  topics = computed(() => this.topicsState.topicsPage().items);
  page = computed(() => this.topicsState.topicsPage().page);
  pageSize = computed(() => this.topicsState.topicsPage().pageSize);
  totalCount = computed(() => this.topicsState.topicsPage().totalCount);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  currentCriteria = computed(() => this.topicsState.currentCriteria());
  hasActiveSearch = computed(() => this.currentCriteria().query.length > 0);
  hasActiveCriteria = computed(() => {
    const criteria = this.currentCriteria();
    return criteria.query.length > 0 || criteria.sortField !== 'memberCount' || criteria.sortDirection !== 'desc';
  });
  overviewDescription = computed(() =>
    this.currentCriteria().sortField === 'memberCount' && this.currentCriteria().sortDirection === 'desc'
      ? 'Sorted by cluster size so the densest themes surface first.'
      : 'Use search and sorting to explore topic groups from different angles.');
  sortOptions = TOPIC_SORT_OPTIONS;

  searchQuery = '';
  selectedSort: TopicSortOptionValue = DEFAULT_SORT_OPTION;

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

  async submitSearch(): Promise<void> {
    const current = this.currentCriteria();
    await this.topicsState.syncUrl(this.router, this.route, {
      ...current,
      query: this.searchQuery,
      page: 1
    });
  }

  async onSortChange(sortValue: string): Promise<void> {
    this.selectedSort = (this.sortOptions.find(item => item.value === sortValue)?.value ?? DEFAULT_SORT_OPTION);
    const option = this.sortOptions.find(item => item.value === sortValue) ?? this.sortOptions[0];
    const current = this.currentCriteria();

    await this.topicsState.syncUrl(this.router, this.route, {
      ...current,
      query: this.searchQuery,
      sortField: option.sortField,
      sortDirection: option.sortDirection,
      page: 1
    });
  }

  async clearCriteria(): Promise<void> {
    this.searchQuery = '';
    this.selectedSort = DEFAULT_SORT_OPTION;
    await this.topicsState.syncUrl(this.router, this.route, this.topicsState.createDefaultCriteria());
  }

  async goToPage(page: number): Promise<void> {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    await this.topicsState.syncUrl(this.router, this.route, {
      ...this.currentCriteria(),
      query: this.searchQuery,
      page
    });
  }

  private async handleQueryParamChange(paramMap: ParamMap): Promise<void> {
    const criteria = this.topicsState.parseQueryParams(paramMap);
    this.applyCriteria(criteria);

    if (!this.topicsState.hasCanonicalQueryParams(paramMap, criteria)) {
      await this.topicsState.syncUrl(this.router, this.route, criteria);
      return;
    }

    await this.topicsState.loadTopicsPage(criteria);
  }

  private applyCriteria(criteria: TopicClusterListCriteria): void {
    this.searchQuery = criteria.query;
    this.selectedSort = this.toSortValue(criteria.sortField, criteria.sortDirection);
  }

  private toSortValue(sortField: TopicClusterSortField, sortDirection: TopicClusterSortDirection): TopicSortOptionValue {
    return this.sortOptions.find(option => option.sortField === sortField && option.sortDirection === sortDirection)?.value
      ?? DEFAULT_SORT_OPTION;
  }
}
