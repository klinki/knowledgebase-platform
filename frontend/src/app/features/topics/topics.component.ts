import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TopicsStateService } from '../../core/services/topics-state.service';

@Component({
  selector: 'app-topics',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './topics.component.html',
  styleUrl: './topics.component.scss'
})
export class TopicsComponent implements OnInit, OnDestroy {
  topicsState = inject(TopicsStateService);
  topics = computed(() => this.topicsState.topicsPage().items);
  page = computed(() => this.topicsState.topicsPage().page);
  pageSize = computed(() => this.topicsState.topicsPage().pageSize);
  totalCount = computed(() => this.topicsState.topicsPage().totalCount);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  async ngOnInit(): Promise<void> {
    await this.topicsState.loadTopicsPage();
  }

  ngOnDestroy(): void {
    this.topicsState.clear();
  }

  async goToPage(page: number): Promise<void> {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    await this.topicsState.loadTopicsPage(page, this.pageSize());
  }
}
