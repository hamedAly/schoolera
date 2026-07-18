import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  AdmissionApplicationStatus,
  AdminAdmissionApplicationListItemDtoPagedResult,
} from '../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { PortalAdmissionStatusBadge } from '../../../school-portal/components/portal-admission-status-badge/portal-admission-status-badge';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-admin-applications-list-page',
  imports: [
    FormsModule,
    PortalAdmissionStatusBadge,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-applications-list-page.html',
  styleUrl: './admin-applications-list-page.scss',
})
export class AdminApplicationsListPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AdminPlatformApi);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly exporting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<AdminAdmissionApplicationListItemDtoPagedResult | null>(null);

  protected readonly statusFilter = signal('');
  protected readonly search = signal('');
  protected readonly dateFrom = signal('');
  protected readonly dateTo = signal('');
  protected readonly pageNumber = signal(1);

  protected readonly items = computed(() => this.page()?.items ?? []);

  protected readonly statusOptions = [
    { value: '', labelKey: 'admin.applicationsList.filters.allStatuses' },
    { value: String(AdmissionApplicationStatus._1), labelKey: 'parent.applications.status.draft' },
    { value: String(AdmissionApplicationStatus._2), labelKey: 'parent.applications.status.submitted' },
    { value: String(AdmissionApplicationStatus._3), labelKey: 'parent.applications.status.underReview' },
    { value: String(AdmissionApplicationStatus._4), labelKey: 'parent.applications.status.accepted' },
    { value: String(AdmissionApplicationStatus._5), labelKey: 'parent.applications.status.rejected' },
    { value: String(AdmissionApplicationStatus._6), labelKey: 'parent.applications.status.cancelled' },
    { value: '7', labelKey: 'parent.applications.status.missingDocuments' },
    { value: '8', labelKey: 'parent.applications.status.interviewRequired' },
    { value: '9', labelKey: 'parent.applications.status.assessmentRequired' },
    { value: '10', labelKey: 'parent.applications.status.waitingList' },
    { value: '11', labelKey: 'parent.applications.status.registered' },
  ] as const;

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.statusFilter.set(params.get('status') ?? '');
      this.search.set(params.get('search') ?? '');
      this.dateFrom.set(params.get('dateFrom') ?? '');
      this.dateTo.set(params.get('dateTo') ?? '');
      this.pageNumber.set(Number(params.get('page') ?? '1') || 1);
      this.load();
    });
  }

  protected formatDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDate(value) : '—';
  }

  protected applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: this.statusFilter() || null,
        search: this.search().trim() || null,
        dateFrom: this.dateFrom() || null,
        dateTo: this.dateTo() || null,
        page: 1,
      },
      queryParamsHandling: 'merge',
    });
  }

  protected clearFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
    });
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
    const statusRaw = this.statusFilter();
    this.api
      .exportAdmissionApplications({
        search: this.search().trim() || undefined,
        status: statusRaw ? Number(statusRaw) : undefined,
        dateFrom: this.dateFrom() ? `${this.dateFrom()}T00:00:00Z` : undefined,
        dateTo: this.dateTo() ? `${this.dateTo()}T23:59:59Z` : undefined,
        sort: 'newest',
      })
      .pipe(
        finalize(() => this.exporting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () => this.errorMessage.set(translateAdminErrorCodes(this.transloco, ['admin.exportFailed'])),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const statusRaw = this.statusFilter();
    this.api
      .listAdmissionApplications({
        search: this.search().trim() || undefined,
        status: statusRaw ? Number(statusRaw) : undefined,
        dateFrom: this.dateFrom() ? `${this.dateFrom()}T00:00:00Z` : undefined,
        dateTo: this.dateTo() ? `${this.dateTo()}T23:59:59Z` : undefined,
        sort: 'newest',
        pageNumber: this.pageNumber(),
        pageSize: PAGE_SIZE,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.page.set(result.data);
          return;
        }
        this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
