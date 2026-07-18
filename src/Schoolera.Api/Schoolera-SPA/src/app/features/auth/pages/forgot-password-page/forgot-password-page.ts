import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import {
  resolveAuthFailureCodes,
  translateAuthErrorCodes,
} from '../../data-access/auth-errors';
import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { requiredTextValidator } from '../../../../shared/validators/required-text.validator';

@Component({
  selector: 'se-forgot-password-page',
  imports: [
    AutofocusDirective,
    Breadcrumbs,
    Button,
    FormField,
    PageHero,
    PublicPageContainer,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './forgot-password-page.html',
  styleUrl: './forgot-password-page.scss',
})
export class ForgotPasswordPage {
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [requiredTextValidator(), Validators.email]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.auth.forgotPassword(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.submitting.set(false);

        if (!result.succeeded) {
          this.showFailure(resolveAuthFailureCodes(result, result.errorCodes));
          return;
        }

        const message = this.transloco.translate('auth.forgotPassword.success');
        this.successMessage.set(message);
        this.toast.success(message);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.showFailure(resolveAuthFailureCodes(error));
      },
    });
  }

  protected fieldError(): string | undefined {
    const control = this.form.controls.email;
    if (!control.touched || !control.errors) {
      return undefined;
    }

    if (control.errors['required']) {
      return this.transloco.translate('validation.required');
    }

    if (control.errors['email']) {
      return this.transloco.translate('auth.validation.invalidEmail');
    }

    return undefined;
  }

  private showFailure(errorCodes: string[]): void {
    const message = translateAuthErrorCodes(this.transloco, errorCodes);
    this.errorMessage.set(message);
    this.toast.error(message);
  }
}
