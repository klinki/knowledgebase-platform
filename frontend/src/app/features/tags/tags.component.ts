import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TagsStateService } from '../../core/services/tags-state.service';

@Component({
  selector: 'app-tags',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tags.component.html',
  styleUrl: './tags.component.scss'
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
