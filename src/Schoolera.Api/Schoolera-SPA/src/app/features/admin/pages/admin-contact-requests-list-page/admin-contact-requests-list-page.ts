import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  ContactRequestListItemDto,
  ContactRequestListItemDtoPagedResult,
  ContactRequestStatus,
} from '../../../../core/api-client/SwaggerClient.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { AdminContactRequestsApi } from '../../data-access/admin-contact-requests.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';

const PAGE_SIZE = 20;

const CATEGORIES = [
  '',
  'general',
  'parent-support',
  'school-partnership',
  'technical',
  'billing',
  'other',
] as const;

@Component({
  selector: 'se-admin-contact-requests-list-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-contact-requests-list-page.html',
  styleUrl: './admin-contact-requests-list-page.scss',
})
export class AdminContactRequestsListPage implements OnInit {
  private readonly api = inject(AdminContactRequestsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<ContactRequestListItemDtoPagedResult | null>(null);
  protected readonly pageNumber = signal(1);

  protected searchTerm = '';
  protected statusFilter = '';
  protected categoryFilter = '';
  protected dateFrom = '';
  protected dateTo = '';

  protected readonly categories = CATEGORIES;
  protected readonly statusOptions = ['', 'New', 'InReview', 'Resolved', 'Closed'] as const;
  protected readonly items = computed(() => {
    const all = this.page()?.items ?? [];
    return all.filter((item) => this.matchesDateFilter(item));
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .list(
        this.searchTerm.trim() || undefined,
        this.statusFilter || undefined,
        this.categoryFilter || undefined,
        this.pageNumber(),
        PAGE_SIZE,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.page.set(result.data);
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

  protected applyFilters(): void {
    this.pageNumber.set(1);
    this.load();
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

  /** Client-side date filter — API list query has no from/to params. */
  private matchesDateFilter(item: ContactRequestListItemDto): boolean {
    if (!this.dateFrom && !this.dateTo) {
      return true;
    }
    if (!item.createdAtUtc) {
      return false;
    }
    const created = new Date(item.createdAtUtc).getTime();
    if (this.dateFrom) {
      const from = new Date(this.dateFrom).setHours(0, 0, 0, 0);
      if (created < from) {
        return false;
      }
    }
    if (this.dateTo) {
      const to = new Date(this.dateTo).setHours(23, 59, 59, 999);
      if (created > to) {
        return false;
      }
    }
    return true;
  }
}
