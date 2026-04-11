import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CaptureStateService } from '../../core/services/capture-state.service';

interface RenderNode {
  type: 'text' | 'list' | 'json';
  value: string;
  items: string[];
}

@Component({
  selector: 'app-capture-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './capture-detail.component.html',
  styleUrl: './capture-detail.component.scss'
})
export class CaptureDetailComponent implements OnInit, OnDestroy {
  captureState = inject(CaptureStateService);
  private route = inject(ActivatedRoute);

  capture = computed(() => this.captureState.captureDetail());
  retrying = computed(() => this.captureState.loadingDetail());
  metadataNode = computed(() => this.parseContent(this.capture()?.metadata));
  keyInsightsNode = computed(() => this.parseContent(this.capture()?.processedInsight?.keyInsights));
  actionItemsNode = computed(() => this.parseContent(this.capture()?.processedInsight?.actionItems));

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    await this.captureState.loadCaptureDetail(id);
  }

  async retryCapture(): Promise<void> {
    const id = this.capture()?.id;
    if (!id) {
      return;
    }

    this.captureState.loadingDetail.set(true);
    this.captureState.detailError.set(null);

    try {
      await this.captureState.retryCapture(id);
      await this.captureState.loadCaptureDetail(id);
    } catch {
      this.captureState.detailError.set('Capture retry could not be started.');
    } finally {
      this.captureState.loadingDetail.set(false);
    }
  }

  ngOnDestroy(): void {
    this.captureState.clearDetail();
  }

  private parseContent(value: string | null | undefined): RenderNode {
    if (!value) {
      return { type: 'text', value: '—', items: [] };
    }

    try {
      const parsed = JSON.parse(value) as unknown;
      if (Array.isArray(parsed)) {
        return {
          type: 'list',
          value: '',
          items: parsed.map(item => String(item))
        };
      }

      if (parsed && typeof parsed === 'object') {
        return {
          type: 'json',
          value: JSON.stringify(parsed, null, 2),
          items: []
        };
      }
    } catch {
      return { type: 'text', value, items: [] };
    }

    return { type: 'text', value, items: [] };
  }
}
