import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  displayName: string;
  role: string;
}

export interface InvitationRequest {
  email: string;
  displayName: string;
  role: string;
}

export interface InvitationResponse {
  invitationId: string;
  email: string;
  role: string;
  token: string;
  invitationUrl: string;
  expiresAt: string;
}

export interface InvitationPreview {
  email: string;
  displayName: string;
  role: string;
  expiresAt: string;
}

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiBaseUrl = `${environment.apiBaseUrl}/auth`;
  private userState = signal<User | null>(null);
  private statusState = signal<AuthStatus>('unknown');
  private sessionPromise: Promise<AuthStatus> | null = null;
  private unauthorizedRedirectInProgress = false;

  currentUser = computed(() => this.userState());
  status = computed(() => this.statusState());
  isAuthenticated = computed(() => this.statusState() === 'authenticated');
  isLoading = computed(() => this.statusState() === 'unknown');

  constructor() {
    void this.ensureSessionResolved();
  }

  async login(email: string, password: string): Promise<User> {
    const user = await firstValueFrom(
      this.http.post<User>(`${this.apiBaseUrl}/login`, { email, password })
    );

    this.setAuthenticated(user);
    return user;
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`${this.apiBaseUrl}/logout`, {}));
    } finally {
      this.clearSession();
      this.unauthorizedRedirectInProgress = false;
    }
  }

  async approveDevice(userCode: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.apiBaseUrl}/device/approve`, { userCode })
    );
  }

  async createInvitation(request: InvitationRequest): Promise<InvitationResponse> {
    return await firstValueFrom(
      this.http.post<InvitationResponse>(`${this.apiBaseUrl}/invitations`, request)
    );
  }

  async previewInvitation(token: string): Promise<InvitationPreview> {
    return await firstValueFrom(
      this.http.get<InvitationPreview>(`${this.apiBaseUrl}/invitations/preview`, {
        params: { token }
      })
    );
  }

  async acceptInvitation(token: string, displayName: string, password: string): Promise<User> {
    const user = await firstValueFrom(
      this.http.post<User>(`${this.apiBaseUrl}/invitations/accept`, {
        token,
        displayName,
        password
      })
    );

    this.setAuthenticated(user);
    return user;
  }

  async ensureSessionResolved(): Promise<AuthStatus> {
    const currentStatus = this.statusState();
    if (currentStatus !== 'unknown') {
      return currentStatus;
    }

    if (this.sessionPromise) {
      return this.sessionPromise;
    }

    this.sessionPromise = this.loadCurrentSession();
    return this.sessionPromise;
  }

  async ensureAuthenticated(): Promise<boolean> {
    return (await this.ensureSessionResolved()) === 'authenticated';
  }

  async refreshSession(): Promise<AuthStatus> {
    this.statusState.set('unknown');
    return this.ensureSessionResolved();
  }

  async handleUnauthorized(): Promise<void> {
    this.clearSession();

    if (this.unauthorizedRedirectInProgress) {
      return;
    }

    const currentUrl = this.router.url || '/dashboard';
    if (currentUrl.startsWith('/login')) {
      return;
    }

    this.unauthorizedRedirectInProgress = true;
    try {
      await this.router.navigate(['/login'], {
        queryParams: { returnUrl: currentUrl }
      });
    } finally {
      this.unauthorizedRedirectInProgress = false;
    }
  }

  private async loadCurrentSession(): Promise<AuthStatus> {
    try {
      const user = await firstValueFrom(this.http.get<User>(`${this.apiBaseUrl}/me`));

      this.setAuthenticated(user);
      return 'authenticated';
    } catch {
      this.clearSession();
      return 'anonymous';
    } finally {
      this.sessionPromise = null;
    }
  }

  private setAuthenticated(user: User): void {
    this.userState.set(user);
    this.statusState.set('authenticated');
  }

  private clearSession(): void {
    this.userState.set(null);
    this.statusState.set('anonymous');
  }
}
