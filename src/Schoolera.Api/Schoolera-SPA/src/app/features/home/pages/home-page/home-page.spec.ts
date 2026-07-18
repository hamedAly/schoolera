import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslocoService, TranslocoTestingModule } from '@jsverse/transloco';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { SCHOOLERA_DEFAULT_LANG } from '../../../../core/i18n/schoolera-lang';
import { SeoService } from '../../../../core/seo/seo.service';
import { PublicContentApi } from '../../../public/data-access/public-content.api';
import { SchoolsApi } from '../../../schools/data-access/schools.api';
import { HomePage } from './home-page';

const homeAr = {
  meta: {
    title: 'Schoolera | ابحث عن المدرسة المناسبة لطفلك',
    description: 'وصف عربي',
  },
  hero: {
    eyebrow: 'منصة عائلات ومدارس موثوقة',
    title: 'اختر المدرسة المناسبة بثقة ووضوح',
    subtitle: 'وصف البطل',
    primaryCta: 'ابحث عن مدرسة الآن',
    secondaryCta: 'استكشف المدارس',
    trust1: 'معلومات موثوقة',
    trust2: 'تواصل مباشر',
    trust3: 'مقارنة شفافة',
    visualAlt: 'رسم',
  },
  trust: {
    eyebrow: 'لماذا نحن',
    title: 'لماذا Schoolera؟',
    subtitle: 'ثقة',
    item1Title: 'اكتشف',
    item1Body: 'ب1',
    item2Title: 'قارن',
    item2Body: 'ب2',
    item3Title: 'معلومات',
    item3Body: 'ب3',
    item4Title: 'تواصل',
    item4Body: 'ب4',
  },
  parents: {
    title: 'مزايا لأولياء الأمور',
    subtitle: 'أهل',
    imageAlt: 'عائلة',
    benefit1Title: 'ك1',
    benefit1Body: 'ب',
    benefit2Title: 'ك2',
    benefit2Body: 'ب',
    benefit3Title: 'ك3',
    benefit3Body: 'ب',
  },
  schools: {
    eyebrow: 'للمنشآت',
    title: 'مزايا للمنشآت التعليمية',
    subtitle: 'مدارس',
    card1Title: 'ك1',
    card1Body: 'ب',
    card2Title: 'ك2',
    card2Body: 'ب',
    card3Title: 'ك3',
    card3Body: 'ب',
    cta: 'انضم كمنشأة تعليمية',
  },
  journey: {
    title: 'كيف تعمل Schoolera؟',
    subtitle: 'رحلة',
    parentTab: 'ولي أمر',
    schoolTab: 'منشأة تعليمية',
    parentStep1Title: 'ابحث عن المدارس',
    parentStep1Body: 'ب',
    parentStep2Title: 'راجع',
    parentStep2Body: 'ب',
    parentStep3Title: 'أنشئ حسابًا',
    parentStep3Body: 'ب',
    parentStep4Title: 'تابع',
    parentStep4Body: 'ب',
    schoolStep1Title: 'أنشئ حساب منشأة',
    schoolStep1Body: 'ب',
    schoolStep2Title: 'قدّم',
    schoolStep2Body: 'ب',
    schoolStep3Title: 'طوّر',
    schoolStep3Body: 'ب',
    schoolStep4Title: 'استقبل',
    schoolStep4Body: 'ب',
  },
  featured: {
    title: 'مدارس على المنصة',
    subtitle: 'معاينة',
    viewAll: 'عرض كل المدارس',
    loadingTitle: 'جاري تحميل المدارس',
    loadingMessage: 'تحميل',
    emptyTitle: 'لا توجد مدارس للعرض بعد',
    emptyMessage: 'فارغ',
    errorTitle: 'تعذر تحميل المعاينة',
    errorMessage: 'خطأ',
    retry: 'إعادة المحاولة',
    cityFallback: 'المدينة غير محددة',
    openSchool: 'فتح صفحة المدرسة',
    openSchoolNamed: 'فتح صفحة المدرسة: {{name}}',
  },
  highlights: {
    title: 'ما نقدّمه بوضوح اليوم',
    subtitle: 'بلا أرقام',
    item1Title: 'بحث',
    item1Body: 'ب',
    item2Title: 'واجهة',
    item2Body: 'ب',
    item3Title: 'مسار',
    item3Body: 'ب',
  },
  faq: {
    title: 'أسئلة شائعة',
    subtitle: 'أسئلة',
    viewAll: 'عرض كل الأسئلة',
    q1: 'هل يمكنني البحث عن مدرسة الآن؟',
    a1: 'نعم يمكن البحث.',
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
    title: 'ابدأ من خطوة واضحة اليوم',
    subtitle: 'ختام',
    search: 'ابحث عن مدرسة',
    account: 'إنشاء حساب',
  },
};

const homeEn = {
  ...homeAr,
  meta: {
    title: 'Schoolera | Find the right school for your child',
    description: 'English description',
  },
  hero: {
    ...homeAr.hero,
    eyebrow: 'A trusted platform for families and schools',
    title: 'Choose the right school with clarity and confidence',
    subtitle: 'Hero subtitle',
    primaryCta: 'Find a School Now',
    secondaryCta: 'Explore Schools',
  },
  trust: { ...homeAr.trust, title: 'Why Schoolera?' },
  parents: { ...homeAr.parents, title: 'Benefits for parents' },
  schools: { ...homeAr.schools, title: 'Benefits for educational institutions', cta: 'Join as an educational institution' },
  journey: {
    ...homeAr.journey,
    title: 'How Schoolera works',
    parentTab: 'Parent',
    schoolTab: 'Educational institution',
    parentStep1Title: 'Search for schools',
    schoolStep1Title: 'Create an institution account',
  },
  featured: {
    ...homeAr.featured,
    title: 'Schools on the platform',
    viewAll: 'View all schools',
    loadingTitle: 'Loading schools',
    emptyTitle: 'No schools to preview yet',
    errorTitle: 'Unable to load the preview',
    retry: 'Retry',
    openSchool: 'Open school page',
    openSchoolNamed: 'Open school page: {{name}}',
  },
  highlights: { ...homeAr.highlights, title: 'What we offer clearly today' },
  faq: {
    ...homeAr.faq,
    title: 'Frequently asked questions',
    viewAll: 'View all FAQs',
    q1: 'Can I search for a school now?',
    a1: 'Yes you can search.',
  },
  finalCta: {
    title: 'Start with one clear step today',
    subtitle: 'Final',
    search: 'Search schools',
    account: 'Create account',
  },
};

function successResult(
  schools: Array<{ id: string; slug: string; name: string; city?: string }>,
) {
  return {
    succeeded: true,
    data: schools,
    totalCount: schools.length,
    pageNumber: 1,
    pageSize: schools.length || 10,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
    errors: [],
  };
}

describe('HomePage', () => {
  let schoolsApi: { getFeaturedSchools: ReturnType<typeof vi.fn> };

  async function setup(resultFactory: () => ReturnType<SchoolsApi['getFeaturedSchools']>) {
    schoolsApi = {
      getFeaturedSchools: vi.fn(resultFactory),
    };

    await TestBed.configureTestingModule({
      imports: [
        HomePage,
        TranslocoTestingModule.forRoot({
          langs: { ar: { home: homeAr }, en: { home: homeEn } },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: SCHOOLERA_DEFAULT_LANG,
            reRenderOnLangChange: true,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideRouter([{ path: '**', component: HomePage }]),
        provideHttpClient(),
        { provide: SchoolsApi, useValue: schoolsApi },
        {
          provide: PublicContentApi,
          useValue: {
            getHome: vi.fn(() => of({ succeeded: false, data: null })),
          },
        },
        {
          provide: SeoService,
          useValue: { apply: vi.fn(), clear: vi.fn() },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the main homepage sections with one hero h1', async () => {
    const fixture = await setup(() => of(successResult([])));
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelectorAll('h1').length).toBe(1);
    expect(compiled.querySelector('#home-hero-title')?.textContent).toContain('اختر المدرسة المناسبة بثقة ووضوح');
    expect(compiled.textContent).toContain('لماذا Schoolera؟');
    expect(compiled.textContent).toContain('مزايا لأولياء الأمور');
    expect(compiled.textContent).toContain('مزايا للمنشآت التعليمية');
    expect(compiled.textContent).toContain('كيف تعمل Schoolera؟');
    expect(compiled.textContent).toContain('مدارس على المنصة');
    expect(compiled.textContent).toContain('ما نقدّمه بوضوح اليوم');
    expect(compiled.textContent).toContain('أسئلة شائعة');
    expect(compiled.textContent).toContain('ابدأ من خطوة واضحة اليوم');
  });

  it('links primary and secondary hero CTAs to the expected routes', async () => {
    const fixture = await setup(() => of(successResult([])));
    const compiled = fixture.nativeElement as HTMLElement;
    const links = Array.from(compiled.querySelectorAll('.home-hero__actions a')) as HTMLAnchorElement[];

    expect(links[0]?.getAttribute('href')).toBe('/schools');
    expect(links[1]?.getAttribute('href')).toBe('/schools');
  });

  it('renders English content after switching language', async () => {
    const fixture = await setup(() => of(successResult([])));
    const transloco = TestBed.inject(TranslocoService);

    transloco.setActiveLang('en');
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Choose the right school with clarity and confidence');
    expect(compiled.textContent).toContain('Why Schoolera?');
    expect(compiled.textContent).toContain('Search schools');
  });

  it('switches the parent/school journey with keyboard arrows', async () => {
    const fixture = await setup(() => of(successResult([])));
    const page = fixture.componentInstance;
    const tablist = fixture.debugElement.query(By.css('[role="tablist"]'));

    expect(page.journeyTab()).toBe('parent');
    expect(fixture.nativeElement.textContent).toContain('ابحث عن المدارس');

    tablist.triggerEventHandler('keydown', new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    fixture.detectChanges();

    expect(page.journeyTab()).toBe('school');
    expect(fixture.nativeElement.textContent).toContain('أنشئ حساب منشأة');
  });

  it('opens and closes the FAQ accordion', async () => {
    const fixture = await setup(() => of(successResult([])));
    const compiled = fixture.nativeElement as HTMLElement;
    const trigger = compiled.querySelector('#faq-1-button') as HTMLButtonElement;
    const panel = compiled.querySelector('#faq-1-panel') as HTMLElement;

    expect(trigger.getAttribute('aria-expanded')).toBe('false');
    expect(panel.hasAttribute('hidden')).toBe(true);

    trigger.click();
    fixture.detectChanges();
    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    expect(panel.hasAttribute('hidden')).toBe(false);
    expect(panel.textContent).toContain('نعم يمكن البحث.');

    trigger.click();
    fixture.detectChanges();
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
  });

  it('shows the featured schools empty state', async () => {
    const fixture = await setup(() => of(successResult([])));
    expect(fixture.nativeElement.textContent).toContain('لا توجد مدارس للعرض بعد');
  });

  it('shows featured schools when the API returns data', async () => {
    const fixture = await setup(() =>
      of(
        successResult([
          { id: 'school-1', slug: 'school-1', name: 'مدرسة النور', city: 'الرياض' },
          { id: 'school-2', slug: 'school-2', name: 'مدرسة الأمل', city: 'جدة' },
        ]),
      ),
    );

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('مدرسة النور');
    expect(compiled.textContent).toContain('الرياض');
    const schoolLink = compiled.querySelector('a[href="/schools/school-1"]');
    expect(schoolLink).toBeTruthy();
  });

  it('shows featured error state and retries through SchoolsApi', async () => {
    let calls = 0;
    const fixture = await setup(() => {
      calls += 1;
      if (calls === 1) {
        return throwError(() => new Error('network'));
      }
      return of(successResult([{ id: 'school-9', slug: 'school-9', name: 'مدرسة بعد إعادة المحاولة', city: 'الدمام' }]));
    });

    expect(fixture.nativeElement.textContent).toContain('تعذر تحميل المعاينة');
    expect(schoolsApi.getFeaturedSchools).toHaveBeenCalledTimes(1);

    const retry = fixture.nativeElement.querySelector('.home-featured__status button') as HTMLButtonElement;
    retry.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(schoolsApi.getFeaturedSchools).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('مدرسة بعد إعادة المحاولة');
  });

  it('defaults to Arabic and avoids hardcoded English marketing strings in the rendered default view', async () => {
    const fixture = await setup(() => of(successResult([])));
    expect(fixture.nativeElement.textContent).not.toContain('Choose the right school with clarity and confidence');
    expect(fixture.nativeElement.textContent).toContain('اختر المدرسة المناسبة بثقة ووضوح');
  });

  it('does not use href="#"', async () => {
    const fixture = await setup(() => of(successResult([])));
    const anchors = fixture.nativeElement.querySelectorAll('a') as NodeListOf<HTMLAnchorElement>;
    for (const anchor of Array.from(anchors)) {
      expect(anchor.getAttribute('href')).not.toBe('#');
    }
  });
});
