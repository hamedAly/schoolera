import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  FeeCategory,
  FeeVisibilityPolicy,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolBranchDto, TuitionFeeDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalContextService } from '../../data-access/portal-context.service';
import { localizedBilingualName } from '../../utils/localized-name';

@Component({
  selector: 'se-portal-fees-page',
  imports: [
    Button,
    FormField,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    FormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-fees-page.html',
  styleUrl: './portal-fees-page.scss',
})
export class PortalFeesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly portalContext = inject(PortalContextService);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly FeeCategory = FeeCategory;
  protected readonly FeeVisibilityPolicy = FeeVisibilityPolicy;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly fees = signal<readonly TuitionFeeDto[]>([]);
  protected readonly branches = signal<readonly SchoolBranchDto[]>([]);
  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly academicYears = signal<TaxonomyItemDto[]>([]);
  protected readonly showForm = signal(false);
  protected readonly feeVisibility = signal<FeeVisibilityPolicy | null | undefined>(undefined);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());
  protected readonly canManageFees = computed(() => this.portalContext.canManageFees());

  protected readonly form = this.formBuilder.nonNullable.group({
    branchId: ['', Validators.required],
    educationalStageId: ['', Validators.required],
    academicYearId: ['', Validators.required],
    category: [FeeCategory._1 as FeeCategory, Validators.required],
    currencyCode: ['EGP', Validators.required],
    amount: [0, [Validators.required, Validators.min(0)]],
    isStartingFrom: [false],
    notesAr: [''],
    notesEn: [''],
    internalNotesAr: [''],
    internalNotesEn: [''],
    sortOrder: [0],
  });

  protected readonly visibilityForm = this.formBuilder.nonNullable.group({
    feeVisibilityPolicy: ['' as '' | '1' | '2'],
  });

  ngOnInit(): void {
    this.loadFees();
    this.taxonomiesApi.getEducationalStages().subscribe((r) => r.succeeded && r.data && this.stages.set(r.data));
    this.taxonomiesApi.getAcademicYears().subscribe((r) => r.succeeded && r.data && this.academicYears.set(r.data));
  }

  protected feeLabel(fee: TuitionFeeDto): string {
    if (fee.nameAr || fee.nameEn) {
      return localizedBilingualName(fee.nameAr ?? '', fee.nameEn, this.activeLang());
    }
    return localizedBilingualName(fee.stageNameAr ?? '', fee.stageNameEn, this.activeLang());
  }

  protected formatAmount(fee: TuitionFeeDto): string {
    const amount = this.localeFormat.formatCurrency(fee.amount ?? 0, fee.currencyCode ?? 'EGP');
    return fee.isStartingFrom
      ? `${this.transloco.translate('portal.fees.startingFrom')} ${amount}`
      : amount;
  }

  protected taxonomyName(item: TaxonomyItemDto): string {
    return item.name ?? '';
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    const raw = this.form.getRawValue();
    this.saving.set(true);
    this.api
      .createTuitionFee(schoolId, {
        branchId: raw.branchId,
        educationalStageId: raw.educationalStageId,
        academicYearId: raw.academicYearId,
        category: raw.category,
        currencyCode: raw.currencyCode,
        amount: raw.amount,
        isStartingFrom: raw.isStartingFrom,
        notesAr: raw.notesAr || undefined,
        notesEn: raw.notesEn || undefined,
        internalNotesAr: raw.internalNotesAr || undefined,
        internalNotesEn: raw.internalNotesEn || undefined,
        sortOrder: raw.sortOrder,
      })
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.showForm.set(false);
          this.loadFees();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected toggleActive(fee: TuitionFeeDto): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId || !fee.id) {
      return;
    }

    const request$ = fee.isActive
      ? this.api.deactivateTuitionFee(schoolId, fee.id)
      : this.api.activateTuitionFee(schoolId, fee.id);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadFees();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  protected togglePublish(fee: TuitionFeeDto): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId || !fee.id) {
      return;
    }

    const request$ = fee.isPublished
      ? this.api.unpublishTuitionFee(schoolId, fee.id)
      : this.api.publishTuitionFee(schoolId, fee.id);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadFees();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  protected saveVisibility(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    const raw = this.visibilityForm.getRawValue().feeVisibilityPolicy;
    const policy =
      raw === '1' ? FeeVisibilityPolicy._1 : raw === '2' ? FeeVisibilityPolicy._2 : null;

    this.saving.set(true);
    this.api
      .updateFeeVisibility(schoolId, { feeVisibilityPolicy: policy ?? undefined })
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.feeVisibility.set(result.data?.feeVisibilityPolicy ?? null);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private loadFees(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api.listTuitionFees(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.loading.set(false);
      if (result.succeeded && result.data) {
        this.fees.set(result.data);
      }
    });

    this.api.listBranches(schoolId).subscribe((result) => {
      if (result.succeeded && result.data) {
        this.branches.set(result.data.filter((b) => b.isActive));
      }
    });

    this.api.getProfile(schoolId).subscribe((result) => {
      if (result.succeeded && result.data) {
        const policy = result.data.feeVisibilityPolicy ?? null;
        this.feeVisibility.set(policy);
        this.visibilityForm.patchValue({
          feeVisibilityPolicy:
            policy === FeeVisibilityPolicy._1 ? '1' : policy === FeeVisibilityPolicy._2 ? '2' : '',
        });
      }
    });
  }
}
