import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminSupportTicketsApi } from '../../data-access/admin-support-tickets.api';
import {
  SUPPORT_TICKET_CATEGORY_OPTIONS,
  SUPPORT_TICKET_PRIORITY_OPTIONS,
  SUPPORT_TICKET_STATUS_OPTIONS,
  SupportTicketPagedResult,
} from '../../../support/data-access/support-ticket.models';
import {
  supportTicketCategoryKey,
  supportTicketPriorityKey,
  supportTicketStatusKey,
} from '../../../support/data-access/support-ticket-labels';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-admin-support-tickets-list-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-support-tickets-list-page.html',
  styleUrl: './admin-support-tickets-list-page.scss',
})
export class AdminSupportTicketsListPage implements OnInit {
  private readonly api = inject(AdminSupportTicketsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly exporting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<SupportTicketPagedResult | null>(null);

  protected readonly search = signal('');
  protected readonly statusFilter = signal('');
  protected readonly priorityFilter = signal('');
  protected readonly categoryFilter = signal('');
  protected readonly unassignedOnly = signal(false);
  protected readonly firstResponseOverdueOnly = signal(false);
  protected readonly resolutionOverdueOnly = signal(false);
  protected readonly pageNumber = signal(1);

  protected readonly items = computed(() => this.page()?.items ?? []);
  protected readonly statusOptions = SUPPORT_TICKET_STATUS_OPTIONS;
  protected readonly priorityOptions = SUPPORT_TICKET_PRIORITY_OPTIONS;
  protected readonly categoryOptions = SUPPORT_TICKET_CATEGORY_OPTIONS;

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.search.set(params.get('search') ?? '');
      this.statusFilter.set(params.get('status') ?? '');
      this.priorityFilter.set(params.get('priority') ?? '');
      this.categoryFilter.set(params.get('category') ?? '');
      this.unassignedOnly.set(params.get('unassignedOnly') === 'true');
      this.firstResponseOverdueOnly.set(params.get('firstResponseOverdueOnly') === 'true');
      this.resolutionOverdueOnly.set(params.get('resolutionOverdueOnly') === 'true');
      this.pageNumber.set(Number(params.get('page') ?? '1') || 1);
      this.load();
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

  protected applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.search().trim() || null,
        status: this.statusFilter() || null,
        priority: this.priorityFilter() || null,
        category: this.categoryFilter() || null,
        unassignedOnly: this.unassignedOnly() ? true : null,
        firstResponseOverdueOnly: this.firstResponseOverdueOnly() ? true : null,
        resolutionOverdueOnly: this.resolutionOverdueOnly() ? true : null,
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

  protected exportCsv(): void {
    this.exporting.set(true);
    this.api
      .exportCsv({
        search: this.search().trim() || undefined,
        status: this.statusFilter() ? Number(this.statusFilter()) : undefined,
        priority: this.priorityFilter() ? Number(this.priorityFilter()) : undefined,
        category: this.categoryFilter() ? Number(this.categoryFilter()) : undefined,
        unassignedOnly: this.unassignedOnly() || undefined,
        firstResponseOverdueOnly: this.firstResponseOverdueOnly() || undefined,
        resolutionOverdueOnly: this.resolutionOverdueOnly() || undefined,
      })
      .pipe(
        finalize(() => this.exporting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () =>
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, ['admin.exportFailed'])),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api
      .list({
        search: this.search().trim() || undefined,
        status: this.statusFilter() ? Number(this.statusFilter()) : undefined,
        priority: this.priorityFilter() ? Number(this.priorityFilter()) : undefined,
        category: this.categoryFilter() ? Number(this.categoryFilter()) : undefined,
        unassignedOnly: this.unassignedOnly() || undefined,
        firstResponseOverdueOnly: this.firstResponseOverdueOnly() || undefined,
        resolutionOverdueOnly: this.resolutionOverdueOnly() || undefined,
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
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
        },
      });
  }
}
