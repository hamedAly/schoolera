import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
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
import { requiredTextValidator } from '../../../../shared/validators/required-text.validator';
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
  selector: 'se-reset-password-page',
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
  templateUrl: './reset-password-page.html',
  styleUrl: './reset-password-page.scss',
})
export class ResetPasswordPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  protected readonly submitting = signal(false);
  protected readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      email: ['', registerFormValidators.email],
      token: ['', [requiredTextValidator()]],
      newPassword: ['', registerFormValidators.password],
      confirmPassword: ['', registerFormValidators.confirmPassword],
    },
    { validators: passwordMatchValidator('newPassword', 'confirmPassword') },
  );

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (email) {
      this.form.controls.email.setValue(email);
    }

    if (token) {
      this.form.controls.token.setValue(token);
    }
  }

  protected submit(): void {
    clearServerFieldErrors(this.form);
    this.summaryErrors.set([]);
    this.successMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set(this.collectClientSummaryErrors());
      return;
    }

    this.submitting.set(true);

    const value = this.form.getRawValue();
    this.auth
      .resetPassword({
        email: value.email,
        token: value.token,
        newPassword: value.newPassword,
        confirmPassword: value.confirmPassword,
      })
      .subscribe({
        next: (result) => {
          this.submitting.set(false);

          if (!result.succeeded) {
            this.applyApiFailure(resolveAuthFailureCodes(result, result.errorCodes));
            return;
          }

          this.toast.success(this.transloco.translate('auth.resetPassword.success'));
          void this.router.navigate(['/auth/login']);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.applyApiFailure(resolveAuthFailureCodes(error));
        },
      });
  }

  protected fieldError(controlName: keyof typeof this.form.controls): string | undefined {
    return serverOrClientFieldError(this.form.controls[controlName], this.clientFieldError(controlName));
  }

  protected hasFieldError(controlName: keyof typeof this.form.controls): boolean {
    return !!this.fieldError(controlName);
  }

  private applyApiFailure(errorCodes: string[]): void {
    const mapped = mapAuthServerErrors(this.transloco, errorCodes, {
      passwordFieldName: 'newPassword',
    });
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

    if (controlName === 'confirmPassword' && this.form.errors?.['passwordMismatch']) {
      return this.transloco.translate('auth.validation.passwordMismatch');
    }

    if (!control.errors || control.errors['server']) {
      return undefined;
    }

    if (control.errors['required']) {
      return this.transloco.translate('validation.required');
    }

    if (control.errors['email']) {
      return this.transloco.translate('auth.validation.invalidEmail');
    }

    if (control.errors['minlength']) {
      return this.transloco.translate('auth.validation.passwordMinLength');
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
}
