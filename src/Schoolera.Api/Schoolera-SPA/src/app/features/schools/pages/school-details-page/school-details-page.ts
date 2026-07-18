import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import {
  catchError,
  distinctUntilChanged,
  map,
  merge,
  of,
  Subject,
  switchMap,
  take,
} from 'rxjs';

import {
  GenderType,
  PublicSchoolBranchDto,
  PublicSchoolListItemDto,
  PublicSchoolProfileDto,
  PublicSchoolProfileDtoResult,
  PublicSchoolStageOfferingDto,
  SchoolType,
} from '../../../../core/api-client/SwaggerClient.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { hasAnyRole, SchooleraRoles } from '../../../../core/auth/auth.models';
import { FeatureFlagsService } from '../../../../core/features/feature-flags.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { extractApiFailure } from '../../../../core/http/extract-api-failure';
import { SeoService } from '../../../../core/seo/seo.service';
import { HomeIcon } from '../../../home/components/home-icon/home-icon';
import { Breadcrumbs, BreadcrumbItem } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { sanitizeHtml } from '../../../../shared/utils/sanitize-html';
import { requiredTextValidator } from '../../../../shared/validators/required-text.validator';
import { PublicAdmissionRequirementSummary, PublicInterviewFaqItem, SchoolsApi } from '../../data-access/schools.api';
import { mapSchoolContactServerErrors } from '../../data-access/school-contact-errors';
import {
  NotificationChannel,
  ParentNotificationsApi,
} from '../../../parent/data-access/parent-notifications.api';
import { ParentFavoritesApi } from '../../../parent/data-access/parent-favorites.api';
import { translateParentErrorCodes } from '../../../parent/data-access/parent-errors';

type PageState = 'loading' | 'loaded' | 'not-found' | 'error';

/** Runtime profile field until NSwag regenerates `isFavorite`. */
type ProfileWithFavorite = PublicSchoolProfileDto & { isFavorite?: boolean | null };

const SCHOOL_CARD_FALLBACK = 'assets/schools/demo-1.svg';
const COVER_FALLBACK = 'assets/schools/demo-2.svg';
const RELATED_LIMIT = 4;
const CONTACT_SOURCE = 'school-profile';

@Component({
  selector: 'se-school-details-page',
  imports: [
    Breadcrumbs,
    FormErrorSummary,
    FormField,
    HomeIcon,
    PublicPageContainer,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './school-details-page.html',
  styleUrl: './school-details-page.scss',
})
export class SchoolDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly schoolsApi = inject(SchoolsApi);
  private readonly parentNotificationsApi = inject(ParentNotificationsApi);
  private readonly parentFavoritesApi = inject(ParentFavoritesApi);
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly seo = inject(SeoService);
  private readonly toast = inject(ToastService);
  private readonly featureFlags = inject(FeatureFlagsService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);
  private readonly retry$ = new Subject<void>();
  private readonly lightboxCloseButton = viewChild<ElementRef<HTMLButtonElement>>('lightboxClose');
  private readonly lang = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  protected readonly schoolCardFallback = SCHOOL_CARD_FALLBACK;
  protected readonly coverFallback = COVER_FALLBACK;
  protected readonly favoritesEnabled = this.featureFlags.favoritesEnabled;
  protected readonly admissionsEnabled = this.featureFlags.admissionsEnabled;
  protected readonly applyRoleBlocked = signal(false);
  protected readonly notifySubmitting = signal(false);
  protected readonly favoriteSubmitting = signal(false);
  protected readonly isFavorite = signal(false);

  protected readonly pageState = signal<PageState>('loading');
  protected readonly profile = signal<PublicSchoolProfileDto | null>(null);
  protected readonly relatedSchools = signal<ReadonlyArray<PublicSchoolListItemDto>>([]);
  protected readonly admissionRequirements = signal<readonly PublicAdmissionRequirementSummary[]>([]);
  protected readonly admissionRequirementsLoading = signal(false);
  protected readonly admissionRequirementsError = signal(false);
  protected readonly interviewFaqs = signal<readonly PublicInterviewFaqItem[]>([]);
  protected readonly interviewFaqsLoading = signal(false);
  protected readonly interviewFaqsError = signal(false);
  protected readonly openInterviewFaqIds = signal<ReadonlySet<string>>(new Set());
  protected readonly contactOpen = signal(false);
  protected readonly contactSubmitting = signal(false);
  protected readonly contactSummaryErrors = signal<FormErrorSummaryItem[]>([]);
  readonly lightboxIndex = signal<number | null>(null);
  protected readonly lightboxFocusReturn = signal<HTMLElement | null>(null);

  readonly contactForm = this.formBuilder.nonNullable.group({
    name: ['', [requiredTextValidator()]],
    phone: ['', [requiredTextValidator()]],
    email: ['', [Validators.email]],
    message: [''],
    consentAccepted: [false, [Validators.requiredTrue]],
    website: [''],
  });

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

  protected readonly breadcrumbs = computed((): readonly BreadcrumbItem[] => {
    this.lang();
    const school = this.profile();
    return [
      { label: this.transloco.translate('common.home') || '', route: '/' },
      { label: this.transloco.translate('nav.searchSchools') || '', route: '/schools' },
      {
        label:
          school?.name ??
          this.transloco.translate('schools.profile.breadcrumbFallback') ??
          '',
      },
    ];
  });

  protected readonly mainBranch = computed(() => this.resolveMainBranch(this.profile()));

  protected readonly heroLocation = computed(() => {
    this.lang();
    const branch = this.mainBranch();
    if (!branch) {
      return this.transloco.translate('schools.search.locationFallback');
    }

    const parts = [branch.city, branch.district].filter(Boolean);
    return parts.length ? parts.join(' · ') : this.transloco.translate('schools.search.locationFallback');
  });

  protected readonly hasOverview = computed(() => {
    const school = this.profile();
    return !!(school?.shortDescription?.trim() || school?.fullDescription?.trim());
  });

  protected readonly hasKeyFacts = computed(() => {
    const school = this.profile();
    if (!school) {
      return false;
    }

    return (
      school.foundedYear != null ||
      school.studentCount != null ||
      school.schoolType != null ||
      school.genderType != null ||
      school.isAdmissionOpen === true
    );
  });

  protected readonly hasBranches = computed(() => (this.profile()?.branches?.length ?? 0) > 0);
  protected readonly hasOfferings = computed(() => (this.profile()?.offerings?.length ?? 0) > 0);
  protected readonly hasCurricula = computed(() => (this.profile()?.curricula?.length ?? 0) > 0);
  protected readonly hasFees = computed(() => (this.profile()?.fees?.length ?? 0) > 0);
  protected readonly feesRequireLogin = computed(() => this.profile()?.feesRequireLogin === true);
  protected readonly hasPublishedFees = computed(() => this.profile()?.hasPublishedFees === true);
  protected readonly showFeesLoginPrompt = computed(
    () => this.feesRequireLogin() && this.hasPublishedFees() && !this.hasFees(),
  );
  protected readonly hasPublishedDiscounts = computed(
    () => (this.profile()?.publishedDiscounts?.length ?? 0) > 0,
  );
  protected readonly hasFinancialNotes = computed(
    () => (this.profile()?.financialNotes?.length ?? 0) > 0,
  );
  protected readonly hasFacilities = computed(() => (this.profile()?.facilities?.length ?? 0) > 0);
  protected readonly hasAdditionalServices = computed(
    () => (this.profile()?.additionalServices?.length ?? 0) > 0,
  );
  protected readonly hasGallery = computed(() => (this.profile()?.images?.length ?? 0) > 0);
  protected readonly hasLocationContact = computed(() => {
    const school = this.profile();
    const branch = this.mainBranch();
    const contact = school?.contact;
    return !!(
      branch?.addressLine ||
      branch?.latitude != null ||
      contact?.phone ||
      contact?.email ||
      contact?.websiteUrl ||
      contact?.whatsAppNumber ||
      branch?.phone ||
      branch?.email
    );
  });

  protected readonly hasAdmissionRequirements = computed(
    () => this.admissionRequirements().length > 0,
  );

  protected readonly hasInterviewFaqs = computed(() => this.interviewFaqs().length > 0);

  protected readonly hasRelatedSchools = computed(() => this.relatedSchools().length > 0);

  protected readonly canShowApplyCta = computed(() => {
    if (!this.admissionsEnabled) {
      return false;
    }

    const school = this.profile();
    if (!school?.isAdmissionOpen || !school.slug) {
      return false;
    }

    const offerings = school.offerings ?? [];
    return offerings.some(
      (offering) =>
        offering.isAdmissionOpen &&
        !!offering.educationalStageId &&
        (offering.grades?.length ?? 0) > 0,
    );
  });

  protected readonly showAdmissionClosedHint = computed(() => {
    if (!this.admissionsEnabled) {
      return false;
    }

    const school = this.profile();
    return !!school && school.isAdmissionOpen === false;
  });

  protected readonly canNotifyAdmissionsOpen = computed(() => {
    const school = this.profile();
    const user = this.auth.currentUser();
    return !!school?.id && hasAnyRole(user, [SchooleraRoles.Parent]);
  });

  protected readonly galleryImages = computed(() =>
    [...(this.profile()?.images ?? [])].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
  );

  protected readonly activeLightboxImage = computed(() => {
    const index = this.lightboxIndex();
    if (index === null) {
      return null;
    }

    return this.galleryImages()[index] ?? null;
  });

  ngOnInit(): void {
    this.bindProfileStream();
    this.transloco.langChanges$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const school = this.profile();
      if (school) {
        this.applySchoolSeo(school);
      }
    });
    this.destroyRef.onDestroy(() => this.seo.clear());
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent): void {
    if (this.lightboxIndex() === null) {
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      this.closeLightbox();
      return;
    }

    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.showPreviousImage();
      return;
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.showNextImage();
    }
  }

  protected schoolTypeLabel(type: SchoolType | undefined): string {
    const match = this.schoolTypes.find((entry) => entry.value === type);
    return match ? this.transloco.translate(match.labelKey) : '';
  }

  protected genderTypeLabel(type: GenderType | undefined): string {
    const match = this.genderTypes.find((entry) => entry.value === type);
    return match ? this.transloco.translate(match.labelKey) : '';
  }

  protected offeringGenderLabel(type: GenderType | undefined): string {
    return this.genderTypeLabel(type);
  }

  protected formatFeeAmount(amount: number | null | undefined, currencyCode: string | undefined): string {
    if (amount == null) {
      return '—';
    }
    if (amount == null) {
      return '';
    }

    return this.localeFormat.formatCurrency(amount, currencyCode ?? 'EGP');
  }

  protected formatCount(value: number | undefined): string {
    if (value == null) {
      return '';
    }

    return this.localeFormat.formatNumber(value);
  }

  protected coverUrl(): string {
    return this.profile()?.coverUrl || this.coverFallback;
  }

  protected logoUrl(): string {
    return this.profile()?.logoUrl || this.schoolCardFallback;
  }

  protected relatedCover(school: PublicSchoolListItemDto): string {
    return school.coverUrl || school.logoUrl || this.schoolCardFallback;
  }

  protected relatedLogo(school: PublicSchoolListItemDto): string {
    return school.logoUrl || this.schoolCardFallback;
  }

  protected relatedLocation(school: PublicSchoolListItemDto): string {
    const parts = [school.city, school.district].filter(Boolean);
    return parts.length ? parts.join(' · ') : this.transloco.translate('schools.search.locationFallback');
  }

  protected relatedFee(school: PublicSchoolListItemDto): string | null {
    if (school.minimumAnnualFee == null) {
      return null;
    }

    const formatted = this.localeFormat.formatCurrency(
      school.minimumAnnualFee,
      school.feeCurrency ?? 'EGP',
    );
    return `${this.transloco.translate('schools.search.feeFrom')} ${formatted}`;
  }

  protected summaryText(values: string[] | undefined): string {
    return (values ?? []).filter(Boolean).join(' · ');
  }

  protected gradeNames(offering: PublicSchoolStageOfferingDto): string {
    return (offering.grades ?? [])
      .map((grade) => grade.name)
      .filter(Boolean)
      .join(' · ');
  }

  protected startApplication(): void {
    this.applyRoleBlocked.set(false);
    const school = this.profile();
    if (!school?.slug || !this.canShowApplyCta()) {
      return;
    }

    const target = `/parent/applications/new?schoolSlug=${encodeURIComponent(school.slug)}`;

    this.auth
      .ensureSession()
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const user = this.auth.currentUser();
        if (!user) {
          void this.router.navigate(['/auth/login'], {
            queryParams: { returnUrl: target },
          });
          return;
        }

        if (hasAnyRole(user, [SchooleraRoles.Parent])) {
          void this.router.navigateByUrl(target);
          return;
        }

        this.applyRoleBlocked.set(true);
        void this.router.navigate(['/unauthorized']);
      });
  }

  protected notifyAdmissionsOpen(): void {
    const school = this.profile();
    if (!school?.id || this.notifySubmitting() || !this.canNotifyAdmissionsOpen()) {
      return;
    }

    this.notifySubmitting.set(true);
    this.parentNotificationsApi
      .createSubscription({
        schoolId: school.id,
        preferredChannel: NotificationChannel._1,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.notifySubmitting.set(false);
          if (result.succeeded) {
            this.toast.success(
              this.transloco.translate('schools.profile.notifyAdmissionsOpen.success'),
            );
            return;
          }
          const codes = result.errorCodes ?? [];
          if (codes.includes('parent.duplicateSubscription')) {
            this.toast.success(
              this.transloco.translate('schools.profile.notifyAdmissionsOpen.alreadySubscribed'),
            );
            return;
          }
          this.toast.error(
            this.transloco.translate('schools.profile.notifyAdmissionsOpen.error'),
          );
        },
        error: () => {
          this.notifySubmitting.set(false);
          this.toast.error(
            this.transloco.translate('schools.profile.notifyAdmissionsOpen.error'),
          );
        },
      });
  }

  protected toggleFavorite(): void {
    if (!this.favoritesEnabled || this.favoriteSubmitting()) {
      return;
    }

    const school = this.profile();
    if (!school?.id) {
      return;
    }

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

        const previous = this.isFavorite();
        const next = !previous;
        this.isFavorite.set(next);
        this.favoriteSubmitting.set(true);

        const request = next
          ? this.parentFavoritesApi.add(school.id!)
          : this.parentFavoritesApi.remove(school.id!);

        request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (result) => {
            this.favoriteSubmitting.set(false);
            if (result.succeeded) {
              this.toast.success(
                this.transloco.translate(
                  next ? 'schools.profile.favoriteAdded' : 'schools.profile.favoriteRemoved',
                ),
              );
              return;
            }
            this.isFavorite.set(previous);
            this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
          },
          error: () => {
            this.favoriteSubmitting.set(false);
            this.isFavorite.set(previous);
            this.toast.error(this.transloco.translate('parent.errors.generic'));
          },
        });
      });
  }

  openContact(): void {
    this.contactSummaryErrors.set([]);
    this.contactOpen.set(true);
  }

  protected closeContact(): void {
    if (this.contactSubmitting()) {
      return;
    }

    this.contactOpen.set(false);
  }

  submitContact(): void {
    if (this.contactSubmitting()) {
      return;
    }

    this.contactSummaryErrors.set([]);

    if (this.contactForm.controls.website.value.trim()) {
      return;
    }

    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      this.contactSummaryErrors.set(this.collectContactSummaryErrors());
      return;
    }

    const slug = this.profile()?.slug;
    if (!slug) {
      return;
    }

    this.contactSubmitting.set(true);
    const value = this.contactForm.getRawValue();

    this.schoolsApi
      .submitContactLead(slug, {
        name: value.name.trim(),
        phone: value.phone.trim(),
        email: value.email.trim() || undefined,
        message: value.message.trim() || undefined,
        consentAccepted: value.consentAccepted,
        source: CONTACT_SOURCE,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.contactSubmitting.set(false);

          if (!result.succeeded) {
            this.applyContactFailure(result.errorCodes);
            return;
          }

          this.toast.success(this.transloco.translate('schools.profile.contact.success'));
          this.contactForm.reset({
            name: '',
            phone: '',
            email: '',
            message: '',
            consentAccepted: false,
            website: '',
          });
          this.contactSummaryErrors.set([]);
          this.contactOpen.set(false);
        },
        error: (error: unknown) => {
          this.contactSubmitting.set(false);
          this.applyContactFailure(extractApiFailure(error).errorCodes);
        },
      });
  }

  protected contactFieldError(
    controlName: 'name' | 'phone' | 'email' | 'message' | 'consentAccepted',
  ): string | undefined {
    const control = this.contactForm.controls[controlName];
    if (!control.touched && !control.dirty) {
      return undefined;
    }

    if (control.errors?.['server']) {
      return control.errors['server'] as string;
    }

    if (control.errors?.['requiredText'] || control.errors?.['required']) {
      return this.transloco.translate('validation.required');
    }

    if (controlName === 'email' && control.errors?.['email']) {
      return this.transloco.translate('auth.validation.invalidEmail');
    }

    if (controlName === 'consentAccepted' && control.errors?.['required']) {
      return this.transloco.translate('schools.profile.contact.errors.consentRequired');
    }

    return undefined;
  }

  openLightbox(index: number, trigger?: HTMLElement): void {
    this.lightboxFocusReturn.set(
      trigger ?? (document.activeElement instanceof HTMLElement ? document.activeElement : null),
    );
    this.lightboxIndex.set(index);

    queueMicrotask(() => {
      this.lightboxCloseButton()?.nativeElement.focus();
    });
  }

  protected closeLightbox(): void {
    const returnFocus = this.lightboxFocusReturn();
    this.lightboxIndex.set(null);
    this.lightboxFocusReturn.set(null);
    returnFocus?.focus();
  }

  protected showPreviousImage(): void {
    const current = this.lightboxIndex();
    if (current === null) {
      return;
    }

    const total = this.galleryImages().length;
    this.lightboxIndex.set((current - 1 + total) % total);
  }

  protected showNextImage(): void {
    const current = this.lightboxIndex();
    if (current === null) {
      return;
    }

    const total = this.galleryImages().length;
    this.lightboxIndex.set((current + 1) % total);
  }

  protected onLightboxBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeLightbox();
    }
  }

  protected onImageError(event: Event, fallback: string): void {
    const img = event.target as HTMLImageElement;
    if (img.src.endsWith(fallback)) {
      return;
    }

    img.src = fallback;
  }

  protected retryLoad(): void {
    this.retry$.next();
  }

  protected externalUrl(url: string | undefined): string | null {
    if (!url?.trim()) {
      return null;
    }

    return /^https?:\/\//i.test(url) ? url : `https://${url}`;
  }

  protected offeringRows(): PublicSchoolStageOfferingDto[] {
    return this.profile()?.offerings ?? [];
  }

  private bindProfileStream(): void {
    const slug$ = this.route.paramMap.pipe(
      map((params) => params.get('slug')?.trim() ?? ''),
      distinctUntilChanged(),
    );

    merge(
      slug$,
      this.retry$.pipe(switchMap(() => slug$.pipe(take(1)))),
    )
      .pipe(
        switchMap((slug) => {
          this.pageState.set('loading');
          this.profile.set(null);
          this.isFavorite.set(false);
          this.relatedSchools.set([]);
          this.seo.clear();

          if (!slug) {
            this.pageState.set('not-found');
            return of(null);
          }

          return this.schoolsApi.getBySlug(slug).pipe(
            catchError((error) => of(this.asProfileFailure(error))),
            map((result) => ({ slug, result })),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((payload) => {
        if (!payload?.result) {
          return;
        }

        const { slug, result } = payload;

        if (!result.succeeded || !result.data) {
          if (result.errorCodes?.includes('school.not_found')) {
            this.pageState.set('not-found');
          } else {
            this.pageState.set('error');
          }
          return;
        }

        const school = result.data as ProfileWithFavorite;
        this.profile.set(school);
        this.isFavorite.set(school.isFavorite === true);
        this.pageState.set('loaded');
        this.applySchoolSeo(school);
        this.loadRelated(slug);
        if (school.slug) {
          this.loadInterviewFaqs(school.slug);
        }
        if (this.admissionsEnabled && school.isAdmissionOpen && school.slug) {
          this.loadAdmissionRequirements(school.slug);
        }
      });
  }

  protected retryAdmissionRequirements(): void {
    const slug = this.profile()?.slug;
    if (slug) {
      this.loadAdmissionRequirements(slug);
    }
  }

  protected retryInterviewFaqs(): void {
    const slug = this.profile()?.slug;
    if (slug) {
      this.loadInterviewFaqs(slug);
    }
  }

  protected isInterviewFaqOpen(id: string): boolean {
    return this.openInterviewFaqIds().has(id);
  }

  protected toggleInterviewFaq(id: string): void {
    const next = new Set(this.openInterviewFaqIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.openInterviewFaqIds.set(next);
  }

  protected sanitizeInterviewFaqAnswer(html: string | undefined): string {
    return sanitizeHtml(this.sanitizer, html ?? '');
  }

  private loadAdmissionRequirements(slug: string): void {
    this.admissionRequirementsLoading.set(true);
    this.admissionRequirementsError.set(false);
    this.schoolsApi
      .getAdmissionRequirements(slug)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.admissionRequirementsLoading.set(false);
        if (result.succeeded) {
          this.admissionRequirements.set(result.data ?? []);
          return;
        }
        this.admissionRequirementsError.set(true);
      });
  }

  private loadInterviewFaqs(slug: string): void {
    this.interviewFaqsLoading.set(true);
    this.interviewFaqsError.set(false);
    this.schoolsApi
      .getInterviewFaqs(slug)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.interviewFaqsLoading.set(false);
        if (result.succeeded) {
          this.interviewFaqs.set(result.data ?? []);
          return;
        }
        this.interviewFaqsError.set(true);
      });
  }

  private loadRelated(slug: string): void {
    this.schoolsApi
      .getRelated(slug, RELATED_LIMIT)
      .pipe(
        catchError(() => of({ succeeded: false, data: [], errors: [], errorCodes: [] })),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.relatedSchools.set(result.data);
        }
      });
  }

  private applySchoolSeo(school: PublicSchoolProfileDto): void {
    const fallbackDescription =
      school.shortDescription?.trim() ||
      school.fullDescription?.trim() ||
      this.transloco.translate('schools.profile.meta.descriptionFallback', { name: school.name ?? '' });

    const title =
      school.seo?.title?.trim() ||
      this.transloco.translate('schools.profile.meta.title', { name: school.name ?? '' }) ||
      '';
    const description =
      school.seo?.description?.trim() || fallbackDescription || '';
    const branch = this.resolveMainBranch(school);
    const image = school.coverUrl || school.logoUrl || undefined;

    this.seo.apply({
      title,
      description,
      canonicalPath: `/schools/${school.slug ?? ''}`,
      image,
      type: 'website',
      jsonLd: {
        '@context': 'https://schema.org',
        '@type': ['EducationalOrganization', 'School'],
        name: school.name,
        description,
        url: this.seo.buildAbsoluteUrl(`/schools/${school.slug ?? ''}`),
        ...(image ? { image } : {}),
        ...(branch?.addressLine ? { address: branch.addressLine } : {}),
        ...(branch?.city ? { addressLocality: branch.city } : {}),
        ...(school.contact?.phone ? { telephone: school.contact.phone } : {}),
        ...(school.contact?.email ? { email: school.contact.email } : {}),
      },
    });
  }

  private resolveMainBranch(school: PublicSchoolProfileDto | null): PublicSchoolBranchDto | null {
    const branches = school?.branches ?? [];
    if (!branches.length) {
      return null;
    }

    return branches.find((branch) => branch.isMainBranch) ?? branches[0] ?? null;
  }

  private asProfileFailure(error: unknown): PublicSchoolProfileDtoResult {
    const failure = extractApiFailure(error);
    return {
      succeeded: false,
      errors: failure.errors,
      errorCodes: failure.errorCodes,
    };
  }

  private applyContactFailure(errorCodes: string[] | undefined): void {
    const mapped = mapSchoolContactServerErrors(this.transloco, errorCodes);
    this.contactSummaryErrors.set(mapped.summaryItems);
    this.toast.error(
      mapped.summaryItems[0]?.message ??
        this.transloco.translate('schools.profile.contact.errors.generic'),
    );
  }

  private collectContactSummaryErrors(): FormErrorSummaryItem[] {
    const items: FormErrorSummaryItem[] = [];
    const fields = ['name', 'phone', 'email', 'consentAccepted'] as const;

    for (const field of fields) {
      const message = this.contactFieldError(field);
      if (message) {
        items.push({
          message,
          fieldId: field === 'consentAccepted' ? 'contact-consent' : `contact-${field}`,
        });
      }
    }

    return items;
  }
}
