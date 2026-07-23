import { NgTemplateOutlet } from '@angular/common';
import {
  Component,
  computed,
  DestroyRef,
  HostListener,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  EMPTY,
  map,
  merge,
  of,
  shareReplay,
  Subject,
  switchMap,
  take,
} from 'rxjs';

import {
  GenderType,
  PublicSchoolListItemDto,
  SchoolType,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { hasAnyRole, SchooleraRoles } from '../../../../core/auth/auth.models';
import { FeatureFlagsService } from '../../../../core/features/feature-flags.service';
import { isHttpRequestCanceled } from '../../../../core/http/is-http-canceled';
import { switchMapLatestLoading } from '../../../../core/http/switch-map-latest-loading';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { HomeIcon } from '../../../home/components/home-icon/home-icon';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import {
  LocationSelection,
  LocationSelector,
} from '../../../../shared/ui/location-selector/location-selector';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { ParentFavoritesApi } from '../../../parent/data-access/parent-favorites.api';
import { translateParentErrorCodes } from '../../../parent/data-access/parent-errors';
import { SchoolsApi } from '../../data-access/schools.api';
import { SchoolsMapPanel } from '../../components/schools-map-panel/schools-map-panel';
import type { MapViewportBounds } from '../../data-access/schools-map-leaflet.adapter';
import {
  areSchoolsSearchQueriesEqual,
  clearsPageOnChange,
  countActiveSchoolFilters,
  createDefaultSchoolsSearchQuery,
  parseSchoolsSearchQuery,
  SCHOOLS_SORT_OPTIONS,
  SchoolsSearchQuery,
  serializeSchoolsSearchQuery,
} from '../../data-access/schools-search-query';

type ExtendedSchoolListItem = PublicSchoolListItemDto & {
  coverUrl?: string;
  district?: string;
  curriculumSummary?: string[];
  educationalStageSummary?: string[];
  minimumAnnualFee?: number;
  feeCurrency?: string;
  hasPublishedFees?: boolean;
  feesRequireLogin?: boolean;
  distanceKm?: number;
  isFavorite?: boolean | null;
};

const SCHOOL_CARD_FALLBACK = 'assets/schools/demo-1.svg';

@Component({
  selector: 'se-school-list-page',
  imports: [
    FormsModule,
    HomeIcon,
    LocationSelector,
    NgTemplateOutlet,
    PublicPageContainer,
    RouterLink,
    SchoolsMapPanel,
    TranslocoPipe,
  ],
  templateUrl: './school-list-page.html',
  styleUrl: './school-list-page.scss',
})
export class SchoolListPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly schoolsApi = inject(SchoolsApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly parentFavoritesApi = inject(ParentFavoritesApi);
  private readonly auth = inject(AuthService);
  private readonly featureFlags = inject(FeatureFlagsService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchDraft$ = new Subject<string>();
  private readonly retry$ = new Subject<void>();

  protected readonly schoolCardFallback = SCHOOL_CARD_FALLBACK;
  protected readonly favoritesEnabled = this.featureFlags.favoritesEnabled;
  protected readonly favoriteBusyIds = signal<ReadonlySet<string>>(new Set());
  protected readonly favoriteOverrides = signal<ReadonlyMap<string, boolean>>(new Map());
  protected readonly sortOptions = SCHOOLS_SORT_OPTIONS;
  protected readonly schoolTypes = [
    { value: SchoolType._1, labelKey: 'schools.enums.schoolType.private' },
    { value: SchoolType._2, labelKey: 'schools.enums.schoolType.international' },
    { value: SchoolType._3, labelKey: 'schools.enums.schoolType.national' },
    { value: SchoolType._4, labelKey: 'schools.enums.schoolType.language' },
  ] as const;
  protected readonly genderTypes = [
    { value: GenderType._1, labelKey: 'schools.enums.genderType.boys' },
    { value: GenderType._2, labelKey: 'schools.enums.genderType.girls' },
    { value: GenderType._3, labelKey: 'schools.enums.genderType.mixed' },
  ] as const;

  protected readonly query = signal<SchoolsSearchQuery>(createDefaultSchoolsSearchQuery());
  protected readonly searchDraft = signal('');
  protected readonly schools = signal<ReadonlyArray<ExtendedSchoolListItem>>([]);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly mobileFiltersOpen = signal(false);
  protected readonly geolocationStatusKey = signal<string | null>(null);

  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);
  protected readonly curricula = signal<TaxonomyItemDto[]>([]);
  protected readonly facilities = signal<TaxonomyItemDto[]>([]);
  protected readonly academicYears = signal<TaxonomyItemDto[]>([]);

  protected readonly activeFilterCount = computed(() => countActiveSchoolFilters(this.query()));
  protected readonly hasActiveFilters = computed(() => this.activeFilterCount() > 0);
  protected readonly showPagination = computed(() => this.totalPages() > 1 && !this.loading() && !this.error());

  ngOnInit(): void {
    this.loadTaxonomies();
    this.bindSearchDraft();
    this.bindResultsStream();
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.mobileFiltersOpen()) {
      this.closeMobileFilters();
    }
  }

  protected taxonomyName(item: TaxonomyItemDto): string {
    return item.name ?? item.slug ?? '';
  }

  protected schoolTypeLabel(type: SchoolType | undefined): string {
    const match = this.schoolTypes.find((entry) => entry.value === type);
    return match ? this.transloco.translate(match.labelKey) : '';
  }

  protected genderTypeLabel(type: GenderType | undefined): string {
    const match = this.genderTypes.find((entry) => entry.value === type);
    return match ? this.transloco.translate(match.labelKey) : '';
  }

  protected formatFee(school: ExtendedSchoolListItem): string | null {
    if (school.feesRequireLogin) {
      return this.transloco.translate('schools.search.feesRequireLogin');
    }

    const amount = school.minimumAnnualFee;
    if (amount === undefined || amount === null) {
      if (school.hasPublishedFees) {
        return this.transloco.translate('schools.search.feesRequireLogin');
      }
      return null;
    }

    const currency = school.feeCurrency ?? 'EGP';
    const formatted = this.localeFormat.formatCurrency(amount, currency);
    return `${this.transloco.translate('schools.search.feeFrom')} ${formatted}`;
  }

  protected formatDistance(distanceKm: number | undefined): string | null {
    if (distanceKm === undefined || distanceKm === null) {
      return null;
    }

    return this.transloco.translate('schools.search.distanceKm', {
      distance: this.localeFormat.formatNumber(distanceKm, { maximumFractionDigits: 1 }),
    });
  }

  protected summaryText(values: string[] | undefined): string {
    return (values ?? []).filter(Boolean).join(' · ');
  }

  protected locationText(school: ExtendedSchoolListItem): string {
    const parts = [school.city, school.district].filter(Boolean);
    if (parts.length > 0) {
      return parts.join(' · ');
    }

    return this.transloco.translate('schools.search.locationFallback');
  }

  protected onSearchInput(value: string): void {
    this.searchDraft.set(value);
    this.searchDraft$.next(value);
  }

  protected onLocationChange(selection: LocationSelection): void {
    this.patchQuery(selection);
  }

  protected useCurrentLocation(): void {
    this.geolocationStatusKey.set(null);
    if (typeof window === 'undefined' || !window.isSecureContext) {
      this.geolocationStatusKey.set('schools.search.geolocation.insecure');
      return;
    }
    if (!navigator.geolocation) {
      this.geolocationStatusKey.set('schools.search.geolocation.unsupported');
      return;
    }

    this.geolocationStatusKey.set('schools.search.geolocation.loading');
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.patchQuery({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          sort: 'nearest',
        });
        this.geolocationStatusKey.set('schools.search.geolocation.success');
      },
      (error) => {
        const key =
          error.code === error.PERMISSION_DENIED
            ? 'denied'
            : error.code === error.TIMEOUT
              ? 'timeout'
              : 'unavailable';
        this.geolocationStatusKey.set(`schools.search.geolocation.${key}`);
      },
      { enableHighAccuracy: false, timeout: 10_000, maximumAge: 60_000 },
    );
  }

  protected clearCurrentLocation(): void {
    const sort = this.query().sort === 'nearest' ? undefined : this.query().sort;
    this.patchQuery({ latitude: undefined, longitude: undefined, sort });
    this.geolocationStatusKey.set(null);
  }

  protected onStageChange(stageId: string): void {
    this.patchQuery({ stageId: stageId || undefined, gradeId: undefined });
    if (stageId) {
      this.loadGrades(stageId);
    } else {
      this.grades.set([]);
    }
  }

  protected toggleCurriculum(id: string, checked: boolean): void {
    const current = this.query().curriculumIds;
    const next = checked ? [...new Set([...current, id])] : current.filter((value) => value !== id);
    this.patchQuery({ curriculumIds: next });
  }

  protected toggleFacility(id: string, checked: boolean): void {
    const current = this.query().facilityIds;
    const next = checked ? [...new Set([...current, id])] : current.filter((value) => value !== id);
    this.patchQuery({ facilityIds: next });
  }

  protected isCurriculumSelected(id: string): boolean {
    return this.query().curriculumIds.includes(id);
  }

  protected isFacilitySelected(id: string): boolean {
    return this.query().facilityIds.includes(id);
  }

  protected onSchoolTypeChange(value: string): void {
    this.patchQuery({ schoolType: value ? (Number(value) as SchoolType) : undefined });
  }

  protected onGenderTypeChange(value: string): void {
    this.patchQuery({ genderType: value ? (Number(value) as GenderType) : undefined });
  }

  protected onGradeChange(value: string): void {
    this.patchQuery({ gradeId: value || undefined });
  }

  protected onAcademicYearChange(value: string): void {
    this.patchQuery({ academicYearId: value || undefined });
  }

  protected onMinimumTuitionChange(value: string): void {
    const parsed = value.trim() ? Number(value) : undefined;
    this.patchQuery({ minimumTuition: Number.isFinite(parsed) ? parsed : undefined });
  }

  protected onMaximumTuitionChange(value: string): void {
    const parsed = value.trim() ? Number(value) : undefined;
    this.patchQuery({ maximumTuition: Number.isFinite(parsed) ? parsed : undefined });
  }

  protected onAdmissionOpenChange(checked: boolean): void {
    this.patchQuery({ admissionOpen: checked ? true : undefined });
  }

  protected onSortChange(value: string): void {
    this.patchQuery({ sort: value || undefined });
  }

  protected isMapView(): boolean {
    return this.query().view === 'map';
  }

  protected setView(view: 'list' | 'map'): void {
    this.patchQuery({
      view: view === 'map' ? 'map' : undefined,
      pageNumber: view === 'map' ? 1 : this.query().pageNumber,
    });
  }

  protected onSearchThisArea(bounds: MapViewportBounds): void {
    this.patchQuery({
      view: 'map',
      northLatitude: bounds.northLatitude,
      southLatitude: bounds.southLatitude,
      eastLongitude: bounds.eastLongitude,
      westLongitude: bounds.westLongitude,
      latitude: undefined,
      longitude: undefined,
      sort: this.query().sort === 'nearest' ? undefined : this.query().sort,
      pageNumber: 1,
    });
  }

  protected onMapBranchSelected(branchId: string | undefined): void {
    this.patchQuery({ selectedBranchId: branchId });
  }

  protected goToPage(pageNumber: number): void {
    this.patchQuery({ pageNumber });
  }

  protected clearAllFilters(): void {
    this.searchDraft.set('');
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true,
    });
  }

  protected clearFilter(key: keyof SchoolsSearchQuery, value?: string): void {
    const current = this.query();

    switch (key) {
      case 'search':
        this.searchDraft.set('');
        this.patchQuery({ search: undefined });
        return;
      case 'countryId':
        this.patchQuery({
          countryId: undefined,
          governorateId: undefined,
          cityId: undefined,
          districtId: undefined,
        });
        return;
      case 'governorateId':
        this.patchQuery({ governorateId: undefined, cityId: undefined, districtId: undefined });
        return;
      case 'cityId':
        this.patchQuery({ cityId: undefined, districtId: undefined });
        return;
      case 'districtId':
        this.patchQuery({ districtId: undefined });
        return;
      case 'stageId':
        this.grades.set([]);
        this.patchQuery({ stageId: undefined, gradeId: undefined });
        return;
      case 'gradeId':
        this.patchQuery({ gradeId: undefined });
        return;
      case 'schoolType':
        this.patchQuery({ schoolType: undefined });
        return;
      case 'genderType':
        this.patchQuery({ genderType: undefined });
        return;
      case 'curriculumIds':
        this.patchQuery({
          curriculumIds: current.curriculumIds.filter((id) => id !== value),
        });
        return;
      case 'facilityIds':
        this.patchQuery({
          facilityIds: current.facilityIds.filter((id) => id !== value),
        });
        return;
      case 'minimumTuition':
        this.patchQuery({ minimumTuition: undefined });
        return;
      case 'maximumTuition':
        this.patchQuery({ maximumTuition: undefined });
        return;
      case 'academicYearId':
        this.patchQuery({ academicYearId: undefined });
        return;
      case 'admissionOpen':
        this.patchQuery({ admissionOpen: undefined });
        return;
      default:
        return;
    }
  }

  protected chipLabel(key: keyof SchoolsSearchQuery, value?: string): string {
    const query = this.query();

    switch (key) {
      case 'search':
        return query.search ?? '';
      case 'stageId':
        return this.taxonomyName(this.findTaxonomy(this.stages(), query.stageId));
      case 'gradeId':
        return this.taxonomyName(this.findTaxonomy(this.grades(), query.gradeId));
      case 'schoolType':
        return this.schoolTypeLabel(query.schoolType);
      case 'genderType':
        return this.genderTypeLabel(query.genderType);
      case 'curriculumIds':
        return this.taxonomyName(this.findTaxonomy(this.curricula(), value));
      case 'facilityIds':
        return this.taxonomyName(this.findTaxonomy(this.facilities(), value));
      case 'minimumTuition':
        return this.transloco.translate('schools.search.chips.minimumTuition', {
          amount: this.localeFormat.formatNumber(query.minimumTuition ?? 0),
        });
      case 'maximumTuition':
        return this.transloco.translate('schools.search.chips.maximumTuition', {
          amount: this.localeFormat.formatNumber(query.maximumTuition ?? 0),
        });
      case 'academicYearId':
        return this.taxonomyName(this.findTaxonomy(this.academicYears(), query.academicYearId));
      case 'admissionOpen':
        return this.transloco.translate('schools.search.filters.admissionOpen');
      default:
        return '';
    }
  }

  protected openMobileFilters(): void {
    this.mobileFiltersOpen.set(true);
  }

  protected closeMobileFilters(): void {
    this.mobileFiltersOpen.set(false);
  }

  protected retryLoad(): void {
    this.retry$.next();
  }

  protected onSchoolImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img.src.endsWith(SCHOOL_CARD_FALLBACK)) {
      return;
    }
    img.src = SCHOOL_CARD_FALLBACK;
  }

  protected isFavorite(school: ExtendedSchoolListItem): boolean {
    if (!school.id) {
      return false;
    }
    const override = this.favoriteOverrides().get(school.id);
    if (override !== undefined) {
      return override;
    }
    return school.isFavorite === true;
  }

  protected isFavoriteBusy(schoolId: string | undefined): boolean {
    return !!schoolId && this.favoriteBusyIds().has(schoolId);
  }

  protected toggleFavorite(school: ExtendedSchoolListItem, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.favoritesEnabled || !school.id || this.isFavoriteBusy(school.id)) {
      return;
    }

    const schoolId = school.id;
    const returnUrl = this.router.url;

    this.auth
      .ensureSession()
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const user = this.auth.currentUser();
        if (!user) {
          void this.router.navigate(['/auth/login'], {
            queryParams: { returnUrl },
          });
          return;
        }

        if (!hasAnyRole(user, [SchooleraRoles.Parent])) {
          void this.router.navigate(['/unauthorized']);
          return;
        }

        const previous = this.isFavorite(school);
        const next = !previous;
        this.favoriteOverrides.update((map) => {
          const copy = new Map(map);
          copy.set(schoolId, next);
          return copy;
        });
        this.favoriteBusyIds.update((set) => new Set(set).add(schoolId));

        const request = next
          ? this.parentFavoritesApi.add(schoolId)
          : this.parentFavoritesApi.remove(schoolId);

        request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (result) => {
            this.favoriteBusyIds.update((set) => {
              const copy = new Set(set);
              copy.delete(schoolId);
              return copy;
            });
            if (result.succeeded) {
              this.toast.success(
                this.transloco.translate(
                  next ? 'schools.profile.favoriteAdded' : 'schools.profile.favoriteRemoved',
                ),
              );
              return;
            }
            this.favoriteOverrides.update((map) => {
              const copy = new Map(map);
              copy.set(schoolId, previous);
              return copy;
            });
            this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
          },
          error: () => {
            this.favoriteBusyIds.update((set) => {
              const copy = new Set(set);
              copy.delete(schoolId);
              return copy;
            });
            this.favoriteOverrides.update((map) => {
              const copy = new Map(map);
              copy.set(schoolId, previous);
              return copy;
            });
            this.toast.error(this.transloco.translate('parent.errors.generic'));
          },
        });
      });
  }

  protected schoolCover(school: ExtendedSchoolListItem): string {
    return school.coverUrl || school.logoUrl || this.schoolCardFallback;
  }

  protected schoolLogo(school: ExtendedSchoolListItem): string {
    return school.logoUrl || this.schoolCardFallback;
  }

  protected pageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.query().pageNumber;
    const windowSize = 5;
    const start = Math.max(1, current - Math.floor(windowSize / 2));
    const end = Math.min(total, start + windowSize - 1);
    const adjustedStart = Math.max(1, end - windowSize + 1);
    const pages: number[] = [];

    for (let page = adjustedStart; page <= end; page += 1) {
      pages.push(page);
    }

    return pages;
  }

  private bindSearchDraft(): void {
    this.searchDraft$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        const trimmed = value.trim();
        const nextSearch = trimmed || undefined;
        if (nextSearch === this.query().search) {
          return;
        }

        this.patchQuery({ search: nextSearch });
      });
  }

  private bindResultsStream(): void {
    const filters$ = this.route.queryParamMap.pipe(
      map((paramMap) => this.syncFromRoute(paramMap)),
      distinctUntilChanged(areSchoolsSearchQueriesEqual),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    merge(filters$, this.retry$.pipe(switchMap(() => filters$.pipe(take(1)))))
      .pipe(
        switchMapLatestLoading(this.loading, (filters) => {
          this.error.set(false);
          this.query.set(filters);
          return this.schoolsApi.getSchools(filters).pipe(
            catchError((error: unknown) => {
              if (isHttpRequestCanceled(error)) {
                return EMPTY;
              }

              return of(null);
            }),
            map((result) => ({ filters, result })),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ result }) => {
        if (!result || !result.succeeded) {
          this.schools.set([]);
          this.totalCount.set(0);
          this.totalPages.set(0);
          this.error.set(true);
          return;
        }

        this.schools.set((result.data ?? []) as ExtendedSchoolListItem[]);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.error.set(false);
      });
  }

  private syncFromRoute(paramMap: ParamMap): SchoolsSearchQuery {
    const parsed = parseSchoolsSearchQuery(paramMap);
    this.searchDraft.set(parsed.search ?? '');

    if (parsed.stageId) {
      this.loadGrades(parsed.stageId, parsed.gradeId);
    } else {
      this.grades.set([]);
    }

    return parsed;
  }

  private patchQuery(partial: Partial<SchoolsSearchQuery>): void {
    const current = this.query();
    const next: SchoolsSearchQuery = {
      ...current,
      ...partial,
      curriculumIds: partial.curriculumIds ?? current.curriculumIds,
      facilityIds: partial.facilityIds ?? current.facilityIds,
    };

    if (partial.pageNumber === undefined && clearsPageOnChange(current, next)) {
      next.pageNumber = 1;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializeSchoolsSearchQuery(next),
      replaceUrl: true,
    });
  }

  private loadTaxonomies(): void {
    this.taxonomiesApi
      .getEducationalStages()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => this.stages.set(result.data ?? []));

    this.taxonomiesApi
      .getCurricula()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => this.curricula.set(result.data ?? []));

    this.taxonomiesApi
      .getFacilities()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => this.facilities.set(result.data ?? []));

    this.taxonomiesApi
      .getAcademicYears()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => this.academicYears.set(result.data ?? []));
  }

  private loadGrades(stageId: string, selectedGradeId?: string): void {
    this.taxonomiesApi
      .getGradesByStage(stageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        const items = result.data ?? [];
        this.grades.set(items);
        if (selectedGradeId && !items.some((item) => item.id === selectedGradeId)) {
          this.patchQuery({ gradeId: undefined });
        }
      });
  }

  private findTaxonomy(items: TaxonomyItemDto[], id: string | undefined): TaxonomyItemDto {
    return items.find((item) => item.id === id) ?? {};
  }
}
