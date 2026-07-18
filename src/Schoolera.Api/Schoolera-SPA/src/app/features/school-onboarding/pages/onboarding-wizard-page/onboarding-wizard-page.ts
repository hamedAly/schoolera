import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  GenderType,
  MyOnboardingApplicationDto,
  OnboardingDocumentTypeDto,
  SchoolType,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { translateOnboardingErrorCodes } from '../../data-access/onboarding-errors';
import { SchoolOnboardingApi } from '../../data-access/school-onboarding.api';

type WizardStep =
  | 'organization'
  | 'representative'
  | 'school'
  | 'branch'
  | 'documents'
  | 'review';

const STEPS: readonly WizardStep[] = [
  'organization',
  'representative',
  'school',
  'branch',
  'documents',
  'review',
] as const;

const LOCKED_STATUSES = new Set(['Submitted', 'UnderReview', 'Approved', 'Rejected']);

@Component({
  selector: 'se-onboarding-wizard-page',
  imports: [
    Breadcrumbs,
    Button,
    FormField,
    PageHero,
    PublicPageContainer,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './onboarding-wizard-page.html',
  styleUrl: './onboarding-wizard-page.scss',
})
export class OnboardingWizardPage implements OnInit {
  private readonly api = inject(SchoolOnboardingApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly validationSummary = viewChild<ElementRef<HTMLElement>>('validationSummary');

  protected readonly steps = STEPS;
  protected readonly schoolTypes = [
    { value: SchoolType._1, labelKey: 'onboarding.enums.schoolType.private' },
    { value: SchoolType._2, labelKey: 'onboarding.enums.schoolType.international' },
    { value: SchoolType._3, labelKey: 'onboarding.enums.schoolType.national' },
    { value: SchoolType._4, labelKey: 'onboarding.enums.schoolType.language' },
  ] as const;
  protected readonly genderTypes = [
    { value: GenderType._1, labelKey: 'onboarding.enums.genderType.boys' },
    { value: GenderType._2, labelKey: 'onboarding.enums.genderType.girls' },
    { value: GenderType._3, labelKey: 'onboarding.enums.genderType.mixed' },
  ] as const;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploadingTypeId = signal<string | null>(null);
  protected readonly uploadPercent = signal(0);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly validationMessages = signal<string[]>([]);
  protected readonly application = signal<MyOnboardingApplicationDto | null>(null);
  protected readonly documentTypes = signal<OnboardingDocumentTypeDto[]>([]);
  protected readonly maxFileSizeBytes = signal(5 * 1024 * 1024);
  protected readonly allowedExtensions = signal<string[]>(['.pdf', '.jpg', '.jpeg', '.png', '.webp']);
  protected readonly cities = signal<TaxonomyItemDto[]>([]);
  protected readonly districts = signal<TaxonomyItemDto[]>([]);
  protected readonly currentStep = signal<WizardStep>('organization');
  protected readonly confirmAccurate = signal(false);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());
  protected readonly stepIndex = computed(() => STEPS.indexOf(this.currentStep()));
  protected readonly isEditable = computed(() => {
    const status = this.application()?.status;
    return !status || status === 'Draft' || status === 'ChangesRequested';
  });

  protected readonly organizationForm = this.formBuilder.nonNullable.group({
    organizationNameAr: ['', Validators.required],
    organizationNameEn: [''],
    legalName: [''],
    countryCode: ['EG', Validators.required],
    registrationOrLicenseNumber: ['', Validators.required],
    taxRegistrationNumber: [''],
    legalForm: [''],
    organizationAddress: [''],
    organizationWebsite: [''],
  });

  protected readonly representativeForm = this.formBuilder.nonNullable.group({
    fullNameAr: ['', Validators.required],
    fullNameEn: [''],
    nationalOrIdentityReference: ['', Validators.required],
    jobTitleAr: ['', Validators.required],
    jobTitleEn: [''],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
  });

  readonly schoolForm = this.formBuilder.nonNullable.group({
    schoolNameAr: ['', Validators.required],
    schoolNameEn: [''],
    schoolType: [SchoolType._1 as SchoolType | null, Validators.required],
    genderType: [GenderType._3 as GenderType | null, Validators.required],
    foundedYear: [null as number | null],
    shortDescriptionAr: [''],
    shortDescriptionEn: [''],
    websiteUrl: [''],
    requestedSlug: [''],
  });

  protected readonly branchForm = this.formBuilder.nonNullable.group({
    cityId: ['' as string, Validators.required],
    districtId: ['' as string, Validators.required],
    addressLineAr: ['', Validators.required],
    addressLineEn: [''],
    buildingNumber: [''],
    streetName: [''],
    landmark: [''],
    postalCode: [''],
    localAddressReference: [''],
    latitude: [null as number | null],
    longitude: [null as number | null],
    publicPhone: ['', Validators.required],
    publicEmail: [''],
    whatsAppOrAlternatePhone: [''],
  });

  ngOnInit(): void {
    this.loadInitial();
    this.branchForm.controls.cityId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((cityId) => this.onCityChanged(cityId));
  }

  protected stepLabelKey(step: WizardStep): string {
    return `onboarding.steps.${step}`;
  }

  protected isStepComplete(step: WizardStep): boolean {
    const app = this.application();
    if (!app) {
      return false;
    }
    switch (step) {
      case 'organization':
        return !!app.organizationComplete;
      case 'representative':
        return !!app.representativeComplete;
      case 'school':
        return !!app.schoolComplete;
      case 'branch':
        return !!app.branchComplete;
      case 'documents':
        return !!app.documentsComplete;
      case 'review':
        return false;
    }
  }

  protected goToStep(step: WizardStep): void {
    if (!this.isEditable()) {
      return;
    }
    this.currentStep.set(step);
    this.errorMessage.set(null);
    this.validationMessages.set([]);
  }

  protected saveAndNext(): void {
    this.persistCurrentStep(true);
  }

  protected saveDraft(): void {
    this.persistCurrentStep(false);
  }

  protected back(): void {
    const index = this.stepIndex();
    if (index > 0) {
      this.currentStep.set(STEPS[index - 1]!);
    }
  }

  protected documentForType(typeId: string | undefined) {
    if (!typeId) {
      return undefined;
    }
    return this.application()?.documents?.find((d) => d.documentTypeId === typeId);
  }

  protected localizedDocName(type: OnboardingDocumentTypeDto): string {
    return this.activeLang() === 'en'
      ? (type.nameEn ?? type.nameAr ?? type.code ?? '')
      : (type.nameAr ?? type.nameEn ?? type.code ?? '');
  }

  protected taxonomyName(item: TaxonomyItemDto): string {
    return item.name ?? item.slug ?? '';
  }

  onFileSelected(typeId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || !this.isEditable()) {
      return;
    }

    const ext = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`;
    if (!this.allowedExtensions().includes(ext)) {
      this.errorMessage.set(this.transloco.translate('onboarding.errors.unsupportedDocumentFormat'));
      return;
    }
    if (file.size > this.maxFileSizeBytes()) {
      this.errorMessage.set(this.transloco.translate('onboarding.errors.documentTooLarge'));
      return;
    }
    if (file.size === 0) {
      this.errorMessage.set(this.transloco.translate('onboarding.errors.emptyDocument'));
      return;
    }

    this.uploadingTypeId.set(typeId);
    this.uploadPercent.set(0);
    this.errorMessage.set(null);

    this.api
      .uploadDocumentWithProgress(typeId, file)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.uploadingTypeId.set(null);
          this.uploadPercent.set(0);
        }),
      )
      .subscribe({
        next: (event) => {
          if (event.kind === 'progress') {
            this.uploadPercent.set(event.percent);
            return;
          }
          if (!event.result.succeeded || !event.result.data) {
            this.errorMessage.set(
              translateOnboardingErrorCodes(this.transloco, event.result.errorCodes),
            );
            return;
          }
          this.applyApplication(event.result.data);
        },
        error: (error: unknown) => this.handleHttpError(error),
      });
  }

  protected removeDocument(documentId: string | undefined): void {
    if (!documentId || !this.isEditable() || this.saving()) {
      return;
    }
    this.saving.set(true);
    this.errorMessage.set(null);
    this.api
      .deleteDocument(documentId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (result) => {
          if (!result.succeeded || !result.data) {
            this.errorMessage.set(translateOnboardingErrorCodes(this.transloco, result.errorCodes));
            return;
          }
          this.applyApplication(result.data);
        },
        error: (error: unknown) => this.handleHttpError(error),
      });
  }

  protected downloadDocument(documentId: string | undefined, fileName: string | undefined): void {
    if (!documentId) {
      return;
    }
    this.api
      .downloadDocument(documentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = fileName || 'document';
          anchor.click();
          URL.revokeObjectURL(url);
        },
        error: () => {
          this.errorMessage.set(this.transloco.translate('onboarding.errors.generic'));
        },
      });
  }

  submitApplication(): void {
    if (!this.confirmAccurate()) {
      this.validationMessages.set([
        this.transloco.translate('onboarding.review.confirmRequired'),
      ]);
      this.focusValidation();
      return;
    }

    const app = this.application();
    const isResubmit = app?.status === 'ChangesRequested';
    this.saving.set(true);
    this.errorMessage.set(null);
    this.validationMessages.set([]);

    const request$ = isResubmit ? this.api.resubmit() : this.api.submit();
    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (result) => {
          if (!result.succeeded || !result.data) {
            this.validationMessages.set(
              (result.errorCodes ?? []).map((code) =>
                translateOnboardingErrorCodes(this.transloco, [code]),
              ),
            );
            this.errorMessage.set(
              translateOnboardingErrorCodes(this.transloco, result.errorCodes),
            );
            this.focusValidation();
            return;
          }
          void this.router.navigate(['/school/onboarding/status']);
        },
        error: (error: unknown) => this.handleHttpError(error),
      });
  }

  protected formatBytes(bytes: number | undefined): string {
    if (!bytes) {
      return '';
    }
    if (bytes < 1024) {
      return `${bytes} B`;
    }
    return `${Math.round(bytes / 1024)} KB`;
  }

  private loadInitial(): void {
    this.loading.set(true);
    this.api
      .getMyApplication()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (!result.succeeded) {
            this.loading.set(false);
            this.errorMessage.set(translateOnboardingErrorCodes(this.transloco, result.errorCodes));
            return;
          }

          const app = result.data ?? null;
          if (app && LOCKED_STATUSES.has(app.status ?? '')) {
            void this.router.navigate(['/school/onboarding/status']);
            return;
          }

          if (app) {
            this.applyApplication(app);
            this.currentStep.set(this.mapBackendStep(app.currentStep));
          }

          this.loadDocumentTypes();
          this.loadCities();
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.handleHttpError(error);
        },
      });
  }

  private loadDocumentTypes(): void {
    this.api
      .getDocumentTypes()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (!result.succeeded || !result.data) {
            return;
          }
          this.documentTypes.set(result.data.documentTypes ?? []);
          if (result.data.maxFileSizeBytes) {
            this.maxFileSizeBytes.set(result.data.maxFileSizeBytes);
          }
          if (result.data.allowedExtensions?.length) {
            this.allowedExtensions.set(result.data.allowedExtensions);
          }
        },
      });
  }

  private loadCities(): void {
    this.taxonomiesApi
      .getCities()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded) {
            this.cities.set(result.data ?? []);
          }
        },
      });
  }

  private onCityChanged(cityId: string | null | undefined, preserveDistrict = false): void {
    if (!preserveDistrict) {
      this.branchForm.controls.districtId.setValue('');
    }
    this.districts.set([]);
    if (!cityId) {
      return;
    }
    this.taxonomiesApi
      .getDistrictsByCity(cityId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded) {
            this.districts.set(result.data ?? []);
          }
        },
      });
  }

  private persistCurrentStep(advance: boolean): void {
    if (!this.isEditable() || this.saving()) {
      return;
    }

    const step = this.currentStep();
    this.errorMessage.set(null);
    this.validationMessages.set([]);

    if (step === 'documents') {
      if (advance) {
        this.currentStep.set('review');
      }
      return;
    }

    if (step === 'review') {
      return;
    }

    const form = this.formForStep(step);
    if (form.invalid) {
      form.markAllAsTouched();
      this.validationMessages.set([this.transloco.translate('onboarding.validation.formIncomplete')]);
      this.focusValidation();
      return;
    }

    this.saving.set(true);
    const request$ = this.saveRequestForStep(step);
    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (result) => {
          if (!result.succeeded || !result.data) {
            this.errorMessage.set(translateOnboardingErrorCodes(this.transloco, result.errorCodes));
            this.focusValidation();
            return;
          }
          this.applyApplication(result.data);
          if (advance) {
            const next = STEPS[this.stepIndex() + 1];
            if (next) {
              this.currentStep.set(next);
            }
          }
        },
        error: (error: unknown) => this.handleHttpError(error),
      });
  }

  private formForStep(step: WizardStep) {
    switch (step) {
      case 'organization':
        return this.organizationForm;
      case 'representative':
        return this.representativeForm;
      case 'school':
        return this.schoolForm;
      case 'branch':
        return this.branchForm;
      default:
        return this.organizationForm;
    }
  }

  private saveRequestForStep(step: WizardStep) {
    switch (step) {
      case 'organization':
        return this.api.saveOrganization(this.organizationForm.getRawValue());
      case 'representative':
        return this.api.saveAuthorizedRepresentative(this.representativeForm.getRawValue());
      case 'school': {
        const value = this.schoolForm.getRawValue();
        return this.api.saveSchoolDetails({
          ...value,
          schoolType: value.schoolType ?? undefined,
          genderType: value.genderType ?? undefined,
          foundedYear: value.foundedYear ?? undefined,
        });
      }
      case 'branch': {
        const value = this.branchForm.getRawValue();
        return this.api.savePrimaryBranch({
          ...value,
          cityId: value.cityId || undefined,
          districtId: value.districtId || undefined,
          latitude: value.latitude ?? undefined,
          longitude: value.longitude ?? undefined,
        });
      }
      default:
        return this.api.getMyApplication();
    }
  }

  private applyApplication(app: MyOnboardingApplicationDto): void {
    this.application.set(app);
    this.organizationForm.patchValue({
      organizationNameAr: app.organizationNameAr ?? '',
      organizationNameEn: app.organizationNameEn ?? '',
      legalName: app.legalName ?? '',
      countryCode: app.countryCode ?? 'EG',
      registrationOrLicenseNumber: app.registrationOrLicenseNumber ?? '',
      taxRegistrationNumber: app.taxRegistrationNumber ?? '',
      legalForm: app.legalForm ?? '',
      organizationAddress: app.organizationAddress ?? '',
      organizationWebsite: app.organizationWebsite ?? '',
    });
    this.representativeForm.patchValue({
      fullNameAr: app.representativeFullNameAr ?? '',
      fullNameEn: app.representativeFullNameEn ?? '',
      nationalOrIdentityReference: app.representativeNationalOrIdentityReference ?? '',
      jobTitleAr: app.representativeJobTitleAr ?? '',
      jobTitleEn: app.representativeJobTitleEn ?? '',
      email: app.representativeEmail ?? '',
      phone: app.representativePhone ?? '',
    });
    this.schoolForm.patchValue({
      schoolNameAr: app.schoolNameAr ?? '',
      schoolNameEn: app.schoolNameEn ?? '',
      schoolType: app.schoolType ?? SchoolType._1,
      genderType: app.genderType ?? GenderType._3,
      foundedYear: app.foundedYear ?? null,
      shortDescriptionAr: app.schoolShortDescriptionAr ?? '',
      shortDescriptionEn: app.schoolShortDescriptionEn ?? '',
      websiteUrl: app.schoolWebsiteUrl ?? '',
      requestedSlug: app.requestedSlug ?? '',
    });
    this.branchForm.patchValue(
      {
        cityId: app.cityId ?? '',
        districtId: app.districtId ?? '',
        addressLineAr: app.addressLineAr ?? '',
        addressLineEn: app.addressLineEn ?? '',
        buildingNumber: app.buildingNumber ?? '',
        streetName: app.streetName ?? '',
        landmark: app.landmark ?? '',
        postalCode: app.postalCode ?? '',
        localAddressReference: app.localAddressReference ?? '',
        latitude: app.latitude ?? null,
        longitude: app.longitude ?? null,
        publicPhone: app.publicPhone ?? '',
        publicEmail: app.publicEmail ?? '',
        whatsAppOrAlternatePhone: app.whatsAppOrAlternatePhone ?? '',
      },
      { emitEvent: false },
    );
    if (app.cityId) {
      this.onCityChanged(app.cityId, true);
    }
  }

  private mapBackendStep(step: string | undefined): WizardStep {
    switch (step) {
      case 'AuthorizedRepresentative':
        return 'representative';
      case 'SchoolDetails':
        return 'school';
      case 'PrimaryBranch':
        return 'branch';
      case 'Documents':
        return 'documents';
      case 'Review':
        return 'review';
      default:
        return 'organization';
    }
  }

  private focusValidation(): void {
    queueMicrotask(() => this.validationSummary()?.nativeElement.focus());
  }

  private handleHttpError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.error?.errorCodes) {
      this.errorMessage.set(
        translateOnboardingErrorCodes(this.transloco, error.error.errorCodes),
      );
      return;
    }
    this.errorMessage.set(this.transloco.translate('onboarding.errors.generic'));
  }
}
