import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  CreatePaymentIntentRequest,
  PaymentIntentDto,
  PaymentMethodKind,
  PaymentSummaryDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ParentPaymentsApi } from '../../data-access/parent-payments.api';

@Component({
  selector: 'se-parent-payments-page',
  imports: [
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    FormsModule,
    TranslocoPipe,
  ],
  templateUrl: './parent-payments-page.html',
  styleUrl: './parent-payments-page.scss',
})
export class ParentPaymentsPage implements OnInit {
  private readonly api = inject(ParentPaymentsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly summary = signal<PaymentSummaryDto | null>(null);
  protected readonly intents = signal<PaymentIntentDto[]>([]);
  protected readonly termsAccepted = signal(false);
  protected readonly privacyAccepted = signal(false);
  protected readonly selectedProviderId = signal<string | null>(null);

  ngOnInit(): void {
    const payableId = this.route.snapshot.queryParamMap.get('payableItemId');
    if (payableId) {
      this.loadSummary(payableId);
    } else {
      this.loadHistory();
    }
  }

  protected loadHistory(): void {
    this.loading.set(true);
    this.api
      .listIntents()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.intents.set(result.data);
            return;
          }
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.loadFailed'));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.loadFailed'));
        },
      });
  }

  protected loadSummary(payableItemId: string): void {
    this.loading.set(true);
    this.api
      .getSummary(payableItemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.summary.set(result.data);
            const def = result.data.availableProviders?.find((p) => p.isDefault)
              ?? result.data.availableProviders?.[0];
            this.selectedProviderId.set(def?.integrationId ?? null);
            return;
          }
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.loadFailed'));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.loadFailed'));
        },
      });
  }

  protected payNow(): void {
    const summary = this.summary();
    if (!summary || !this.termsAccepted() || !this.privacyAccepted()) {
      return;
    }

    this.submitting.set(true);
    const body: CreatePaymentIntentRequest = {
      payableItemId: summary.payableItemId,
      integrationConfigurationId: this.selectedProviderId() ?? undefined,
      paymentMethod: PaymentMethodKind._5,
      idempotencyKey: crypto.randomUUID(),
      termsAccepted: true,
      privacyAccepted: true,
    };

    this.api
      .createIntent(body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.submitting.set(false);
          if (result.succeeded && result.data?.redirectUrl) {
            // Relative hosted/sandbox return path only.
            void this.router.navigateByUrl(result.data.redirectUrl);
            return;
          }
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.payFailed'));
        },
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.payFailed'));
        },
      });
  }
}
