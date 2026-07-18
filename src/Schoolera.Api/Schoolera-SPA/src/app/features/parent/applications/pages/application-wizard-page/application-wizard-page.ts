import { Component, computed, DestroyRef, inject, OnInit, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import {
  debounceTime,
  distinctUntilChanged,
  finalize,
  of,
  Subject,
  switchMap,
  firstValueFrom,
} from 'rxjs';

import {
  AdmissionApplicationDetailDto,
  AgeEligibilityResultDto,
  ChildAgeEligibilityResultCode,
  ChildDocumentDto,
  ChildGender,
  ChildProfileListItemDto,
  GenderType,
  ParentProfileDto,
  PublicSchoolProfileDto,
  TaxonomyItemDto,
} from '../../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { sanitizeReturnUrl } from '../../../../../core/auth/return-url';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../../shared/ui/form-field/form-field';
import { ToastService } from '../../../../../shared/ui/toast/toast.service';
import { ConfirmationDialog } from '../../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../../school-portal/components/portal-page-header/portal-page-header';
import { SchoolsApi } from '../../../../schools/data-access/schools.api';
import { TaxonomiesApi } from '../../../../taxonomies/data-access/taxonomies.api';
import {
  mapAdmissionErrorSummaryItems,
  translateAdmissionErrorCodes,
} from '../../../data-access/admission-errors';
import { ParentApi } from '../../../data-access/parent.api';
import {
  AdmissionApplicationWithRequirements,
  AdmissionRequirementChecklistItem,
  AdmissionRequirementKind,
} from '../../../data-access/admission-requirements.models';
import { MissingAdmissionQuestion } from '../../../data-access/admission-questions.models';
import { ApplicationStepper, ApplicationStepDef } from '../../components/application-stepper/application-stepper';
import { ApplicationSummary } from '../../components/application-summary/application-summary';
import { ApplicationRequirementsChecklist } from '../../components/application-requirements-checklist/application-requirements-checklist';
import { ApplicationQuestionsForm } from '../../components/application-questions-form/application-questions-form';
import { AttachmentList } from '../../components/attachment-list/attachment-list';
import { HasUnsavedApplicationChanges } from '../../guards/unsaved-application.models';

type SaveState = 'idle' | 'saving' | 'saved' | 'error';

@Component({
  selector: 'se-application-wizard-page',
  imports: [
    ApplicationStepper,
    ApplicationSummary,
    ApplicationRequirementsChecklist,
    ApplicationQuestionsForm,
    AttachmentList,
    ConfirmationDialog,
    FormErrorSummary,
    FormField,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './application-wizard-page.html',
  styleUrl: './application-wizard-page.scss',
})
export class ApplicationWizardPage implements OnInit, HasUnsavedApplicationChanges {
  private readonly api = inject(ParentApi);
  private readonly schoolsApi = inject(SchoolsApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly notesSave$ = new Subject<string>();
  private readonly questionsForm = viewChild(ApplicationQuestionsForm);

  protected readonly steps: ReadonlyArray<ApplicationStepDef> = [
    { id: 'student', labelKey: 'parent.applications.wizard.steps.student' },
    { id: 'school', labelKey: 'parent.applications.wizard.steps.school' },
    { id: 'parent', labelKey: 'parent.applications.wizard.steps.parent' },
    { id: 'notes', labelKey: 'parent.applications.wizard.steps.notes' },
    { id: 'questions', labelKey: 'parent.applications.wizard.steps.questions' },
    { id: 'review', labelKey: 'parent.applications.wizard.steps.review' },
  ];

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);
  protected readonly currentStep = signal(0);
  protected readonly maxReachableStep = signal(0);
  protected readonly children = signal<ChildProfileListItemDto[]>([]);
  protected readonly profile = signal<PublicSchoolProfileDto | null>(null);
  protected readonly parentProfile = signal<ParentProfileDto | null>(null);
  protected readonly academicYears = signal<TaxonomyItemDto[]>([]);
  protected readonly application = signal<AdmissionApplicationWithRequirements | null>(null);
  protected readonly ageEligibility = signal<AgeEligibilityResultDto | null>(null);
  protected readonly creating = signal(false);
  protected readonly saving = signal(false);
  protected readonly submitting = signal(false);
  protected readonly bypassUnsavedGuard = signal(false);
  protected readonly saveState = signal<SaveState>('idle');
  protected readonly uploading = signal(false);
  protected readonly uploadPercent = signal(0);
  protected readonly vaultDocuments = signal<ChildDocumentDto[]>([]);
  protected readonly copyingFromVault = signal(false);
  protected readonly showSubmitDialog = signal(false);
  protected readonly showScopeChangeDialog = signal(false);
  protected readonly dirty = signal(false);
  protected readonly requirementsForStep = computed(() => {
    const app = this.application();
    const requirements = app?.requirements ?? [];
    const step = this.currentStep();

    if (step === 2) {
      return requirements.filter(
        (item) =>
          item.kind === AdmissionRequirementKind.InformationalText ||
          item.kind === AdmissionRequirementKind.ParentProfileField ||
          item.wizardSection === 'parent',
      );
    }

    if (step === 3) {
      return requirements.filter(
        (item) =>
          item.kind === AdmissionRequirementKind.ApplicationDocument ||
          item.kind === AdmissionRequirementKind.ChildProfileField ||
          item.wizardSection === 'documents' ||
          item.wizardSection === 'child',
      );
    }

    return requirements;
  });

  protected readonly canUploadAttachments = computed(
    () => !!this.application()?.capabilities?.canUploadAttachments,
  );

  readonly schoolForm = this.formBuilder.nonNullable.group({
    childProfileId: ['', Validators.required],
    schoolBranchId: ['', Validators.required],
    educationalStageId: ['', Validators.required],
    gradeId: ['', Validators.required],
    academicYearId: ['', Validators.required],
    parentNotes: [''],
    consentAccurate: [false, Validators.requiredTrue],
    consentTerms: [false, Validators.requiredTrue],
  });

  protected readonly isEditMode = computed(() => !!this.route.snapshot.paramMap.get('applicationId'));
  protected readonly titleKey = computed(() =>
    this.isEditMode()
      ? 'parent.applications.wizard.titleEdit'
      : 'parent.applications.wizard.titleNew',
  );

  protected readonly selectedChild = computed(() => {
    const id = this.schoolForm.controls.childProfileId.value;
    return this.children().find((child) => child.id === id) ?? null;
  });

  protected readonly openOfferings = computed(() => {
    const offerings = this.profile()?.offerings ?? [];
    const child = this.selectedChild();
    return offerings.filter((offering) => {
      if (!offering.isAdmissionOpen) {
        return false;
      }
      if (!child?.gender) {
        return true;
      }
      return this.isGenderEligible(child.gender, offering.genderType);
    });
  });

  protected readonly branchOptions = computed(() => {
    const branches = this.profile()?.branches ?? [];
    const openBranchIds = new Set(
      this.openOfferings()
        .map((o) => o.branchId)
        .filter((id): id is string => !!id),
    );
    return branches.filter((branch) => branch.id && openBranchIds.has(branch.id));
  });

  protected readonly stageOptions = computed(() => {
    const branchId = this.schoolForm.controls.schoolBranchId.value;
    const map = new Map<string, { id: string; name: string }>();
    for (const offering of this.openOfferings()) {
      if (offering.branchId !== branchId || !offering.educationalStageId) {
        continue;
      }
      map.set(offering.educationalStageId, {
        id: offering.educationalStageId,
        name: offering.stageName ?? '',
      });
    }
    return [...map.values()];
  });

  protected readonly gradeOptions = computed(() => {
    const branchId = this.schoolForm.controls.schoolBranchId.value;
    const stageId = this.schoolForm.controls.educationalStageId.value;
    const grades = new Map<string, { id: string; name: string }>();
    for (const offering of this.openOfferings()) {
      if (offering.branchId !== branchId || offering.educationalStageId !== stageId) {
        continue;
      }
      for (const grade of offering.grades ?? []) {
        if (grade.id) {
          grades.set(grade.id, { id: grade.id, name: grade.name ?? '' });
        }
      }
    }
    return [...grades.values()];
  });

  protected readonly selectedTuition = computed(() => {
    const fees = this.profile()?.fees ?? [];
    if (!fees.length) {
      return null;
    }
    const gradeName = this.gradeOptions().find(
      (g) => g.id === this.schoolForm.controls.gradeId.value,
    )?.name;
    const stageName = this.stageOptions().find(
      (s) => s.id === this.schoolForm.controls.educationalStageId.value,
    )?.name;
    const branchName = this.branchOptions().find(
      (b) => b.id === this.schoolForm.controls.schoolBranchId.value,
    )?.name;
    return (
      fees.find(
        (fee) =>
          (!branchName || fee.branchName === branchName) &&
          (!stageName || fee.stageName === stageName) &&
          (!gradeName || !fee.gradeName || fee.gradeName === gradeName),
      ) ?? fees[0]
    );
  });

  protected readonly addChildReturnUrl = computed(() => {
    const query = this.route.snapshot.queryParamMap;
    const params = new URLSearchParams();
    const schoolSlug = this.profile()?.slug ?? query.get('schoolSlug');
    if (schoolSlug) {
      params.set('schoolSlug', schoolSlug);
    }
    for (const key of ['branchId', 'stageId', 'gradeId', 'academicYearId'] as const) {
      const value =
        key === 'branchId'
          ? this.schoolForm.controls.schoolBranchId.value || query.get(key)
          : key === 'stageId'
            ? this.schoolForm.controls.educationalStageId.value || query.get(key)
            : key === 'gradeId'
              ? this.schoolForm.controls.gradeId.value || query.get(key)
              : this.schoolForm.controls.academicYearId.value || query.get(key);
      if (value) {
        params.set(key, value);
      }
    }
    const qs = params.toString();
    return `/parent/applications/new${qs ? `?${qs}` : ''}`;
  });

  protected readonly profileReturnUrl = computed(() => {
    const app = this.application();
    if (app?.id) {
      return `/parent/applications/${app.id}/edit`;
    }
    return this.addChildReturnUrl();
  });

  ngOnInit(): void {
    this.schoolForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.schoolForm.dirty) {
        this.dirty.set(true);
      }
    });

    this.schoolForm.controls.schoolBranchId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.schoolForm.controls.educationalStageId.setValue('');
        this.schoolForm.controls.gradeId.setValue('');
      });

    this.schoolForm.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.schoolForm.controls.gradeId.setValue('');
      });

    this.notesSave$
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap((notes) => {
          const app = this.application();
          if (!app?.id || !app.capabilities?.canEdit) {
            return of(null);
          }
          this.saveState.set('saving');
          return this.api
            .updateAdmissionApplication(app.id, this.buildUpdateBody(notes))
            .pipe(finalize(() => undefined));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result) {
          return;
        }
        if (result.succeeded && result.data) {
          this.setApplication(result.data);
          this.schoolForm.markAsPristine();
          this.dirty.set(false);
          this.saveState.set('saved');
          return;
        }
        this.saveState.set('error');
        this.summaryErrors.set(mapAdmissionErrorSummaryItems(this.transloco, result.errorCodes));
      });

    this.schoolForm.controls.parentNotes.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((notes) => {
        if (this.application()?.capabilities?.canEdit && this.currentStep() === 3) {
          this.notesSave$.next(notes ?? '');
        }
      });

    void this.bootstrap();
  }

  hasUnsavedChanges(): boolean {
    if (this.bypassUnsavedGuard() || this.saving() || this.submitting() || this.creating()) {
      return false;
    }
    return this.dirty() && this.schoolForm.dirty;
  }

  selectChild(childId: string | undefined): void {
    if (!childId) {
      return;
    }
    this.schoolForm.controls.childProfileId.setValue(childId);
    this.schoolForm.controls.childProfileId.markAsDirty();
    this.dirty.set(true);
  }

  protected formatBirthDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDate(value) : '—';
  }

  protected genderLabel(gender: ChildGender | undefined): string {
    if (gender === ChildGender._1) {
      return this.transloco.translate('parent.enums.gender.male');
    }
    if (gender === ChildGender._2) {
      return this.transloco.translate('parent.enums.gender.female');
    }
    return '—';
  }

  protected goToStep(index: number): void {
    if (index <= this.maxReachableStep()) {
      this.currentStep.set(index);
      if (index === 4) {
        this.enterQuestionsStep();
      }
    }
  }

  async next(): Promise<void> {
    this.summaryErrors.set([]);
    const step = this.currentStep();

    if (step === 0) {
      if (!this.schoolForm.controls.childProfileId.value) {
        this.summaryErrors.set([
          {
            message: this.transloco.translate('parent.applications.wizard.validation.childRequired'),
            fieldId: 'application-child',
          },
        ]);
        return;
      }
      this.advanceTo(1);
      return;
    }

    if (step === 1) {
      if (!this.schoolSelectionValid()) {
        this.markSchoolTouched();
        this.summaryErrors.set([
          {
            message: this.transloco.translate('parent.applications.wizard.validation.schoolRequired'),
          },
        ]);
        return;
      }

      const eligibleToContinue = await this.checkAgeEligibility();
      if (!eligibleToContinue) {
        return;
      }

      if (!this.application()) {
        await this.createDraft();
        return;
      }

      const saved = await this.saveDraft();
      if (saved) {
        this.advanceTo(2);
      }
      return;
    }

    if (step === 2) {
      this.advanceTo(3);
      return;
    }

    if (step === 3) {
      const saved = await this.saveDraft();
      if (saved) {
        this.advanceTo(4);
        this.enterQuestionsStep();
      }
      return;
    }

    if (step === 4) {
      this.advanceTo(5);
      return;
    }
  }

  protected back(): void {
    const step = this.currentStep();
    if (step > 0) {
      this.currentStep.set(step - 1);
    }
  }

  protected ageEligibilityKey(code: ChildAgeEligibilityResultCode | undefined): string {
    const keys: Record<number, string> = {
      1: 'eligible', 2: 'belowMin', 3: 'aboveMax', 4: 'birthDateRequired',
      5: 'invalidBirthDate', 6: 'ruleNotConfigured', 7: 'manualExceptionApproved',
    };
    return `parent.applications.ageEligibility.${keys[code ?? 6] ?? 'ruleNotConfigured'}`;
  }

  protected needsBirthDate(): boolean {
    return this.ageEligibility()?.resultCode === ChildAgeEligibilityResultCode._4;
  }

  protected openSubmitDialog(): void {
    this.summaryErrors.set([]);
    if (!this.schoolForm.controls.consentAccurate.value || !this.schoolForm.controls.consentTerms.value) {
      this.summaryErrors.set([
        {
          message: this.transloco.translate('parent.applications.wizard.validation.consentRequired'),
        },
      ]);
      return;
    }
    if (!this.application()?.capabilities?.canSubmit) {
      this.summaryErrors.set([
        {
          message: this.transloco.translate('parent.applications.wizard.cannotSubmit'),
        },
      ]);
      return;
    }
    this.showSubmitDialog.set(true);
  }

  protected confirmSubmit(): void {
    const app = this.application();
    if (!app?.id || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.api
      .submitAdmissionApplication(app.id)
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.showSubmitDialog.set(false);
        if (!result.succeeded || !result.data?.application) {
          if (result.data?.application) {
            this.setApplication(result.data.application);
          }
          if (result.errorCodes?.includes('admission.application.requirementsIncomplete')) {
            const missing = result.data?.missingRequirements ?? [];
            this.summaryErrors.set(
              missing.map((item) => ({
                message: this.formatMissingRequirementMessage(item),
              })),
            );
            return;
          }
          if (result.errorCodes?.includes('admission.application.questionsIncomplete')) {
            const outcome = result.data as { missingQuestions?: MissingAdmissionQuestion[] } | undefined;
            const missing = outcome?.missingQuestions ?? [];
            this.summaryErrors.set(
              missing.map((item: MissingAdmissionQuestion) => ({
                message: this.formatMissingQuestionMessage(item),
              })),
            );
            return;
          }
          this.summaryErrors.set(mapAdmissionErrorSummaryItems(this.transloco, result.errorCodes));
          return;
        }

        this.bypassUnsavedGuard.set(true);
        this.dirty.set(false);
        this.schoolForm.markAsPristine();
        void this.router.navigate([
          '/parent/applications',
          result.data.application.id,
          'success',
        ]);
      });
  }

  protected onRequirementUpload(event: {
    file: File;
    requirement: AdmissionRequirementChecklistItem;
  }): void {
    this.onUpload({
      file: event.file,
      attachmentType: event.requirement.documentCode ?? 1,
      requirementSnapshotId: event.requirement.snapshotId ?? undefined,
    });
  }

  protected onUpload(event: { file: File; attachmentType: number; requirementSnapshotId?: string }): void {
    const app = this.application();
    if (!app?.id || !app.capabilities?.canUploadAttachments || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.uploadPercent.set(0);
    this.api
      .uploadAdmissionAttachmentWithProgress(
        app.id,
        event.file,
        event.attachmentType,
        event.requirementSnapshotId,
      )
      .pipe(
        finalize(() => this.uploading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (uploadEvent) => {
          if (uploadEvent.kind === 'progress') {
            this.uploadPercent.set(uploadEvent.percent);
            return;
          }
          if (!uploadEvent.result.succeeded || !uploadEvent.result.data) {
            this.toast.error(
              translateAdmissionErrorCodes(this.transloco, uploadEvent.result.errorCodes),
            );
            return;
          }
          this.setApplication(uploadEvent.result.data);
          this.toast.success(this.transloco.translate('parent.applications.wizard.uploadSuccess'));
        },
        error: () => this.toast.error(this.transloco.translate('parent.errors.generic')),
      });
  }

  protected onRequirementCopyFromVault(event: {
    childDocumentId: string;
    requirement: AdmissionRequirementChecklistItem;
  }): void {
    const app = this.application();
    if (!app?.id || !app.capabilities?.canUploadAttachments || this.copyingFromVault()) {
      return;
    }

    this.copyingFromVault.set(true);
    this.api
      .copyFromVault(app.id, {
        childDocumentId: event.childDocumentId,
        requirementSnapshotId: event.requirement.snapshotId ?? undefined,
      })
      .pipe(
        finalize(() => this.copyingFromVault.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.setApplication(result.data);
        this.toast.success(this.transloco.translate('parent.applications.wizard.copyFromVaultSuccess'));
      });
  }

  protected onRemoveAttachment(attachmentId: string): void {
    const app = this.application();
    if (!app?.id || !app.capabilities?.canRemoveAttachments) {
      return;
    }

    this.api
      .removeAdmissionAttachment(app.id, attachmentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.setApplication(result.data);
      });
  }

  protected copyFromVault(childDocumentId: string): void {
    const app = this.application();
    if (!app?.id || !app.capabilities?.canUploadAttachments || this.copyingFromVault()) {
      return;
    }

    this.copyingFromVault.set(true);
    this.api
      .copyFromVault(app.id, { childDocumentId })
      .pipe(
        finalize(() => this.copyingFromVault.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.setApplication(result.data);
        this.toast.success(this.transloco.translate('parent.applications.wizard.copyFromVaultSuccess'));
      });
  }

  protected onDownloadAttachment(attachment: {
    id?: string;
    originalFileName?: string | undefined;
  }): void {
    const app = this.application();
    if (!app?.id || !attachment.id) {
      return;
    }

    this.api
      .downloadAdmissionAttachment(app.id, attachment.id, attachment.originalFileName ?? 'attachment')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.toast.error(this.transloco.translate('parent.errors.generic')),
      });
  }

  protected displayName(item: { name?: string | undefined } | null | undefined): string {
    return item?.name ?? '';
  }

  protected confirmScopeChange(): void {
    this.showScopeChangeDialog.set(false);
    void this.saveDraft(true);
  }

  protected onQuestionsUpdated(data: AdmissionApplicationDetailDto): void {
    this.setApplication(data);
  }

  private enterQuestionsStep(): void {
    this.questionsForm()?.ensureSnapshots();
  }

  private advanceTo(step: number): void {
    this.currentStep.set(step);
    this.maxReachableStep.update((max) => Math.max(max, step));
    if (step === 4) {
      this.enterQuestionsStep();
    }
  }

  private schoolSelectionValid(): boolean {
    const { schoolBranchId, educationalStageId, gradeId, academicYearId } = this.schoolForm.controls;
    return !!(
      schoolBranchId.value &&
      educationalStageId.value &&
      gradeId.value &&
      academicYearId.value &&
      this.profile()?.id
    );
  }

  private markSchoolTouched(): void {
    this.schoolForm.controls.schoolBranchId.markAsTouched();
    this.schoolForm.controls.educationalStageId.markAsTouched();
    this.schoolForm.controls.gradeId.markAsTouched();
    this.schoolForm.controls.academicYearId.markAsTouched();
  }

  private async createDraft(): Promise<void> {
    if (this.creating()) {
      return;
    }

    this.creating.set(true);
    this.summaryErrors.set([]);

    const raw = this.schoolForm.getRawValue();
    const slug = this.profile()?.slug;

    this.api
      .createAdmissionApplication({
        childProfileId: raw.childProfileId,
        schoolId: this.profile()?.id,
        schoolSlug: slug,
        schoolBranchId: raw.schoolBranchId,
        educationalStageId: raw.educationalStageId,
        gradeId: raw.gradeId,
        academicYearId: raw.academicYearId,
        parentNotes: raw.parentNotes.trim() || undefined,
      })
      .pipe(
        finalize(() => this.creating.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result.succeeded || !result.data?.id) {
          this.summaryErrors.set(mapAdmissionErrorSummaryItems(this.transloco, result.errorCodes));
          // Duplicate active application: try to surface existing id if present in errors only via message
          return;
        }

        this.setApplication(result.data);
        this.bypassUnsavedGuard.set(true);
        this.dirty.set(false);
        this.schoolForm.markAsPristine();
        void this.router.navigate(['/parent/applications', result.data.id, 'edit'], {
          replaceUrl: true,
        });
      });
  }

  private saveDraft(confirmClearQuestionAnswers = false): Promise<boolean> {
    const app = this.application();
    if (!app?.id) {
      return Promise.resolve(false);
    }
    if (!app.capabilities?.canEdit) {
      void this.router.navigate(['/parent/applications', app.id]);
      return Promise.resolve(false);
    }

    this.saving.set(true);
    this.saveState.set('saving');
    this.summaryErrors.set([]);

    return new Promise((resolve) => {
      this.api
        .updateAdmissionApplication(app.id!, {
          ...this.buildUpdateBody(),
          confirmClearQuestionAnswers,
        })
        .pipe(
          finalize(() => this.saving.set(false)),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe((result) => {
          if (!result.succeeded || !result.data) {
            if (
              !confirmClearQuestionAnswers &&
              result.errorCodes?.includes('admission.application.questionAnswersBlockScopeChange')
            ) {
              this.showScopeChangeDialog.set(true);
              this.saveState.set('idle');
              resolve(false);
              return;
            }
            this.saveState.set('error');
            this.summaryErrors.set(mapAdmissionErrorSummaryItems(this.transloco, result.errorCodes));
            resolve(false);
            return;
          }

          this.setApplication(result.data);
          this.schoolForm.markAsPristine();
          this.dirty.set(false);
          this.saveState.set('saved');
          resolve(true);
        });
    });
  }

  private buildUpdateBody(notes?: string) {
    const raw = this.schoolForm.getRawValue();
    const app = this.application();
    return {
      schoolBranchId: raw.schoolBranchId || app?.schoolBranchId,
      educationalStageId: raw.educationalStageId || app?.educationalStageId,
      gradeId: raw.gradeId || app?.gradeId,
      academicYearId: raw.academicYearId || app?.academicYearId,
      parentNotes: (notes ?? raw.parentNotes).trim() || undefined,
      rowVersion: app?.rowVersion,
    };
  }

  private async bootstrap(): Promise<void> {
    const applicationId = this.route.snapshot.paramMap.get('applicationId');
    const query = this.route.snapshot.queryParamMap;

    this.taxonomiesApi
      .getAcademicYears()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.academicYears.set(result.data);
        }
      });

    this.api
      .listChildren()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.children.set(result.data.filter((child) => child.isActive !== false));
        }
      });

    this.api
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.parentProfile.set(result.data);
        }
      });

    if (applicationId) {
      this.api
        .getAdmissionApplication(applicationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (!result.succeeded || !result.data) {
            this.loading.set(false);
            this.errorMessage.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
            return;
          }

          const app = result.data;
          if (!app.capabilities?.canEdit) {
            void this.router.navigate(['/parent/applications', app.id]);
            return;
          }

          this.setApplication(app);
          if (app.childProfileId) {
            this.loadVaultDocuments(app.childProfileId);
          }
          this.schoolForm.patchValue({
            childProfileId: app.childProfileId ?? '',
            schoolBranchId: app.schoolBranchId ?? '',
            educationalStageId: app.educationalStageId ?? '',
            gradeId: app.gradeId ?? '',
            academicYearId: app.academicYearId ?? '',
            parentNotes: app.parentNotes ?? '',
          });
          this.schoolForm.markAsPristine();
          this.dirty.set(false);
          this.maxReachableStep.set(5);
          this.currentStep.set(2);

          if (app.schoolSlug) {
            this.loadSchool(app.schoolSlug, false);
          } else {
            this.loading.set(false);
          }
        });
      return;
    }

    const schoolSlug = query.get('schoolSlug');
    if (!schoolSlug) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('parent.applications.wizard.schoolContextRequired'));
      return;
    }

    const childId = query.get('childId');
    if (childId) {
      this.schoolForm.controls.childProfileId.setValue(childId);
    }

    this.loadSchool(schoolSlug, true, {
      branchId: query.get('branchId'),
      stageId: query.get('stageId'),
      gradeId: query.get('gradeId'),
      academicYearId: query.get('academicYearId'),
    });
  }

  private loadVaultDocuments(childId: string): void {
    this.api
      .listChildDocuments(childId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.vaultDocuments.set(result.data ?? []);
        }
      });
  }

  private loadSchool(
    slug: string,
    applyHints: boolean,
    hints?: {
      branchId: string | null;
      stageId: string | null;
      gradeId: string | null;
      academicYearId: string | null;
    },
  ): void {
    this.schoolsApi
      .getBySlug(slug)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(this.transloco.translate('parent.applications.wizard.schoolLoadFailed'));
          return;
        }

        const school = result.data;
        if (!school.isAdmissionOpen) {
          this.errorMessage.set(this.transloco.translate('parent.applications.wizard.admissionClosed'));
          return;
        }

        this.profile.set(school);

        if (applyHints && hints) {
          if (hints.branchId) {
            this.schoolForm.controls.schoolBranchId.setValue(hints.branchId);
          }
          if (hints.stageId) {
            this.schoolForm.controls.educationalStageId.setValue(hints.stageId);
          }
          if (hints.gradeId) {
            this.schoolForm.controls.gradeId.setValue(hints.gradeId);
          }
          if (hints.academicYearId) {
            this.schoolForm.controls.academicYearId.setValue(hints.academicYearId);
          }
          this.schoolForm.markAsPristine();
        }

        if (this.schoolForm.controls.childProfileId.value) {
          this.maxReachableStep.set(1);
        }
      });
  }

  private isGenderEligible(
    childGender: ChildGender,
    offeringGender: GenderType | undefined,
  ): boolean {
    if (offeringGender === undefined || offeringGender === GenderType._3) {
      return true;
    }
    if (childGender === ChildGender._1) {
      return offeringGender === GenderType._1;
    }
    if (childGender === ChildGender._2) {
      return offeringGender === GenderType._2;
    }
    return false;
  }

  protected sanitizeProfileLink(): string {
    return sanitizeReturnUrl(this.profileReturnUrl(), '/parent/profile');
  }

  private formatMissingQuestionMessage(item: {
    displayName?: string;
    reasonCode?: string;
  }): string {
    const name =
      item.displayName ??
      this.transloco.translate('parent.applications.wizard.questions.missingItem') ??
      '';
    if (!item.reasonCode) {
      return name;
    }

    const reasonKey = `parent.applications.wizard.questions.reasons.${item.reasonCode.replace(/\./g, '_')}`;
    const reason = this.transloco.translate(reasonKey);
    return reason === reasonKey ? name : `${name} — ${reason}`;
  }

  private formatMissingRequirementMessage(item: {
    displayName?: string;
    reasonCode?: string;
    wizardSection?: string;
    kind?: number;
  }): string {
    const name =
      item.displayName ??
      this.transloco.translate('parent.applications.wizard.requirements.missingItem') ??
      '';
    if (!item.reasonCode) {
      return name;
    }

    const reasonKey = `parent.applications.wizard.requirements.reasons.${item.reasonCode.replace(/\./g, '_')}`;
    const reason = this.transloco.translate(reasonKey);
    return reason === reasonKey ? name : `${name} — ${reason}`;
  }

  private setApplication(data: AdmissionApplicationDetailDto): void {
    this.application.set(data as AdmissionApplicationWithRequirements);
    this.ageEligibility.set(data.ageEligibility ?? null);
  }

  private async checkAgeEligibility(): Promise<boolean> {
    const raw = this.schoolForm.getRawValue();
    const schoolId = this.profile()?.id;
    if (!schoolId) return false;
    try {
      const result = await firstValueFrom(this.api.checkAgeEligibility({
        schoolId,
        schoolBranchId: raw.schoolBranchId,
        educationalStageId: raw.educationalStageId,
        gradeId: raw.gradeId,
        academicYearId: raw.academicYearId,
        childProfileId: raw.childProfileId,
      }));
      if (!result.succeeded || !result.data) {
        this.summaryErrors.set(mapAdmissionErrorSummaryItems(this.transloco, result.errorCodes));
        return false;
      }
      this.ageEligibility.set(result.data);
      return result.data.canContinue !== false;
    } catch {
      this.summaryErrors.set([{ message: this.transloco.translate('parent.errors.generic') }]);
      return false;
    }
  }
}
