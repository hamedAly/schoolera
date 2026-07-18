import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PaymentIntentDto } from '../../../../core/api-client/SwaggerClient.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ParentPaymentsApi } from '../../data-access/parent-payments.api';

@Component({
  selector: 'se-parent-payment-return-page',
  imports: [
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-payment-return-page.html',
  styleUrl: './parent-payment-return-page.scss',
})
export class ParentPaymentReturnPage implements OnInit {
  private readonly api = inject(ParentPaymentsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly intent = signal<PaymentIntentDto | null>(null);

  ngOnInit(): void {
    const ref = this.route.snapshot.queryParamMap.get('ref');
    if (!ref) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('parent.payments.errors.missingReturnRef'));
      return;
    }

    this.api
      .processReturn(ref)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.intent.set(result.data);
            return;
          }
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.returnFailed'));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('parent.payments.errors.returnFailed'));
        },
      });
  }
}
