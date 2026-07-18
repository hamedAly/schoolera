import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { sanitizeReturnUrl } from '../../../../core/auth/return-url';
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

@Component({
  selector: 'se-login-page',
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
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  protected readonly submitting = signal(false);
  protected readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [requiredTextValidator(), Validators.email]],
    password: ['', [requiredTextValidator(), Validators.minLength(8)]],
  });

  protected submit(): void {
    this.summaryErrors.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set(this.collectClientSummaryErrors());
      return;
    }

    this.submitting.set(true);

    const value = this.form.getRawValue();
    this.auth.login(value).subscribe({
      next: (result) => {
        this.submitting.set(false);

        if (!result.succeeded || !result.data) {
          this.applyApiFailure(resolveAuthFailureCodes(result, result.errorCodes));
          return;
        }

        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const destination = sanitizeReturnUrl(returnUrl, result.data.postLoginDestination);
        void this.router.navigateByUrl(destination);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.applyApiFailure(resolveAuthFailureCodes(error));
      },
    });
  }

  protected fieldError(controlName: 'email' | 'password'): string | undefined {
    const control = this.form.controls[controlName];
    if (!control.touched || !control.errors) {
      return undefined;
    }

    if (control.errors['required']) {
      return this.transloco.translate('validation.required');
    }

    if (controlName === 'email' && control.errors['email']) {
      return this.transloco.translate('auth.validation.invalidEmail');
    }

    if (controlName === 'password' && control.errors['minlength']) {
      return this.transloco.translate('auth.validation.passwordMinLength');
    }

    return undefined;
  }

  protected hasFieldError(controlName: 'email' | 'password'): boolean {
    return !!this.fieldError(controlName);
  }

  private applyApiFailure(errorCodes: string[]): void {
    const mapped = mapAuthServerErrors(this.transloco, errorCodes);
    this.summaryErrors.set(mapped.summaryItems);
    this.toast.error(
      mapped.summaryItems[0]?.message ?? this.transloco.translate('auth.errors.generic'),
    );
  }

  private collectClientSummaryErrors(): FormErrorSummaryItem[] {
    const items: FormErrorSummaryItem[] = [];
    for (const name of ['email', 'password'] as const) {
      const message = this.fieldError(name);
      if (message) {
        items.push({ message, fieldId: name });
      }
    }
    return items;
  }
}
