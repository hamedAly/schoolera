import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  ContactRequestDetailDto,
  ContactRequestStatus,
} from '../../../../core/api-client/SwaggerClient.service';
import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { AdminContactRequestsApi } from '../../data-access/admin-contact-requests.api';
import { AdminSupportTicketsApi } from '../../data-access/admin-support-tickets.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';

type ConfirmAction = 'startReview' | 'resolve' | 'close' | null;

@Component({
  selector: 'se-admin-contact-request-detail-page',
  imports: [
    ConfirmationDialog,
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-contact-request-detail-page.html',
  styleUrl: './admin-contact-request-detail-page.scss',
})
export class AdminContactRequestDetailPage implements OnInit {
  private readonly api = inject(AdminContactRequestsApi);
  private readonly supportTicketsApi = inject(AdminSupportTicketsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<ContactRequestDetailDto | null>(null);
  protected readonly confirmAction = signal<ConfirmAction>(null);
  protected readonly confirming = signal(false);
  protected readonly converting = signal(false);
  protected readonly newStatus = ContactRequestStatus._1;
  protected readonly inReviewStatus = ContactRequestStatus._2;

  protected adminNote = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('requestId');
    if (!id) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
      return;
    }
    this.load(id);
  }

  protected load(id: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .get(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.detail.set(result.data);
            this.adminNote = result.data.adminNote ?? '';
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

  protected requestAction(action: ConfirmAction): void {
    this.confirmAction.set(action);
  }

  protected cancelConfirm(): void {
    this.confirmAction.set(null);
  }

  protected confirmPending(): void {
    const action = this.confirmAction();
    const id = this.detail()?.id;
    if (!action || !id) {
      return;
    }

    this.confirming.set(true);
    const body = { adminNote: this.adminNote.trim() || undefined };
    const request =
      action === 'startReview'
        ? this.api.startReview(id, body)
        : action === 'resolve'
          ? this.api.resolve(id, body)
          : this.api.close(id, body);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.confirming.set(false);
        this.confirmAction.set(null);
        if (result.succeeded && result.data) {
          this.detail.set(result.data);
          this.adminNote = result.data.adminNote ?? '';
          this.toast.success(
            this.transloco.translate(`admin.contactRequests.detail.${action}Success`),
          );
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => {
        this.confirming.set(false);
        this.confirmAction.set(null);
        this.toast.error(this.transloco.translate('admin.errors.generic'));
      },
    });
  }

  protected statusKey(status: ContactRequestStatus | undefined): string {
    switch (status) {
      case ContactRequestStatus._1:
        return 'admin.contactRequests.status.new';
      case ContactRequestStatus._2:
        return 'admin.contactRequests.status.inReview';
      case ContactRequestStatus._3:
        return 'admin.contactRequests.status.resolved';
      case ContactRequestStatus._4:
        return 'admin.contactRequests.status.closed';
      default:
        return 'admin.common.unknown';
    }
  }

  protected categoryKey(category: string | undefined): string {
    return category
      ? `admin.contactRequests.categories.${category}`
      : 'admin.common.unknown';
  }

  protected confirmTitleKey(): string {
    const action = this.confirmAction();
    return action
      ? `admin.contactRequests.detail.confirm.${action}Title`
      : 'admin.contactRequests.detail.confirm.startReviewTitle';
  }

  protected confirmMessageKey(): string {
    const action = this.confirmAction();
    return action
      ? `admin.contactRequests.detail.confirm.${action}Message`
      : 'admin.contactRequests.detail.confirm.startReviewMessage';
  }

  protected convertToTicket(): void {
    const id = this.detail()?.id;
    if (!id || this.converting()) {
      return;
    }
    this.converting.set(true);
    this.supportTicketsApi
      .convertContact(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.converting.set(false);
          if (result.succeeded && result.data?.id) {
            this.toast.success(
              this.transloco.translate('admin.contactRequests.detail.convertSuccess'),
            );
            void this.router.navigate(['/admin/support-tickets', result.data.id]);
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.converting.set(false);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }
}
