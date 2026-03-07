import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
        <p class="subtitle" *ngIf="userCode">Approve this sign-in to connect your browser extension.</p>
        <p class="status success" *ngIf="approvalComplete()">{{ approvalMessage() }}</p>
        <p class="status error" *ngIf="error()">{{ error() }}</p>
        
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
      margin-bottom: 1rem;
    }

    .status {
      border-radius: 10px;
      margin: 1rem 0;
      padding: 0.85rem 1rem;
      text-align: left;
    }

    .status.success {
      background: rgba(34, 197, 94, 0.14);
      border: 1px solid rgba(34, 197, 94, 0.25);
      color: #bbf7d0;
    }

    .status.error {
      background: rgba(239, 68, 68, 0.14);
      border: 1px solid rgba(239, 68, 68, 0.25);
      color: #fecaca;
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
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  email = '';
  password = '';
  loading = signal(false);
  error = signal<string | null>(null);
  approvalComplete = signal(false);
  approvalMessage = signal<string | null>(null);
  userCode: string | null = null;

  async ngOnInit(): Promise<void> {
    this.userCode = this.route.snapshot.queryParamMap.get('userCode');

    if (this.userCode) {
      const isAuthenticated = await this.authService.ensureAuthenticated();
      if (isAuthenticated) {
        await this.completeDeviceApproval();
      }
    }
  }

  async onSubmit(): Promise<void> {
    if (!this.email || !this.password) return;
    
    this.loading.set(true);
    this.error.set(null);

    try {
      await this.authService.login(this.email, this.password);

      if (this.userCode) {
        await this.completeDeviceApproval();
        return;
      }

      await this.router.navigate(['/dashboard']);
    } catch {
      this.error.set('Authentication failed. Check your email and password.');
    } finally {
      this.loading.set(false);
    }
  }

  private async completeDeviceApproval(): Promise<void> {
    if (!this.userCode) {
      return;
    }

    try {
      await this.authService.approveDevice(this.userCode);
      this.approvalComplete.set(true);
      this.approvalMessage.set('Device approved. Return to the Sentinel extension to finish signing in.');
    } catch {
      this.error.set('Device approval failed. Start the sign-in flow again from the extension.');
    }
  }
}
