import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import {
  NotificationChannel,
  ParentAdmissionOpenSubscriptionDto,
  ParentNotificationsApi,
} from '../../data-access/parent-notifications.api';

@Component({
  selector: 'se-parent-admission-subscriptions-page',
  imports: [
    ConfirmationDialog,
    DatePipe,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    TranslocoPipe,
  ],
  templateUrl: './parent-admission-subscriptions-page.html',
  styleUrl: './parent-admission-subscriptions-page.scss',
})
export class ParentAdmissionSubscriptionsPage implements OnInit {
  private readonly api = inject(ParentNotificationsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<ParentAdmissionOpenSubscriptionDto[]>([]);
  protected readonly unsubscribeId = signal<string | null>(null);
  protected readonly confirming = signal(false);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listSubscriptions()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.items.set(result.data);
            return;
          }
          this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  protected requestUnsubscribe(id: string | undefined): void {
    if (!id) {
      return;
    }
    this.unsubscribeId.set(id);
  }

  protected cancelUnsubscribe(): void {
    this.unsubscribeId.set(null);
  }

  protected confirmUnsubscribe(): void {
    const id = this.unsubscribeId();
    if (!id) {
      return;
    }

    this.confirming.set(true);
    this.api
      .unsubscribe(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.confirming.set(false);
          this.unsubscribeId.set(null);
          if (result.succeeded) {
            this.toast.success(this.transloco.translate('parent.subscriptions.unsubscribed'));
            this.load();
            return;
          }
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.confirming.set(false);
          this.unsubscribeId.set(null);
          this.toast.error(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  protected channelKey(channel: NotificationChannel | undefined): string {
    switch (channel) {
      case NotificationChannel._2:
        return 'parent.subscriptions.channels.email';
      case NotificationChannel._3:
        return 'parent.subscriptions.channels.sms';
      case NotificationChannel._4:
        return 'parent.subscriptions.channels.whatsApp';
      default:
        return 'parent.subscriptions.channels.inApp';
    }
  }
}
