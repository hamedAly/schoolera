import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalAdmissionQuestionsPage } from './portal-admission-questions-page';

describe('PortalAdmissionQuestionsPage', () => {
  let fixture: ComponentFixture<PortalAdmissionQuestionsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PortalAdmissionQuestionsPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                admissionQuestions: {
                  title: 'أسئلة القبول',
                  subtitle: 'تهيئة الأسئلة',
                  add: 'إضافة',
                  published: 'منشور',
                  draft: 'مسودة',
                  active: 'نشط',
                  inactive: 'غير نشط',
                  emptyTitle: 'فارغ',
                  emptyMessage: 'لا عناصر',
                  filters: {
                    all: 'الكل',
                    branch: 'فرع',
                    stage: 'مرحلة',
                    grade: 'صف',
                    academicYear: 'عام',
                    type: 'نوع',
                    publication: 'نشر',
                    active: 'نشط',
                  },
                  types: { shortText: 'نص قصير', longText: 'نص طويل' },
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
          useValue: { lang: signal('ar'), dir: signal('rtl'), activeLang: signal('ar') },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            parent: { snapshot: { paramMap: { get: () => 'school-1' } } },
          },
        },
        {
          provide: SchoolPortalApi,
          useValue: {
            listAdmissionQuestions: vi.fn(() => of({ succeeded: true, data: [] })),
            listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
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

    fixture = TestBed.createComponent(PortalAdmissionQuestionsPage);
    fixture.detectChanges();
  });

  it('renders page title', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('أسئلة القبول');
  });
});
