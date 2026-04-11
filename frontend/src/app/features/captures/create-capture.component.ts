import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CaptureStateService } from '../../core/services/capture-state.service';

type ManualContentType = 'Article' | 'Code' | 'Note' | 'Other';

interface LabelRow {
  id: number;
  category: string;
  value: string;
}

interface CaptureLabelDto {
  category: string;
  value: string;
}

@Component({
  selector: 'app-create-capture',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './create-capture.component.html',
  styleUrl: './create-capture.component.scss'
})
export class CreateCaptureComponent {
  readonly maxRawContentLength = 10000;
  readonly contentTypes: ManualContentType[] = ['Article', 'Code', 'Note', 'Other'];

  captureState = inject(CaptureStateService);
  private router = inject(Router);
  private nextLabelRowId = 1;

  sourceUrl = '';
  rawContent = '';
  tags = '';
  labelRows: LabelRow[] = [this.createLabelRow()];
  selectedContentType = signal<string>('');
  validationError = signal<string | null>(null);
  submissionError = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  isUrlOnlyMode = computed(() => this.sourceUrl.trim().length > 0 && this.rawContent.trim().length === 0);
  trimmedContentLength = computed(() => this.rawContent.trim().length);

  async submit(): Promise<void> {
    this.validationError.set(null);
    this.submissionError.set(null);
    this.successMessage.set(null);

    const normalizedUrl = this.sourceUrl.trim();
    const normalizedContent = this.rawContent.trim();
    const normalizedType = this.isUrlOnlyMode() ? 'Article' : this.selectedContentType().trim();
    const labels = this.collectLabels(true);

    if (!normalizedUrl && !normalizedContent) {
      this.validationError.set('Provide a URL or direct content.');
      return;
    }

    if (!this.isUrlOnlyMode() && !normalizedContent) {
      this.validationError.set('Direct content is required unless you are creating a URL-only capture.');
      return;
    }

    if (!this.isUrlOnlyMode() && !normalizedType) {
      this.validationError.set('Select a content type for direct content capture.');
      return;
    }

    if (labels === null) {
      return;
    }

    try {
      const accepted = await this.captureState.createCapture({
        sourceUrl: normalizedUrl,
        contentType: normalizedType,
        rawContent: normalizedContent,
        tags: this.tags.split(','),
        labels
      });

      this.successMessage.set('Capture created successfully.');
      await this.router.navigate(['/captures', accepted.id]);
    } catch {
      this.submissionError.set(this.captureState.createError() ?? 'Capture could not be created.');
    }
  }

  addLabelRow(): void {
    this.labelRows = [...this.labelRows, this.createLabelRow()];
  }

  removeLabelRow(id: number): void {
    if (this.labelRows.length === 1) {
      this.labelRows = [this.createLabelRow()];
      return;
    }

    this.labelRows = this.labelRows.filter(row => row.id !== id);
  }

  hasInvalidLabelRows(): boolean {
    return this.collectLabels(false) === null;
  }

  private createLabelRow(): LabelRow {
    return {
      id: this.nextLabelRowId++,
      category: '',
      value: ''
    };
  }

  private collectLabels(setError: boolean): CaptureLabelDto[] | null {
    const seenCategories = new Set<string>();
    const labels: CaptureLabelDto[] = [];

    for (const row of this.labelRows) {
      const category = row.category.trim();
      const value = row.value.trim();
      const isBlank = category.length === 0 && value.length === 0;

      if (isBlank) {
        continue;
      }

      if (category.length === 0 || value.length === 0) {
        if (setError) {
          this.validationError.set('Each label row needs both a category and a value.');
        }
        return null;
      }

      const normalizedCategory = category.toLowerCase();
      if (seenCategories.has(normalizedCategory)) {
        if (setError) {
          this.validationError.set(`The label category "${category}" can only be used once.`);
        }
        return null;
      }

      seenCategories.add(normalizedCategory);
      labels.push({ category, value });
    }

    return labels;
  }
}
