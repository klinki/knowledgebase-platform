import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KnowledgeService } from '../../core/services/knowledge.service';

@Component({
  selector: 'app-tags',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="tags-page">
      <header>
        <h1>Tags Vault</h1>
        <p>Manage your semantic knowledge classification.</p>
      </header>

      <div class="glass-card">
        <table class="premium-table">
          <thead>
            <tr>
              <th>Tag Name</th>
              <th>Occurrences</th>
              <th>Last Used</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (tag of knowledgeService.tags(); track tag.id) {
              <tr>
                <td>
                  <span class="tag-pill">{{ tag.name }}</span>
                </td>
                <td>{{ tag.count }}</td>
                <td>{{ tag.lastUsed | date:'mediumDate' }}</td>
                <td>
                  <button class="action-btn">Edit</button>
                  <button class="action-btn delete">Delete</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    h1 { font-size: 3rem; margin-bottom: 0.5rem; letter-spacing: -1px; }
    header p { color: #94a3b8; margin-bottom: 3rem; font-size: 1.1rem; }

    .premium-table {
      width: 100%;
      border-collapse: collapse;
      
      th {
        text-align: left;
        padding: 1.25rem;
        color: #64748b;
        font-weight: 500;
        border-bottom: 2px solid rgba(255, 255, 255, 0.05);
        font-size: 0.9rem;
        text-transform: uppercase;
        letter-spacing: 1px;
      }
      
      td {
        padding: 1.25rem;
        border-bottom: 1px solid rgba(255, 255, 255, 0.03);
        color: #cbd5e1;
      }
    }

    .tag-pill {
      background: rgba(99, 102, 241, 0.1);
      color: #818cf8;
      padding: 6px 14px;
      border-radius: 8px;
      font-size: 0.95rem;
      font-weight: 500;
      border: 1px solid rgba(129, 140, 248, 0.15);
    }

    .action-btn {
      background: transparent;
      border: 1px solid rgba(255, 255, 255, 0.1);
      color: #94a3b8;
      padding: 6px 14px;
      border-radius: 8px;
      cursor: pointer;
      margin-right: 12px;
      font-size: 0.85rem;
      transition: all 0.2s;
      
      &:hover {
        border-color: #6366f1;
        color: white;
        background: rgba(99, 102, 241, 0.05);
      }
      
      &.delete:hover {
        border-color: #ef4444;
        color: #ef4444;
        background: rgba(239, 68, 68, 0.05);
      }
    }
  `]
})
export class TagsComponent {
  knowledgeService = inject(KnowledgeService);
}
