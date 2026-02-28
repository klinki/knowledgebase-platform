import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { KnowledgeService } from '../../core/services/knowledge.service';
import { switchMap, debounceTime, distinctUntilChanged, tap } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dashboard">
      <header class="dashboard-header">
        <div class="header-content">
          <h1>Sentinel Vault</h1>
          <p>Search and manage your semantic knowledge</p>
          
          <div class="search-container">
            <div class="search-bar glass" [class.is-loading]="knowledgeService.loading()">
              <span class="search-icon">@if(knowledgeService.loading()){ 🔄 } @else { 🔍 }</span>
              <input 
                type="text" 
                [(ngModel)]="searchQuery" 
                placeholder="Search across your knowledge..." 
                class="search-input"
              >
            </div>
          </div>
          
          @if (knowledgeService.error()) {
            <div class="search-error">
              ⚠️ {{ knowledgeService.error() }}
            </div>
          }
        </div>
      </header>

      <div class="main-grid">
        <section class="glass-card list-section">
          <h2>Recent Captures</h2>
          <div class="items-list">
            @for (item of filteredItems(); track item.id) {
              <div class="knowledge-item">
                <span class="status-dot"></span>
                <div class="item-info">
                  <span class="title">{{ item.title }}</span>
                  <span class="meta">{{ item.capturedAt | date:'mediumDate' }} • {{ item.url }}</span>
                </div>
              </div>
            } @empty {
              @if (!knowledgeService.loading()) {
                <div class="empty-state">
                  <p>No knowledge items found matching your search.</p>
                </div>
              }
            }
          </div>
        </section>

        <!-- Sidebar column omitted for brevity in replace_file_content, but should be preserved -->
        <section class="secondary-column">
          <div class="glass-card tags-section">
            <h2>Trending Tags</h2>
            <div class="tags-cloud">
              @for (tag of knowledgeService.topTags(); track tag.id) {
                <span class="tag-badge">
                  {{ tag.name }} <span class="count">{{ tag.count }}</span>
                </span>
              }
            </div>
          </div>
          
          <div class="glass-card stats-card">
            <h2>Quick Stats</h2>
            <div class="stat">
              <span class="stat-label">Total Items</span>
              <span class="stat-value">{{ knowledgeService.items().length }}</span>
            </div>
            <div class="stat">
              <span class="stat-label">Active Tags</span>
              <span class="stat-value">{{ knowledgeService.tags().length }}</span>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { animation: fadeIn 0.4s ease-out; }

    .dashboard-header {
      padding: 3rem 0 4rem;
      text-align: center;
      
      h1 {
        font-size: 3.5rem;
        margin-bottom: 0.5rem;
        background: linear-gradient(to bottom, #fff, #94a3b8);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }
      
      p { color: #94a3b8; font-size: 1.1rem; margin-bottom: 3rem; }
    }

    .search-container {
      max-width: 800px;
      margin: 0 auto;
    }

    .search-bar {
      display: flex;
      align-items: center;
      padding: 1rem 2rem;
      border-radius: 20px;
      transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
      border: 1px solid rgba(255, 255, 255, 0.1);
      
      &:focus-within {
        border-color: #6366f1;
        box-shadow: 0 0 20px rgba(99, 102, 241, 0.2);
        transform: scale(1.02);
      }

      &.is-loading .search-icon {
        animation: spin 1s linear infinite;
      }

      .search-icon { margin-right: 1.5rem; font-size: 1.25rem; }
      
      .search-input {
        background: transparent;
        border: none;
        color: white;
        font-size: 1.25rem;
        width: 100%;
        outline: none;
        &::placeholder { color: #475569; }
      }
    }

    .search-error {
      margin-top: 1rem;
      color: #f43f5e;
      font-size: 0.9rem;
    }

    .main-grid {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      padding-bottom: 4rem;
    }

    .glass-card {
      padding: 2rem;
      h2 {
        font-size: 1.25rem;
        margin-bottom: 2rem;
        color: #f8fafc;
        display: flex;
        align-items: center;
        gap: 12px;
      }
    }

    .knowledge-item {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      padding: 1.25rem;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.02);
      margin-bottom: 1rem;
      transition: background 0.2s;
      &:hover { background: rgba(255, 255, 255, 0.05); }
      .status-dot { width: 10px; height: 10px; border-radius: 50%; background: #6366f1; flex-shrink: 0; }
      .item-info { display: flex; flex-direction: column; }
      .title { color: #f8fafc; font-weight: 500; font-size: 1.1rem; margin-bottom: 4px; }
      .meta { font-size: 0.85rem; color: #64748b; }
    }

    .secondary-column { display: flex; flex-direction: column; gap: 2rem; }
    .tags-cloud { display: flex; flex-wrap: wrap; gap: 10px; }
    .tag-badge {
      background: rgba(99, 102, 241, 0.1);
      color: #818cf8;
      padding: 8px 16px;
      border-radius: 10px;
      font-size: 0.9rem;
      border: 1px solid rgba(129, 140, 248, 0.1);
      .count { margin-left: 6px; color: #6366f1; opacity: 0.8; font-size: 0.8rem; }
    }

    .stats-card .stat {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
      &:last-child { border: none; margin: 0; padding: 0; }
      .stat-label { color: #94a3b8; font-size: 0.9rem; }
      .stat-value { color: #f8fafc; font-weight: 600; font-size: 1.25rem; }
    }

    .empty-state { text-align: center; padding: 4rem 0; color: #64748b; }

    @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class DashboardComponent {
  knowledgeService = inject(KnowledgeService);
  
  searchQuery = signal('');
  
  // Reactively trigger search with debounce
  filteredItems = toSignal(
    toObservable(this.searchQuery).pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => this.knowledgeService.search(query))
    ),
    { initialValue: this.knowledgeService.items() }
  );
}
