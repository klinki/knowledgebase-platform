import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  displayName: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiBaseUrl = `${environment.apiBaseUrl}/auth`;
  private userState = signal<User | null>(null);
  private loadingState = signal(true);
  private sessionPromise: Promise<boolean> | null = null;

  currentUser = computed(() => this.userState());
  isAuthenticated = computed(() => !!this.userState());
  isLoading = computed(() => this.loadingState());

  constructor() {
    this.sessionPromise = this.refreshSession();
  }

  async login(email: string, password: string): Promise<User> {
    const user = await firstValueFrom(
      this.http.post<User>(`${this.apiBaseUrl}/login`, { email, password }, { withCredentials: true })
    );

    this.userState.set(user);
    return user;
  }

  async logout(): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.apiBaseUrl}/logout`, {}, { withCredentials: true })
    );
    this.userState.set(null);
  }

  async approveDevice(userCode: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${this.apiBaseUrl}/device/approve`, { userCode }, { withCredentials: true })
    );
  }

  async ensureAuthenticated(): Promise<boolean> {
    if (this.userState()) {
      return true;
    }

    if (!this.sessionPromise) {
      this.sessionPromise = this.refreshSession();
    }

    return this.sessionPromise;
  }

  async refreshSession(): Promise<boolean> {
    this.loadingState.set(true);

    try {
      const user = await firstValueFrom(
        this.http.get<User>(`${this.apiBaseUrl}/me`, { withCredentials: true })
      );

      this.userState.set(user);
      return true;
    } catch {
      this.userState.set(null);
      return false;
    } finally {
      this.loadingState.set(false);
      this.sessionPromise = null;
    }
  }
}
