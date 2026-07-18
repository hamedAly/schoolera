import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { SCHOOLERA_DEFAULT_LANG } from '../../../../core/i18n/schoolera-lang';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolOnboardingApi } from '../../data-access/school-onboarding.api';
import { OnboardingWizardPage } from './onboarding-wizard-page';

describe('OnboardingWizardPage', () => {
  let onboardingApi: {
    getMyApplication: ReturnType<typeof vi.fn>;
    getDocumentTypes: ReturnType<typeof vi.fn>;
    saveOrganization: ReturnType<typeof vi.fn>;
    uploadDocumentWithProgress: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    onboardingApi = {
      getMyApplication: vi.fn(() => of({ succeeded: true, data: null })),
      getDocumentTypes: vi.fn(() =>
        of({
          succeeded: true,
          data: {
            documentTypes: [{ id: 'doc-1', code: 'REG', nameAr: 'سجل', isRequired: true }],
            maxFileSizeBytes: 1024,
            allowedExtensions: ['.pdf'],
          },
        }),
      ),
      saveOrganization: vi.fn(() =>
        of({
          succeeded: true,
          data: {
            id: 'app-1',
            status: 'Draft',
            currentStep: 'AuthorizedRepresentative',
            organizationNameAr: 'مؤسسة',
            organizationComplete: true,
            representativeComplete: false,
            schoolComplete: false,
            branchComplete: false,
            documentsComplete: false,
            documents: [],
            statusHistory: [],
            missingRequiredDocumentTypeIds: [],
          },
        }),
      ),
      uploadDocumentWithProgress: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [
        OnboardingWizardPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              onboarding: {
                common: { loading: 'تحميل' },
                wizard: {
                  title: 'تسجيل',
                  subtitle: 'وصف',
                  breadcrumb: 'تسجيل',
                  progressLabel: 'خطوات',
                },
                steps: {
                  organization: 'المنظمة',
                  representative: 'ممثل',
                  school: 'مدرسة',
                  branch: 'عنوان',
                  documents: 'مستندات',
                  review: 'مراجعة',
                },
                fields: {
                  organizationNameAr: 'اسم',
                  organizationNameEn: 'Name',
                  legalName: 'قانوني',
                  countryCode: 'دولة',
                  registrationNumber: 'سجل',
                  taxNumber: 'ضريبة',
                  legalForm: 'شكل',
                  organizationAddress: 'عنوان',
                  organizationWebsite: 'موقع',
                  selectCity: 'مدينة',
                  selectDistrict: 'حي',
                },
                enums: {
                  schoolType: { private: 'خاصة', international: 'دولية', national: 'قومية', language: 'لغات' },
                  genderType: { boys: 'بنين', girls: 'بنات', mixed: 'مختلط' },
                },
                documents: {
                  help: 'مساعدة',
                  required: 'مطلوب',
                  optional: 'اختياري',
                  none: 'لا يوجد',
                  upload: 'رفع',
                  replace: 'استبدال',
                  remove: 'إزالة',
                  download: 'تنزيل',
                  uploading: '{{percent}}',
                },
                review: {
                  intro: 'مراجعة',
                  missing: 'ناقص',
                  edit: 'تعديل',
                  documentsReady: 'جاهز',
                  documentsMissing: 'ناقص',
                  confirmation: 'أقر',
                  confirmRequired: 'مطلوب',
                },
                actions: {
                  back: 'سابق',
                  saveDraft: 'حفظ',
                  saveAndContinue: 'متابعة',
                  continue: 'متابعة',
                  submit: 'إرسال',
                  resubmit: 'إعادة',
                  viewStatus: 'حالة',
                },
                validation: { summaryTitle: 'تحقق', formIncomplete: 'غير مكتمل' },
                errors: { generic: 'خطأ', unsupportedDocumentFormat: 'صيغة', documentTooLarge: 'كبير', emptyDocument: 'فارغ' },
                status: { changesRequestedReason: 'سبب' },
              },
              common: { home: 'الرئيسية', bilingual: { arabicLabel: 'ع', englishLabel: 'E' } },
              auth: { portals: { school: { breadcrumb: 'مدرسة' } } },
            },
          },
          translocoConfig: {
            availableLangs: ['ar', 'en'],
            defaultLang: SCHOOLERA_DEFAULT_LANG,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        { provide: SchoolOnboardingApi, useValue: onboardingApi },
        {
          provide: TaxonomiesApi,
          useValue: {
            getCities: () => of({ succeeded: true, data: [] }),
            getDistrictsByCity: () => of({ succeeded: true, data: [] }),
          },
        },
        {
          provide: DocumentLanguageService,
          useValue: { activeLang: signal('ar') },
        },
      ],
    }).compileComponents();
  });

  it('renders the wizard and loads draft state', async () => {
    const fixture = TestBed.createComponent(OnboardingWizardPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(onboardingApi.getMyApplication).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('تسجيل');
    expect(fixture.nativeElement.textContent).toContain('المنظمة');
  });

  it('requires confirmation before submit on the review step', async () => {
    onboardingApi.getMyApplication.mockReturnValue(
      of({
        succeeded: true,
        data: {
          id: 'app-1',
          status: 'Draft',
          currentStep: 'Review',
          organizationComplete: true,
          representativeComplete: true,
          schoolComplete: true,
          branchComplete: true,
          documentsComplete: true,
          documents: [],
          statusHistory: [],
          missingRequiredDocumentTypeIds: [],
        },
      }),
    );

    const fixture = TestBed.createComponent(OnboardingWizardPage);
    const page = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    page['currentStep'].set('review');
    page['confirmAccurate'].set(false);
    page.submitApplication();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('مطلوب');
  });

  it('rejects unsupported upload extensions client-side', async () => {
    const fixture = TestBed.createComponent(OnboardingWizardPage);
    const page = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    const input = document.createElement('input');
    const file = new File(['x'], 'virus.exe', { type: 'application/octet-stream' });
    Object.defineProperty(input, 'files', { value: [file] });
    page.onFileSelected('doc-1', { target: input } as unknown as Event);
    fixture.detectChanges();

    expect(page['errorMessage']()).toContain('صيغة');
    expect(onboardingApi.uploadDocumentWithProgress).not.toHaveBeenCalled();
  });
});
