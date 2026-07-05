import { computed, Injectable, signal } from '@angular/core';

import { AuthUser } from './auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly user = signal<AuthUser | null>(null);

  readonly currentUser = this.user.asReadonly();
  readonly isSignedIn = computed(() => this.user() !== null);

  setUser(user: AuthUser | null): void {
    this.user.set(user);
  }

  isAuthenticated(): boolean {
    return this.isSignedIn();
  }
}
