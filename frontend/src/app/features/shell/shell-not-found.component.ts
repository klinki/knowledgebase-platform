import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-shell-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="not-found-page glass-card">
      <p class="eyebrow">404</p>
      <h1>Page not found</h1>
      <p class="copy">The page you requested does not exist inside Sentinel.</p>

      <div class="actions">
        <a routerLink="/dashboard" class="premium-btn">Go to dashboard</a>
        <a routerLink="/captures" class="secondary-btn">Browse captures</a>
      </div>
    </div>
  `,
  styles: [`
    .not-found-page {
      max-width: 640px;
      margin: 4rem auto;
      text-align: center;
    }

    .eyebrow {
      margin: 0 0 0.6rem;
      color: #818cf8;
      text-transform: uppercase;
      letter-spacing: 0.2em;
      font-size: 0.85rem;
    }

    h1 {
      margin: 0 0 0.8rem;
      font-size: 3rem;
      letter-spacing: -0.05em;
    }

    .copy {
      color: #94a3b8;
      margin-bottom: 1.5rem;
    }

    .actions {
      display: flex;
      justify-content: center;
      gap: 1rem;
    }

    a.premium-btn,
    .secondary-btn {
      text-decoration: none;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }

    .secondary-btn {
      border: 1px solid rgba(255, 255, 255, 0.12);
      border-radius: 8px;
      padding: 0.75rem 1.5rem;
      color: #cbd5e1;
    }
  `]
})
export class ShellNotFoundComponent {
}
