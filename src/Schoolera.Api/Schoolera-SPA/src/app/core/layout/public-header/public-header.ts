import {
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  HostListener,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { filter } from 'rxjs/operators';

import { AuthService } from '../../auth/auth.service';
import { LanguageSwitcher } from '../language-switcher/language-switcher';
import {
  accountAvatarInitials,
  accountDisplayName,
  buildPublicAccountMenuItems,
  PublicAccountMenuItem,
} from './public-header-account-menu';

interface NavItem {
  readonly labelKey: string;
  readonly route: string;
  readonly exact?: boolean;
}

@Component({
  selector: 'se-public-header',
  imports: [RouterLink, RouterLinkActive, TranslocoPipe, LanguageSwitcher],
  templateUrl: './public-header.html',
  styleUrl: './public-header.scss',
})
export class PublicHeader {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly menuPanel = viewChild<ElementRef<HTMLElement>>('menuPanel');
  private readonly menuButton = viewChild<ElementRef<HTMLButtonElement>>('menuButton');
  private readonly accountButton = viewChild<ElementRef<HTMLButtonElement>>('accountButton');
  private readonly accountPanel = viewChild<ElementRef<HTMLElement>>('accountPanel');

  readonly menuOpen = signal(false);
  readonly accountMenuOpen = signal(false);
  protected readonly menuId = 'public-mobile-nav';
  protected readonly accountMenuId = 'public-account-menu';

  protected readonly isSignedIn = this.auth.isSignedIn;
  protected readonly currentUser = this.auth.currentUser;

  protected readonly displayName = computed(() => accountDisplayName(this.currentUser()));
  protected readonly avatarInitials = computed(() => accountAvatarInitials(this.displayName()));
  protected readonly accountMenuItems = computed(() =>
    buildPublicAccountMenuItems(this.currentUser()),
  );

  protected readonly navItems: readonly NavItem[] = [
    { labelKey: 'nav.home', route: '/', exact: true },
    { labelKey: 'nav.searchSchools', route: '/schools' },
    { labelKey: 'nav.about', route: '/about' },
    { labelKey: 'nav.howItWorks', route: '/how-it-works' },
    { labelKey: 'nav.faq', route: '/faq' },
    { labelKey: 'nav.contact', route: '/contact' },
  ];

  protected readonly menuToggleLabelKey = computed(() =>
    this.menuOpen() ? 'nav.closeMenu' : 'nav.openMenu',
  );

  protected readonly accountToggleLabelKey = computed(() =>
    this.accountMenuOpen() ? 'nav.account.closeMenu' : 'nav.account.openMenu',
  );

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.closeMenu(false);
        this.closeAccountMenu(false);
      });

    effect(() => {
      const open = this.menuOpen();
      document.body.style.overflow = open ? 'hidden' : '';
    });

    this.destroyRef.onDestroy(() => {
      document.body.style.overflow = '';
    });
  }

  toggleMenu(): void {
    this.closeAccountMenu(false);
    this.menuOpen.update((open) => !open);
    if (this.menuOpen()) {
      queueMicrotask(() => this.menuPanel()?.nativeElement.querySelector<HTMLElement>('a')?.focus());
    }
  }

  closeMenu(restoreFocus = true): void {
    if (!this.menuOpen()) {
      return;
    }

    this.menuOpen.set(false);
    if (restoreFocus) {
      queueMicrotask(() => this.menuButton()?.nativeElement.focus());
    }
  }

  toggleAccountMenu(): void {
    this.closeMenu(false);
    const opening = !this.accountMenuOpen();
    this.accountMenuOpen.set(opening);
    if (opening) {
      queueMicrotask(() => {
        const panel = this.accountPanel()?.nativeElement;
        panel
          ?.querySelector<HTMLElement>('[role="menuitem"]')
          ?.focus();
      });
    }
  }

  closeAccountMenu(restoreFocus = true): void {
    if (!this.accountMenuOpen()) {
      return;
    }

    this.accountMenuOpen.set(false);
    if (restoreFocus) {
      queueMicrotask(() => this.accountButton()?.nativeElement.focus());
    }
  }

  protected onAccountItemActivate(item: PublicAccountMenuItem, event?: Event): void {
    event?.preventDefault();
    this.closeAccountMenu(false);
    this.closeMenu(false);

    if (item.action === 'logout') {
      this.logout();
      return;
    }

    if (item.route) {
      void this.router.navigateByUrl(item.route);
    }
  }

  protected logout(): void {
    this.auth
      .logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        void this.router.navigateByUrl('/');
      });
  }

  protected readonly mobileNavHidden = computed(() => !this.menuOpen());

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.accountMenuOpen()) {
      this.closeAccountMenu();
      return;
    }

    this.closeMenu();
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.accountMenuOpen()) {
      return;
    }

    const target = event.target as Node | null;
    if (target && this.host.nativeElement.contains(target)) {
      const accountRoot = this.host.nativeElement.querySelector('.public-header__account');
      if (accountRoot?.contains(target)) {
        return;
      }
    }

    this.closeAccountMenu(false);
  }

  @HostListener('document:keydown', ['$event'])
  protected onAccountMenuKeydown(event: KeyboardEvent): void {
    if (!this.accountMenuOpen() || event.key !== 'Tab') {
      return;
    }

    const panel = this.accountPanel()?.nativeElement;
    if (!panel) {
      return;
    }

    const items = Array.from(
      panel.querySelectorAll<HTMLElement>('[role="menuitem"]'),
    );
    if (items.length === 0) {
      return;
    }

    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement as HTMLElement | null;

    if (event.shiftKey && active === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }
}
