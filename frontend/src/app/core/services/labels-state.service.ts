import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  LabelAssignment,
  LabelCategorySummary,
  LabelSearchResult,
  LabelValueSummary
} from '../../shared/models/knowledge.model';

@Injectable({
  providedIn: 'root'
})
export class LabelsStateService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/v1/labels`;
  private searchApiUrl = `${environment.apiBaseUrl}/v1/search/labels`;

  categories = signal<LabelCategorySummary[]>([]);
  searchResults = signal<LabelSearchResult[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  mutating = signal(false);
  mutationError = signal<string | null>(null);
  searchLoading = signal(false);
  searchError = signal<string | null>(null);

  async loadLabels(force = false): Promise<void> {
    if (this.loading()) {
      return;
    }

    if (!force && this.categories().length > 0) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await firstValueFrom(this.http.get<unknown>(this.apiUrl));
      this.categories.set(normalizeLabelCategoriesResponse(response));
    } catch {
      this.categories.set([]);
      this.error.set('Labels could not be loaded.');
    } finally {
      this.loading.set(false);
    }
  }

  async createCategory(name: string): Promise<boolean> {
    const normalized = name.trim();
    if (!normalized) {
      this.mutationError.set('Category name is required.');
      return false;
    }

    return this.mutateCategory(async () => {
      await firstValueFrom(this.http.post(`${this.apiUrl}/categories`, { name: normalized }));
      await this.loadLabels(true);
    }, normalized, 'create');
  }

  async renameCategory(id: string, name: string): Promise<boolean> {
    const normalized = name.trim();
    if (!normalized) {
      this.mutationError.set('Category name is required.');
      return false;
    }

    return this.mutateCategory(async () => {
      await firstValueFrom(this.http.patch(`${this.apiUrl}/categories/${id}`, { name: normalized }));
      await this.loadLabels(true);
    }, normalized, 'rename');
  }

  async deleteCategory(id: string): Promise<boolean> {
    return this.mutateCategory(async () => {
      await firstValueFrom(this.http.delete(`${this.apiUrl}/categories/${id}`));
      await this.loadLabels(true);
    }, id, 'delete');
  }

  async createValue(categoryId: string, value: string): Promise<boolean> {
    const normalized = value.trim();
    if (!normalized) {
      this.mutationError.set('Label value is required.');
      return false;
    }

    return this.mutateCategory(async () => {
      await firstValueFrom(
        this.http.post(`${this.apiUrl}/categories/${categoryId}/values`, { value: normalized })
      );
      await this.loadLabels(true);
    }, normalized, 'create-value');
  }

  async renameValue(valueId: string, value: string): Promise<boolean> {
    const normalized = value.trim();
    if (!normalized) {
      this.mutationError.set('Label value is required.');
      return false;
    }

    return this.mutateCategory(async () => {
      await firstValueFrom(this.http.patch(`${this.apiUrl}/values/${valueId}`, { value: normalized }));
      await this.loadLabels(true);
    }, normalized, 'rename-value');
  }

  async deleteValue(valueId: string): Promise<boolean> {
    return this.mutateCategory(async () => {
      await firstValueFrom(this.http.delete(`${this.apiUrl}/values/${valueId}`));
      await this.loadLabels(true);
    }, valueId, 'delete-value');
  }

  async searchLabels(labels: LabelAssignment[], matchAll: boolean): Promise<boolean> {
    const normalizedLabels = labels
      .map(label => ({
        category: label.category.trim(),
        value: label.value.trim()
      }))
      .filter(label => label.category.length > 0 && label.value.length > 0);

    if (normalizedLabels.length === 0) {
      this.searchError.set('Add at least one label pair before searching.');
      this.searchResults.set([]);
      return false;
    }

    this.searchLoading.set(true);
    this.searchError.set(null);

    try {
      const response = await firstValueFrom(
        this.http.post<unknown>(this.searchApiUrl, {
          labels: normalizedLabels,
          matchAll
        })
      );

      this.searchResults.set(normalizeLabelSearchResponse(response));
      return true;
    } catch {
      this.searchResults.set([]);
      this.searchError.set('Label search could not be completed.');
      return false;
    } finally {
      this.searchLoading.set(false);
    }
  }

  clearSearch(): void {
    this.searchResults.set([]);
    this.searchError.set(null);
    this.searchLoading.set(false);
  }

  private async mutateCategory(
    action: () => Promise<void>,
    label: string,
    operation: 'create' | 'rename' | 'delete' | 'create-value' | 'rename-value' | 'delete-value'
  ): Promise<boolean> {
    this.mutating.set(true);
    this.mutationError.set(null);

    try {
      await action();
      return true;
    } catch (err: unknown) {
      this.mutationError.set(resolveMutationError(err, label, operation));
      return false;
    } finally {
      this.mutating.set(false);
    }
  }
}

function normalizeLabelCategoriesResponse(response: unknown): LabelCategorySummary[] {
  const source = Array.isArray(response)
    ? response
    : isRecord(response)
      ? (readArray(response['categories']) ?? readArray(response['items']) ?? readArray(response['data']))
      : null;

  if (!source) {
    return [];
  }

  return source.map(normalizeLabelCategorySummary).filter((category): category is LabelCategorySummary => category !== null);
}

function normalizeLabelCategorySummary(value: unknown): LabelCategorySummary | null {
  if (!isRecord(value)) {
    return null;
  }

  const id = readString(value['id']);
  const name = readString(value['name']);
  if (!id || !name) {
    return null;
  }

  const values = readArray(value['values'])?.map(normalizeLabelValueSummary).filter((item): item is LabelValueSummary => item !== null) ?? [];
  const count = readNumber(value['processedInsightCount'])
    ?? readNumber(value['rawCaptureCount'])
    ?? readNumber(value['count'])
    ?? values.reduce((total, item) => total + item.count, 0);
  const lastUsedAt = readNullableString(value['lastUsedAt']) ?? null;

  return {
    id,
    name,
    count,
    lastUsedAt,
    values
  };
}

function normalizeLabelValueSummary(value: unknown): LabelValueSummary | null {
  if (!isRecord(value)) {
    return null;
  }

  const id = readString(value['id']);
  const labelValue = readString(value['value']);
  if (!id || !labelValue) {
    return null;
  }

  return {
    id,
    value: labelValue,
    count: readNumber(value['processedInsightCount'])
      ?? readNumber(value['rawCaptureCount'])
      ?? readNumber(value['count'])
      ?? 0,
    lastUsedAt: readNullableString(value['lastUsedAt']) ?? null
  };
}

function normalizeLabelSearchResponse(response: unknown): LabelSearchResult[] {
  const source = Array.isArray(response)
    ? response
    : isRecord(response)
      ? (readArray(response['results']) ?? readArray(response['items']) ?? readArray(response['data']))
      : null;

  if (!source) {
    return [];
  }

  return source.map(normalizeLabelSearchResult).filter((item): item is LabelSearchResult => item !== null);
}

function normalizeLabelSearchResult(value: unknown): LabelSearchResult | null {
  if (!isRecord(value)) {
    return null;
  }

  const id = readString(value['id']);
  const title = readString(value['title']);
  const sourceUrl = readString(value['sourceUrl']);
  if (!id || !title || !sourceUrl) {
    return null;
  }

  const tags = readStringArray(value['tags']);
  const labels = readArray(value['labels'])?.map(normalizeLabelAssignment).filter((item): item is LabelAssignment => item !== null) ?? [];

  return {
    id,
    title,
    summary: readNullableString(value['summary']),
    sourceUrl,
    processedAt: readNullableString(value['processedAt']),
    tags,
    labels
  };
}

function normalizeLabelAssignment(value: unknown): LabelAssignment | null {
  if (!isRecord(value)) {
    return null;
  }

  const category = readString(value['category']);
  const labelValue = readString(value['value']);
  if (!category || !labelValue) {
    return null;
  }

  return {
    category,
    value: labelValue
  };
}

function resolveMutationError(
  error: unknown,
  label: string,
  operation: 'create' | 'rename' | 'delete' | 'create-value' | 'rename-value' | 'delete-value'
): string {
  const status = isRecord(error) && typeof error['status'] === 'number' ? error['status'] as number : undefined;
  const messages: Record<'create' | 'rename' | 'delete' | 'create-value' | 'rename-value' | 'delete-value', string> = {
    create: `A category named "${label}" already exists.`,
    rename: `A category named "${label}" already exists.`,
    delete: 'Category could not be deleted.',
    'create-value': `A label value named "${label}" already exists.`,
    'rename-value': `A label value named "${label}" already exists.`,
    'delete-value': 'Label value could not be deleted.'
  };

  if (status === 409) {
    return messages[operation];
  }

  return operation === 'delete' || operation === 'delete-value'
    ? messages[operation]
    : `Label mutation failed for "${label}".`;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readArray(value: unknown): unknown[] | null {
  return Array.isArray(value) ? value : null;
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

function readNullableString(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function readStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .filter((item): item is string => typeof item === 'string')
    .map(item => item.trim())
    .filter(item => item.length > 0);
}

function readNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}
