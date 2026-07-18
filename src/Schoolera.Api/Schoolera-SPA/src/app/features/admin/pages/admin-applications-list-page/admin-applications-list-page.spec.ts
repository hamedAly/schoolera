import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AdminPlatformApi } from '../../data-access/admin-platform.api';
import { AdminApplicationsListPage } from './admin-applications-list-page';

describe('AdminApplicationsListPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AdminApplicationsListPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              admin: {
                applicationsList: {
                  title: 'طلبات',
                  subtitle: 'وصف',
                  empty: 'لا طلبات',
                  export: 'تصدير',
                  exporting: 'جاري',
                  filters: {
                    status: 'حالة',
                    search: 'بحث',
                    searchPlaceholder: '…',
                    dateFrom: 'من',
                    dateTo: 'إلى',
                    clear: 'مسح',
                    allStatuses: 'الكل',
                  },
                  columns: {
                    number: 'رقم',
                    school: 'مدرسة',
                    student: 'طالب',
                    parent: 'ولي',
                    grade: 'صف',
                    status: 'حالة',
                    submitted: 'تقديم',
                  },
                },
                common: { apply: 'تطبيق', previous: 'السابق', next: 'التالي', pageInfo: '{{page}}/{{total}}' },
              },
              parent: {
                applications: {
                  status: {
                    draft: 'مسودة',
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
        provideRouter([]),
        {
          provide: AdminPlatformApi,
          useValue: {
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
    const fixture = TestBed.createComponent(AdminApplicationsListPage);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('لا طلبات');
  });
});
