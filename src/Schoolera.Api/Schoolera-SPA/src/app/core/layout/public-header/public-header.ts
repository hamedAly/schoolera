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

import { LanguageSwitcher } from '../language-switcher/language-switcher';

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
  private readonly destroyRef = inject(DestroyRef);
  private readonly menuPanel = viewChild<ElementRef<HTMLElement>>('menuPanel');
  private readonly menuButton = viewChild<ElementRef<HTMLButtonElement>>('menuButton');

  readonly menuOpen = signal(false);
  protected readonly menuId = 'public-mobile-nav';

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

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.closeMenu(false);
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

  protected readonly mobileNavHidden = computed(() => !this.menuOpen());

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closeMenu();
  }
}
