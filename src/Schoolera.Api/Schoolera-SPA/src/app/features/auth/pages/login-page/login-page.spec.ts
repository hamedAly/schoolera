import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthService } from '../../../../core/auth/auth.service';
import { LoginPage } from './login-page';

const authScope = {
  login: {
    title: 'Log in',
    subtitle: 'Sign in',
    breadcrumb: 'Log in',
    forgotPassword: 'Forgot',
    noAccount: 'No account',
    createAccount: 'Create',
  },
  fields: {
    email: 'Email',
    password: 'Password',
  },
  actions: {
    signIn: 'Log in',
    signingIn: 'Signing in',
  },
  errors: {
    generic: 'Error',
    invalidCredentials: 'Invalid credentials',
  },
};

describe('LoginPage', () => {
  const loginMock = vi.fn();

  beforeEach(async () => {
    loginMock.mockReset();

    await TestBed.configureTestingModule({
      imports: [
        LoginPage,
        TranslocoTestingModule.forRoot({
          langs: {
            en: {
              auth: authScope,
              formErrors: { summaryTitle: 'Please fix the following:' },
              toast: { dismiss: 'Dismiss' },
            },
          },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            login: loginMock,
            ensureSession: () => of(undefined),
          },
        },
      ],
    }).compileComponents();
  });

  it('renders login form fields', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Log in');
    expect(element.querySelector('input[formControlName="email"]')).toBeTruthy();
    expect(element.querySelector('input[formControlName="password"]')).toBeTruthy();
  });

  it('shows translated error when login fails', () => {
    loginMock.mockReturnValue(
      of({
        succeeded: false,
        data: null,
        errors: ['bad'],
        errorCodes: ['auth.invalidCredentials'],
      }),
    );

    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    fixture.componentInstance['form'].setValue({
      email: 'test@example.com',
      password: 'password123',
    });
    fixture.componentInstance['submit']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Invalid credentials');
  });
});
