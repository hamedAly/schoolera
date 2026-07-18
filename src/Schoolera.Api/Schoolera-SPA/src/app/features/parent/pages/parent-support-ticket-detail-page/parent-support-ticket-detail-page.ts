import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import {
  SupportTicketMessageVisibility,
  SupportTicketParentDetailDto,
  SupportTicketStatus,
} from '../../../support/data-access/support-ticket.models';
import {
  supportTicketCategoryKey,
  supportTicketPriorityKey,
  supportTicketStatusKey,
} from '../../../support/data-access/support-ticket-labels';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import { ParentSupportTicketsApi } from '../../data-access/parent-support-tickets.api';

@Component({
  selector: 'se-parent-support-ticket-detail-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-support-ticket-detail-page.html',
  styleUrl: './parent-support-ticket-detail-page.scss',
})
export class ParentSupportTicketDetailPage implements OnInit {
  private readonly api = inject(ParentSupportTicketsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<SupportTicketParentDetailDto | null>(null);
  protected replyBody = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('ticketId');
    if (!id) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('parent.errors.generic'));
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
            this.detail.set(result.data as SupportTicketParentDetailDto);
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

  protected statusKey(status: number): string {
    return `parent.supportTickets.${supportTicketStatusKey(status)}`;
  }

  protected priorityKey(priority: number): string {
    return `parent.supportTickets.${supportTicketPriorityKey(priority)}`;
  }

  protected categoryKey(category: number): string {
    return `parent.supportTickets.${supportTicketCategoryKey(category)}`;
  }

  protected canReply(): boolean {
    const status = this.detail()?.status;
    return (
      status === SupportTicketStatus.Open ||
      status === SupportTicketStatus.InProgress ||
      status === SupportTicketStatus.WaitingForCustomer
    );
  }

  protected canReopen(): boolean {
    return this.detail()?.status === SupportTicketStatus.Resolved;
  }

  protected visibleMessages() {
    return (this.detail()?.messages ?? []).filter(
      (m) => m.visibility === SupportTicketMessageVisibility.CustomerVisible,
    );
  }

  protected sendReply(): void {
    const id = this.detail()?.id;
    const body = this.replyBody.trim();
    if (!id || !body || this.acting()) {
      return;
    }
    this.acting.set(true);
    this.api
      .reply(id, { body })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded && result.data) {
            this.detail.set(result.data as SupportTicketParentDetailDto);
            this.replyBody = '';
            this.toast.success(this.transloco.translate('parent.supportTickets.detail.replySuccess'));
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

  protected reopen(): void {
    const id = this.detail()?.id;
    if (!id || this.acting()) {
      return;
    }
    this.acting.set(true);
    this.api
      .reopen(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded && result.data) {
            this.detail.set(result.data as SupportTicketParentDetailDto);
            this.toast.success(this.transloco.translate('parent.supportTickets.detail.reopenSuccess'));
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
}
