import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { TaxonomyItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { ConfirmationDialog } from '../../components/confirmation-dialog/confirmation-dialog';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import {
  ChildAgeEligibilityPublicationStatus,
  ChildAgeEligibilityResultCode,
  ChildAgeReferenceDateMode,
  CreateSchoolChildAgeEligibilityRuleRequest,
  ListSchoolChildAgeEligibilityRulesParams,
  PreviewSchoolChildAgeEligibilityDto,
  SchoolChildAgeEligibilityRuleDetailDto,
  SchoolChildAgeEligibilityRuleListItemDto,
  UpdateSchoolChildAgeEligibilityRuleRequest,
} from '../../data-access/school-portal.models';

type ConfirmKind = 'deactivate' | 'clone';

@Component({
  selector: 'se-portal-age-eligibility-rules-page',
  imports: [
    BilingualFieldGroup, Button, ConfirmationDialog, FormField, PortalEmptyState,
    PortalErrorState, PortalLoadingSkeleton, PortalPageHeader, ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-age-eligibility-rules-page.html',
  styleUrl: './portal-age-eligibility-rules-page.scss',
})
export class PortalAgeEligibilityRulesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomies = inject(TaxonomiesApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly PublicationStatus = ChildAgeEligibilityPublicationStatus;
  protected readonly ReferenceDateMode = ChildAgeReferenceDateMode;
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly previewLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<readonly SchoolChildAgeEligibilityRuleListItemDto[]>([]);
  protected readonly branches = signal<readonly { id: string; name: string }[]>([]);
  protected readonly stages = signal<readonly TaxonomyItemDto[]>([]);
  protected readonly grades = signal<readonly TaxonomyItemDto[]>([]);
  protected readonly academicYears = signal<readonly TaxonomyItemDto[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly editingRowVersion = signal<string | null>(null);
  protected readonly previewResult = signal<PreviewSchoolChildAgeEligibilityDto | null>(null);
  protected readonly confirmAction = signal<{ kind: ConfirmKind; item: SchoolChildAgeEligibilityRuleListItemDto } | null>(null);

  protected readonly filterForm = this.fb.nonNullable.group({
    branchId: [''], educationalStageId: [''], gradeId: [''], academicYearId: [''],
    publicationStatus: [''], isActive: [''],
  });
  protected readonly form = this.fb.nonNullable.group({
    schoolBranchId: [''],
    educationalStageId: ['', Validators.required],
    gradeId: [''],
    academicYearId: ['', Validators.required],
    minAgeCompletedMonths: [0, [Validators.required, Validators.min(0)]],
    maxAgeCompletedMonths: [0, [Validators.required, Validators.min(0)]],
    referenceDateMode: [ChildAgeReferenceDateMode._1 as number, Validators.required],
    explanationAr: [''],
    explanationEn: [''],
    manualExceptionAllowed: [false],
  });
  protected readonly previewForm = this.fb.nonNullable.group({
    birthDate: ['', Validators.required],
    schoolBranchId: [''], educationalStageId: ['', Validators.required],
    gradeId: [''], academicYearId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadItems();
    this.filterForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.loadItems());
    this.filterForm.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((id) => this.loadGrades(id));
    this.form.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((id) => this.loadGrades(id));
    this.previewForm.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((id) => this.loadGrades(id));
  }

  protected openCreate(): void {
    this.editingId.set(null);
    this.editingRowVersion.set(null);
    this.form.reset({
      schoolBranchId: '', educationalStageId: '', gradeId: '', academicYearId: '',
      minAgeCompletedMonths: 0, maxAgeCompletedMonths: 0,
      referenceDateMode: ChildAgeReferenceDateMode._1, explanationAr: '',
      explanationEn: '', manualExceptionAllowed: false,
    });
    this.showForm.set(true);
  }

  protected openEdit(item: SchoolChildAgeEligibilityRuleListItemDto): void {
    if (!item.id) return;
    this.api.getAgeEligibilityRule(this.schoolId(), item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.patchForm(result.data);
        this.editingId.set(item.id!);
        this.editingRowVersion.set(result.data.rowVersion ?? null);
        this.showForm.set(true);
      });
  }

  protected cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
    this.editingRowVersion.set(null);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    if (raw.minAgeCompletedMonths > raw.maxAgeCompletedMonths) {
      this.errorMessage.set(this.transloco.translate('portal.ageEligibilityRules.validation.ageRange'));
      return;
    }
    const body = {
      schoolBranchId: raw.schoolBranchId || undefined,
      educationalStageId: raw.educationalStageId,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId,
      minAgeCompletedMonths: raw.minAgeCompletedMonths,
      maxAgeCompletedMonths: raw.maxAgeCompletedMonths,
      referenceDateMode: ChildAgeReferenceDateMode._1,
      explanationAr: raw.explanationAr.trim() || undefined,
      explanationEn: raw.explanationEn.trim() || undefined,
      manualExceptionAllowed: raw.manualExceptionAllowed,
    };
    const id = this.editingId();
    const request$ = id
      ? this.api.updateAgeEligibilityRule(this.schoolId(), id, {
          ...body, rowVersion: this.editingRowVersion() ?? undefined,
        } as UpdateSchoolChildAgeEligibilityRuleRequest)
      : this.api.createAgeEligibilityRule(this.schoolId(), body as CreateSchoolChildAgeEligibilityRuleRequest);
    this.saving.set(true);
    request$.pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.cancelForm();
          this.loadItems();
        } else {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
        }
      });
  }

  protected publish(item: SchoolChildAgeEligibilityRuleListItemDto): void {
    this.mutate((schoolId) => this.api.publishAgeEligibilityRule(schoolId, item.id!));
  }
  protected unpublish(item: SchoolChildAgeEligibilityRuleListItemDto): void {
    this.mutate((schoolId) => this.api.unpublishAgeEligibilityRule(schoolId, item.id!));
  }
  protected requestConfirm(kind: ConfirmKind, item: SchoolChildAgeEligibilityRuleListItemDto): void {
    this.confirmAction.set({ kind, item });
  }
  protected confirm(): void {
    const action = this.confirmAction();
    this.confirmAction.set(null);
    if (!action?.item.id) return;
    this.mutate((schoolId) => action.kind === 'clone'
      ? this.api.cloneAgeEligibilityRule(schoolId, action.item.id!)
      : this.api.deactivateAgeEligibilityRule(schoolId, action.item.id!));
  }

  protected preview(): void {
    if (this.previewForm.invalid) {
      this.previewForm.markAllAsTouched();
      return;
    }
    const raw = this.previewForm.getRawValue();
    this.previewLoading.set(true);
    this.api.previewAgeEligibility(this.schoolId(), {
      birthDate: raw.birthDate,
      schoolBranchId: raw.schoolBranchId || undefined,
      educationalStageId: raw.educationalStageId,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId,
    }).pipe(finalize(() => this.previewLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) this.previewResult.set(result.data ?? null);
        else this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected resultKey(code: ChildAgeEligibilityResultCode | undefined): string {
    const names: Record<number, string> = {
      1: 'eligible', 2: 'belowMin', 3: 'aboveMax', 4: 'birthDateRequired',
      5: 'invalidBirthDate', 6: 'ruleNotConfigured', 7: 'manualExceptionApproved',
    };
    return `portal.ageEligibilityRules.results.${names[code ?? 6] ?? 'ruleNotConfigured'}`;
  }

  protected retry(): void { this.loadItems(); }
  private schoolId(): string { return this.route.parent?.snapshot.paramMap.get('schoolId') ?? ''; }
  private mutate(factory: (schoolId: string) => Observable<{ succeeded?: boolean; errorCodes?: string[] }>): void {
    factory(this.schoolId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) this.loadItems();
      else this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }
  private loadItems(): void {
    this.loading.set(true);
    const raw = this.filterForm.getRawValue();
    const params: ListSchoolChildAgeEligibilityRulesParams = {
      branchId: raw.branchId || undefined, educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined, academicYearId: raw.academicYearId || undefined,
      publicationStatus: raw.publicationStatus ? Number(raw.publicationStatus) : undefined,
      isActive: raw.isActive === '' ? undefined : raw.isActive === 'true',
    };
    this.api.listAgeEligibilityRules(this.schoolId(), params)
      .pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.items.set(result.data ?? []);
          this.errorMessage.set(null);
        } else this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
  private loadLookups(): void {
    this.api.listBranches(this.schoolId()).subscribe((r) => {
      if (r.succeeded) this.branches.set((r.data ?? []).filter((b) => !!b.id).map((b) => ({
        id: b.id!, name: b.nameAr || b.nameEn || '',
      })));
    });
    this.taxonomies.getEducationalStages().subscribe((r) => { if (r.succeeded) this.stages.set(r.data ?? []); });
    this.taxonomies.getAcademicYears().subscribe((r) => { if (r.succeeded) this.academicYears.set(r.data ?? []); });
  }
  private loadGrades(stageId: string): void {
    if (!stageId) { this.grades.set([]); return; }
    this.taxonomies.getGradesByStage(stageId).subscribe((r) => { if (r.succeeded) this.grades.set(r.data ?? []); });
  }
  private patchForm(detail: SchoolChildAgeEligibilityRuleDetailDto): void {
    this.loadGrades(detail.educationalStageId ?? '');
    this.form.patchValue({
      schoolBranchId: detail.schoolBranchId ?? '', educationalStageId: detail.educationalStageId ?? '',
      gradeId: detail.gradeId ?? '', academicYearId: detail.academicYearId ?? '',
      minAgeCompletedMonths: detail.minAgeCompletedMonths ?? 0,
      maxAgeCompletedMonths: detail.maxAgeCompletedMonths ?? 0,
      referenceDateMode: detail.referenceDateMode ?? ChildAgeReferenceDateMode._1,
      explanationAr: detail.explanationAr ?? '', explanationEn: detail.explanationEn ?? '',
      manualExceptionAllowed: detail.manualExceptionAllowed ?? false,
    });
  }
}
