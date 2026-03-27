import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { LabelsStateService } from '../../core/services/labels-state.service';
import { LabelCategorySummary, LabelValueSummary } from '../../shared/models/knowledge.model';

interface SearchRow {
  id: string;
  category: string;
  value: string;
}

@Component({
  selector: 'app-labels',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="labels-page">
      <header class="page-header">
        <div class="hero-copy">
          <p class="eyebrow">Labels</p>
          <h1>Category-value labels</h1>
          <p class="hero-text">
            Organize captures with exact pairs such as <strong>Language: English</strong> or
            <strong>Source: Twitter</strong>.
          </p>
        </div>

        <div class="hero-stats">
          <div class="hero-stat">
            <span class="stat-value">{{ totalCategories() }}</span>
            <span class="stat-label">Categories</span>
          </div>
          <div class="hero-stat">
            <span class="stat-value">{{ totalValues() }}</span>
            <span class="stat-label">Values</span>
          </div>
          <div class="hero-stat">
            <span class="stat-value">{{ totalUses() }}</span>
            <span class="stat-label">Uses</span>
          </div>
        </div>
      </header>

      <div class="top-grid">
        <section class="glass-card search-card">
          <div class="section-header">
            <div>
              <h2>Exact-pair search</h2>
              <p>Match processed insights by one or more category/value pairs.</p>
            </div>
          </div>

          <form class="search-form" (ngSubmit)="submitSearch()">
            <div class="search-rows">
              @for (row of searchRows; track row.id) {
                <div class="search-row">
                  <input
                    type="text"
                    [name]="'search-category-' + row.id"
                    [ngModel]="row.category"
                    (ngModelChange)="updateSearchRow(row.id, 'category', $event)"
                    list="label-category-suggestions"
                    placeholder="Category"
                    autocomplete="off"
                  >

                  <input
                    type="text"
                    [name]="'search-value-' + row.id"
                    [ngModel]="row.value"
                    (ngModelChange)="updateSearchRow(row.id, 'value', $event)"
                    list="label-value-suggestions"
                    placeholder="Value"
                    autocomplete="off"
                  >

                  <button
                    type="button"
                    class="icon-btn"
                    (click)="removeSearchRow(row.id)"
                    [disabled]="searchRows.length === 1"
                  >
                    ✕
                  </button>
                </div>
              }
            </div>

            <div class="search-actions">
              <button type="button" class="secondary-btn" (click)="addSearchRow()">Add pair</button>
              <label class="toggle">
                <input
                  type="checkbox"
                  name="matchAll"
                  [(ngModel)]="searchMatchAll"
                >
                Match all pairs
              </label>
              <button type="submit" class="premium-btn" [disabled]="labelsState.searchLoading()">
                @if (labelsState.searchLoading()) {
                  Searching...
                } @else {
                  Search labels
                }
              </button>
            </div>
          </form>

          @if (labelsState.searchError()) {
            <div class="message error">{{ labelsState.searchError() }}</div>
          }

          @if (labelsState.searchLoading()) {
            <div class="empty-state compact">
              <p>Searching processed insights...</p>
            </div>
          } @else if (searchSubmitted && searchResults().length === 0 && !labelsState.searchError()) {
            <div class="empty-state compact">
              <p>No processed insights matched the selected label pairs.</p>
            </div>
          } @else if (searchResults().length > 0) {
            <div class="search-results">
              @for (result of searchResults(); track result.id) {
                <article class="result-card">
                  <div class="result-header">
                    <div>
                      <h3>{{ result.title }}</h3>
                      <p>
                        @if (result.processedAt) {
                          {{ result.processedAt | date:'mediumDate' }} •
                        }
                        {{ result.sourceUrl }}
                      </p>
                    </div>
                  </div>

                  @if (result.summary) {
                    <p class="result-summary">{{ result.summary }}</p>
                  }

                  @if (result.labels.length > 0) {
                    <div class="chip-group">
                      @for (label of result.labels; track label.category + ':' + label.value) {
                        <span class="chip accent">{{ label.category }}: {{ label.value }}</span>
                      }
                    </div>
                  }

                  @if (result.tags.length > 0) {
                    <div class="chip-group subtle">
                      @for (tag of result.tags; track tag) {
                        <span class="chip">{{ tag }}</span>
                      }
                    </div>
                  }
                </article>
              }
            </div>
          } @else {
            <div class="empty-state compact">
              <p>Add a label pair and run a search to filter processed insights.</p>
            </div>
          }
        </section>

        <section class="glass-card create-card">
          <div class="section-header">
            <div>
              <h2>Create category</h2>
              <p>Categories stay separate from tags and can hold many values.</p>
            </div>
          </div>

          <form class="create-form" (ngSubmit)="submitCategory()">
            <input
              type="text"
              name="newCategoryName"
              [(ngModel)]="newCategoryName"
              placeholder="Language, Source, Topic..."
              autocomplete="off"
              maxlength="100"
            >
            <button type="submit" class="premium-btn" [disabled]="labelsState.mutating() || !newCategoryName.trim()">
              @if (labelsState.mutating()) {
                Saving...
              } @else {
                Create category
              }
            </button>
          </form>

          @if (labelsState.mutationError()) {
            <div class="message error">{{ labelsState.mutationError() }}</div>
          }
        </section>
      </div>

      <section class="glass-card catalog-card">
        <div class="section-header">
          <div>
            <h2>Catalog</h2>
            <p>Group values under categories to keep meaning and source distinct.</p>
          </div>
        </div>

        @if (labelsState.loading()) {
          <div class="empty-state">
            <p>Loading labels...</p>
          </div>
        } @else if (labelsState.error()) {
          <div class="empty-state error">
            <p>{{ labelsState.error() }}</p>
          </div>
        } @else if (categories().length === 0) {
          <div class="empty-state">
            <p>No label categories yet. Create the first one above.</p>
          </div>
        } @else {
          <div class="category-list">
            @for (category of categories(); track category.id) {
              <article class="category-card">
                <div class="category-header">
                  <div class="category-title">
                    @if (editingCategoryId === category.id) {
                      <input
                        type="text"
                        [name]="'category-edit-' + category.id"
                        [(ngModel)]="editingCategoryName"
                        maxlength="100"
                        autocomplete="off"
                      >
                    } @else {
                      <h3>{{ category.name }}</h3>
                    }

                    <p>
                      {{ category.count }} uses • {{ category.values.length }} values
                      @if (category.lastUsedAt) {
                        • Last used {{ category.lastUsedAt | date:'mediumDate' }}
                      }
                    </p>
                  </div>

                  <div class="category-actions">
                    @if (editingCategoryId === category.id) {
                      <button type="button" class="secondary-btn small" (click)="saveCategory(category.id)">
                        Save
                      </button>
                      <button type="button" class="ghost-btn small" (click)="cancelCategoryEdit()">Cancel</button>
                    } @else if (pendingDeleteCategoryId === category.id) {
                      <span class="confirm-label">Delete?</span>
                      <button type="button" class="danger-btn small" (click)="confirmCategoryDelete(category.id)">
                        Yes
                      </button>
                      <button type="button" class="ghost-btn small" (click)="cancelCategoryDelete()">No</button>
                    } @else {
                      <button type="button" class="icon-btn" (click)="startCategoryEdit(category)">✏️</button>
                      <button type="button" class="icon-btn danger" (click)="pendingDeleteCategoryId = category.id">🗑️</button>
                    }
                  </div>
                </div>

                <div class="value-list">
                  @for (value of category.values; track value.id) {
                    <div class="value-row">
                      <div class="value-main">
                        @if (editingValueId === value.id) {
                          <input
                            type="text"
                            [name]="'value-edit-' + value.id"
                            [(ngModel)]="editingValueName"
                            maxlength="100"
                            autocomplete="off"
                          >
                        } @else {
                          <span class="chip">{{ value.value }}</span>
                        }

                        <span class="value-meta">
                          {{ value.count }} uses
                          @if (value.lastUsedAt) {
                            • {{ value.lastUsedAt | date:'mediumDate' }}
                          }
                        </span>
                      </div>

                      <div class="value-actions">
                        @if (editingValueId === value.id) {
                          <button type="button" class="secondary-btn small" (click)="saveValue(value.id)">Save</button>
                          <button type="button" class="ghost-btn small" (click)="cancelValueEdit()">Cancel</button>
                        } @else if (pendingDeleteValueId === value.id) {
                          <span class="confirm-label">Delete?</span>
                          <button type="button" class="danger-btn small" (click)="confirmValueDelete(value.id)">
                            Yes
                          </button>
                          <button type="button" class="ghost-btn small" (click)="cancelValueDelete()">No</button>
                        } @else {
                          <button type="button" class="icon-btn" (click)="startValueEdit(value)">✏️</button>
                          <button type="button" class="icon-btn danger" (click)="pendingDeleteValueId = value.id">🗑️</button>
                        }
                      </div>
                    </div>
                  }
                </div>

                <form class="value-form" (ngSubmit)="submitValue(category.id)">
                  <input
                    type="text"
                    [name]="'new-value-' + category.id"
                    [ngModel]="valueDraft(category.id)"
                    (ngModelChange)="setValueDraft(category.id, $event)"
                    [attr.list]="'label-values-' + category.id"
                    placeholder="Add a value to this category"
                    autocomplete="off"
                    maxlength="100"
                  >
                  <button type="submit" class="secondary-btn" [disabled]="labelsState.mutating() || !valueDraft(category.id).trim()">
                    Add value
                  </button>
                </form>

                <datalist [id]="'label-values-' + category.id">
                  @for (suggestion of category.values; track suggestion.id) {
                    <option [value]="suggestion.value"></option>
                  }
                </datalist>
              </article>
            }
          </div>
        }
      </section>

      <datalist id="label-category-suggestions">
        @for (category of categories(); track category.id) {
          <option [value]="category.name"></option>
        }
      </datalist>

      <datalist id="label-value-suggestions">
        @for (value of valueSuggestions(); track value) {
          <option [value]="value"></option>
        }
      </datalist>
    </div>
  `,
  styles: [`
    .labels-page {
      display: grid;
      gap: 1.5rem;
      animation: fadeIn 0.35s ease-out;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      gap: 1.5rem;
      align-items: flex-start;
    }

    .hero-copy {
      max-width: 52rem;
    }

    .eyebrow {
      margin: 0 0 0.5rem;
      text-transform: uppercase;
      letter-spacing: 0.22em;
      color: #64748b;
      font-size: 0.75rem;
    }

    h1 {
      margin: 0 0 0.6rem;
      font-size: clamp(2.5rem, 5vw, 4rem);
      letter-spacing: -0.05em;
      line-height: 0.95;
    }

    .hero-text {
      margin: 0;
      color: #94a3b8;
      font-size: 1.05rem;
      line-height: 1.6;
    }

    .hero-text strong {
      color: #e2e8f0;
      font-weight: 600;
    }

    .hero-stats {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 0.75rem;
      min-width: min(100%, 26rem);
    }

    .hero-stat {
      background: rgba(15, 23, 42, 0.38);
      border: 1px solid rgba(255, 255, 255, 0.06);
      border-radius: 14px;
      padding: 1rem;
      text-align: center;
    }

    .stat-value {
      display: block;
      font-size: 1.7rem;
      font-weight: 700;
      color: #f8fafc;
      line-height: 1;
    }

    .stat-label {
      display: block;
      margin-top: 0.35rem;
      color: #94a3b8;
      font-size: 0.8rem;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    .top-grid {
      display: grid;
      grid-template-columns: 1.15fr 0.85fr;
      gap: 1.5rem;
    }

    .glass-card {
      padding: 1.5rem;
    }

    .section-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .section-header h2 {
      margin: 0 0 0.35rem;
      font-size: 1.05rem;
      color: #f8fafc;
    }

    .section-header p {
      margin: 0;
      color: #94a3b8;
      font-size: 0.92rem;
      line-height: 1.5;
    }

    .create-form,
    .value-form {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .search-form {
      display: grid;
      gap: 1rem;
    }

    .search-rows {
      display: grid;
      gap: 0.75rem;
    }

    .search-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
      gap: 0.75rem;
    }

    .search-actions {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.75rem;
    }

    .toggle {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      color: #cbd5e1;
      font-size: 0.95rem;
      user-select: none;
    }

    input {
      width: 100%;
      background: rgba(15, 23, 42, 0.6);
      color: #f8fafc;
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 12px;
      padding: 0.85rem 1rem;
      outline: none;
      transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s;
    }

    input:focus {
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.16);
    }

    input::placeholder {
      color: #64748b;
    }

    .secondary-btn,
    .ghost-btn,
    .danger-btn,
    .premium-btn {
      border: none;
      cursor: pointer;
      font: inherit;
      transition: all 0.2s;
      white-space: nowrap;
    }

    .secondary-btn,
    .ghost-btn,
    .danger-btn {
      border-radius: 10px;
      padding: 0.8rem 1rem;
    }

    .secondary-btn {
      background: rgba(99, 102, 241, 0.12);
      color: #c7d2fe;
      border: 1px solid rgba(129, 140, 248, 0.2);
    }

    .secondary-btn:hover {
      background: rgba(99, 102, 241, 0.2);
    }

    .ghost-btn {
      background: rgba(255, 255, 255, 0.04);
      color: #94a3b8;
      border: 1px solid rgba(255, 255, 255, 0.08);
    }

    .ghost-btn:hover {
      background: rgba(255, 255, 255, 0.08);
      color: #e2e8f0;
    }

    .danger-btn {
      background: rgba(239, 68, 68, 0.12);
      color: #fecaca;
      border: 1px solid rgba(239, 68, 68, 0.24);
    }

    .danger-btn:hover {
      background: rgba(239, 68, 68, 0.2);
    }

    .small {
      padding: 0.45rem 0.75rem;
      font-size: 0.85rem;
    }

    .premium-btn {
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: white;
      border-radius: 12px;
      padding: 0.85rem 1.1rem;
      box-shadow: 0 12px 24px rgba(99, 102, 241, 0.14);
    }

    .premium-btn:hover:not(:disabled) {
      filter: brightness(1.08);
      transform: translateY(-1px);
    }

    .premium-btn:disabled,
    .secondary-btn:disabled,
    .ghost-btn:disabled,
    .danger-btn:disabled,
    .icon-btn:disabled {
      cursor: not-allowed;
      opacity: 0.55;
      transform: none;
    }

    .icon-btn {
      background: transparent;
      border: none;
      cursor: pointer;
      padding: 0.4rem 0.5rem;
      border-radius: 8px;
      opacity: 0.75;
      color: #cbd5e1;
      transition: all 0.15s;
    }

    .icon-btn:hover {
      background: rgba(255, 255, 255, 0.06);
      opacity: 1;
    }

    .icon-btn.danger:hover {
      background: rgba(239, 68, 68, 0.1);
    }

    .message {
      margin-top: 1rem;
      padding: 0.8rem 1rem;
      border-radius: 12px;
      font-size: 0.92rem;
    }

    .message.error {
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.18);
      color: #fecaca;
    }

    .empty-state {
      padding: 2rem 0.5rem;
      color: #94a3b8;
      text-align: center;
    }

    .empty-state.compact {
      padding: 1rem 0;
      text-align: left;
    }

    .empty-state.error {
      color: #fecaca;
    }

    .catalog-card {
      display: grid;
      gap: 1rem;
    }

    .category-list {
      display: grid;
      gap: 1rem;
    }

    .category-card {
      border-radius: 16px;
      padding: 1rem;
      background: rgba(15, 23, 42, 0.38);
      border: 1px solid rgba(255, 255, 255, 0.06);
      display: grid;
      gap: 1rem;
    }

    .category-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
    }

    .category-title h3 {
      margin: 0 0 0.35rem;
      font-size: 1.1rem;
      color: #f8fafc;
    }

    .category-title p {
      margin: 0;
      color: #94a3b8;
      font-size: 0.88rem;
    }

    .category-title input {
      min-width: min(32rem, 100%);
    }

    .category-actions,
    .value-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
      justify-content: flex-end;
    }

    .confirm-label {
      color: #fca5a5;
      font-size: 0.85rem;
      margin-right: 0.15rem;
    }

    .value-list {
      display: grid;
      gap: 0.75rem;
    }

    .value-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 0.8rem 0.9rem;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid rgba(255, 255, 255, 0.04);
    }

    .value-main {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .value-main input {
      min-width: 16rem;
    }

    .value-meta {
      color: #94a3b8;
      font-size: 0.85rem;
    }

    .value-form {
      padding-top: 0.25rem;
    }

    .value-form input {
      flex: 1;
    }

    .chip-group {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-top: 0.9rem;
    }

    .chip {
      display: inline-flex;
      align-items: center;
      padding: 0.35rem 0.7rem;
      border-radius: 999px;
      background: rgba(255, 255, 255, 0.06);
      color: #dbeafe;
      border: 1px solid rgba(255, 255, 255, 0.08);
      font-size: 0.86rem;
    }

    .chip.accent {
      background: rgba(99, 102, 241, 0.14);
      border-color: rgba(129, 140, 248, 0.16);
      color: #c7d2fe;
    }

    .chip-group.subtle .chip {
      color: #cbd5e1;
    }

    .search-results {
      display: grid;
      gap: 0.9rem;
      margin-top: 1rem;
    }

    .result-card {
      border-radius: 14px;
      padding: 1rem;
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid rgba(255, 255, 255, 0.05);
    }

    .result-header h3 {
      margin: 0 0 0.3rem;
      font-size: 1rem;
      color: #f8fafc;
    }

    .result-header p {
      margin: 0;
      color: #94a3b8;
      font-size: 0.84rem;
      word-break: break-word;
    }

    .result-summary {
      margin: 0.8rem 0 0;
      color: #dbeafe;
      line-height: 1.5;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(8px);
      }

      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    @media (max-width: 1100px) {
      .page-header,
      .top-grid {
        grid-template-columns: 1fr;
        display: grid;
      }

      .hero-stats {
        min-width: 0;
      }
    }

    @media (max-width: 720px) {
      .hero-stats {
        grid-template-columns: 1fr;
      }

      .search-row,
      .create-form,
      .value-form,
      .category-header,
      .value-row {
        grid-template-columns: 1fr;
        flex-direction: column;
        align-items: stretch;
      }

      .category-actions,
      .value-actions,
      .search-actions {
        justify-content: flex-start;
      }
    }
  `]
})
export class LabelsComponent implements OnInit {
  private static searchRowSeed = 0;
  private static createSearchRow(): SearchRow {
    return {
      id: `search-row-${++LabelsComponent.searchRowSeed}`,
      category: '',
      value: ''
    };
  }

  labelsState = inject(LabelsStateService);

  newCategoryName = '';
  editingCategoryId: string | null = null;
  editingCategoryName = '';
  pendingDeleteCategoryId: string | null = null;
  editingValueId: string | null = null;
  editingValueName = '';
  pendingDeleteValueId: string | null = null;
  valueDrafts: Record<string, string> = {};
  searchRows: SearchRow[] = [LabelsComponent.createSearchRow()];
  searchMatchAll = true;
  searchSubmitted = false;

  categories = computed(() => this.labelsState.categories());
  searchResults = computed(() => this.labelsState.searchResults());
  totalCategories = computed(() => this.categories().length);
  totalValues = computed(() => this.categories().reduce((total, category) => total + category.values.length, 0));
  totalUses = computed(() => this.categories().reduce((total, category) => total + category.count, 0));

  async ngOnInit(): Promise<void> {
    await this.labelsState.loadLabels();
  }

  async submitCategory(): Promise<void> {
    const name = this.newCategoryName.trim();
    if (!name) {
      return;
    }

    const ok = await this.labelsState.createCategory(name);
    if (ok) {
      this.newCategoryName = '';
    }
  }

  startCategoryEdit(category: LabelCategorySummary): void {
    this.editingCategoryId = category.id;
    this.editingCategoryName = category.name;
    this.pendingDeleteCategoryId = null;
    this.cancelValueEdit();
  }

  cancelCategoryEdit(): void {
    this.editingCategoryId = null;
    this.editingCategoryName = '';
  }

  async saveCategory(categoryId: string): Promise<void> {
    const name = this.editingCategoryName.trim();
    if (!name) {
      return;
    }

    const ok = await this.labelsState.renameCategory(categoryId, name);
    if (ok) {
      this.cancelCategoryEdit();
    }
  }

  async confirmCategoryDelete(categoryId: string): Promise<void> {
    const ok = await this.labelsState.deleteCategory(categoryId);
    if (ok) {
      this.cancelCategoryDelete();
    }
  }

  cancelCategoryDelete(): void {
    this.pendingDeleteCategoryId = null;
  }

  startValueEdit(value: LabelValueSummary): void {
    this.editingValueId = value.id;
    this.editingValueName = value.value;
    this.pendingDeleteValueId = null;
    this.pendingDeleteCategoryId = null;
  }

  cancelValueEdit(): void {
    this.editingValueId = null;
    this.editingValueName = '';
  }

  async saveValue(valueId: string): Promise<void> {
    const value = this.editingValueName.trim();
    if (!value) {
      return;
    }

    const ok = await this.labelsState.renameValue(valueId, value);
    if (ok) {
      this.cancelValueEdit();
    }
  }

  async confirmValueDelete(valueId: string): Promise<void> {
    const ok = await this.labelsState.deleteValue(valueId);
    if (ok) {
      this.cancelValueDelete();
    }
  }

  cancelValueDelete(): void {
    this.pendingDeleteValueId = null;
  }

  setValueDraft(categoryId: string, value: string): void {
    this.valueDrafts = {
      ...this.valueDrafts,
      [categoryId]: value
    };
  }

  valueDraft(categoryId: string): string {
    return this.valueDrafts[categoryId] ?? '';
  }

  async submitValue(categoryId: string): Promise<void> {
    const draft = this.valueDrafts[categoryId]?.trim() ?? '';
    if (!draft) {
      return;
    }

    const ok = await this.labelsState.createValue(categoryId, draft);
    if (ok) {
      this.setValueDraft(categoryId, '');
    }
  }

  addSearchRow(): void {
    this.searchRows = [...this.searchRows, LabelsComponent.createSearchRow()];
  }

  removeSearchRow(rowId: string): void {
    if (this.searchRows.length === 1) {
      return;
    }

    this.searchRows = this.searchRows.filter(row => row.id !== rowId);
  }

  updateSearchRow(rowId: string, field: 'category' | 'value', value: string): void {
    this.searchRows = this.searchRows.map(row =>
      row.id === rowId ? { ...row, [field]: value } : row
    );
  }

  async submitSearch(): Promise<void> {
    this.searchSubmitted = true;
    const labels = this.searchRows
      .map(row => ({
        category: row.category.trim(),
        value: row.value.trim()
      }))
      .filter(row => row.category.length > 0 && row.value.length > 0);

    await this.labelsState.searchLabels(labels, this.searchMatchAll);
  }

  valueSuggestions(): string[] {
    const values = new Set<string>();
    for (const category of this.categories()) {
      for (const value of category.values) {
        values.add(value.value);
      }
    }

    return Array.from(values).sort((left, right) => left.localeCompare(right));
  }
}
