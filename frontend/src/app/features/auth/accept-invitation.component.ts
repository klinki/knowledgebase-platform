import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, InvitationPreview } from '../../core/services/auth.service';

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="invite-page">
      <div class="glass-card invite-card">
        <h1>Join Sentinel</h1>
        <p class="subtitle">Complete your account registration from the invitation link.</p>

        @if (loading()) {
          <p class="status">Loading invitation...</p>
        } @else if (error()) {
          <p class="status error">{{ error() }}</p>
        } @else if (invitation()) {
          <div class="invite-summary">
            <p><strong>Email:</strong> {{ invitation()!.email }}</p>
            <p><strong>Role:</strong> {{ invitation()!.role }}</p>
            <p><strong>Expires:</strong> {{ invitation()!.expiresAt | date:'medium' }}</p>
          </div>

          <form (ngSubmit)="onSubmit()">
            <div class="form-group">
              <label for="displayName">Display Name</label>
              <input id="displayName" name="displayName" [(ngModel)]="displayName" required />
            </div>

            <div class="form-group">
              <label for="password">Password</label>
              <input id="password" name="password" type="password" [(ngModel)]="password" required />
            </div>

            <div class="form-group">
              <label for="confirmPassword">Confirm Password</label>
              <input id="confirmPassword" name="confirmPassword" type="password" [(ngModel)]="confirmPassword" required />
            </div>

            <button type="submit" class="premium-btn" [disabled]="submitting()">
              {{ submitting() ? 'Creating account...' : 'Create Account' }}
            </button>
          </form>
        }
      </div>
    </div>
  `,
  styles: [`
    .invite-page {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 2rem;
      background: radial-gradient(circle at top right, #1e293b 0%, #0f172a 100%);
    }

    .invite-card {
      width: 100%;
      max-width: 480px;
    }

    h1 {
      margin: 0 0 0.5rem;
      font-size: 2.4rem;
      letter-spacing: -0.05em;
    }

    .subtitle {
      color: #94a3b8;
      margin-bottom: 1.5rem;
    }

    .invite-summary,
    .status {
      margin-bottom: 1.5rem;
      color: #cbd5e1;
    }

    .status.error {
      color: #fecaca;
    }

    .form-group {
      margin-bottom: 1rem;
    }

    label {
      display: block;
      margin-bottom: 0.4rem;
      color: #f8fafc;
    }

    input {
      width: 100%;
      padding: 0.8rem 1rem;
      border-radius: 10px;
      border: 1px solid rgba(255, 255, 255, 0.1);
      background: rgba(15, 23, 42, 0.55);
      color: #fff;
    }

    button {
      width: 100%;
      margin-top: 0.5rem;
    }
  `]
})
export class AcceptInvitationComponent implements OnInit {
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  invitation = signal<InvitationPreview | null>(null);
  loading = signal(true);
  submitting = signal(false);
  error = signal<string | null>(null);

  token = '';
  displayName = '';
  password = '';
  confirmPassword = '';

  async ngOnInit(): Promise<void> {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.token) {
      this.error.set('Invitation token is missing.');
      this.loading.set(false);
      return;
    }

    try {
      const invitation = await this.authService.previewInvitation(this.token);
      this.invitation.set(invitation);
      this.displayName = invitation.displayName;
    } catch {
      this.error.set('Invitation is invalid or expired.');
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit(): Promise<void> {
    if (!this.token || !this.displayName || !this.password || !this.confirmPassword) {
      this.error.set('All fields are required.');
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    try {
      await this.authService.acceptInvitation(this.token, this.displayName, this.password);
      await this.router.navigateByUrl('/dashboard');
    } catch {
      this.error.set('Invitation acceptance failed. The invitation may be invalid or expired.');
    } finally {
      this.submitting.set(false);
    }
  }
}
