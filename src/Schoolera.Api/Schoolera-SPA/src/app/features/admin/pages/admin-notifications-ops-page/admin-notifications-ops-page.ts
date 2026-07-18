import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import {
  AdminIntegrationsApi,
  NotificationOpsSummaryDto,
} from '../../data-access/admin-integrations.api';

@Component({
  selector: 'se-admin-notifications-ops-page',
  imports: [PortalPageHeader, PortalErrorState, PortalLoadingSkeleton, TranslocoPipe],
  templateUrl: './admin-notifications-ops-page.html',
  styleUrl: './admin-notifications-ops-page.scss',
})
export class AdminNotificationsOpsPage implements OnInit {
  private readonly api = inject(AdminIntegrationsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly summary = signal<NotificationOpsSummaryDto | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getOpsSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.summary.set(result.data);
            return;
          }
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
        },
      });
  }
}
