import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { interval } from 'rxjs';

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
import { maskEmail } from '../../utils/mask-email';

const RESEND_COOLDOWN_SECONDS = 60;

export interface VerifyNavigationState {
  deliverySucceeded?: boolean;
  deliveryMode?: string;
  codeExpiresInMinutes?: number;
}

@Component({
  selector: 'se-verify-page',
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
  templateUrl: './verify-page.html',
  styleUrl: './verify-page.scss',
})
export class VerifyPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly submitting = signal(false);
  protected readonly resending = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly maskedEmail = signal<string | null>(null);
  protected readonly deliverySucceeded = signal<boolean | null>(null);
  protected readonly deliveryMode = signal<string | null>(null);
  protected readonly resendSecondsLeft = signal(0);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [requiredTextValidator(), Validators.email]],
    code: [
      '',
      [requiredTextValidator(), Validators.minLength(6), Validators.maxLength(6), Validators.pattern(/^\d{6}$/)],
    ],
  });

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email');
    if (email) {
      this.form.controls.email.setValue(email);
      this.maskedEmail.set(maskEmail(email));
    }

    const historyState = (typeof history !== 'undefined' ? history.state : null) as VerifyNavigationState | null;
    if (historyState) {
      if (typeof historyState.deliverySucceeded === 'boolean') {
        this.deliverySucceeded.set(historyState.deliverySucceeded);
      }
      if (historyState.deliveryMode) {
        this.deliveryMode.set(historyState.deliveryMode);
      }
    }

    this.form.controls.email.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value) => {
      this.maskedEmail.set(value ? maskEmail(value) : null);
    });

    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const remaining = this.resendSecondsLeft();
        if (remaining > 0) {
          this.resendSecondsLeft.set(remaining - 1);
        }
      });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.auth.verify(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.submitting.set(false);

        if (!result.succeeded) {
          this.showFailure(resolveAuthFailureCodes(result, result.errorCodes));
          return;
        }

        this.successMessage.set(this.transloco.translate('auth.verify.success'));
        this.toast.success(this.transloco.translate('auth.verify.success'));
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.showFailure(resolveAuthFailureCodes(error));
      },
    });
  }

  protected resend(): void {
    if (this.resendSecondsLeft() > 0 || this.resending()) {
      return;
    }

    const email = this.form.controls.email.value;
    if (!email) {
      this.form.controls.email.markAsTouched();
      return;
    }

    this.resending.set(true);
    this.errorMessage.set(null);

    this.auth.resendVerification({ email }).subscribe({
      next: (result) => {
        this.resending.set(false);

        if (!result.succeeded) {
          this.showFailure(resolveAuthFailureCodes(result, result.errorCodes));
          return;
        }

        const data = result.data as { deliverySucceeded?: boolean; deliveryMode?: string } | null;
        if (typeof data?.deliverySucceeded === 'boolean') {
          this.deliverySucceeded.set(data.deliverySucceeded);
        } else {
          this.deliverySucceeded.set(true);
        }
        if (data?.deliveryMode) {
          this.deliveryMode.set(data.deliveryMode);
        }

        this.successMessage.set(this.transloco.translate('auth.verify.resendSuccess'));
        this.toast.info(this.transloco.translate('auth.verify.resendSuccess'));
        this.resendSecondsLeft.set(RESEND_COOLDOWN_SECONDS);
      },
      error: (error: unknown) => {
        this.resending.set(false);
        this.showFailure(resolveAuthFailureCodes(error));
      },
    });
  }

  private showFailure(errorCodes: string[]): void {
    const message = translateAuthErrorCodes(this.transloco, errorCodes);
    this.errorMessage.set(message);
    this.toast.error(message);

    if (errorCodes.includes('auth.resendTooSoon')) {
      this.resendSecondsLeft.set(RESEND_COOLDOWN_SECONDS);
    }
  }

  protected fieldError(controlName: 'email' | 'code'): string | undefined {
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

    if (controlName === 'code' && (control.errors['minlength'] || control.errors['maxlength'] || control.errors['pattern'])) {
      return this.transloco.translate('auth.verify.codeLengthError');
    }

    return undefined;
  }

  protected hasFieldError(controlName: 'email' | 'code'): boolean {
    return !!this.fieldError(controlName);
  }
}
