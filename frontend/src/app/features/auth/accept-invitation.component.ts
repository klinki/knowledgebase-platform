import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, InvitationPreview } from '../../core/services/auth.service';

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accept-invitation.component.html',
  styleUrl: './accept-invitation.component.scss'
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
