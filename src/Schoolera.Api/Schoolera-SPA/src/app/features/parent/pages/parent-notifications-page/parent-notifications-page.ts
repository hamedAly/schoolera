import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import {
  ParentInAppNotificationDto,
  ParentNotificationPagedResult,
  ParentNotificationsApi,
} from '../../data-access/parent-notifications.api';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-parent-notifications-page',
  imports: [
    DatePipe,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-notifications-page.html',
  styleUrl: './parent-notifications-page.scss',
})
export class ParentNotificationsPage implements OnInit {
  private readonly api = inject(ParentNotificationsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<ParentNotificationPagedResult | null>(null);
  protected readonly pageNumber = signal(1);
  protected readonly unreadCount = signal(0);

  protected readonly items = computed(() => this.page()?.items ?? []);

  ngOnInit(): void {
    this.load();
    this.loadUnread();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listNotifications(this.pageNumber(), PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.page.set(result.data);
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

  protected loadUnread(): void {
    this.api
      .unreadCount()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data != null) {
          this.unreadCount.set(result.data);
        }
      });
  }

  protected markRead(item: ParentInAppNotificationDto): void {
    if (!item.id || item.isRead || this.acting()) {
      return;
    }
    this.acting.set(true);
    this.api
      .markRead(item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded) {
            this.load();
            this.loadUnread();
            return;
          }
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  protected markAllRead(): void {
    if (this.acting()) {
      return;
    }
    this.acting.set(true);
    this.api
      .markAllRead()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded) {
            this.toast.success(this.transloco.translate('parent.notifications.markedAllRead'));
            this.load();
            this.loadUnread();
            return;
          }
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  protected previousPage(): void {
    if (this.page()?.hasPreviousPage) {
      this.pageNumber.update((n) => n - 1);
      this.load();
    }
  }

  protected nextPage(): void {
    if (this.page()?.hasNextPage) {
      this.pageNumber.update((n) => n + 1);
      this.load();
    }
  }
}
