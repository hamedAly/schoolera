import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PreferredContactMethod } from '../../../../core/api-client/SwaggerClient.service';
import { ParentApi } from '../../data-access/parent.api';
import { ParentProfilePage } from './parent-profile-page';

describe('ParentProfilePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ParentProfilePage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                profile: {
                  title: 'ملف',
                  subtitle: 'وصف',
                  fields: {
                    email: 'بريد',
                    firstName: 'اسم',
                    lastName: 'عائلة',
                    phone: 'هاتف',
                    alternatePhone: 'بديل',
                    addressLine: 'عنوان',
                    city: 'مدينة',
                    district: 'حي',
                    preferredContactMethod: 'تواصل',
                    preferredLanguage: 'لغة',
                  },
                  validation: {
                    required: 'مطلوب',
                    firstNameRequired: 'الاسم مطلوب',
                    lastNameRequired: 'العائلة مطلوبة',
                    phoneRequired: 'الهاتف مطلوب',
                  },
                },
                enums: {
                  contactMethod: { phone: 'هاتف', whatsApp: 'واتس', email: 'بريد' },
                  language: { ar: 'عربي', en: 'En' },
                },
                common: { save: 'حفظ', saving: 'حفظ...', select: 'اختر' },
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
            getProfile: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'p1',
                  email: 'parent@test.com',
                  firstName: 'Ali',
                  lastName: 'Hassan',
                  phone: '0500000000',
                  preferredContactMethod: PreferredContactMethod._1,
                  preferredLanguage: 'ar',
                  isComplete: false,
                },
              }),
            ),
            updateProfile: vi.fn(() => of({ succeeded: true, data: {} })),
          },
        },
        {
          provide: TaxonomiesApi,
          useValue: {
            getCities: vi.fn(() => of({ succeeded: true, data: [] })),
            getDistrictsByCity: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
        {
          provide: ToastService,
          useValue: { success: vi.fn() },
        },
      ],
    }).compileComponents();
  });

  it('loads profile and renders email as read-only', () => {
    const fixture = TestBed.createComponent(ParentProfilePage);
    fixture.detectChanges();

    const emailInput = fixture.nativeElement.querySelector('#parent-email') as HTMLInputElement;
    expect(emailInput?.readOnly).toBe(true);
    expect(emailInput?.value).toBe('parent@test.com');
  });

  it('shows validation summary when required fields are empty on submit', async () => {
    TestBed.overrideProvider(ParentApi, {
      useValue: {
        getProfile: vi.fn(() =>
          of({
            succeeded: true,
            data: {
              id: 'p1',
              email: 'parent@test.com',
              firstName: '',
              lastName: '',
              phone: '',
              preferredContactMethod: PreferredContactMethod._1,
              preferredLanguage: 'ar',
              isComplete: false,
            },
          }),
        ),
        updateProfile: vi.fn(() => of({ succeeded: true, data: {} })),
      },
    });

    const fixture = TestBed.createComponent(ParentProfilePage);
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('الاسم مطلوب');
  });
});
