import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { GenderType, TaxonomyItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolBranchDto, SchoolStageOfferingDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

@Component({
  selector: 'se-portal-stages-page',
  imports: [
    Button,
    FormField,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-stages-page.html',
  styleUrl: './portal-stages-page.scss',
})
export class PortalStagesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly offerings = signal<readonly SchoolStageOfferingDto[]>([]);
  protected readonly branches = signal<readonly SchoolBranchDto[]>([]);
  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);
  protected readonly showForm = signal(false);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly genderTypes = [
    { value: GenderType._1, labelKey: 'portal.enums.genderType.boys' },
    { value: GenderType._2, labelKey: 'portal.enums.genderType.girls' },
    { value: GenderType._3, labelKey: 'portal.enums.genderType.mixed' },
  ] as const;

  protected readonly form = this.formBuilder.nonNullable.group({
    branchId: ['', Validators.required],
    educationalStageId: ['', Validators.required],
    genderType: [GenderType._3 as GenderType, Validators.required],
    capacity: [null as number | null],
    isAdmissionOpen: [true],
    gradeIds: [[] as string[]],
  });

  ngOnInit(): void {
    this.loadData();
    this.taxonomiesApi.getEducationalStages().subscribe((result) => {
      if (result.succeeded && result.data) {
        this.stages.set(result.data);
      }
    });

    this.form.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((stageId) => {
        this.grades.set([]);
        if (!stageId) {
          return;
        }
        this.taxonomiesApi.getGradesByStage(stageId).subscribe((result) => {
          if (result.succeeded && result.data) {
            this.grades.set(result.data);
          }
        });
      });
  }

  protected stageName(offering: SchoolStageOfferingDto): string {
    return localizedBilingualName(offering.stageNameAr ?? '', offering.stageNameEn, this.activeLang());
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

    this.saving.set(true);
    const raw = this.form.getRawValue();
    this.api
      .createOffering(schoolId, {
        ...raw,
        capacity: raw.capacity ?? undefined,
      })
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.showForm.set(false);
          this.loadData();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected toggleActive(offering: SchoolStageOfferingDto): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId || !offering.id) {
      return;
    }

    const request$ = offering.isActive
      ? this.api.deactivateOffering(schoolId, offering.id)
      : this.api.activateOffering(schoolId, offering.id);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadData();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private loadData(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api.listOfferings(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.loading.set(false);
      if (result.succeeded && result.data) {
        this.offerings.set(result.data);
      }
    });

    this.api.listBranches(schoolId).subscribe((result) => {
      if (result.succeeded && result.data) {
        this.branches.set(result.data.filter((b) => b.isActive));
      }
    });
  }
}
