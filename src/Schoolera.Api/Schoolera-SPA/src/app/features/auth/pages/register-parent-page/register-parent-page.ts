import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { resolveSchooleraLang } from '../../../../core/i18n/schoolera-lang';
import {
  mapAuthServerErrors,
  resolveAuthFailureCodes,
} from '../../data-access/auth-errors';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { Button } from '../../../../shared/ui/button/button';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import {
  applyServerFieldErrors,
  clearServerFieldErrors,
  serverOrClientFieldError,
} from '../../utils/auth-server-errors';
import {
  passwordMatchValidator,
  registerFormValidators,
} from '../../utils/auth-form.validators';

@Component({
  selector: 'se-register-parent-page',
  imports: [
    AutofocusDirective,
    Breadcrumbs,
    Button,
    FormErrorSummary,
    FormField,
    PageHero,
    PublicPageContainer,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './register-parent-page.html',
  styleUrl: './register-parent-page.scss',
})
export class RegisterParentPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  protected readonly submitting = signal(false);
  protected readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      firstName: ['', registerFormValidators.firstName],
      lastName: ['', registerFormValidators.lastName],
      email: ['', registerFormValidators.email],
      phoneNumber: ['', registerFormValidators.phoneNumber],
      password: ['', registerFormValidators.password],
      confirmPassword: ['', registerFormValidators.confirmPassword],
      termsAccepted: [false, registerFormValidators.termsAccepted],
    },
    { validators: passwordMatchValidator() },
  );

  protected submit(): void {
    clearServerFieldErrors(this.form);
    this.summaryErrors.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set(this.collectClientSummaryErrors());
      return;
    }

    this.submitting.set(true);

    const value = this.form.getRawValue();
    this.auth
      .registerParent({
        ...value,
        preferredLanguage: resolveSchooleraLang(this.transloco.getActiveLang()),
      })
      .subscribe({
        next: (result) => {
          this.submitting.set(false);

          if (!result.succeeded || !result.data) {
            this.applyApiFailure(resolveAuthFailureCodes(result, result.errorCodes));
            return;
          }

          this.toast.success(this.transloco.translate('auth.register.successToast'));
          void this.router.navigate(['/auth/verify'], {
            queryParams: { email: result.data.email ?? value.email },
            state: {
              deliverySucceeded: result.data.verificationDeliverySucceeded ?? true,
              deliveryMode: result.data.verificationDeliveryMode ?? null,
              codeExpiresInMinutes: result.data.codeExpiresInMinutes ?? 15,
            },
          });
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.applyApiFailure(resolveAuthFailureCodes(error));
        },
      });
  }

  protected fieldError(controlName: keyof typeof this.form.controls): string | undefined {
    const control = this.form.controls[controlName];
    const clientMessage = this.clientFieldError(controlName);
    return serverOrClientFieldError(control, clientMessage);
  }

  protected hasFieldError(controlName: keyof typeof this.form.controls): boolean {
    return !!this.fieldError(controlName);
  }

  private applyApiFailure(errorCodes: string[]): void {
    const mapped = mapAuthServerErrors(this.transloco, errorCodes);
    applyServerFieldErrors(this.form, mapped.fieldMessages);
    this.summaryErrors.set(mapped.summaryItems);

    this.toast.error(
      mapped.summaryItems[0]?.message ?? this.transloco.translate('auth.errors.generic'),
    );
  }

  private clientFieldError(controlName: keyof typeof this.form.controls): string | undefined {
    const control = this.form.controls[controlName];
    if (!control.touched) {
      return undefined;
    }

    if (this.form.errors?.['passwordMismatch'] && controlName === 'confirmPassword') {
      return this.transloco.translate('auth.validation.passwordMismatch');
    }

    if (control.errors && !control.errors['server']) {
      return this.validationMessage(control.errors);
    }

    return undefined;
  }

  private collectClientSummaryErrors(): FormErrorSummaryItem[] {
    const items: FormErrorSummaryItem[] = [];
    const names = Object.keys(this.form.controls) as Array<keyof typeof this.form.controls>;

    for (const name of names) {
      const message = this.clientFieldError(name);
      if (message) {
        items.push({ message, fieldId: String(name) });
      }
    }

    return items;
  }

  private validationMessage(errors: Record<string, unknown>): string {
    if (errors['required'] || errors['requiredTrue']) {
      return this.transloco.translate('validation.required');
    }

    if (errors['email']) {
      return this.transloco.translate('auth.validation.invalidEmail');
    }

    if (errors['minlength']) {
      return this.transloco.translate('auth.validation.passwordMinLength');
    }

    return this.transloco.translate('validation.required');
  }
}
