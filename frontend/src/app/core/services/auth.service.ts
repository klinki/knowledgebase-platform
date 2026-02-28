import { Injectable, signal, computed } from '@angular/core';

export interface User {
  email: string;
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // State using Signals
  private userState = signal<User | null>(null);

  // Derived state
  currentUser = computed(() => this.userState());
  isAuthenticated = computed(() => !!this.userState());

  login(email: string, password: string) {
    // Artificial delay for premium feel
    return new Promise((resolve) => {
      setTimeout(() => {
        this.userState.set({ email, name: email.split('@')[0] });
        resolve(true);
      }, 1000);
    });
  }

  logout() {
    this.userState.set(null);
  }
}
