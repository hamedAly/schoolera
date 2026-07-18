import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { AdminCmsApi } from '../../data-access/admin-cms.api';
import { AdminCmsFaqPage } from './admin-cms-faq-page';

describe('AdminCmsFaqPage interview mode', () => {
  let fixture: ComponentFixture<AdminCmsFaqPage>;
  let api: {
    listFaqCategories: ReturnType<typeof vi.fn>;
    listInterviewFaqItems: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    api = {
      listFaqCategories: vi.fn(() => of({ succeeded: true, data: [] })),
      listInterviewFaqItems: vi.fn(() =>
        of({
          succeeded: true,
          data: [
            {
              id: 'i1',
              questionAr: 'سؤال',
              questionEn: 'Question',
              answerAr: '<p>جواب</p>',
              answerEn: '<p>Answer</p>',
              interviewCategory: 1,
              isPublished: true,
              isActive: true,
            },
          ],
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [
        AdminCmsFaqPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              admin: {
                cms: {
                  status: { published: 'منشور', draft: 'مسودة' },
                  faq: {
                    title: 'الأسئلة الشائعة',
                    subtitle: 'إدارة',
                    tabsLabel: 'أقسام',
                    tabs: { general: 'عام', interview: 'مقابلة' },
                    categories: 'تصنيفات',
                    items: 'أسئلة',
                    addCategory: 'إضافة',
                    addItem: 'إضافة',
                    edit: 'تعديل',
                    publish: 'نشر',
                    unpublish: 'إلغاء',
                    save: 'حفظ',
                    cancel: 'إلغاء',
                    selectCategory: 'اختر',
                    emptyItems: 'فارغ',
                    interview: {
                      items: 'أسئلة المقابلة',
                      addItem: 'إضافة سؤال',
                      editItem: 'تعديل سؤال',
                      emptyItems: 'لا أسئلة',
                      active: 'نشط',
                      inactive: 'غير نشط',
                      activate: 'تفعيل',
                      deactivate: 'تعطيل',
                      filters: {
                        all: 'الكل',
                        category: 'التصنيف',
                        published: 'النشر',
                        active: 'الحالة',
                        search: 'بحث',
                      },
                      categories: {
                        interview: 'مقابلة',
                        assessment: 'تقييم',
                        interviewAndAssessment: 'كلاهما',
                      },
                      fields: { category: 'التصنيف' },
                    },
                  },
                },
                errors: { generic: 'خطأ' },
              },
            },
          },
          translocoConfig: { defaultLang: 'ar', availableLangs: ['ar'] },
        }),
      ],
      providers: [
        provideHttpClient(),
        { provide: AdminCmsApi, useValue: api },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
        {
          provide: DocumentLanguageService,
          useValue: { activeLang: signal('ar') },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminCmsFaqPage);
    fixture.detectChanges();
  });

  it('lists interview items when interview tab is selected', () => {
    const tab = fixture.nativeElement.querySelector(
      '[data-testid="interview-faq-tab"]',
    ) as HTMLButtonElement;
    tab.click();
    fixture.detectChanges();

    expect(api.listInterviewFaqItems).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('سؤال');
    expect(fixture.nativeElement.querySelector('[data-testid="admin-interview-category-filter"]')).toBeTruthy();
  });

  it('reloads interview list when filters change', () => {
    const page = fixture.componentInstance as unknown as {
      setViewMode: (mode: 'general' | 'interview') => void;
      interviewFilterForm: { patchValue: (v: Record<string, string>) => void };
    };
    page.setViewMode('interview');
    fixture.detectChanges();
    api.listInterviewFaqItems.mockClear();
    page.interviewFilterForm.patchValue({ interviewCategory: '2', search: 'prep' });
    fixture.detectChanges();
    expect(api.listInterviewFaqItems).toHaveBeenCalledWith(2, undefined, undefined, 'prep');
  });
});
