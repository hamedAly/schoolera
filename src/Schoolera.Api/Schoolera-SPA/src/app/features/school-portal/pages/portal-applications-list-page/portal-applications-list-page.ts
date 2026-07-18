import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import {
  AdmissionApplicationStatus,
  SchoolAdmissionApplicationListItemDto,
  SchoolAdmissionApplicationListItemDtoPagedResult,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { resolveSchooleraLang } from '../../../../core/i18n/schoolera-lang';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { translateAdmissionErrorCodes } from '../../../parent/data-access/admission-errors';
import { PortalAdmissionStatusBadge } from '../../components/portal-admission-status-badge/portal-admission-status-badge';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { SchoolBranchDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-portal-applications-list-page',
  imports: [
    FormsModule,
    PortalAdmissionStatusBadge,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './portal-applications-list-page.html',
  styleUrl: './portal-applications-list-page.scss',
})
export class PortalApplicationsListPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<SchoolAdmissionApplicationListItemDtoPagedResult | null>(null);
  protected readonly branches = signal<readonly SchoolBranchDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);

  protected readonly statusFilter = signal('');
  protected readonly branchFilter = signal('');
  protected readonly gradeFilter = signal('');
  protected readonly search = signal('');
  protected readonly dateFrom = signal('');
  protected readonly dateTo = signal('');
  protected readonly pageNumber = signal(1);

  protected readonly items = computed(() => this.page()?.items ?? []);
  protected readonly schoolId = computed(() => this.route.parent?.snapshot.paramMap.get('schoolId') ?? '');

  protected readonly statusOptions = [
    { value: '', labelKey: 'portal.applications.list.filters.allStatuses' },
    { value: String(AdmissionApplicationStatus._2), labelKey: 'parent.applications.status.submitted' },
    { value: String(AdmissionApplicationStatus._3), labelKey: 'parent.applications.status.underReview' },
    { value: String(AdmissionApplicationStatus._4), labelKey: 'parent.applications.status.accepted' },
    { value: String(AdmissionApplicationStatus._5), labelKey: 'parent.applications.status.rejected' },
    { value: String(AdmissionApplicationStatus._6), labelKey: 'parent.applications.status.cancelled' },
  ] as const;

  ngOnInit(): void {
    const schoolId = this.schoolId();
    if (schoolId) {
      this.api
        .listBranches(schoolId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (result.succeeded && result.data) {
            this.branches.set(result.data);
          }
        });
    }

    this.taxonomiesApi
      .getEducationalStages()
      .pipe(
        switchMap((result) => {
          const stageIds = (result.data ?? []).map((stage) => stage.id).filter((id): id is string => !!id);
          if (!stageIds.length) {
            return of([] as TaxonomyItemDto[]);
          }

          return forkJoin(stageIds.map((stageId) => this.taxonomiesApi.getGradesByStage(stageId))).pipe(
            switchMap((gradeResults) => {
              const byId = new Map<string, TaxonomyItemDto>();
              for (const gradeResult of gradeResults) {
                for (const grade of gradeResult.data ?? []) {
                  if (grade.id) {
                    byId.set(grade.id, grade);
                  }
                }
              }
              return of([...byId.values()]);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((grades) => this.grades.set(grades));

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.statusFilter.set(params.get('status') ?? '');
      this.branchFilter.set(params.get('branchId') ?? '');
      this.gradeFilter.set(params.get('gradeId') ?? '');
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

  protected branchLabel(branch: SchoolBranchDto): string {
    return localizedBilingualName(
      branch.nameAr,
      branch.nameEn,
      resolveSchooleraLang(this.transloco.getActiveLang()),
    );
  }

  protected gradeLabel(grade: TaxonomyItemDto): string {
    return grade.name?.trim() ?? '';
  }

  protected applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: this.statusFilter() || null,
        branchId: this.branchFilter() || null,
        gradeId: this.gradeFilter() || null,
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

  protected detailLink(item: SchoolAdmissionApplicationListItemDto): string[] {
    return ['/school', this.schoolId(), 'applications', item.id ?? ''];
  }

  private load(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const statusRaw = this.statusFilter();
    this.api
      .listAdmissionApplications(schoolId, {
        status: statusRaw ? Number(statusRaw) : undefined,
        branchId: this.branchFilter() || undefined,
        gradeId: this.gradeFilter() || undefined,
        search: this.search().trim() || undefined,
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
        this.errorMessage.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
