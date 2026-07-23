import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { RegisterParentPage } from './register-parent-page';

describe('RegisterParentPage consent mapping', () => {
  let fixture: ComponentFixture<RegisterParentPage>;
  let page: RegisterParentPage;
  let registerParent: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    registerParent = vi.fn(() =>
      of({
        succeeded: true,
        data: {
          email: 'parent@example.invalid',
          requiresVerification: true,
          verificationDeliverySucceeded: true,
          codeExpiresInMinutes: 15,
        },
        errors: [],
        errorCodes: [],
      }),
    );

    await TestBed.configureTestingModule({
      imports: [
        RegisterParentPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              auth: {
                registerParent: {
                  title: 'تسجيل ولي الأمر',
                  subtitle: 'إنشاء حساب',
                  breadcrumb: 'تسجيل ولي الأمر',
                },
                register: {
                  termsAcceptBefore: 'أوافق على',
                  termsLink: 'الشروط والأحكام',
                  termsAcceptAfter: '',
                  privacyAcceptBefore: 'أوافق على',
                  privacyLink: 'سياسة الخصوصية',
                  privacyAcceptAfter: '',
                  haveAccount: 'لديك حساب؟',
                  successToast: 'تم',
                },
                fields: {
                  firstName: 'الاسم الأول',
                  lastName: 'اسم العائلة',
                  email: 'البريد',
                  phoneNumber: 'الهاتف',
                  password: 'كلمة المرور',
                  confirmPassword: 'تأكيد كلمة المرور',
                },
                actions: {
                  createAccount: 'إنشاء حساب',
                  registering: 'جاري التسجيل',
                  signIn: 'تسجيل الدخول',
                },
                errors: {
                  termsRequired: 'الشروط مطلوبة',
                  privacyRequired: 'الخصوصية مطلوبة',
                },
              },
              common: { home: 'الرئيسية' },
              formErrors: { summaryTitle: 'يرجى إصلاح التالي:' },
              toast: { dismiss: 'إغلاق' },
            },
          },
          translocoConfig: {
            availableLangs: ['ar'],
            defaultLang: 'ar',
          },
        }),
      ],
      providers: [
        provideHttpClient(),
        provideRouter([{ path: '**', children: [] }]),
        {
          provide: AuthService,
          useValue: { registerParent },
        },
        {
          provide: ToastService,
          useValue: { success: vi.fn(), error: vi.fn() },
        },
      ],
    }).compileComponents();

    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(RegisterParentPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  function fillValidFields(options?: { terms?: boolean; privacy?: boolean }): void {
    page['form'].setValue({
      firstName: 'نور',
      lastName: 'أحمد',
      email: 'parent@example.invalid',
      phoneNumber: '+201000000099',
      password: 'Schoolera@Dev1',
      confirmPassword: 'Schoolera@Dev1',
      termsAccepted: options?.terms ?? false,
      privacyAccepted: options?.privacy ?? false,
    });
  }

  it('blocks submit when Privacy is not accepted', () => {
    fillValidFields({ terms: true, privacy: false });
    page['submit']();
    expect(registerParent).not.toHaveBeenCalled();
    expect(page['form'].controls.privacyAccepted.invalid).toBe(true);
  });

  it('blocks submit when Terms is not accepted', () => {
    fillValidFields({ terms: false, privacy: true });
    page['submit']();
    expect(registerParent).not.toHaveBeenCalled();
    expect(page['form'].controls.termsAccepted.invalid).toBe(true);
  });

  it('sends termsAccepted and privacyAccepted as true in the NSwag request', () => {
    fillValidFields({ terms: true, privacy: true });
    const body = page.buildRegisterRequest();
    expect(body.termsAccepted).toBe(true);
    expect(body.privacyAccepted).toBe(true);
    page['submit']();
    expect(registerParent).toHaveBeenCalledWith(
      expect.objectContaining({
        termsAccepted: true,
        privacyAccepted: true,
        preferredLanguage: 'ar',
      }),
    );
  });
});
