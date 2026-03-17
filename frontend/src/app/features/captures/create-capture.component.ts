import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CaptureStateService } from '../../core/services/capture-state.service';

type ManualContentType = 'Article' | 'Code' | 'Note' | 'Other';

@Component({
  selector: 'app-create-capture',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="create-capture-page">
      <header class="page-header">
        <div>
          <h1>Create Capture</h1>
          <p>Add a new capture directly from the frontend using a URL or pasted content.</p>
        </div>
        <a routerLink="/captures" class="secondary-link">Back to captures</a>
      </header>

      <div class="glass-card form-card">
        @if (submissionError()) {
          <p class="status error">{{ submissionError() }}</p>
        }

        @if (validationError()) {
          <p class="status error">{{ validationError() }}</p>
        }

        @if (successMessage()) {
          <p class="status success">{{ successMessage() }}</p>
        }

        <form (ngSubmit)="submit()">
          <div class="form-group">
            <label for="sourceUrl">Source URL</label>
            <input
              id="sourceUrl"
              name="sourceUrl"
              type="url"
              [(ngModel)]="sourceUrl"
              placeholder="https://example.com/article"
            />
            <p class="help-text">Optional for direct content. URL-only capture stores a minimal article-style record.</p>
          </div>

          <div class="form-group">
            <label for="contentType">Content Type</label>
            <select
              id="contentType"
              name="contentType"
              [ngModel]="selectedContentType()"
              (ngModelChange)="selectedContentType.set($event)"
              [disabled]="isUrlOnlyMode()"
            >
              <option value="">Select content type</option>
              @for (type of contentTypes; track type) {
                <option [value]="type">{{ type }}</option>
              }
            </select>
            @if (isUrlOnlyMode()) {
              <p class="help-text">URL-only capture is submitted as <strong>Article</strong>.</p>
            }
          </div>

          <div class="form-group">
            <label for="rawContent">Content</label>
            <textarea
              id="rawContent"
              name="rawContent"
              [(ngModel)]="rawContent"
              [attr.maxlength]="maxRawContentLength"
              rows="14"
              placeholder="Paste or type content here"
            ></textarea>
            <div class="content-meta">
              <span>{{ trimmedContentLength() }}/{{ maxRawContentLength }}</span>
              <span *ngIf="isUrlOnlyMode()">URL-only mode will generate minimal content from the URL.</span>
            </div>
          </div>

          <div class="form-group">
            <label for="tags">Tags</label>
            <input
              id="tags"
              name="tags"
              type="text"
              [(ngModel)]="tags"
              placeholder="tag-one, tag-two"
            />
          </div>

          <div class="actions">
            <button type="submit" class="premium-btn" [disabled]="captureState.creating()">
              {{ captureState.creating() ? 'Creating capture...' : 'Create Capture' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 2rem;
    }

    h1 {
      font-size: 3rem;
      margin: 0 0 0.5rem;
      letter-spacing: -1px;
    }

    .page-header p {
      margin: 0;
      color: #94a3b8;
    }

    .secondary-link {
      color: #c7d2fe;
      text-decoration: none;
      padding-top: 0.8rem;
    }

    .form-card {
      max-width: 880px;
    }

    .form-group + .form-group {
      margin-top: 1.25rem;
    }

    label {
      display: block;
      color: #f8fafc;
      margin-bottom: 0.45rem;
      font-weight: 500;
    }

    input,
    select,
    textarea {
      width: 100%;
      border-radius: 10px;
      border: 1px solid rgba(255, 255, 255, 0.1);
      background: rgba(15, 23, 42, 0.55);
      color: #fff;
      padding: 0.85rem 1rem;
    }

    textarea {
      resize: vertical;
      min-height: 18rem;
    }

    .help-text,
    .content-meta {
      margin-top: 0.45rem;
      color: #94a3b8;
      font-size: 0.88rem;
    }

    .content-meta {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
    }

    .actions {
      margin-top: 1.5rem;
    }

    .status {
      border-radius: 10px;
      padding: 0.85rem 1rem;
      margin-bottom: 1rem;
    }

    .status.error {
      color: #fecaca;
      background: rgba(239, 68, 68, 0.14);
      border: 1px solid rgba(239, 68, 68, 0.25);
    }

    .status.success {
      color: #bbf7d0;
      background: rgba(34, 197, 94, 0.14);
      border: 1px solid rgba(34, 197, 94, 0.25);
    }
  `]
})
export class CreateCaptureComponent {
  readonly maxRawContentLength = 10000;
  readonly contentTypes: ManualContentType[] = ['Article', 'Code', 'Note', 'Other'];

  captureState = inject(CaptureStateService);
  private router = inject(Router);

  sourceUrl = '';
  rawContent = '';
  tags = '';
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

    try {
      const accepted = await this.captureState.createCapture({
        sourceUrl: normalizedUrl,
        contentType: normalizedType,
        rawContent: normalizedContent,
        tags: this.tags.split(',')
      });

      this.successMessage.set('Capture created successfully.');
      await this.router.navigate(['/captures', accepted.id]);
    } catch {
      this.submissionError.set(this.captureState.createError() ?? 'Capture could not be created.');
    }
  }
}
