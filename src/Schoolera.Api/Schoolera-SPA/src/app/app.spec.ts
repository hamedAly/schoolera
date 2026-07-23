import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, TitleStrategy } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';

import { App } from './app';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { DocumentLanguageService } from './core/i18n/document-language.service';
import { SCHOOLERA_DEFAULT_LANG } from './core/i18n/schoolera-lang';
import { LanguageSwitcher } from './core/layout/language-switcher/language-switcher';
import { PublicFooter } from './core/layout/public-footer/public-footer';
import { PublicHeader } from './core/layout/public-header/public-header';
import { PublicLayout } from './core/layout/public-layout/public-layout';
import { TranslocoTitleStrategy } from './core/i18n/transloco-title.strategy';
import { API_BASE_URL } from './core/api-client/SwaggerClient.service';

const ar = {
  app: {
    name: 'Schoolera',
    brandMark: 'س',
    tagline: 'مسارك إلى المدرسة المناسبة',
    skipToContent: 'تخطَّ إلى المحتوى',
    brandHomeAria: 'Schoolera — الصفحة الرئيسية',
  },
  language: {
    label: 'اللغة',
    switchToEnglish: 'English',
    switchToArabic: 'العربية',
  },
  nav: {
    main: 'التنقل الرئيسي',
    mobile: 'التنقل للجوال',
    openMenu: 'فتح قائمة التنقل',
    closeMenu: 'إغلاق قائمة التنقل',
    home: 'الرئيسية',
    searchSchools: 'البحث عن مدرسة',
    about: 'عن Schoolera',
    howItWorks: 'كيف تعمل المنصة',
    faq: 'الأسئلة الشائعة',
    contact: 'تواصل معنا',
    login: 'تسجيل الدخول',
    register: 'إنشاء حساب',
    searchCta: 'ابحث عن مدرسة',
    account: {
      menu: 'قائمة الحساب',
      openMenu: 'فتح قائمة الحساب',
      closeMenu: 'إغلاق قائمة الحساب',
      dashboard: 'لوحة التحكم',
      parentDashboard: 'لوحة ولي الأمر',
      profile: 'ملفي الشخصي',
      children: 'أبنائي',
      applications: 'طلباتي',
      notifications: 'الإشعارات',
      schoolPortal: 'بوابة المدرسة',
      adminDashboard: 'لوحة الإدارة',
      support: 'الدعم',
      logout: 'تسجيل الخروج',
    },
  },
  footer: {
    brandTitle: 'Schoolera',
    brandDescription: 'وصف',
    socialLabel: 'وسائل التواصل',
    socialX: 'X',
    socialLinkedIn: 'in',
    socialInstagram: 'IG',
    comingSoon: 'قريبًا',
    parents: 'لأولياء الأمور',
    schools: 'للمدارس',
    importantLinks: 'روابط مهمة',
    support: 'الدعم',
    joinSchools: 'انضمام',
    privacy: 'خصوصية',
    terms: 'شروط',
    helpCenter: 'مساعدة',
    copyright: '© {{year}} Schoolera',
  },
  home: {
    meta: { title: 'Schoolera', description: 'وصف' },
    hero: {
      eyebrow: 'منصة',
      title: 'Schoolera',
      subtitle: 'وصف',
      primaryCta: 'ابحث عن مدرسة',
      secondaryCta: 'إنشاء حساب',
      schoolOwnerCta: 'للمنشآت',
      highlightDiscover: 'اكتشاف',
      highlightCompare: 'معلومات',
      highlightConnect: 'تواصل',
      visualAlt: 'رسم',
    },
    trust: {
      title: 'لماذا',
      subtitle: 'ثقة',
      item1Title: 'ا1',
      item1Body: 'ب',
      item2Title: 'ا2',
      item2Body: 'ب',
      item3Title: 'ا3',
      item3Body: 'ب',
      item4Title: 'ا4',
      item4Body: 'ب',
    },
    parents: {
      title: 'أهل',
      subtitle: 'وصف',
      card1Title: 'ك',
      card1Body: 'ب',
      card2Title: 'ك',
      card2Body: 'ب',
      card3Title: 'ك',
      card3Body: 'ب',
      card4Title: 'ك',
      card4Body: 'ب',
    },
    schools: {
      title: 'مدارس',
      subtitle: 'وصف',
      card1Title: 'ك',
      card1Body: 'ب',
      card2Title: 'ك',
      card2Body: 'ب',
      card3Title: 'ك',
      card3Body: 'ب',
      cta: 'انضم',
    },
    journey: {
      title: 'رحلة',
      subtitle: 'وصف',
      parentTab: 'ولي أمر',
      schoolTab: 'منشأة',
      parentStep1Title: 'خطوة',
      parentStep1Body: 'ب',
      parentStep2Title: 'خطوة',
      parentStep2Body: 'ب',
      parentStep3Title: 'خطوة',
      parentStep3Body: 'ب',
      parentStep4Title: 'خطوة',
      parentStep4Body: 'ب',
      schoolStep1Title: 'خطوة',
      schoolStep1Body: 'ب',
      schoolStep2Title: 'خطوة',
      schoolStep2Body: 'ب',
      schoolStep3Title: 'خطوة',
      schoolStep3Body: 'ب',
      schoolStep4Title: 'خطوة',
      schoolStep4Body: 'ب',
    },
    featured: {
      title: 'معاينة',
      subtitle: 'وصف',
      viewAll: 'الكل',
      loadingTitle: 'تحميل',
      loadingMessage: 'تحميل',
      emptyTitle: 'فارغ',
      emptyMessage: 'فارغ',
      errorTitle: 'خطأ',
      errorMessage: 'خطأ',
      retry: 'إعادة',
      cityFallback: 'مدينة',
      openSchool: 'فتح',
      openSchoolNamed: 'فتح: {{name}}',
    },
    highlights: {
      title: 'اليوم',
      subtitle: 'وصف',
      item1Title: 'ك',
      item1Body: 'ب',
      item2Title: 'ك',
      item2Body: 'ب',
      item3Title: 'ك',
      item3Body: 'ب',
    },
    faq: {
      title: 'أسئلة',
      subtitle: 'وصف',
      viewAll: 'الكل',
      q1: 'س1',
      a1: 'ج1',
      q2: 'س2',
      a2: 'ج2',
      q3: 'س3',
      a3: 'ج3',
      q4: 'س4',
      a4: 'ج4',
      q5: 'س5',
      a5: 'ج5',
    },
    finalCta: {
      title: 'ابدأ',
      subtitle: 'وصف',
      search: 'ابحث',
      account: 'حساب',
      institution: 'منشأة',
    },
  },
  titles: {
    home: 'الرئيسية',
    notFound: 'غير موجود',
    about: 'عن',
    howItWorks: 'كيف',
    faq: 'أسئلة',
    contact: 'تواصل',
    login: 'دخول',
  },
  notFound: {
    title: 'الصفحة غير موجودة',
    body: 'غير موجودة',
    homeCta: 'الرئيسية',
    schoolsCta: 'المدارس',
  },
  pages: {
    placeholderNote: 'ملاحظة',
    backHome: 'العودة',
    about: { title: 'عن', description: 'وصف' },
    howItWorks: { title: 'كيف', description: 'وصف', startSearch: 'ابدأ' },
    faq: { title: 'أسئلة', description: 'وصف' },
    contact: { title: 'تواصل', description: 'وصف' },
  },
  common: { home: 'الرئيسية' },
  ops: {
    navLabel: 'عمليات',
    brandSubtitle: 'عمليات',
    dashboard: 'لوحة',
    schools: 'مدارس',
    students: 'طلاب',
    staff: 'موظفون',
    classes: 'فصول',
  },
};

const en = {
  ...ar,
  app: {
    ...ar.app,
    brandMark: 'S',
    tagline: 'Your path to the right school',
    skipToContent: 'Skip to content',
    brandHomeAria: 'Schoolera — Home',
  },
  language: {
    label: 'Language',
    switchToEnglish: 'English',
    switchToArabic: 'العربية',
  },
  nav: {
    ...ar.nav,
    home: 'Home',
    searchSchools: 'Find a school',
    about: 'About Schoolera',
    howItWorks: 'How it works',
    faq: 'FAQ',
    contact: 'Contact us',
    login: 'Log in',
    register: 'Create account',
    searchCta: 'Search schools',
    main: 'Main navigation',
    mobile: 'Mobile navigation',
    openMenu: 'Open menu',
    closeMenu: 'Close menu',
  },
  notFound: {
    title: 'Page not found',
    body: 'Missing',
    homeCta: 'Home',
    schoolsCta: 'Find a school',
  },
};

describe('App public shell localization', () => {
  beforeEach(async () => {
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      configurable: true,
      value: (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });

    await TestBed.configureTestingModule({
      imports: [
        App,
        TranslocoTestingModule.forRoot({
          langs: { ar, en },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: SCHOOLERA_DEFAULT_LANG,
            reRenderOnLangChange: true,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        { provide: API_BASE_URL, useValue: '' },
        {
          provide: AuthService,
          useValue: {
            initSession: () => of(undefined),
            ensureSession: () => of(undefined),
            currentUser: signal(null),
            isSignedIn: signal(false),
            sessionReady: signal(true),
          },
        },
        { provide: TitleStrategy, useClass: TranslocoTitleStrategy },
        DocumentLanguageService,
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render public header, main, and footer on the home route', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('se-public-header')).toBeTruthy();
    expect(compiled.querySelector('main#main-content')).toBeTruthy();
    expect(compiled.querySelector('se-public-footer')).toBeTruthy();
    expect(compiled.textContent).toContain('Schoolera');
  });

  it('should expose localized header navigation links', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('الرئيسية');
    expect(compiled.textContent).toContain('البحث عن مدرسة');
    expect(compiled.textContent).toContain('عن Schoolera');
  });

  it('should default the document language to Arabic RTL', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('should switch document direction when English is selected', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();

    const switcher = fixture.debugElement.query(By.directive(LanguageSwitcher))
      .componentInstance as LanguageSwitcher;
    switcher.switchLanguage();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(document.documentElement.lang).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
  });

  it('should open and close the mobile menu, including Escape', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();

    const header = fixture.debugElement.query(By.directive(PublicHeader)).componentInstance as PublicHeader;
    expect(header.menuOpen()).toBe(false);

    header.toggleMenu();
    fixture.detectChanges();
    expect(header.menuOpen()).toBe(true);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(header.menuOpen()).toBe(false);
  });

  it('should close the mobile menu after a language change from the drawer switcher', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();

    const header = fixture.debugElement.query(By.directive(PublicHeader)).componentInstance as PublicHeader;
    header.toggleMenu();
    fixture.detectChanges();
    expect(header.menuOpen()).toBe(true);

    const switchers = fixture.debugElement.queryAll(By.directive(LanguageSwitcher));
    expect(switchers.length).toBeGreaterThan(1);
    (switchers[1].componentInstance as LanguageSwitcher).switchLanguage();
    fixture.detectChanges();
    expect(header.menuOpen()).toBe(false);
  });

  it('should render the Angular 404 page for unknown frontend routes', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/does-not-exist-route');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('الصفحة غير موجودة');
  });

  it('should load public placeholder routes', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);

    for (const path of ['/about', '/how-it-works', '/faq', '/contact', '/auth/login']) {
      await router.navigateByUrl(path);
      fixture.detectChanges();
      await fixture.whenStable();
      expect(fixture.debugElement.query(By.directive(PublicLayout))).toBeTruthy();
    }
  });

  it('should not use href="#" in the public footer', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    fixture.detectChanges();
    await fixture.whenStable();

    const footer = fixture.debugElement.query(By.directive(PublicFooter));
    const anchors = footer.nativeElement.querySelectorAll('a') as NodeListOf<HTMLAnchorElement>;
    for (const anchor of Array.from(anchors)) {
      expect(anchor.getAttribute('href')).not.toBe('#');
    }
  });
});
