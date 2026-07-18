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
  SUPPORT_TICKET_CATEGORY_OPTIONS,
  SUPPORT_TICKET_PRIORITY_OPTIONS,
  SUPPORT_TICKET_STATUS_OPTIONS,
  SupportTicketMessageVisibility,
  SupportTicketSupportDetailDto,
} from '../../data-access/support-ticket.models';
import {
  supportTicketCategoryKey,
  supportTicketPriorityKey,
  supportTicketStatusKey,
} from '../../data-access/support-ticket-labels';
import { SupportTicketsApi } from '../../data-access/support-tickets.api';

@Component({
  selector: 'se-support-ticket-detail-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './support-ticket-detail-page.html',
  styleUrl: './support-ticket-detail-page.scss',
})
export class SupportTicketDetailPage implements OnInit {
  private readonly api = inject(SupportTicketsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<SupportTicketSupportDetailDto | null>(null);

  protected readonly statusOptions = SUPPORT_TICKET_STATUS_OPTIONS;
  protected readonly priorityOptions = SUPPORT_TICKET_PRIORITY_OPTIONS;
  protected readonly categoryOptions = SUPPORT_TICKET_CATEGORY_OPTIONS;

  protected replyBody = '';
  protected noteBody = '';
  protected statusValue = '';
  protected priorityValue = '';
  protected categoryValue = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('ticketId');
    if (!id) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('support.errors.generic'));
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
            this.applyDetail(result.data as SupportTicketSupportDetailDto);
            return;
          }
          this.errorMessage.set(this.transloco.translate('support.errors.generic'));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('support.errors.generic'));
        },
      });
  }

  protected statusKey(status: number): string {
    return `support.${supportTicketStatusKey(status)}`;
  }

  protected priorityKey(priority: number): string {
    return `support.${supportTicketPriorityKey(priority)}`;
  }

  protected categoryKey(category: number): string {
    return `support.${supportTicketCategoryKey(category)}`;
  }

  protected isInternal(visibility: number): boolean {
    return visibility === SupportTicketMessageVisibility.InternalSupportNote;
  }

  protected assignToMe(): void {
    this.runAction((id) => this.api.assignToMe(id), 'support.tickets.detail.assignSuccess');
  }

  protected unassign(): void {
    this.runAction((id) => this.api.unassign(id), 'support.tickets.detail.unassignSuccess');
  }

  protected changeStatus(): void {
    const status = Number(this.statusValue);
    if (!status) {
      return;
    }
    this.runAction(
      (id) => this.api.changeStatus(id, { status }),
      'support.tickets.detail.statusSuccess',
    );
  }

  protected changePriority(): void {
    const priority = Number(this.priorityValue);
    if (!priority) {
      return;
    }
    this.runAction(
      (id) => this.api.changePriority(id, { priority }),
      'support.tickets.detail.prioritySuccess',
    );
  }

  protected changeCategory(): void {
    const category = Number(this.categoryValue);
    if (!category) {
      return;
    }
    this.runAction(
      (id) => this.api.changeCategory(id, { category }),
      'support.tickets.detail.categorySuccess',
    );
  }

  protected sendReply(): void {
    const body = this.replyBody.trim();
    if (!body) {
      return;
    }
    this.runAction(
      (id) => this.api.reply(id, { body }),
      'support.tickets.detail.replySuccess',
      () => {
        this.replyBody = '';
      },
    );
  }

  protected addNote(): void {
    const body = this.noteBody.trim();
    if (!body) {
      return;
    }
    this.runAction(
      (id) => this.api.addInternalNote(id, { body }),
      'support.tickets.detail.noteSuccess',
      () => {
        this.noteBody = '';
      },
    );
  }

  private applyDetail(data: SupportTicketSupportDetailDto): void {
    this.detail.set(data);
    this.statusValue = String(data.status);
    this.priorityValue = String(data.priority);
    this.categoryValue = String(data.category);
  }

  private runAction(
    factory: (id: string) => ReturnType<SupportTicketsApi['assignToMe']>,
    successKey: string,
    onSuccess?: () => void,
  ): void {
    const id = this.detail()?.id;
    if (!id || this.acting()) {
      return;
    }
    this.acting.set(true);
    factory(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded && result.data) {
            this.applyDetail(result.data as SupportTicketSupportDetailDto);
            onSuccess?.();
            this.toast.success(this.transloco.translate(successKey));
            return;
          }
          this.toast.error(this.transloco.translate('support.errors.generic'));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('support.errors.generic'));
        },
      });
  }
}
