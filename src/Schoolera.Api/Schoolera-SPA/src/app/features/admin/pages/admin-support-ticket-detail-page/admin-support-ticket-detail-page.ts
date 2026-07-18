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
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminSupportTicketsApi } from '../../data-access/admin-support-tickets.api';
import {
  SupportTicketMessageVisibility,
  SupportTicketSupportDetailDto,
} from '../../../support/data-access/support-ticket.models';
import {
  supportTicketCategoryKey,
  supportTicketPriorityKey,
  supportTicketStatusKey,
} from '../../../support/data-access/support-ticket-labels';

@Component({
  selector: 'se-admin-support-ticket-detail-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-support-ticket-detail-page.html',
  styleUrl: './admin-support-ticket-detail-page.scss',
})
export class AdminSupportTicketDetailPage implements OnInit {
  private readonly api = inject(AdminSupportTicketsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<SupportTicketSupportDetailDto | null>(null);
  protected assignAgentId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('ticketId');
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
            this.detail.set(result.data as SupportTicketSupportDetailDto);
            this.assignAgentId = result.data.assignedSupportAgentUserId ?? '';
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

  protected statusKey(status: number): string {
    return `admin.supportTickets.${supportTicketStatusKey(status)}`;
  }

  protected priorityKey(priority: number): string {
    return `admin.supportTickets.${supportTicketPriorityKey(priority)}`;
  }

  protected categoryKey(category: number): string {
    return `admin.supportTickets.${supportTicketCategoryKey(category)}`;
  }

  protected isInternal(visibility: number): boolean {
    return visibility === SupportTicketMessageVisibility.InternalSupportNote;
  }

  protected assign(): void {
    const id = this.detail()?.id;
    const agentId = this.assignAgentId.trim();
    if (!id || !agentId || this.acting()) {
      return;
    }
    this.acting.set(true);
    this.api
      .assign(id, { supportAgentUserId: agentId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded && result.data) {
            this.detail.set(result.data as SupportTicketSupportDetailDto);
            this.toast.success(this.transloco.translate('admin.supportTickets.detail.assignSuccess'));
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }
}
