import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { SchoolsApi } from '../../../schools/data-access/schools.api';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalAdmissionRequirementsPage } from './portal-admission-requirements-page';

describe('PortalAdmissionRequirementsPage', () => {
  let fixture: ComponentFixture<PortalAdmissionRequirementsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PortalAdmissionRequirementsPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                admissionRequirements: {
                  title: 'متطلبات القبول',
                  subtitle: 'تهيئة المتطلبات',
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
                    kind: 'نوع',
                    publication: 'نشر',
                    active: 'نشط',
                  },
                  kinds: {
                    informational: 'معلومة',
                    parentProfile: 'ولي أمر',
                    childProfile: 'طفل',
                    document: 'مستند',
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
          useValue: { lang: signal('ar'), dir: signal('rtl') },
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
            listAdmissionRequirements: vi.fn(() => of({ succeeded: true, data: [] })),
            listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
            getProfile: vi.fn(() => of({ succeeded: true, data: { slug: 'demo' } })),
          },
        },
        {
          provide: SchoolsApi,
          useValue: { getAdmissionRequirements: vi.fn(() => of({ succeeded: true, data: [] })) },
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

    fixture = TestBed.createComponent(PortalAdmissionRequirementsPage);
    fixture.detectChanges();
  });

  it('renders page title', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('متطلبات القبول');
  });
});
