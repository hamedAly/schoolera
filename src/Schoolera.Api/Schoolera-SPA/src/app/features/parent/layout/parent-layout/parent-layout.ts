import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { LanguageSwitcher } from '../../../../core/layout/language-switcher/language-switcher';

interface ParentNavItem {
  readonly path: string;
  readonly labelKey: string;
}

const PARENT_NAV: readonly ParentNavItem[] = [
  { path: 'dashboard', labelKey: 'parent.nav.dashboard' },
  { path: 'profile', labelKey: 'parent.nav.profile' },
  { path: 'children', labelKey: 'parent.nav.children' },
  { path: 'applications', labelKey: 'parent.nav.applications' },
  { path: 'favorites', labelKey: 'parent.nav.favorites' },
  { path: 'payments', labelKey: 'parent.nav.payments' },
  { path: 'support-tickets', labelKey: 'parent.nav.supportTickets' },
  { path: 'notifications', labelKey: 'parent.nav.notifications' },
  { path: 'notification-preferences', labelKey: 'parent.nav.notificationPreferences' },
  { path: 'admission-subscriptions', labelKey: 'parent.nav.admissionSubscriptions' },
] as const;

@Component({
  selector: 'se-parent-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoPipe, LanguageSwitcher],
  templateUrl: './parent-layout.html',
  styleUrl: './parent-layout.scss',
})
export class ParentLayout {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly navItems = PARENT_NAV;
  protected readonly mobileNavOpen = signal(false);
  protected readonly userMenuOpen = signal(false);
  protected readonly authUser = this.auth.currentUser;

  protected toggleMobileNav(): void {
    this.mobileNavOpen.update((open) => !open);
  }

  protected closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.update((open) => !open);
  }

  protected closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  protected logout(): void {
    this.auth
      .logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        void this.router.navigate(['/']);
      });
  }
}
