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
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss'
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
