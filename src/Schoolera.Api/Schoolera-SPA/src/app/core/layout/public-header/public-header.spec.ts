import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../auth/auth.service';
import { AuthUser, SchooleraRoles } from '../../auth/auth.models';
import { PublicHeader } from './public-header';

const parentUser: AuthUser = {
  id: 'parent-1',
  displayName: 'نور أحمد',
  email: 'parent@example.invalid',
  phoneNumber: null,
  roles: [SchooleraRoles.Parent],
  accountStatus: 'Active',
  preferredLanguage: 'ar',
  postLoginDestination: '/parent',
};

const navI18n = {
  main: 'Main',
  mobile: 'Mobile',
  openMenu: 'Open',
  closeMenu: 'Close',
  home: 'Home',
  searchSchools: 'Schools',
  about: 'About',
  howItWorks: 'How',
  faq: 'FAQ',
  contact: 'Contact',
  login: 'Log in',
  register: 'Create account',
  searchCta: 'Search',
  account: {
    menu: 'Account menu',
    openMenu: 'Open account menu',
    closeMenu: 'Close account menu',
    dashboard: 'Dashboard',
    parentDashboard: 'Parent dashboard',
    profile: 'My profile',
    children: 'My children',
    applications: 'My applications',
    notifications: 'Notifications',
    schoolPortal: 'School portal',
    adminDashboard: 'Admin dashboard',
    support: 'Support',
    logout: 'Log out',
  },
};

describe('PublicHeader auth actions', () => {
  let fixture: ComponentFixture<PublicHeader>;
  let currentUser: ReturnType<typeof signal<AuthUser | null>>;
  let isSignedIn: ReturnType<typeof signal<boolean>>;
  let logout: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    currentUser = signal<AuthUser | null>(null);
    isSignedIn = signal(false);
    logout = vi.fn(() => of(undefined));

    await TestBed.configureTestingModule({
      imports: [
        PublicHeader,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              app: {
                name: 'Schoolera',
                brandMark: 'S',
                tagline: 'Tag',
                brandHomeAria: 'Home',
              },
              language: {
                label: 'Language',
                switchToEnglish: 'English',
                switchToArabic: 'Arabic',
              },
              nav: navI18n,
            },
            en: {
              app: {
                name: 'Schoolera',
                brandMark: 'S',
                tagline: 'Tag',
                brandHomeAria: 'Home',
              },
              language: {
                label: 'Language',
                switchToEnglish: 'English',
                switchToArabic: 'Arabic',
              },
              nav: navI18n,
            },
          },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: 'ar',
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideHttpClient(),
        provideRouter([
          { path: 'auth/login', children: [] },
          { path: 'auth/account-type', children: [] },
          { path: 'parent/dashboard', children: [] },
          { path: 'parent/profile', children: [] },
          { path: 'parent/children', children: [] },
          { path: 'parent/applications', children: [] },
          { path: 'parent/notifications', children: [] },
          { path: 'school', children: [] },
          { path: '**', children: [] },
        ]),
        {
          provide: AuthService,
          useValue: {
            currentUser,
            isSignedIn,
            logout,
            ensureSession: () => of(undefined),
            initSession: () => of(undefined),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublicHeader);
    fixture.detectChanges();
  });

  it('shows Login and Create account for anonymous users', () => {
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('a[href="/auth/login"]')).toBeTruthy();
    expect(root.querySelector('a[href="/auth/account-type"]')).toBeTruthy();
    expect(root.querySelector('.public-header__account')).toBeNull();
  });

  it('hides Login/Create account and shows Parent account menu when signed in', () => {
    currentUser.set(parentUser);
    isSignedIn.set(true);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.public-header__actions a[href="/auth/login"]')).toBeNull();
    expect(root.querySelector('.public-header__actions a[href="/auth/account-type"]')).toBeNull();
    expect(root.querySelector('.public-header__account')).toBeTruthy();
    expect(root.textContent).toContain('نور أحمد');

    const header = fixture.componentInstance;
    header.toggleAccountMenu();
    fixture.detectChanges();

    const hrefs = fixture.debugElement
      .queryAll(By.css('.public-header__account-menu a'))
      .map((el) => el.nativeElement.getAttribute('href'));
    expect(hrefs).toEqual([
      '/parent/dashboard',
      '/parent/profile',
      '/parent/children',
      '/parent/applications',
      '/parent/notifications',
    ]);
    expect(root.querySelector('.public-header__account-menu button[role="menuitem"]')).toBeTruthy();
  });

  it('logs out, closes the menu, and navigates home', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    currentUser.set(parentUser);
    isSignedIn.set(true);
    fixture.detectChanges();

    const header = fixture.componentInstance;
    header.toggleAccountMenu();
    fixture.detectChanges();
    expect(header.accountMenuOpen()).toBe(true);

    header['onAccountItemActivate']({
      id: 'logout',
      labelKey: 'nav.account.logout',
      action: 'logout',
    });
    fixture.detectChanges();

    expect(logout).toHaveBeenCalled();
    expect(header.accountMenuOpen()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith('/');
  });

  it('uses school portal destination for school owners', () => {
    currentUser.set({
      ...parentUser,
      displayName: 'School Owner',
      roles: [SchooleraRoles.SchoolOwner],
      postLoginDestination: '/school',
    });
    isSignedIn.set(true);
    fixture.detectChanges();

    const header = fixture.componentInstance;
    header.toggleAccountMenu();
    fixture.detectChanges();

    const hrefs = fixture.debugElement
      .queryAll(By.css('.public-header__account-menu a'))
      .map((el) => el.nativeElement.getAttribute('href'));
    expect(hrefs).toEqual(['/school']);
  });
});
