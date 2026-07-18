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

interface AdminNavItem {
  readonly path: string;
  readonly labelKey: string;
}

const ADMIN_NAV: readonly AdminNavItem[] = [
  { path: 'dashboard', labelKey: 'admin.nav.dashboard' },
  { path: 'onboarding', labelKey: 'admin.nav.onboarding' },
  { path: 'applications', labelKey: 'admin.nav.applications' },
  { path: 'schools', labelKey: 'admin.nav.schools' },
  { path: 'users', labelKey: 'admin.nav.users' },
  { path: 'taxonomies', labelKey: 'admin.nav.taxonomies' },
  { path: 'cms/pages', labelKey: 'admin.nav.content' },
  { path: 'cms/faq', labelKey: 'admin.nav.faq' },
  { path: 'cms/home', labelKey: 'admin.nav.homepage' },
  { path: 'contact-requests', labelKey: 'admin.nav.contactRequests' },
  { path: 'support-tickets', labelKey: 'admin.nav.supportTickets' },
  { path: 'integrations', labelKey: 'admin.nav.integrations' },
  { path: 'notification-templates', labelKey: 'admin.nav.notificationTemplates' },
  { path: 'notifications-ops', labelKey: 'admin.nav.notificationsOps' },
  { path: 'audit', labelKey: 'admin.nav.audit' },
] as const;

@Component({
  selector: 'se-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoPipe, LanguageSwitcher],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
})
export class AdminLayout {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly navItems = ADMIN_NAV;
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
