import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TagsStateService } from '../../core/services/tags-state.service';

@Component({
  selector: 'app-tags',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tags-page">
      <header>
        <h1>Tags Vault</h1>
        <p>Create and manage your semantic knowledge classification.</p>
      </header>

      <!-- Create Tag Form -->
      <div class="glass-card create-card">
        <h2>Create Tag</h2>
        <form (ngSubmit)="submitCreate()" class="create-form">
          <div class="input-group">
            <input
              id="new-tag-name"
              type="text"
              [(ngModel)]="newTagName"
              name="newTagName"
              placeholder="Enter tag name…"
              class="tag-input"
              [disabled]="creating()"
              maxlength="100"
              autocomplete="off"
            >
            <button
              type="submit"
              class="btn btn-primary"
              [disabled]="creating() || !newTagName.trim()"
            >
              @if (creating()) {
                <span class="spinner"></span> Creating…
              } @else {
                + Create
              }
            </button>
          </div>
          @if (tagsState.mutationError() && lastMutation === 'create') {
            <p class="form-error">{{ tagsState.mutationError() }}</p>
          }
        </form>
      </div>

      <!-- Tags List -->
      <div class="glass-card">
        @if (tagsState.loading()) {
          <div class="empty-state"><p>Loading tags…</p></div>
        } @else if (tagsState.error()) {
          <div class="empty-state"><p class="error-text">{{ tagsState.error() }}</p></div>
        } @else if (tagsState.tags().length === 0) {
          <div class="empty-state">
            <p>No tags yet. Create your first one above.</p>
          </div>
        } @else {
          @if (tagsState.mutationError() && lastMutation !== 'create') {
            <div class="banner-error">{{ tagsState.mutationError() }}</div>
          }
          <table class="premium-table">
            <thead>
              <tr>
                <th>Tag</th>
                <th>Occurrences</th>
                <th>Last Used</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (tag of tagsState.tags(); track tag.id) {
                <tr [class.editing]="editingId() === tag.id">
                  <td>
                    @if (editingId() === tag.id) {
                      <input
                        type="text"
                        [(ngModel)]="editName"
                        class="tag-input inline"
                        maxlength="100"
                        autocomplete="off"
                        (keydown.enter)="confirmRename(tag.id)"
                        (keydown.escape)="cancelEdit()"
                      >
                    } @else {
                      <span class="tag-pill">{{ tag.name }}</span>
                    }
                  </td>
                  <td>{{ tag.count }}</td>
                  <td>{{ tag.lastUsedAt ? (tag.lastUsedAt | date:'mediumDate') : '—' }}</td>
                  <td class="actions-cell">
                    @if (editingId() === tag.id) {
                      <button
                        class="btn btn-sm btn-confirm"
                        [disabled]="mutating()"
                        (click)="confirmRename(tag.id)"
                        title="Save rename"
                      >✓ Save</button>
                      <button
                        class="btn btn-sm btn-ghost"
                        (click)="cancelEdit()"
                        title="Cancel"
                      >✕</button>
                    } @else if (pendingDeleteId() === tag.id) {
                      <span class="confirm-label">Delete?</span>
                      <button
                        class="btn btn-sm btn-danger"
                        [disabled]="mutating()"
                        (click)="confirmDelete(tag.id)"
                      >Yes</button>
                      <button
                        class="btn btn-sm btn-ghost"
                        (click)="pendingDeleteId.set(null)"
                      >No</button>
                    } @else {
                      <button
                        class="icon-btn"
                        title="Rename tag"
                        (click)="startEdit(tag.id, tag.name)"
                        [disabled]="mutating()"
                      >✏️</button>
                      <button
                        class="icon-btn danger"
                        title="Delete tag"
                        (click)="pendingDeleteId.set(tag.id)"
                        [disabled]="mutating()"
                      >🗑️</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </div>
  `,
  styles: [`
    .tags-page { animation: fadeIn 0.4s ease-out; }

    h1 { font-size: 3rem; margin-bottom: 0.5rem; letter-spacing: -1px; }
    header p { color: #94a3b8; margin-bottom: 3rem; font-size: 1.1rem; }

    h2 {
      font-size: 1.1rem;
      color: #f8fafc;
      margin-bottom: 1.5rem;
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 1px;
      font-size: 0.85rem;
      color: #64748b;
    }

    /* ── Cards ── */
    .glass-card { margin-bottom: 2rem; padding: 2rem; }
    .create-card { padding: 1.75rem 2rem; }

    /* ── Create form ── */
    .create-form { display: flex; flex-direction: column; gap: 0.75rem; }
    .input-group { display: flex; gap: 1rem; }

    .tag-input {
      flex: 1;
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 10px;
      color: #f8fafc;
      font-size: 1rem;
      padding: 0.75rem 1.25rem;
      outline: none;
      transition: border-color 0.2s, box-shadow 0.2s;
      &::placeholder { color: #475569; }
      &:focus {
        border-color: #6366f1;
        box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
      }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
      &.inline { flex: 1; padding: 0.4rem 0.75rem; font-size: 0.95rem; }
    }

    /* ── Buttons ── */
    .btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      border: none;
      border-radius: 10px;
      cursor: pointer;
      font-size: 0.95rem;
      font-weight: 500;
      padding: 0.75rem 1.5rem;
      transition: all 0.2s;
      white-space: nowrap;
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
    .btn-primary {
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: white;
      &:not(:disabled):hover { filter: brightness(1.15); transform: translateY(-1px); }
    }
    .btn-sm { padding: 0.35rem 0.85rem; font-size: 0.85rem; border-radius: 7px; }
    .btn-confirm {
      background: rgba(52, 211, 153, 0.12);
      border: 1px solid rgba(52, 211, 153, 0.3);
      color: #34d399;
      &:not(:disabled):hover { background: rgba(52, 211, 153, 0.2); }
    }
    .btn-danger {
      background: rgba(239, 68, 68, 0.12);
      border: 1px solid rgba(239, 68, 68, 0.3);
      color: #ef4444;
      &:not(:disabled):hover { background: rgba(239, 68, 68, 0.2); }
    }
    .btn-ghost {
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid rgba(255, 255, 255, 0.1);
      color: #94a3b8;
      &:hover { background: rgba(255, 255, 255, 0.08); }
    }

    /* ── Icon buttons ── */
    .icon-btn {
      background: transparent;
      border: none;
      cursor: pointer;
      font-size: 1.1rem;
      opacity: 0.6;
      padding: 4px 6px;
      border-radius: 6px;
      transition: opacity 0.15s, background 0.15s;
      &:hover { opacity: 1; background: rgba(255, 255, 255, 0.06); }
      &:disabled { opacity: 0.25; cursor: not-allowed; }
      &.danger:hover { background: rgba(239, 68, 68, 0.1); }
    }

    /* ── Error messages ── */
    .form-error {
      color: #f87171;
      font-size: 0.9rem;
      margin: 0;
      animation: fadeIn 0.2s ease-out;
    }
    .error-text { color: #f87171; }
    .banner-error {
      background: rgba(239, 68, 68, 0.08);
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 10px;
      color: #f87171;
      font-size: 0.9rem;
      margin-bottom: 1.5rem;
      padding: 0.75rem 1rem;
    }

    /* ── Table ── */
    .premium-table {
      width: 100%;
      border-collapse: collapse;

      th {
        text-align: left;
        padding: 1rem 1.25rem;
        color: #64748b;
        font-weight: 500;
        border-bottom: 2px solid rgba(255, 255, 255, 0.05);
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 1px;
      }

      td {
        padding: 1rem 1.25rem;
        border-bottom: 1px solid rgba(255, 255, 255, 0.03);
        color: #cbd5e1;
        vertical-align: middle;
      }

      tr:last-child td { border-bottom: none; }

      tr.editing { background: rgba(99, 102, 241, 0.04); }
    }

    .tag-pill {
      background: rgba(99, 102, 241, 0.1);
      color: #818cf8;
      padding: 5px 14px;
      border-radius: 8px;
      font-size: 0.95rem;
      font-weight: 500;
      border: 1px solid rgba(129, 140, 248, 0.15);
      display: inline-block;
    }

    .actions-cell {
      display: flex;
      align-items: center;
      gap: 8px;
      min-width: 140px;
    }

    .confirm-label {
      font-size: 0.85rem;
      color: #f87171;
      margin-right: 4px;
    }

    /* ── Spinner ── */
    .spinner {
      width: 14px;
      height: 14px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
      display: inline-block;
    }

    .empty-state {
      color: #94a3b8;
      padding: 3rem 0;
      text-align: center;
    }

    @keyframes spin { to { transform: rotate(360deg); } }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(6px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class TagsComponent implements OnInit {
  tagsState = inject(TagsStateService);

  newTagName = '';
  editName = '';

  creating = signal(false);
  mutating = signal(false);
  editingId = signal<string | null>(null);
  pendingDeleteId = signal<string | null>(null);
  lastMutation: 'create' | 'edit' | null = null;

  async ngOnInit(): Promise<void> {
    await this.tagsState.loadTags();
  }

  async submitCreate(): Promise<void> {
    const name = this.newTagName.trim();
    if (!name) return;

    this.lastMutation = 'create';
    this.creating.set(true);
    const ok = await this.tagsState.createTag(name);
    this.creating.set(false);

    if (ok) {
      this.newTagName = '';
    }
  }

  startEdit(id: string, currentName: string): void {
    this.tagsState.mutationError.set(null);
    this.editingId.set(id);
    this.editName = currentName;
    this.pendingDeleteId.set(null);
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.editName = '';
    this.tagsState.mutationError.set(null);
  }

  async confirmRename(id: string): Promise<void> {
    const name = this.editName.trim();
    if (!name) return;

    this.lastMutation = 'edit';
    this.mutating.set(true);
    const ok = await this.tagsState.renameTag(id, name);
    this.mutating.set(false);

    if (ok) {
      this.editingId.set(null);
      this.editName = '';
    }
  }

  async confirmDelete(id: string): Promise<void> {
    this.lastMutation = 'edit';
    this.mutating.set(true);
    await this.tagsState.deleteTag(id);
    this.mutating.set(false);
    this.pendingDeleteId.set(null);
  }
}
