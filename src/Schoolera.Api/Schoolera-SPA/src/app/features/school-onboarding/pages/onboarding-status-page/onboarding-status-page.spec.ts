import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SCHOOLERA_DEFAULT_LANG } from '../../../../core/i18n/schoolera-lang';
import { SchoolOnboardingApi } from '../../data-access/school-onboarding.api';
import { OnboardingStatusPage } from './onboarding-status-page';

describe('OnboardingStatusPage', () => {
  it('renders ChangesRequested owner-visible reason and edit CTA', async () => {
    await TestBed.configureTestingModule({
      imports: [
        OnboardingStatusPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              onboarding: {
                common: { loading: 'تحميل' },
                status: {
                  title: 'حالة',
                  subtitle: 'وصف',
                  breadcrumb: 'حالة',
                  changesRequestedTitle: 'مطلوب تعديلات',
                  changesRequestedBody: 'حدّث الطلب',
                  changesRequestedReason: 'ملاحظات:',
                  editAndResubmitCta: 'تعديل وإعادة الإرسال',
                },
              },
              common: { home: 'الرئيسية' },
              auth: { portals: { school: { breadcrumb: 'مدرسة' } } },
            },
          },
          translocoConfig: {
            availableLangs: ['ar'],
            defaultLang: SCHOOLERA_DEFAULT_LANG,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: SchoolOnboardingApi,
          useValue: {
            getMyApplication: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'app-1',
                  status: 'ChangesRequested',
                  currentStep: 'Documents',
                  ownerVisibleReason: 'أكمل الترخيص',
                  documents: [],
                  statusHistory: [],
                  missingRequiredDocumentTypeIds: [],
                  organizationComplete: true,
                  representativeComplete: true,
                  schoolComplete: true,
                  branchComplete: true,
                  documentsComplete: false,
                },
              }),
            ),
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(OnboardingStatusPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('مطلوب تعديلات');
    expect(fixture.nativeElement.textContent).toContain('أكمل الترخيص');
    expect(fixture.nativeElement.textContent).toContain('تعديل وإعادة الإرسال');
  });

  it('renders Approved state with unpublished note', async () => {
    await TestBed.configureTestingModule({
      imports: [
        OnboardingStatusPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              onboarding: {
                common: { loading: 'تحميل' },
                status: {
                  title: 'حالة',
                  subtitle: 'وصف',
                  breadcrumb: 'حالة',
                  approvedTitle: 'تمت الموافقة',
                  approvedBody: 'موافق',
                  approvedSchool: 'المدرسة: {{name}}',
                  approvedNotPublicNote: 'غير منشورة',
                  goToPortalCta: 'البوابة',
                },
              },
              common: { home: 'الرئيسية' },
              auth: { portals: { school: { breadcrumb: 'مدرسة' } } },
            },
          },
          translocoConfig: {
            availableLangs: ['ar'],
            defaultLang: SCHOOLERA_DEFAULT_LANG,
          },
          preloadLangs: true,
        }),
      ],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: SchoolOnboardingApi,
          useValue: {
            getMyApplication: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'app-1',
                  status: 'Approved',
                  currentStep: 'Review',
                  approvedSchoolName: 'مدرسة النور',
                  documents: [],
                  statusHistory: [],
                  missingRequiredDocumentTypeIds: [],
                  organizationComplete: true,
                  representativeComplete: true,
                  schoolComplete: true,
                  branchComplete: true,
                  documentsComplete: true,
                },
              }),
            ),
          },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(OnboardingStatusPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('تمت الموافقة');
    expect(fixture.nativeElement.textContent).toContain('مدرسة النور');
    expect(fixture.nativeElement.textContent).toContain('غير منشورة');
  });
});
