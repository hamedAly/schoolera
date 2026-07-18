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
import { InterviewFaqCategory } from '../../data-access/school-portal.models';
import { PortalInterviewFaqsPage } from './portal-interview-faqs-page';

describe('PortalInterviewFaqsPage', () => {
  let fixture: ComponentFixture<PortalInterviewFaqsPage>;
  let api: {
    listInterviewFaqs: ReturnType<typeof vi.fn>;
    listBranches: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    api = {
      listInterviewFaqs: vi.fn(() => of({ succeeded: true, data: [] })),
      listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
    };

    await TestBed.configureTestingModule({
      imports: [
        PortalInterviewFaqsPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                interviewFaqs: {
                  title: 'أسئلة المقابلة',
                  subtitle: 'تهيئة',
                  add: 'إضافة',
                  published: 'منشور',
                  draft: 'مسودة',
                  active: 'نشط',
                  inactive: 'غير نشط',
                  emptyTitle: 'فارغ',
                  emptyMessage: 'لا عناصر',
                  filters: {
                    all: 'الكل',
                    category: 'التصنيف',
                    branch: 'فرع',
                    stage: 'مرحلة',
                    grade: 'صف',
                    academicYear: 'عام',
                    publication: 'نشر',
                    active: 'نشط',
                  },
                  categories: {
                    interview: 'مقابلة',
                    assessment: 'تقييم',
                    interviewAndAssessment: 'كلاهما',
                  },
                  fields: {
                    category: 'التصنيف',
                    questionAr: 'س',
                    questionEn: 'Q',
                    answerAr: 'ج',
                    answerEn: 'A',
                    scopeBranch: 'فرع',
                    scopeStage: 'مرحلة',
                    scopeGrade: 'صف',
                    scopeYear: 'عام',
                  },
                },
                confirm: { cancel: 'إلغاء' },
                common: { activate: 'تفعيل' },
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

    fixture = TestBed.createComponent(PortalInterviewFaqsPage);
    fixture.detectChanges();
  });

  it('loads list on init', () => {
    expect(api.listInterviewFaqs).toHaveBeenCalledWith('school-1', {
      interviewCategory: undefined,
      branchId: undefined,
      educationalStageId: undefined,
      gradeId: undefined,
      academicYearId: undefined,
      isPublished: undefined,
      isActive: undefined,
    });
    expect(fixture.nativeElement.textContent).toContain('أسئلة المقابلة');
  });

  it('shows category field when creating', () => {
    const page = fixture.componentInstance as unknown as {
      openCreate: () => void;
      form: { controls: { interviewCategory: { value: number } } };
    };
    page.openCreate();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="interview-faq-category-field"]')).toBeTruthy();
    expect(page.form.controls.interviewCategory.value).toBe(InterviewFaqCategory._1);
  });

  it('uses canManageContent portal permission on route', async () => {
    const { schoolPortalRoutes } = await import('../../school-portal.routes');
    const schoolChildren = schoolPortalRoutes[1]?.children ?? [];
    const faqRoute = schoolChildren.find((route) => route.path === 'interview-faqs');
    expect(faqRoute?.data?.['portalPermission']).toBe('canManageContent');
  });

  it('reloads list when category filter changes', () => {
    api.listInterviewFaqs.mockClear();
    const page = fixture.componentInstance as unknown as {
      filterForm: { patchValue: (v: Record<string, string>) => void };
    };
    page.filterForm.patchValue({ interviewCategory: String(InterviewFaqCategory._2) });
    fixture.detectChanges();
    expect(api.listInterviewFaqs).toHaveBeenCalledWith('school-1', {
      interviewCategory: InterviewFaqCategory._2,
      branchId: undefined,
      educationalStageId: undefined,
      gradeId: undefined,
      academicYearId: undefined,
      isPublished: undefined,
      isActive: undefined,
    });
  });
});
