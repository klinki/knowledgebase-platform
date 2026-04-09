import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { SearchCriteria, SearchMatchMode, SearchStateService } from '../../core/services/search-state.service';
import { TagsStateService } from '../../core/services/tags-state.service';
import { LabelsStateService } from '../../core/services/labels-state.service';
import { LabelAssignment } from '../../shared/models/knowledge.model';

interface LabelRow {
  id: string;
  category: string;
  value: string;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="search-page">
      <header class="page-header">
        <div class="hero-copy">
          <p class="eyebrow">Search</p>
          <h1>Search the knowledgebase</h1>
          <p class="hero-text">
            Combine semantic query, exact tags, and category-value labels in one dedicated search flow.
          </p>
        </div>

        <div class="hero-stats">
          <div class="hero-stat">
            <span class="stat-value">{{ searchState.results().length }}</span>
            <span class="stat-label">Results</span>
          </div>
          <div class="hero-stat">
            <span class="stat-value">{{ selectedTags.length }}</span>
            <span class="stat-label">Tags</span>
          </div>
          <div class="hero-stat">
            <span class="stat-value">{{ activeLabelCount() }}</span>
            <span class="stat-label">Label Pairs</span>
          </div>
        </div>
      </header>

      <section class="glass-card search-card">
        <form class="search-form" (ngSubmit)="submitSearch()">
          <div class="field-block">
            <label for="search-query">Semantic query</label>
            <input
              id="search-query"
              type="text"
              name="searchQuery"
              [(ngModel)]="searchQuery"
              placeholder="Find concepts, summaries, and related captures"
              autocomplete="off"
            >
          </div>

          <div class="field-block">
            <div class="field-head">
              <label for="search-tag-input">Tags</label>
              <select
                name="tagMatchMode"
                [(ngModel)]="tagMatchMode"
              >
                <option value="any">Match any tag</option>
                <option value="all">Match all tags</option>
              </select>
            </div>

            <div class="tag-entry">
              <input
                id="search-tag-input"
                type="text"
                name="tagInput"
                [(ngModel)]="tagInput"
                (keydown)="onTagInputKeydown($event)"
                placeholder="Add an exact tag filter"
                list="search-tag-suggestions"
                autocomplete="off"
              >
              <button type="button" class="secondary-btn" (click)="addTagFromInput()">Add tag</button>
            </div>

            <datalist id="search-tag-suggestions">
              @for (tag of suggestedTags(); track tag) {
                <option [value]="tag"></option>
              }
            </datalist>

            @if (selectedTags.length > 0) {
              <div class="chip-group">
                @for (tag of selectedTags; track tag) {
                  <button type="button" class="chip removable" (click)="removeTag(tag)">
                    {{ tag }} <span>✕</span>
                  </button>
                }
              </div>
            } @else {
              <p class="hint">Leave blank to search without tag filters.</p>
            }
          </div>

          <div class="field-block">
            <div class="field-head">
              <label>Labels</label>
              <div class="field-actions">
                <select
                  name="labelMatchMode"
                  [(ngModel)]="labelMatchMode"
                >
                  <option value="all">Match all labels</option>
                  <option value="any">Match any label</option>
                </select>
                <button type="button" class="secondary-btn" (click)="addLabelRow()">Add pair</button>
              </div>
            </div>

            <div class="label-rows">
              @for (row of labelRows; track row.id) {
                <div class="label-row">
                  <input
                    type="text"
                    [name]="'label-category-' + row.id"
                    [(ngModel)]="row.category"
                    list="search-label-category-suggestions"
                    placeholder="Category"
                    autocomplete="off"
                  >
                  <input
                    type="text"
                    [name]="'label-value-' + row.id"
                    [(ngModel)]="row.value"
                    [attr.list]="'search-label-value-suggestions-' + row.id"
                    placeholder="Value"
                    autocomplete="off"
                  >
                  <button type="button" class="icon-btn" (click)="removeLabelRow(row.id)" [disabled]="labelRows.length === 1">
                    ✕
                  </button>

                  <datalist [id]="'search-label-value-suggestions-' + row.id">
                    @for (value of labelValueSuggestions(row); track value) {
                      <option [value]="value"></option>
                    }
                  </datalist>
                </div>
              }
            </div>

            <datalist id="search-label-category-suggestions">
              @for (category of labelCategorySuggestions(); track category) {
                <option [value]="category"></option>
              }
            </datalist>

            <p class="hint">Use exact category-value pairs such as Language: English or Source: Twitter.</p>
          </div>

          <div class="search-actions">
            <button type="submit" class="premium-btn" [disabled]="searchState.loading() || !canSubmit()">
              @if (searchState.loading()) {
                Searching...
              } @else {
                Search
              }
            </button>
            <button type="button" class="ghost-btn" (click)="clearSearch()" [disabled]="searchState.loading() && searchState.results().length === 0">
              Clear
            </button>
            <span class="results-count">
              {{ searchState.results().length }} result{{ searchState.results().length === 1 ? '' : 's' }}
            </span>
          </div>
        </form>

        @if (searchState.error()) {
          <div class="message error">{{ searchState.error() }}</div>
        }
      </section>

      <section class="glass-card results-card">
        @if (searchState.loading() && searchState.results().length === 0) {
          <div class="empty-state">
            <p>Searching captures...</p>
          </div>
        } @else if (searchState.results().length > 0) {
          <div class="result-list">
            @for (result of searchState.results(); track result.id) {
              <a class="result-card" [routerLink]="['/captures', result.id]">
                <div class="result-head">
                  <div>
                    <h2>{{ result.title }}</h2>
                    <p>
                      @if (result.processedAt) {
                        {{ result.processedAt | date:'mediumDate' }} •
                      }
                      {{ result.sourceUrl }}
                    </p>
                  </div>
                  @if (result.similarity !== null) {
                    <span class="score-pill">{{ result.similarity | number:'1.2-2' }}</span>
                  }
                </div>

                @if (result.summary) {
                  <p class="summary">{{ result.summary }}</p>
                }

                @if (result.tags.length > 0) {
                  <div class="chip-group subtle">
                    @for (tag of result.tags; track tag) {
                      <span class="chip">{{ tag }}</span>
                    }
                  </div>
                }

                @if (result.labels.length > 0) {
                  <div class="chip-group">
                    @for (label of result.labels; track label.category + ':' + label.value) {
                      <span class="chip accent">{{ label.category }}: {{ label.value }}</span>
                    }
                  </div>
                }
              </a>
            }
          </div>
        } @else if (canSubmit()) {
          <div class="empty-state">
            <p>No results matched the current search criteria.</p>
          </div>
        } @else {
          <div class="empty-state">
            <p>Enter a semantic query, add exact tags, or add label pairs to start searching.</p>
          </div>
        }
      </section>
    </div>
  `,
  styles: [`
    .search-page {
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
      max-width: 54rem;
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

    .glass-card {
      padding: 1.5rem;
    }

    .search-form {
      display: grid;
      gap: 1.5rem;
    }

    .field-block {
      display: grid;
      gap: 0.75rem;
    }

    .field-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .field-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    label {
      color: #e2e8f0;
      font-size: 0.95rem;
      font-weight: 600;
    }

    input,
    select {
      width: 100%;
      background: rgba(15, 23, 42, 0.6);
      color: #f8fafc;
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 12px;
      padding: 0.85rem 1rem;
      outline: none;
      transition: border-color 0.2s, box-shadow 0.2s;
    }

    input:focus,
    select:focus {
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.16);
    }

    input::placeholder {
      color: #64748b;
    }

    .tag-entry {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      gap: 0.75rem;
    }

    .label-rows {
      display: grid;
      gap: 0.75rem;
    }

    .label-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
      gap: 0.75rem;
      align-items: center;
    }

    .search-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
      align-items: center;
    }

    .premium-btn,
    .secondary-btn,
    .ghost-btn,
    .icon-btn {
      border: none;
      cursor: pointer;
      font: inherit;
      transition: all 0.2s;
    }

    .premium-btn,
    .secondary-btn,
    .ghost-btn {
      border-radius: 12px;
      padding: 0.85rem 1.1rem;
      white-space: nowrap;
    }

    .premium-btn {
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: white;
      box-shadow: 0 12px 24px rgba(99, 102, 241, 0.14);
    }

    .premium-btn:hover:not(:disabled) {
      filter: brightness(1.08);
      transform: translateY(-1px);
    }

    .secondary-btn {
      background: rgba(99, 102, 241, 0.12);
      color: #c7d2fe;
      border: 1px solid rgba(129, 140, 248, 0.2);
    }

    .secondary-btn:hover:not(:disabled) {
      background: rgba(99, 102, 241, 0.2);
    }

    .ghost-btn {
      background: rgba(255, 255, 255, 0.04);
      color: #94a3b8;
      border: 1px solid rgba(255, 255, 255, 0.08);
    }

    .ghost-btn:hover:not(:disabled) {
      background: rgba(255, 255, 255, 0.08);
      color: #e2e8f0;
    }

    .icon-btn {
      background: transparent;
      color: #cbd5e1;
      padding: 0.5rem;
      border-radius: 8px;
      opacity: 0.8;
    }

    .icon-btn:hover:not(:disabled) {
      background: rgba(255, 255, 255, 0.06);
      opacity: 1;
    }

    .premium-btn:disabled,
    .secondary-btn:disabled,
    .ghost-btn:disabled,
    .icon-btn:disabled {
      cursor: not-allowed;
      opacity: 0.55;
    }

    .chip-group {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .chip {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
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

    .chip.removable {
      background: rgba(255, 255, 255, 0.06);
      color: #f8fafc;
      border: 1px solid rgba(255, 255, 255, 0.08);
    }

    .chip.removable span {
      font-size: 0.75rem;
      opacity: 0.75;
    }

    .chip-group.subtle .chip {
      color: #cbd5e1;
    }

    .hint,
    .results-count {
      color: #94a3b8;
      font-size: 0.9rem;
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

    .result-list {
      display: grid;
      gap: 1rem;
    }

    .result-card {
      display: grid;
      gap: 0.85rem;
      padding: 1.1rem;
      border-radius: 16px;
      background: rgba(15, 23, 42, 0.38);
      border: 1px solid rgba(255, 255, 255, 0.06);
      text-decoration: none;
      transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease;
    }

    .result-card:hover {
      transform: translateY(-1px);
      background: rgba(15, 23, 42, 0.55);
      border-color: rgba(129, 140, 248, 0.22);
    }

    .result-head {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: flex-start;
    }

    .result-head h2 {
      margin: 0 0 0.35rem;
      font-size: 1.15rem;
      color: #f8fafc;
    }

    .result-head p {
      margin: 0;
      color: #94a3b8;
      font-size: 0.84rem;
      word-break: break-word;
    }

    .score-pill {
      flex-shrink: 0;
      background: rgba(52, 211, 153, 0.12);
      border: 1px solid rgba(52, 211, 153, 0.24);
      color: #86efac;
      padding: 0.35rem 0.7rem;
      border-radius: 999px;
      font-size: 0.82rem;
      font-weight: 600;
    }

    .summary {
      margin: 0;
      color: #dbeafe;
      line-height: 1.55;
    }

    .empty-state {
      padding: 2rem 0.5rem;
      color: #94a3b8;
      text-align: center;
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
      .page-header {
        grid-template-columns: 1fr;
        display: grid;
      }

      .hero-stats {
        min-width: 0;
      }
    }

    @media (max-width: 720px) {
      .hero-stats,
      .label-row,
      .tag-entry {
        grid-template-columns: 1fr;
      }

      .field-head,
      .field-actions,
      .search-actions,
      .result-head {
        align-items: stretch;
      }
    }
  `]
})
export class SearchComponent implements OnInit {
  private static rowSeed = 0;

  searchState = inject(SearchStateService);
  tagsState = inject(TagsStateService);
  labelsState = inject(LabelsStateService);

  private route = inject(ActivatedRoute);
  private router = inject(Router);

  searchQuery = '';
  tagInput = '';
  selectedTags: string[] = [];
  tagMatchMode: SearchMatchMode = 'any';
  labelMatchMode: SearchMatchMode = 'all';
  labelRows: LabelRow[] = [SearchComponent.createLabelRow()];

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.tagsState.loadTags(),
      this.labelsState.loadLabels()
    ]);

    const criteria = this.searchState.parseQueryParams(this.route.snapshot.queryParamMap);
    this.applyCriteria(criteria);

    if (this.searchState.hasCriteria(criteria)) {
      await this.searchState.search(criteria);
    }
  }

  canSubmit(): boolean {
    return this.searchState.hasCriteria(this.buildCriteria());
  }

  activeLabelCount(): number {
    return this.buildCriteria().labels.length;
  }

  addTagFromInput(): void {
    const normalized = this.tagInput.trim();
    if (!normalized) {
      return;
    }

    if (this.selectedTags.some(tag => tag.toLowerCase() === normalized.toLowerCase())) {
      this.tagInput = '';
      return;
    }

    this.selectedTags = [...this.selectedTags, normalized];
    this.tagInput = '';
  }

  removeTag(tag: string): void {
    this.selectedTags = this.selectedTags.filter(existing => existing !== tag);
  }

  addLabelRow(): void {
    this.labelRows = [...this.labelRows, SearchComponent.createLabelRow()];
  }

  removeLabelRow(rowId: string): void {
    if (this.labelRows.length === 1) {
      this.labelRows = [SearchComponent.createLabelRow()];
      return;
    }

    this.labelRows = this.labelRows.filter(row => row.id !== rowId);
  }

  labelCategorySuggestions(): string[] {
    return this.labelsState.categories().map(category => category.name);
  }

  labelValueSuggestions(row: LabelRow): string[] {
    const normalizedCategory = row.category.trim().toLowerCase();
    if (!normalizedCategory) {
      const values = new Set<string>();
      for (const category of this.labelsState.categories()) {
        for (const value of category.values) {
          values.add(value.value);
        }
      }

      return Array.from(values).sort((left, right) => left.localeCompare(right));
    }

    const category = this.labelsState.categories().find(item => item.name.toLowerCase() === normalizedCategory);
    return category?.values.map(value => value.value) ?? [];
  }

  suggestedTags(): string[] {
    const query = this.tagInput.trim().toLowerCase();
    return this.tagsState.tags()
      .map(tag => tag.name)
      .filter(tag => !this.selectedTags.some(selected => selected.toLowerCase() === tag.toLowerCase()))
      .filter(tag => query.length === 0 || tag.toLowerCase().includes(query))
      .slice(0, 10);
  }

  onTagInputKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter') {
      return;
    }

    event.preventDefault();
    this.addTagFromInput();
  }

  async submitSearch(): Promise<void> {
    const criteria = this.buildCriteria();
    if (!this.searchState.hasCriteria(criteria)) {
      return;
    }

    await this.searchState.syncUrl(this.router, this.route, criteria);
    await this.searchState.search(criteria);
  }

  async clearSearch(): Promise<void> {
    this.searchQuery = '';
    this.tagInput = '';
    this.selectedTags = [];
    this.tagMatchMode = 'any';
    this.labelMatchMode = 'all';
    this.labelRows = [SearchComponent.createLabelRow()];
    this.searchState.clear();
    await this.searchState.syncUrl(this.router, this.route, this.buildCriteria());
  }

  private applyCriteria(criteria: SearchCriteria): void {
    this.searchQuery = criteria.query;
    this.selectedTags = [...criteria.tags];
    this.tagMatchMode = criteria.tagMatchMode;
    this.labelMatchMode = criteria.labelMatchMode;
    this.labelRows = criteria.labels.length > 0
      ? criteria.labels.map(label => SearchComponent.createLabelRow(label.category, label.value))
      : [SearchComponent.createLabelRow()];
  }

  private buildCriteria(): SearchCriteria {
    return {
      query: this.searchQuery,
      tags: this.selectedTags,
      tagMatchMode: this.tagMatchMode,
      labels: this.labelRows.map(row => ({
        category: row.category,
        value: row.value
      })),
      labelMatchMode: this.labelMatchMode,
      limit: 20,
      threshold: 0.3
    };
  }

  private static createLabelRow(category = '', value = ''): LabelRow {
    SearchComponent.rowSeed += 1;
    return {
      id: `label-row-${SearchComponent.rowSeed}`,
      category,
      value
    };
  }
}
