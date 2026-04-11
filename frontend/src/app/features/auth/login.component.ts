import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
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
  returnUrl: string | null = null;

  async ngOnInit(): Promise<void> {
    this.userCode = this.route.snapshot.queryParamMap.get('userCode');
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    const status = await this.authService.ensureSessionResolved();
    if (status !== 'authenticated') {
      return;
    }

    if (this.userCode) {
      await this.completeDeviceApproval();
      return;
    }

    await this.navigateAfterLogin();
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

      await this.navigateAfterLogin();
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

  private async navigateAfterLogin(): Promise<void> {
    const target = this.returnUrl &&
      this.returnUrl.startsWith('/') &&
      !this.returnUrl.startsWith('/login')
      ? this.returnUrl
      : '/dashboard';

    await this.router.navigateByUrl(target);
  }
}
