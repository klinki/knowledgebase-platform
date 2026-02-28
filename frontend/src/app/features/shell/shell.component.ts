import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { fadeAnimation } from '../../shared/animations';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  animations: [fadeAnimation],
  template: `
    <div class="premium-layout">
      <aside class="sidebar glass">
        <div class="brand">
          <span class="logo">S</span>
          <span class="name">Sentinel</span>
        </div>
        
        <nav>
          <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
            <span class="icon">📊</span> Dashboard
          </a>
          <a routerLink="/tags" routerLinkActive="active" class="nav-item">
            <span class="icon">🏷️</span> Tags
          </a>
        </nav>
        
        <div class="user-footer">
          <div class="user-info">
            <span class="user-name">{{ authService.currentUser()?.name }}</span>
          </div>
          <button (click)="logout()" class="logout-btn">Log out</button>
        </div>
      </aside>
      
      <main class="content" [@fadeAnimation]="o.isActivated ? o.activatedRoute : ''">
        <router-outlet #o="outlet"></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .premium-layout {
      display: flex;
      height: 100vh;
      width: 100vw;
      background: #0f172a;
    }

    .sidebar {
      width: 260px;
      height: 100%;
      display: flex;
      flex-direction: column;
      padding: 1.5rem;
      border-right: 1px solid rgba(255, 255, 255, 0.1);
      background: rgba(15, 23, 42, 0.8);
      backdrop-filter: blur(12px);
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 3rem;
      
      .logo {
        background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);
        width: 32px;
        height: 32px;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 8px;
        font-weight: bold;
        color: white;
      }
      
      .name {
        font-size: 1.25rem;
        font-weight: 600;
        letter-spacing: -0.5px;
      }
    }

    nav {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      border-radius: 12px;
      text-decoration: none;
      color: #94a3b8;
      transition: all 0.2s;
      
      &:hover {
        background: rgba(255, 255, 255, 0.05);
        color: white;
      }
      
      &.active {
        background: rgba(99, 102, 241, 0.1);
        color: #818cf8;
        font-weight: 500;
      }
    }

    .user-footer {
      margin-top: auto;
      padding-top: 1rem;
      border-top: 1px solid rgba(255, 255, 255, 0.05);
      
      .user-info {
        margin-bottom: 12px;
        font-size: 0.9rem;
        color: #f8fafc;
      }
    }

    .logout-btn {
      width: 100%;
      background: transparent;
      border: 1px solid rgba(255, 255, 255, 0.1);
      color: #94a3b8;
      padding: 8px;
      border-radius: 8px;
      cursor: pointer;
      font-size: 0.85rem;
      transition: all 0.2s;
      
      &:hover {
        background: rgba(239, 68, 68, 0.1);
        color: #ef4444;
        border-color: #ef4444;
      }
    }

    .content {
      flex: 1;
      overflow-y: auto;
      padding: 2rem;
    }
  `]
})
export class ShellComponent {
  authService = inject(AuthService);

  logout() {
    this.authService.logout();
    window.location.reload();
  }
}
