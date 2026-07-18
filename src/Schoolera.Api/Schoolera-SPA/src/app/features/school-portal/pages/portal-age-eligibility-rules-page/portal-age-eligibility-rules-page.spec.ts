import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalAgeEligibilityRulesPage } from './portal-age-eligibility-rules-page';

describe('PortalAgeEligibilityRulesPage', () => {
  let fixture: ComponentFixture<PortalAgeEligibilityRulesPage>;
  const api = {
    listAgeEligibilityRules: vi.fn(() => of({ succeeded: true, data: [] })),
    listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [
        PortalAgeEligibilityRulesPage,
        TranslocoTestingModule.forRoot({
          langs: { ar: { portal: { ageEligibilityRules: {
            title: 'قواعد العمر', subtitle: 'إدارة', add: 'إضافة', emptyTitle: 'فارغ',
            emptyMessage: 'لا توجد قواعد', filters: { all: 'الكل', branch: 'فرع',
              stage: 'مرحلة', grade: 'صف', year: 'عام', status: 'حالة', active: 'نشط' },
          } } } },
          translocoConfig: { defaultLang: 'ar', availableLangs: ['ar'] },
        }),
      ],
      providers: [
        { provide: ActivatedRoute, useValue: { parent: { snapshot: { paramMap: { get: () => 'school-1' } } } } },
        { provide: SchoolPortalApi, useValue: api },
        { provide: TaxonomiesApi, useValue: {
          getEducationalStages: vi.fn(() => of({ succeeded: true, data: [] })),
          getAcademicYears: vi.fn(() => of({ succeeded: true, data: [] })),
          getGradesByStage: vi.fn(() => of({ succeeded: true, data: [] })),
        } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(PortalAgeEligibilityRulesPage);
    fixture.detectChanges();
  });

  it('loads rules with empty filters', () => {
    expect(api.listAgeEligibilityRules).toHaveBeenCalledWith('school-1', {
      branchId: undefined, educationalStageId: undefined, gradeId: undefined,
      academicYearId: undefined, publicationStatus: undefined, isActive: undefined,
    });
    expect(fixture.nativeElement.textContent).toContain('قواعد العمر');
  });
});
