import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { requiredTextValidator } from '../../../../shared/validators/required-text.validator';
import {
  SUPPORT_TICKET_CATEGORY_OPTIONS,
  SUPPORT_TICKET_PRIORITY_OPTIONS,
  SupportTicketPriority,
} from '../../../support/data-access/support-ticket.models';
import { mapParentErrorSummaryItems, translateParentErrorCodes } from '../../data-access/parent-errors';
import { ParentSupportTicketsApi } from '../../data-access/parent-support-tickets.api';

@Component({
  selector: 'se-parent-support-ticket-create-page',
  imports: [
    FormErrorSummary,
    FormField,
    PortalPageHeader,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-support-ticket-create-page.html',
  styleUrl: './parent-support-ticket-create-page.scss',
})
export class ParentSupportTicketCreatePage {
  private readonly api = inject(ParentSupportTicketsApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly submitting = signal(false);
  protected readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);
  protected readonly categoryOptions = SUPPORT_TICKET_CATEGORY_OPTIONS;
  protected readonly priorityOptions = SUPPORT_TICKET_PRIORITY_OPTIONS;

  readonly form = this.formBuilder.nonNullable.group({
    subject: ['', [requiredTextValidator(), Validators.maxLength(200)]],
    body: ['', [requiredTextValidator(), Validators.maxLength(4000)]],
    category: [SUPPORT_TICKET_CATEGORY_OPTIONS[0].value, [Validators.required]],
    priority: [SupportTicketPriority.Normal, [Validators.required]],
    admissionApplicationId: [''],
  });

  constructor() {
    const applicationId = this.route.snapshot.queryParamMap.get('admissionApplicationId')?.trim();
    if (applicationId && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(applicationId)) {
      this.form.controls.admissionApplicationId.setValue(applicationId);
    }
  }

  protected submit(): void {
    this.summaryErrors.set([]);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set([
        { message: this.transloco.translate('parent.supportTickets.create.validation') },
      ]);
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);
    this.api
      .create({
        subject: raw.subject.trim(),
        body: raw.body.trim(),
        category: Number(raw.category),
        priority: Number(raw.priority),
        admissionApplicationId: raw.admissionApplicationId.trim() || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.submitting.set(false);
          if (result.succeeded && result.data?.id) {
            this.toast.success(this.transloco.translate('parent.supportTickets.create.success'));
            void this.router.navigate(['/parent/support-tickets', result.data.id]);
            return;
          }
          this.summaryErrors.set(mapParentErrorSummaryItems(this.transloco, result.errorCodes));
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.submitting.set(false);
          this.toast.error(this.transloco.translate('parent.errors.generic'));
        },
      });
  }
}
