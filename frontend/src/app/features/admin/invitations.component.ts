import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, InvitationResponse } from '../../core/services/auth.service';

@Component({
  selector: 'app-invitations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="invitations-page">
      <header>
        <h1>Invitations</h1>
        <p>Create invitation links for new users.</p>
      </header>

      @if (!isAdmin()) {
        <div class="glass-card">
          <p>You do not have permission to manage invitations.</p>
        </div>
      } @else {
        <div class="glass-card form-card">
          <p class="status error" *ngIf="error()">{{ error() }}</p>

          <form (ngSubmit)="createInvitation()">
            <div class="form-grid">
              <div class="form-group">
                <label for="email">Email</label>
                <input id="email" name="email" type="email" [(ngModel)]="email" required />
              </div>

              <div class="form-group">
                <label for="displayName">Display Name</label>
                <input id="displayName" name="displayName" [(ngModel)]="displayName" required />
              </div>

              <div class="form-group">
                <label for="role">Role</label>
                <select id="role" name="role" [(ngModel)]="role">
                  <option value="member">Member</option>
                  <option value="admin">Admin</option>
                </select>
              </div>
            </div>

            <button type="submit" class="premium-btn" [disabled]="submitting()">
              {{ submitting() ? 'Creating invitation...' : 'Create Invitation' }}
            </button>
          </form>
        </div>

        @if (createdInvitation()) {
          <div class="glass-card result-card">
            <h2>Invite Ready</h2>
            <p><strong>Email:</strong> {{ createdInvitation()!.email }}</p>
            <p><strong>Role:</strong> {{ createdInvitation()!.role }}</p>
            <p><strong>Expires:</strong> {{ createdInvitation()!.expiresAt | date:'medium' }}</p>

            <label for="inviteUrl">Invitation Link</label>
            <textarea id="inviteUrl" readonly>{{ createdInvitation()!.invitationUrl }}</textarea>

            <div class="actions">
              <button type="button" class="premium-btn" (click)="copyInvitationUrl()">Copy Link</button>
              <span class="copied" *ngIf="copied()">Copied</span>
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    h1 {
      font-size: 3rem;
      margin-bottom: 0.5rem;
      letter-spacing: -1px;
    }

    header p {
      color: #94a3b8;
      margin-bottom: 2rem;
      font-size: 1.05rem;
    }

    .form-card,
    .result-card {
      max-width: 760px;
    }

    .form-grid {
      display: grid;
      gap: 1rem;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    }

    .form-group {
      margin-bottom: 1rem;
    }

    label {
      display: block;
      margin-bottom: 0.45rem;
      color: #f8fafc;
    }

    input,
    select,
    textarea {
      width: 100%;
      padding: 0.85rem 1rem;
      border-radius: 10px;
      border: 1px solid rgba(255, 255, 255, 0.1);
      background: rgba(15, 23, 42, 0.55);
      color: #fff;
    }

    textarea {
      min-height: 120px;
      resize: vertical;
      margin-top: 0.5rem;
    }

    .actions {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-top: 1rem;
    }

    .status.error,
    .copied {
      color: #bbf7d0;
    }
  `]
})
export class InvitationsComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  displayName = '';
  role = 'member';
  submitting = signal(false);
  error = signal<string | null>(null);
  copied = signal(false);
  createdInvitation = signal<InvitationResponse | null>(null);

  isAdmin(): boolean {
    return this.authService.currentUser()?.role === 'admin';
  }

  async createInvitation(): Promise<void> {
    if (!this.isAdmin()) {
      await this.router.navigateByUrl('/dashboard');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.copied.set(false);

    try {
      const invitation = await this.authService.createInvitation({
        email: this.email,
        displayName: this.displayName,
        role: this.role
      });

      this.createdInvitation.set(invitation);
    } catch {
      this.error.set('Invitation creation failed. Check the values and try again.');
    } finally {
      this.submitting.set(false);
    }
  }

  async copyInvitationUrl(): Promise<void> {
    const invitation = this.createdInvitation();
    if (!invitation) {
      return;
    }

    await navigator.clipboard.writeText(invitation.invitationUrl);
    this.copied.set(true);
  }
}
