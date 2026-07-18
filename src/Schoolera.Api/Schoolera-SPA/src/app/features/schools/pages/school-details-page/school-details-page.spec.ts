import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Meta, Title } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TranslocoService, TranslocoTestingModule } from '@jsverse/transloco';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { signal } from '@angular/core';

import { GenderType, SchoolType } from '../../../../core/api-client/SwaggerClient.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { FeatureFlagsService } from '../../../../core/features/feature-flags.service';
import { SeoService } from '../../../../core/seo/seo.service';
import { SCHOOLERA_DEFAULT_LANG } from '../../../../core/i18n/schoolera-lang';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { SchoolsApi } from '../../data-access/schools.api';
import { SchoolDetailsPage } from './school-details-page';

const schoolsAr = {
  search: {
    admissionOpen: 'القبول مفتوح',
    locationFallback: 'الموقع غير متوفر',
    feeFrom: 'تبدأ من',
    viewSchool: 'عرض المدرسة',
  },
  enums: {
    schoolType: { private: 'خاصة', international: 'دولية', national: 'وطنية', language: 'لغات' },
    genderType: { boys: 'بنين', girls: 'بنات', mixed: 'مختلطة' },
  },
  profile: {
    breadcrumbFallback: 'تفاصيل المدرسة',
    loading: 'جاري تحميل ملف المدرسة',
    notFoundTitle: 'المدرسة غير موجودة',
    notFoundMessage: 'هذه المدرسة غير متاحة أو ربما تمت إزالتها.',
    errorTitle: 'تعذر تحميل المدرسة',
    errorMessage: 'حدث خطأ أثناء تحميل ملف هذه المدرسة. يرجى المحاولة مرة أخرى.',
    retry: 'حاول مرة أخرى',
    backToList: 'العودة إلى قائمة المدارس',
    contactCta: 'تواصل مع المدرسة',
    favorite: 'حفظ في المفضلة',
    mainBranch: 'الفرع الرئيسي',
    capacity: 'السعة: {{count}}',
    viewMap: 'عرض على الخريطة',
    meta: {
      title: '{{name}} | Schoolera',
      descriptionFallback: 'استكشف {{name}} على Schoolera — المنهج والرسوم والمرافق وبيانات التواصل.',
    },
    sections: {
      overview: 'نظرة عامة',
      keyFacts: 'حقائق أساسية',
      branches: 'الفروع',
      stagesGrades: 'المراحل والصفوف',
      curricula: 'المنهج',
      fees: 'الرسوم',
      facilities: 'المرافق',
      additionalServices: 'خدمات إضافية',
      gallery: 'معرض الصور',
      locationContact: 'الموقع والتواصل',
      relatedSchools: 'مدارس ذات صلة',
    },
    facts: {
      foundedYear: 'سنة التأسيس',
      studentCount: 'عدد الطلاب',
      schoolType: 'نوع المدرسة',
      genderType: 'النوع',
      admission: 'حالة القبول',
    },
    fees: {
      branch: 'الفرع',
      stage: 'المرحلة',
      grade: 'الصف',
      year: 'العام الدراسي',
      amount: 'الرسوم السنوية',
    },
    contactFields: {
      phone: 'الهاتف',
      email: 'البريد الإلكتروني',
      website: 'الموقع الإلكتروني',
      whatsApp: 'واتساب',
    },
    gallery: {
      openImage: 'فتح صورة المعرض {{index}}',
      lightboxLabel: 'معرض صور المدرسة',
      close: 'إغلاق المعرض',
      previous: 'الصورة السابقة',
      next: 'الصورة التالية',
    },
    contact: {
      title: 'تواصل مع المدرسة',
      close: 'إغلاق نموذج التواصل',
      cancel: 'إلغاء',
      submit: 'إرسال الرسالة',
      submitting: 'جاري الإرسال…',
      success: 'تم إرسال رسالتك. قد تتواصل معك المدرسة قريبًا.',
      consent: 'أوافق على أن تتواصل معي المدرسة بخصوص استفساري.',
      honeypotLabel: 'الموقع الإلكتروني',
      errorsTitle: 'يرجى تصحيح ما يلي',
      fields: {
        name: 'الاسم الكامل',
        phone: 'رقم الهاتف',
        email: 'البريد الإلكتروني (اختياري)',
        message: 'الرسالة (اختياري)',
      },
      errors: {
        generic: 'تعذر إرسال رسالتك. يرجى المحاولة مرة أخرى.',
        notFound: 'هذه المدرسة لم تعد متاحة.',
        consentRequired: 'يجب الموافقة قبل إرسال رسالتك.',
        invalidSource: 'تعذر إرسال رسالتك من هذه الصفحة.',
        rejected: 'تعذر إرسال رسالتك الآن. يرجى المحاولة لاحقًا.',
        validation: 'يرجى مراجعة النموذج والمحاولة مرة أخرى.',
      },
    },
  },
};

const commonAr = {
  home: 'الرئيسية',
};

const navAr = {
  searchSchools: 'ابحث عن مدرسة',
};

const validationAr = {
  required: 'هذا الحقل مطلوب',
};

const authAr = {
  validation: {
    invalidEmail: 'البريد الإلكتروني غير صالح',
  },
};

function mockProfile() {
  return {
    id: 'school-1',
    slug: 'school-1',
    name: 'مدرسة النور',
    shortDescription: 'وصف مختصر',
    fullDescription: 'وصف كامل',
    logoUrl: '/uploads/logo.png',
    coverUrl: '/uploads/cover.png',
    schoolType: SchoolType._1,
    genderType: GenderType._3,
    foundedYear: 2010,
    studentCount: 500,
    isAdmissionOpen: true,
    contact: { phone: '01000000000', email: 'info@school.test' },
    seo: { title: 'SEO Title', description: 'SEO Description' },
    branches: [
      {
        id: 'branch-1',
        slug: 'main',
        name: 'الفرع الرئيسي',
        city: 'القاهرة',
        district: 'مدينة نصر',
        addressLine: 'شارع 1',
        latitude: 30.05,
        longitude: 31.25,
        phone: '01000000000',
        email: 'branch@school.test',
        isMainBranch: true,
      },
    ],
    curricula: [{ id: 'curr-1', slug: 'american', name: 'American' }],
    facilities: [{ id: 'fac-1', slug: 'lab', name: 'Lab' }],
    images: [
      { id: 'img-1', imageUrl: '/uploads/1.jpg', caption: 'Caption 1', altText: 'Alt 1', sortOrder: 1 },
      { id: 'img-2', imageUrl: '/uploads/2.jpg', caption: 'Caption 2', altText: 'Alt 2', sortOrder: 2 },
    ],
    offerings: [
      {
        id: 'off-1',
        branchName: 'الفرع الرئيسي',
        stageName: 'Primary',
        genderType: GenderType._3,
        isAdmissionOpen: true,
        capacity: 120,
        grades: [{ id: 'g-1', slug: 'g1', name: 'Grade 1' }],
      },
    ],
    fees: [
      {
        branchName: 'الفرع الرئيسي',
        stageName: 'Primary',
        gradeName: 'Grade 1',
        academicYearName: '2025/2026',
        currencyCode: 'EGP',
        amount: 50000,
      },
    ],
    additionalServices: [{ id: 'svc-1', name: 'Transport', description: 'Bus service', sortOrder: 1 }],
  };
}

describe('SchoolDetailsPage', () => {
  let schoolsApi: {
    getBySlug: ReturnType<typeof vi.fn>;
    getRelated: ReturnType<typeof vi.fn>;
    getInterviewFaqs: ReturnType<typeof vi.fn>;
    submitContactLead: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let seo: { apply: ReturnType<typeof vi.fn>; clear: ReturnType<typeof vi.fn>; buildAbsoluteUrl: ReturnType<typeof vi.fn> };
  let paramMap$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  async function setup(options?: {
    slug?: string;
    getBySlugImpl?: () => ReturnType<SchoolsApi['getBySlug']>;
    relatedResult?: ReturnType<SchoolsApi['getRelated']>;
  }) {
    paramMap$ = new BehaviorSubject(convertToParamMap({ slug: options?.slug ?? 'school-1' }));

    schoolsApi = {
      getBySlug: vi.fn(
        options?.getBySlugImpl ??
          (() => of({ succeeded: true, data: mockProfile() })),
      ),
      getRelated: vi.fn(
        () =>
          options?.relatedResult ??
          of({
            succeeded: true,
            data: [{ id: 'school-2', slug: 'school-2', name: 'مدرسة الأمل', city: 'الجيزة' }],
            errors: [],
            errorCodes: [],
          }),
      ),
      getInterviewFaqs: vi.fn(() =>
        of({ succeeded: true, data: [], errors: [], errorCodes: [] }),
      ),
      submitContactLead: vi.fn(() =>
        of({
          succeeded: true,
          data: { leadId: 'lead-1', submittedAtUtc: '', message: 'ok' },
          errors: [],
          errorCodes: [],
        }),
      ),
    };

    toast = {
      success: vi.fn(),
      error: vi.fn(),
    };

    seo = {
      apply: vi.fn(),
      clear: vi.fn(),
      buildAbsoluteUrl: vi.fn((path: string) => path),
    };

    await TestBed.configureTestingModule({
      imports: [
        SchoolDetailsPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              schools: schoolsAr,
              common: commonAr,
              nav: navAr,
              validation: validationAr,
              auth: authAr,
            },
          },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: SCHOOLERA_DEFAULT_LANG,
            reRenderOnLangChange: true,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideRouter([{ path: 'schools/:slug', component: SchoolDetailsPage }]),
        provideHttpClient(),
        { provide: SchoolsApi, useValue: schoolsApi },
        {
          provide: FeatureFlagsService,
          useValue: { favoritesEnabled: false, admissionsEnabled: false },
        },
        { provide: ToastService, useValue: toast },
        { provide: SeoService, useValue: seo },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal(null),
            isSignedIn: signal(false),
            sessionReady: signal(true),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: paramMap$.asObservable(),
            snapshot: { paramMap: convertToParamMap({ slug: options?.slug ?? 'school-1' }) },
          },
        },
        Title,
        Meta,
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(SchoolDetailsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads the school profile from the route slug', async () => {
    const fixture = await setup();
    expect(schoolsApi.getBySlug).toHaveBeenCalledWith('school-1');
    expect(fixture.nativeElement.textContent).toContain('مدرسة النور');
    expect(fixture.nativeElement.textContent).toContain('نظرة عامة');
    expect(seo.apply).toHaveBeenCalled();
  });

  it('shows a not-found state for missing schools', async () => {
    const fixture = await setup({
      getBySlugImpl: () => of({ succeeded: false, errorCodes: ['school.not_found'], errors: ['missing'] }),
    });

    expect(fixture.nativeElement.textContent).toContain('المدرسة غير موجودة');
  });

  it('shows an error state and retries loading', async () => {
    let calls = 0;
    const fixture = await setup({
      getBySlugImpl: () => {
        calls += 1;
        if (calls === 1) {
          return throwError(() => new Error('network'));
        }
        return of({ succeeded: true, data: mockProfile() });
      },
    });

    expect(fixture.nativeElement.textContent).toContain('تعذر تحميل المدرسة');

    const retry = fixture.nativeElement.querySelector('.school-profile__status button') as HTMLButtonElement;
    retry.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('مدرسة النور');
    expect(schoolsApi.getBySlug).toHaveBeenCalledTimes(2);
  });

  it('validates the contact form before submit', async () => {
    const fixture = await setup();
    fixture.componentInstance.openContact();
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('.school-profile__contact-form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(schoolsApi.submitContactLead).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('هذا الحقل مطلوب');
  });

  it('submits a valid contact form and shows success toast', async () => {
    const fixture = await setup();
    fixture.componentInstance.openContact();
    fixture.componentInstance.contactForm.patchValue({
      name: 'Parent Name',
      phone: '01012345678',
      email: 'parent@test.com',
      message: 'Hello',
      consentAccepted: true,
    });
    fixture.detectChanges();

    fixture.componentInstance.submitContact();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(schoolsApi.submitContactLead).toHaveBeenCalledWith('school-1', expect.objectContaining({
      name: 'Parent Name',
      phone: '01012345678',
      source: 'school-profile',
      consentAccepted: true,
    }));
    expect(toast.success).toHaveBeenCalled();
  });

  it('hides the favorite button when the feature flag is disabled', async () => {
    const fixture = await setup();
    expect(fixture.nativeElement.textContent).not.toContain('حفظ في المفضلة');
  });

  it('does not render an apply/admissions button', async () => {
    const fixture = await setup();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button, a')) as HTMLElement[];
    const labels = buttons.map((element) => element.textContent?.trim() ?? '');
    expect(labels.some((label) => /apply|تقديم|قبول/i.test(label) && !/القبول مفتوح|حالة القبول/.test(label))).toBe(false);
  });

  it('supports gallery keyboard navigation and cleanup', async () => {
    const fixture = await setup();
    const page = fixture.componentInstance;

    page.openLightbox(0);
    fixture.detectChanges();
    expect(page.lightboxIndex()).toBe(0);

    page.onDocumentKeydown(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    expect(page.lightboxIndex()).toBe(1);

    page.onDocumentKeydown(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(page.lightboxIndex()).toBeNull();

    fixture.destroy();
    expect(seo.clear).toHaveBeenCalled();
  });
});
