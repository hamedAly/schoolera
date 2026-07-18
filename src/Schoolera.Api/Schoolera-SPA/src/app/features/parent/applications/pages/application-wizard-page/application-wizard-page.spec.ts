import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ParentApi } from '../../../data-access/parent.api';
import { SchoolsApi } from '../../../../schools/data-access/schools.api';
import { TaxonomiesApi } from '../../../../taxonomies/data-access/taxonomies.api';
import { ApplicationWizardPage } from './application-wizard-page';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';

describe('ApplicationWizardPage', () => {
  let createSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    createSpy = vi.fn(() =>
      of({
        succeeded: true,
        data: { id: 'app-1', applicationNumber: 'APP-2026-000001', capabilities: { canEdit: true } },
      }),
    );

    await TestBed.configureTestingModule({
      imports: [
        ApplicationWizardPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                applications: {
                  wizard: {
                    titleNew: 'جديد',
                    subtitle: 'وصف',
                    stepperLabel: 'خطوات',
                    currentStepAnnounce: '{{step}}/{{total}} {{label}}',
                    errorSummaryTitle: 'أخطاء',
                    next: 'التالي',
                    back: 'رجوع',
                    createDraft: 'حفظ',
                    creatingDraft: 'جارٍ',
                    selectChild: 'اختر',
                    addChild: 'إضافة',
                    noChildren: 'لا أبناء',
                    validation: { childRequired: 'مطلوب ابن', schoolRequired: 'مطلوب مدرسة', consentRequired: 'موافقة' },
                    steps: {
                      student: 'طالب',
                      school: 'مدرسة',
                      parent: 'ولي',
                      notes: 'ملاحظات',
                      review: 'مراجعة',
                    },
                    fields: { branch: 'فرع', stage: 'مرحلة', grade: 'صف', academicYear: 'عام' },
                  },
                  detail: { backToList: 'رجوع' },
                  summary: {
                    title: 'ملخص',
                    applicationNumber: 'رقم',
                    child: 'ابن',
                    school: 'مدرسة',
                    branch: 'فرع',
                    stage: 'مرحلة',
                    grade: 'صف',
                    academicYear: 'عام',
                    notes: 'ملاحظات',
                    attachments: 'مرفقات',
                    emptyValue: '—',
                  },
                },
                enums: { gender: { male: 'ذكر', female: 'أنثى' } },
                common: { select: 'اختر' },
                dashboard: { startApplication: 'ابدأ' },
                profile: {
                  fields: {
                    firstName: 'اسم',
                    lastName: 'عائلة',
                    email: 'بريد',
                    phone: 'هاتف',
                    alternatePhone: 'بديل',
                    addressLine: 'عنوان',
                    city: 'مدينة',
                    district: 'حي',
                  },
                },
              },
              schools: { search: { admissionOpen: 'مفتوح' }, profile: { admissionClosed: 'مغلق' } },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideRouter([
          { path: 'parent/applications/:applicationId/edit', component: ApplicationWizardPage },
          { path: '**', children: [] },
        ]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => null },
              queryParamMap: {
                get: (key: string) => (key === 'schoolSlug' ? 'demo-school' : null),
              },
            },
          },
        },
        {
          provide: ParentApi,
          useValue: {
            listChildren: vi.fn(() =>
              of({
                succeeded: true,
                data: [
                  {
                    id: 'child-1',
                    fullName: 'Child',
                    maskedIdentity: '****',
                    birthDate: '2015-01-01',
                    gender: 1,
                    currentGradeName: 'G1',
                    isActive: true,
                  },
                ],
              }),
            ),
            getProfile: vi.fn(() => of({ succeeded: true, data: { isComplete: true } })),
            checkAgeEligibility: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  resultCode: 1,
                  messageKey: 'eligible',
                  canContinue: true,
                  manualExceptionAllowed: false,
                  hasManualException: false,
                },
              }),
            ),
            createAdmissionApplication: createSpy,
            updateAdmissionApplication: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'app-1',
                  applicationNumber: 'APP-2026-000001',
                  capabilities: { canEdit: true },
                  schoolBranchId: 'b1',
                  educationalStageId: 's1',
                  gradeId: 'g1',
                  academicYearId: 'y1',
                  rowVersion: 'rv-1',
                },
              }),
            ),
            submitAdmissionApplication: vi.fn(),
          },
        },
        {
          provide: SchoolsApi,
          useValue: {
            getBySlug: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'school-1',
                  slug: 'demo-school',
                  name: 'Demo',
                  isAdmissionOpen: true,
                  branches: [{ id: 'b1', name: 'Main' }],
                  offerings: [
                    {
                      id: 'o1',
                      branchId: 'b1',
                      educationalStageId: 's1',
                      stageName: 'Primary',
                      isAdmissionOpen: true,
                      genderType: 3,
                      grades: [{ id: 'g1', name: 'G1' }],
                    },
                  ],
                  fees: [],
                },
              }),
            ),
          },
        },
        {
          provide: TaxonomiesApi,
          useValue: {
            getAcademicYears: vi.fn(() =>
              of({ succeeded: true, data: [{ id: 'y1', name: '2026/2027' }] }),
            ),
          },
        },
      ],
    }).compileComponents();
  });

  it('does not create a draft when advancing without a selected child', async () => {
    const fixture = TestBed.createComponent(ApplicationWizardPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as ApplicationWizardPage;
    await page.next();
    expect(createSpy).not.toHaveBeenCalled();
  });

  it('creates a draft only once when school selection is valid', async () => {
    const fixture = TestBed.createComponent(ApplicationWizardPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.componentInstance as ApplicationWizardPage;
    page.selectChild('child-1');
    await page.next();
    page.schoolForm.patchValue({
      schoolBranchId: 'b1',
      educationalStageId: 's1',
      gradeId: 'g1',
      academicYearId: 'y1',
    });
    await page.next();
    await page.next();
    expect(createSpy).toHaveBeenCalledTimes(1);
  });
});
