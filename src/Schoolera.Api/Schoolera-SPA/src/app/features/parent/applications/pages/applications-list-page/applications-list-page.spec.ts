import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ParentApi } from '../../../data-access/parent.api';
import { ApplicationsListPage } from './applications-list-page';

describe('ApplicationsListPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ApplicationsListPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                applications: {
                  list: {
                    title: 'طلباتي',
                    subtitle: 'وصف',
                    emptyTitle: 'فارغ',
                    emptyMessage: 'لا طلبات',
                    clearFilters: 'مسح',
                    resultCount: '{{count}} نتيجة',
                    pagination: 'تصفح',
                    prevPage: 'السابق',
                    nextPage: 'التالي',
                    pageOf: '{{page}}/{{total}}',
                    filters: {
                      status: 'حالة',
                      child: 'ابن',
                      search: 'بحث',
                      searchPlaceholder: '…',
                      apply: 'تطبيق',
                      allStatuses: 'الكل',
                      allChildren: 'الكل',
                    },
                    columns: {
                      number: 'رقم',
                      status: 'حالة',
                      child: 'ابن',
                      school: 'مدرسة',
                      branch: 'فرع',
                      grade: 'صف',
                      year: 'عام',
                      created: 'إنشاء',
                      submitted: 'تقديم',
                      actions: 'إجراءات',
                    },
                    actions: { view: 'عرض', continueDraft: 'متابعة' },
                  },
                  status: {
                    draft: 'مسودة',
                    submitted: 'مقدم',
                    underReview: 'مراجعة',
                    accepted: 'مقبول',
                    rejected: 'مرفوض',
                    cancelled: 'ملغى',
                    descriptions: {
                      draft: 'مسودة',
                      submitted: 'مقدم',
                      underReview: 'مراجعة',
                      accepted: 'مقبول',
                      rejected: 'مرفوض',
                      cancelled: 'ملغى',
                      unknown: 'غير معروف',
                    },
                  },
                },
                dashboard: { startApplication: 'ابدأ' },
                common: { retry: 'إعادة' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideRouter([]),
        {
          provide: ParentApi,
          useValue: {
            listChildren: vi.fn(() => of({ succeeded: true, data: [] })),
            listAdmissionApplications: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  items: [],
                  totalCount: 0,
                  pageNumber: 1,
                  pageSize: 10,
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
    const fixture = TestBed.createComponent(ApplicationsListPage);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('فارغ');
  });
});
