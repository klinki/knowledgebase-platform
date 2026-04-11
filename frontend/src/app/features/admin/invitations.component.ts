import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, InvitationResponse } from '../../core/services/auth.service';

@Component({
  selector: 'app-invitations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './invitations.component.html',
  styleUrl: './invitations.component.scss'
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
