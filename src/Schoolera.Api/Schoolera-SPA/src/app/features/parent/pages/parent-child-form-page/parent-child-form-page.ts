import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  ChildGender,
  ChildIdentityType,
  ChildStudyLanguage,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { sanitizeReturnUrl } from '../../../../core/auth/return-url';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { Button } from '../../../../shared/ui/button/button';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import {
  mapParentErrorSummaryItems,
  translateParentErrorCodes,
} from '../../data-access/parent-errors';
import { ParentApi } from '../../data-access/parent.api';
import { ChildDocumentVault } from '../../components/child-document-vault/child-document-vault';
import { HasUnsavedChildChanges } from '../../guards/unsaved-child.models';

@Component({
  selector: 'se-parent-child-form-page',
  imports: [
    Button,
    ChildDocumentVault,
    FormErrorSummary,
    FormField,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-child-form-page.html',
  styleUrl: './parent-child-form-page.scss',
})
export class ParentChildFormPage implements OnInit, HasUnsavedChildChanges {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ParentApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);
  protected readonly maskedIdentity = signal<string | null>(null);
  protected readonly replaceIdentity = signal(false);
  private readonly bypassUnsavedGuard = signal(false);
  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);

  protected readonly childId = computed(() => this.route.snapshot.paramMap.get('childId'));
  protected readonly isEditMode = computed(() => !!this.childId());

  protected readonly genders = [
    { value: ChildGender._1, labelKey: 'parent.enums.gender.male' },
    { value: ChildGender._2, labelKey: 'parent.enums.gender.female' },
  ] as const;

  protected readonly identityTypes = [
    { value: ChildIdentityType._1, labelKey: 'parent.enums.identityType.nationalId' },
    { value: ChildIdentityType._2, labelKey: 'parent.enums.identityType.residencyId' },
  ] as const;

  protected readonly studyLanguages = [
    { value: ChildStudyLanguage._1, labelKey: 'parent.enums.studyLanguage.arabic' },
    { value: ChildStudyLanguage._2, labelKey: 'parent.enums.studyLanguage.english' },
    { value: ChildStudyLanguage._3, labelKey: 'parent.enums.studyLanguage.french' },
    { value: ChildStudyLanguage._4, labelKey: 'parent.enums.studyLanguage.german' },
    { value: ChildStudyLanguage._99, labelKey: 'parent.enums.studyLanguage.other' },
  ] as const;

  protected readonly form = this.formBuilder.nonNullable.group({
    fullName: ['', Validators.required],
    identityType: [ChildIdentityType._1 as ChildIdentityType, Validators.required],
    identityValue: ['', Validators.required],
    birthDate: ['', Validators.required],
    gender: [ChildGender._1 as ChildGender, Validators.required],
    educationalStageId: ['', Validators.required],
    currentGradeId: ['', Validators.required],
    currentSchoolName: [''],
    preferredStudyLanguage: [0 as ChildStudyLanguage | 0],
    skills: [''],
    hobbies: [''],
    strengths: [''],
    improvementAreas: [''],
    hasSpecialNeeds: [false],
    specialNeedsNotes: [''],
    healthNotes: [''],
    replaceIdentityType: [ChildIdentityType._1 as ChildIdentityType],
    replaceIdentityValue: [''],
  });

  ngOnInit(): void {
    this.taxonomiesApi
      .getEducationalStages()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.stages.set(result.data);
        }
      });

    this.form.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((stageId) => {
        this.form.controls.currentGradeId.setValue('');
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

    this.form.controls.hasSpecialNeeds.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((hasSpecialNeeds) => {
        const notesControl = this.form.controls.specialNeedsNotes;
        if (hasSpecialNeeds) {
          notesControl.setValidators([Validators.required]);
        } else {
          notesControl.clearValidators();
          notesControl.setValue('');
        }
        notesControl.updateValueAndValidity();
      });

    const childId = this.childId();
    if (!childId) {
      this.loading.set(false);
      return;
    }

    this.form.controls.identityType.clearValidators();
    this.form.controls.identityValue.clearValidators();
    this.form.controls.identityType.updateValueAndValidity();
    this.form.controls.identityValue.updateValueAndValidity();

    this.api
      .getChild(childId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        const child = result.data;
        this.maskedIdentity.set(child.maskedIdentity ?? null);
        this.form.patchValue({
          fullName: child.fullName ?? '',
          birthDate: child.birthDate ?? '',
          gender: child.gender ?? ChildGender._1,
          currentGradeId: child.currentGradeId ?? '',
          currentSchoolName: child.currentSchoolName ?? '',
          preferredStudyLanguage: child.preferredStudyLanguage ?? 0,
          skills: child.skills ?? '',
          hobbies: child.hobbies ?? '',
          strengths: child.strengths ?? '',
          improvementAreas: child.improvementAreas ?? '',
          hasSpecialNeeds: child.hasSpecialNeeds ?? false,
          specialNeedsNotes: child.hasSpecialNeeds ? (child.specialNeedsNotes ?? '') : '',
          healthNotes: child.healthNotes ?? '',
        });

        this.form.controls.educationalStageId.clearValidators();
        this.form.controls.educationalStageId.updateValueAndValidity();
        if (child.currentGradeId) {
          this.resolveStageForGrade(child.currentGradeId);
        }

        if (child.hasSpecialNeeds) {
          this.form.controls.specialNeedsNotes.setValidators([Validators.required]);
          this.form.controls.specialNeedsNotes.updateValueAndValidity();
        }

        this.form.markAsPristine();
      });
  }

  hasUnsavedChanges(): boolean {
    return !this.bypassUnsavedGuard() && !this.saving() && this.form.dirty;
  }

  protected toggleReplaceIdentity(): void {
    this.replaceIdentity.update((value) => !value);
    const replaceValueControl = this.form.controls.replaceIdentityValue;
    if (this.replaceIdentity()) {
      replaceValueControl.setValidators([Validators.required]);
    } else {
      replaceValueControl.clearValidators();
      replaceValueControl.setValue('');
    }
    replaceValueControl.updateValueAndValidity();
  }

  protected save(): void {
    this.summaryErrors.set([]);
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set(this.collectClientSummaryErrors());
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const childId = this.childId();

    if (childId) {
      this.api
        .updateChild(childId, {
          fullName: raw.fullName.trim(),
          birthDate: raw.birthDate,
          gender: raw.gender,
          currentGradeId: raw.currentGradeId,
          currentSchoolName: this.optionalText(raw.currentSchoolName),
          preferredStudyLanguage: raw.preferredStudyLanguage || undefined,
          skills: this.optionalText(raw.skills),
          hobbies: this.optionalText(raw.hobbies),
          strengths: this.optionalText(raw.strengths),
          improvementAreas: this.optionalText(raw.improvementAreas),
          hasSpecialNeeds: raw.hasSpecialNeeds,
          specialNeedsNotes: raw.hasSpecialNeeds ? raw.specialNeedsNotes.trim() : undefined,
          healthNotes: this.optionalText(raw.healthNotes),
          identityType: this.replaceIdentity() ? raw.replaceIdentityType : undefined,
          identityValue: this.replaceIdentity() ? raw.replaceIdentityValue.trim() : undefined,
        })
        .pipe(
          finalize(() => this.saving.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe((result) => this.handleSaveResult(result));
      return;
    }

    this.api
      .createChild({
        fullName: raw.fullName.trim(),
        identityType: raw.identityType,
        identityValue: raw.identityValue.trim(),
        birthDate: raw.birthDate,
        gender: raw.gender,
        currentGradeId: raw.currentGradeId,
        currentSchoolName: this.optionalText(raw.currentSchoolName),
        preferredStudyLanguage: raw.preferredStudyLanguage || undefined,
        skills: this.optionalText(raw.skills),
        hobbies: this.optionalText(raw.hobbies),
        strengths: this.optionalText(raw.strengths),
        improvementAreas: this.optionalText(raw.improvementAreas),
        hasSpecialNeeds: raw.hasSpecialNeeds,
        specialNeedsNotes: raw.hasSpecialNeeds ? raw.specialNeedsNotes.trim() : undefined,
        healthNotes: this.optionalText(raw.healthNotes),
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => this.handleSaveResult(result));
  }

  protected fieldError(controlName: keyof typeof this.form.controls): string | undefined {
    const control = this.form.controls[controlName];
    if (!control.touched || !control.errors) {
      return undefined;
    }
    if (control.errors['required']) {
      return this.transloco.translate('parent.childForm.validation.required');
    }
    return undefined;
  }

  protected taxonomyName(item: TaxonomyItemDto): string {
    return item.name ?? '';
  }

  private resolveStageForGrade(gradeId: string): void {
    const stages = this.stages();
    if (!stages.length) {
      this.taxonomiesApi
        .getEducationalStages()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (result.succeeded && result.data) {
            this.stages.set(result.data);
            this.findStageForGrade(gradeId, result.data);
          }
        });
      return;
    }
    this.findStageForGrade(gradeId, stages);
  }

  private findStageForGrade(gradeId: string, stages: TaxonomyItemDto[]): void {
    for (const stage of stages) {
      if (!stage.id) {
        continue;
      }
      this.taxonomiesApi.getGradesByStage(stage.id).subscribe((result) => {
        if (!result.succeeded || !result.data?.some((grade) => grade.id === gradeId)) {
          return;
        }
        this.form.controls.educationalStageId.setValue(stage.id!);
        this.grades.set(result.data);
        this.form.controls.currentGradeId.setValue(gradeId);
      });
    }
  }

  private handleSaveResult(result: {
    succeeded?: boolean;
    data?: { id?: string } | null;
    errorCodes?: string[] | undefined;
  }): void {
    if (result.succeeded) {
      this.bypassUnsavedGuard.set(true);
      this.form.markAsPristine();
      this.toast.success(this.transloco.translate('parent.childForm.saved'));
      const returnUrl = sanitizeReturnUrl(
        this.route.snapshot.queryParamMap.get('returnUrl'),
        '/parent/children',
      );

      if (returnUrl.includes('/parent/applications') && result.data?.id) {
        const separator = returnUrl.includes('?') ? '&' : '?';
        void this.router.navigateByUrl(`${returnUrl}${separator}childId=${encodeURIComponent(result.data.id)}`);
        return;
      }

      void this.router.navigateByUrl(returnUrl);
      return;
    }
    this.summaryErrors.set(mapParentErrorSummaryItems(this.transloco, result.errorCodes));
  }

  private optionalText(value: string): string | undefined {
    return value.trim() || undefined;
  }

  private collectClientSummaryErrors(): FormErrorSummaryItem[] {
    const items: FormErrorSummaryItem[] = [];
    const addRequired = (controlName: keyof typeof this.form.controls, messageKey: string, fieldId: string) => {
      if (this.form.controls[controlName].invalid) {
        items.push({
          message: this.transloco.translate(messageKey),
          fieldId,
        });
      }
    };

    addRequired('fullName', 'parent.childForm.validation.fullNameRequired', 'parent-child-fullName');
    if (!this.isEditMode()) {
      addRequired('identityValue', 'parent.childForm.validation.identityRequired', 'parent-child-identityValue');
      addRequired('educationalStageId', 'parent.childForm.validation.stageRequired', 'parent-child-stage');
    }
    addRequired('birthDate', 'parent.childForm.validation.birthDateRequired', 'parent-child-birthDate');
    addRequired('currentGradeId', 'parent.childForm.validation.gradeRequired', 'parent-child-grade');

    if (this.form.controls.hasSpecialNeeds.value && this.form.controls.specialNeedsNotes.invalid) {
      items.push({
        message: this.transloco.translate('parent.childForm.validation.specialNeedsNotesRequired'),
        fieldId: 'parent-child-specialNeedsNotes',
      });
    }

    if (this.isEditMode() && this.replaceIdentity() && this.form.controls.replaceIdentityValue.invalid) {
      items.push({
        message: this.transloco.translate('parent.childForm.validation.replaceIdentityRequired'),
        fieldId: 'parent-child-replaceIdentityValue',
      });
    }

    return items;
  }
}
