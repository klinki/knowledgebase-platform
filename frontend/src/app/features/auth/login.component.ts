import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-page">
      <div class="glass-card login-card">
        <h1>Sentinel</h1>
        <p class="subtitle">Secure Knowledge Curation</p>
        
        <form (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="email">Email</label>
            <input 
              type="email" 
              id="email" 
              [(ngModel)]="email" 
              name="email" 
              required 
              placeholder="you@example.com"
            >
          </div>
          
          <div class="form-group">
            <label for="password">Password</label>
            <input 
              type="password" 
              id="password" 
              [(ngModel)]="password" 
              name="password" 
              required 
              placeholder="••••••••"
            >
          </div>
          
          <button type="submit" class="premium-btn" [disabled]="loading()">
            {{ loading() ? 'Authenticating...' : 'Sign In' }}
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .login-page {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 100vh;
      width: 100vw;
      background: radial-gradient(circle at top right, #1e293b 0%, #0f172a 100%);
    }

    .login-card {
      width: 100%;
      max-width: 400px;
      text-align: center;
      animation: fadeInScale 0.6s cubic-bezier(0.16, 1, 0.3, 1);
    }

    h1 {
      margin: 0;
      font-size: 2.5rem;
      letter-spacing: -1px;
      background: linear-gradient(135deg, #fff 0%, #94a3b8 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .subtitle {
      color: #94a3b8;
      margin-bottom: 2.5rem;
    }

    form {
      text-align: left;
    }

    .form-group {
      margin-bottom: 1.5rem;
      
      label {
        display: block;
        margin-bottom: 0.5rem;
        color: #f8fafc;
        font-size: 0.9rem;
      }

      input {
        width: 100%;
        background: rgba(15, 23, 42, 0.5);
        border: 1px solid rgba(255, 255, 255, 0.1);
        border-radius: 8px;
        padding: 0.75rem 1rem;
        color: white;
        transition: border 0.3s;

        &:focus {
          outline: none;
          border-color: #6366f1;
        }
      }
    }

    button {
      width: 100%;
      margin-top: 1rem;
    }

    @keyframes fadeInScale {
      from { opacity: 0; transform: scale(0.95) translateY(10px); }
      to { opacity: 1; transform: scale(1) translateY(0); }
    }
  `]
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);

  async onSubmit() {
    if (!this.email || !this.password) return;
    
    this.loading.set(true);
    try {
      await this.authService.login(this.email, this.password);
      await this.router.navigate(['/dashboard']);
    } finally {
      this.loading.set(false);
    }
  }
}
