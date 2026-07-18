import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import {
  SUPPORT_TICKET_PRIORITY_OPTIONS,
  SUPPORT_TICKET_STATUS_OPTIONS,
  SupportTicketPagedResult,
} from '../../../support/data-access/support-ticket.models';
import {
  supportTicketCategoryKey,
  supportTicketPriorityKey,
  supportTicketStatusKey,
} from '../../../support/data-access/support-ticket-labels';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import { ParentSupportTicketsApi } from '../../data-access/parent-support-tickets.api';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-parent-support-tickets-list-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-support-tickets-list-page.html',
  styleUrl: './parent-support-tickets-list-page.scss',
})
export class ParentSupportTicketsListPage implements OnInit {
  private readonly api = inject(ParentSupportTicketsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<SupportTicketPagedResult | null>(null);
  protected readonly statusFilter = signal('');
  protected readonly priorityFilter = signal('');
  protected readonly pageNumber = signal(1);

  protected readonly items = computed(() => this.page()?.items ?? []);
  protected readonly statusOptions = SUPPORT_TICKET_STATUS_OPTIONS;
  protected readonly priorityOptions = SUPPORT_TICKET_PRIORITY_OPTIONS;

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.statusFilter.set(params.get('status') ?? '');
      this.priorityFilter.set(params.get('priority') ?? '');
      this.pageNumber.set(Number(params.get('page') ?? '1') || 1);
      this.load();
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

  protected applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: this.statusFilter() || null,
        priority: this.priorityFilter() || null,
        page: 1,
      },
      queryParamsHandling: 'merge',
    });
  }

  protected clearFilters(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  protected previousPage(): void {
    if (this.page()?.hasPreviousPage) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: this.pageNumber() - 1 },
        queryParamsHandling: 'merge',
      });
    }
  }

  protected nextPage(): void {
    if (this.page()?.hasNextPage) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: this.pageNumber() + 1 },
        queryParamsHandling: 'merge',
      });
    }
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    const statusRaw = this.statusFilter();
    const priorityRaw = this.priorityFilter();

    this.api
      .list({
        status: statusRaw ? Number(statusRaw) : undefined,
        priority: priorityRaw ? Number(priorityRaw) : undefined,
        pageNumber: this.pageNumber(),
        pageSize: PAGE_SIZE,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.page.set(result.data as SupportTicketPagedResult);
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
}
