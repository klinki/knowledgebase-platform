import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { TopicsStateService } from '../../core/services/topics-state.service';

@Component({
  selector: 'app-topic-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './topic-detail.component.html',
  styleUrl: './topic-detail.component.scss'
})
export class TopicDetailComponent implements OnInit, OnDestroy {
  topicsState = inject(TopicsStateService);
  private route = inject(ActivatedRoute);

  topic = computed(() => this.topicsState.topicDetail());

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    await this.topicsState.loadTopic(id);
  }

  ngOnDestroy(): void {
    this.topicsState.clear();
  }
}
