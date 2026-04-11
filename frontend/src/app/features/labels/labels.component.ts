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
  templateUrl: './labels.component.html',
  styleUrl: './labels.component.scss'
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
