import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import {
  InterviewAssessmentDeliveryMode,
  InterviewAssessmentRequirementMode,
} from '../../data-access/school-portal.models';
import { PortalInterviewAssessmentPoliciesPage } from './portal-interview-assessment-policies-page';

describe('PortalInterviewAssessmentPoliciesPage', () => {
  let fixture: ComponentFixture<PortalInterviewAssessmentPoliciesPage>;
  let api: {
    listInterviewAssessmentPolicies: ReturnType<typeof vi.fn>;
    listBranches: ReturnType<typeof vi.fn>;
    listMeetingProviderOptions: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    api = {
      listInterviewAssessmentPolicies: vi.fn(() => of({ succeeded: true, data: [] })),
      listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
      listMeetingProviderOptions: vi.fn(() =>
        of({
          succeeded: true,
          data: [{ providerCode: 'Development', displayNameAr: 'تجريبي', displayNameEn: 'Dev' }],
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [
        PortalInterviewAssessmentPoliciesPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                interviewAssessmentPolicies: {
                  title: 'سياسات المقابلة والتقييم',
                  subtitle: 'تهيئة',
                  add: 'إضافة',
                  published: 'منشور',
                  draft: 'مسودة',
                  active: 'نشط',
                  inactive: 'غير نشط',
                  emptyTitle: 'فارغ',
                  emptyMessage: 'لا عناصر',
                  previewTitle: 'معاينة',
                  previewLoad: 'تحميل',
                  previewHint: 'تلميح',
                  previewEmpty: 'لا معاينة',
                  filters: {
                    all: 'الكل',
                    branch: 'فرع',
                    stage: 'مرحلة',
                    grade: 'صف',
                    academicYear: 'عام',
                    publication: 'نشر',
                    active: 'نشط',
                  },
                  modes: {
                    notRequired: 'غير مطلوب',
                    interviewOnly: 'مقابلة',
                    assessmentOnly: 'تقييم',
                    interviewAndAssessment: 'كلاهما',
                  },
                  delivery: {
                    online: 'عن بعد',
                    onSite: 'حضوري',
                    hybrid: 'هجين',
                  },
                  fields: {
                    meetingProvider: 'مزود الاجتماع',
                    selectMeetingProvider: 'اختر',
                    deliveryMode: 'طريقة التسليم',
                  },
                },
                confirm: { cancel: 'إلغاء' },
              },
              common: { save: 'حفظ' },
            },
          },
          translocoConfig: { defaultLang: 'ar', availableLangs: ['ar'] },
        }),
      ],
      providers: [
        provideHttpClient(),
        {
          provide: DocumentLanguageService,
          useValue: { activeLang: signal('ar') },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            parent: { snapshot: { paramMap: { get: () => 'school-1' } } },
          },
        },
        { provide: SchoolPortalApi, useValue: api },
        {
          provide: TaxonomiesApi,
          useValue: {
            getEducationalStages: vi.fn(() => of({ succeeded: true, data: [] })),
            getAcademicYears: vi.fn(() => of({ succeeded: true, data: [] })),
            getGradesByStage: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalInterviewAssessmentPoliciesPage);
    fixture.detectChanges();
  });

  it('loads list on init', () => {
    expect(api.listInterviewAssessmentPolicies).toHaveBeenCalledWith('school-1', {
      branchId: undefined,
      educationalStageId: undefined,
      gradeId: undefined,
      academicYearId: undefined,
      publicationStatus: undefined,
      isActive: undefined,
    });
    expect(fixture.nativeElement.textContent).toContain('سياسات المقابلة والتقييم');
  });

  it('reloads list when filters change', () => {
    api.listInterviewAssessmentPolicies.mockClear();
    const page = fixture.componentInstance as unknown as {
      filterForm: { patchValue: (v: Record<string, string>) => void };
    };
    page.filterForm.patchValue({ publicationStatus: '2', isActive: 'true' });
    fixture.detectChanges();
    expect(api.listInterviewAssessmentPolicies).toHaveBeenCalledWith('school-1', {
      branchId: undefined,
      educationalStageId: undefined,
      gradeId: undefined,
      academicYearId: undefined,
      publicationStatus: 2,
      isActive: true,
    });
  });

  it('hides delivery fields when requirement mode is NotRequired', () => {
    const page = fixture.componentInstance as unknown as {
      openCreate: () => void;
      isOperational: () => boolean;
      showOnlineFields: () => boolean;
      form: {
        controls: {
          requirementMode: { setValue: (v: number) => void };
        };
      };
    };
    page.openCreate();
    fixture.detectChanges();
    expect(page.isOperational()).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-testid="meeting-provider"]')).toBeTruthy();

    page.form.controls.requirementMode.setValue(InterviewAssessmentRequirementMode._1);
    fixture.detectChanges();
    expect(page.isOperational()).toBe(false);
    expect(page.showOnlineFields()).toBe(false);
    expect(fixture.nativeElement.querySelector('[data-testid="meeting-provider"]')).toBeNull();
  });

  it('shows meeting provider when delivery mode is Online', () => {
    const page = fixture.componentInstance as unknown as {
      openCreate: () => void;
      showOnlineFields: () => boolean;
      form: {
        controls: {
          requirementMode: { setValue: (v: number) => void };
          deliveryMode: { setValue: (v: number) => void };
        };
      };
    };
    page.openCreate();
    page.form.controls.requirementMode.setValue(InterviewAssessmentRequirementMode._2);
    page.form.controls.deliveryMode.setValue(InterviewAssessmentDeliveryMode._1);
    fixture.detectChanges();
    expect(page.showOnlineFields()).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-testid="meeting-provider"]')).toBeTruthy();
  });
});
