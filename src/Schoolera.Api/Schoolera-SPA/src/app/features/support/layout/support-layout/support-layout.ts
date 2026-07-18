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

interface SupportNavItem {
  readonly path: string;
  readonly labelKey: string;
}

const SUPPORT_NAV: readonly SupportNavItem[] = [
  { path: 'tickets', labelKey: 'support.nav.tickets' },
] as const;

@Component({
  selector: 'se-support-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoPipe, LanguageSwitcher],
  templateUrl: './support-layout.html',
  styleUrl: './support-layout.scss',
})
export class SupportLayout {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly navItems = SUPPORT_NAV;
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
