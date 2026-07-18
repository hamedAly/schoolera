import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  AdmissionApplicationListItemDto,
  AdmissionApplicationStatus,
  ChildProfileListItemDto,
} from '../../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { PortalEmptyState } from '../../../../school-portal/components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdmissionErrorCodes } from '../../../data-access/admission-errors';
import { isDraftStatus } from '../../../data-access/admission-status';
import { ParentApi } from '../../../data-access/parent.api';
import { ApplicationStatusBadge } from '../../components/application-status-badge/application-status-badge';

@Component({
  selector: 'se-applications-list-page',
  imports: [
    ApplicationStatusBadge,
    FormsModule,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './applications-list-page.html',
  styleUrl: './applications-list-page.scss',
})
export class ApplicationsListPage implements OnInit {
  private readonly api = inject(ParentApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<AdmissionApplicationListItemDto[]>([]);
  protected readonly children = signal<ChildProfileListItemDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageNumber = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly totalPages = signal(1);

  protected readonly statusFilter = signal<string>('');
  protected readonly childFilter = signal<string>('');
  protected readonly search = signal('');

  protected readonly statusOptions = [
    { value: '', labelKey: 'parent.applications.list.filters.allStatuses' },
    { value: String(AdmissionApplicationStatus._1), labelKey: 'parent.applications.status.draft' },
    { value: String(AdmissionApplicationStatus._2), labelKey: 'parent.applications.status.submitted' },
    { value: String(AdmissionApplicationStatus._3), labelKey: 'parent.applications.status.underReview' },
    { value: String(AdmissionApplicationStatus._4), labelKey: 'parent.applications.status.accepted' },
    { value: String(AdmissionApplicationStatus._5), labelKey: 'parent.applications.status.rejected' },
    { value: String(AdmissionApplicationStatus._6), labelKey: 'parent.applications.status.cancelled' },
  ] as const;

  ngOnInit(): void {
    this.api
      .listChildren()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.children.set(result.data.filter((child) => child.isActive !== false));
        }
      });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.statusFilter.set(params.get('status') ?? '');
      this.childFilter.set(params.get('childId') ?? '');
      this.search.set(params.get('search') ?? '');
      this.pageNumber.set(Number(params.get('page') ?? '1') || 1);
      this.load();
    });
  }

  protected isDraft(status: number | undefined): boolean {
    return isDraftStatus(status);
  }

  protected formatDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDate(value) : '—';
  }

  protected applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: this.statusFilter() || null,
        childId: this.childFilter() || null,
        search: this.search().trim() || null,
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

  protected goToPage(page: number): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page },
      queryParamsHandling: 'merge',
    });
  }

  protected retry(): void {
    this.load();
  }

  protected onSearchInput(value: string): void {
    this.search.set(value);
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const statusRaw = this.statusFilter();
    this.api
      .listAdmissionApplications({
        status: statusRaw ? Number(statusRaw) : undefined,
        childProfileId: this.childFilter() || undefined,
        search: this.search().trim() || undefined,
        sort: 'newest',
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        this.items.set(result.data.items ?? []);
        this.totalCount.set(result.data.totalCount ?? 0);
        this.pageNumber.set(result.data.pageNumber ?? 1);
        this.pageSize.set(result.data.pageSize ?? 10);
        this.totalPages.set(result.data.totalPages ?? 1);
      });
  }
}
