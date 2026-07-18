import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalApplicationsListPage } from './portal-applications-list-page';

describe('PortalApplicationsListPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PortalApplicationsListPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                applications: {
                  list: {
                    title: 'طلبات',
                    subtitle: 'وصف',
                    emptyTitle: 'فارغ',
                    emptyMessage: 'لا طلبات',
                    previous: 'السابق',
                    next: 'التالي',
                    pageInfo: '{{page}}/{{total}}',
                    filters: {
                      status: 'حالة',
                      branch: 'فرع',
                      grade: 'صف',
                      search: 'بحث',
                      searchPlaceholder: '…',
                      apply: 'تطبيق',
                      clear: 'مسح',
                      allStatuses: 'الكل',
                      allBranches: 'الكل',
                      allGrades: 'الكل',
                      dateFrom: 'من',
                      dateTo: 'إلى',
                    },
                    columns: {
                      number: 'رقم',
                      student: 'طالب',
                      parent: 'ولي',
                      branch: 'فرع',
                      grade: 'صف',
                      status: 'حالة',
                      submitted: 'تقديم',
                    },
                  },
                },
              },
              parent: {
                applications: {
                  status: {
                    submitted: 'مقدم',
                    underReview: 'مراجعة',
                    accepted: 'مقبول',
                    rejected: 'مرفوض',
                    cancelled: 'ملغى',
                  },
                },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideRouter([{ path: 'school/:schoolId/applications', component: PortalApplicationsListPage }]),
        {
          provide: TaxonomiesApi,
          useValue: {
            getEducationalStages: vi.fn(() => of({ succeeded: true, data: [] })),
            getGradesByStage: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
        {
          provide: SchoolPortalApi,
          useValue: {
            listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
            listAdmissionApplications: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  items: [],
                  totalCount: 0,
                  pageNumber: 1,
                  pageSize: 20,
                  totalPages: 1,
                },
              }),
            ),
          },
        },
      ],
    }).compileComponents();
  });

  it('shows empty state when no applications', () => {
    const fixture = TestBed.createComponent(PortalApplicationsListPage);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('فارغ');
  });
});
