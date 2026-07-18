import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { TaxonomyItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
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
import {
  CreateSchoolInterviewAssessmentPolicyRequest,
  HybridDeliverySelectionAuthority,
  InterviewAssessmentDeliveryMode,
  InterviewAssessmentPolicyPublicationStatus,
  InterviewAssessmentRequiredParticipants,
  InterviewAssessmentRequirementMode,
  ListSchoolInterviewAssessmentPoliciesParams,
  PreviewSchoolInterviewAssessmentPolicyApplicabilityDto,
  SafeMeetingProviderOptionDto,
  SchoolInterviewAssessmentPolicyDetail,
  SchoolInterviewAssessmentPolicyListItem,
  UpdateSchoolInterviewAssessmentPolicyRequest,
} from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

type ConfirmKind = 'deactivate' | 'clone';

@Component({
  selector: 'se-portal-interview-assessment-policies-page',
  imports: [
    BilingualFieldGroup,
    Button,
    ConfirmationDialog,
    FormField,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-interview-assessment-policies-page.html',
  styleUrl: './portal-interview-assessment-policies-page.scss',
})
export class PortalInterviewAssessmentPoliciesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly RequirementMode = InterviewAssessmentRequirementMode;
  protected readonly DeliveryMode = InterviewAssessmentDeliveryMode;
  protected readonly Participants = InterviewAssessmentRequiredParticipants;
  protected readonly HybridAuthority = HybridDeliverySelectionAuthority;
  protected readonly PublicationStatus = InterviewAssessmentPolicyPublicationStatus;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly previewLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<readonly SchoolInterviewAssessmentPolicyListItem[]>([]);
  protected readonly previewResult = signal<PreviewSchoolInterviewAssessmentPolicyApplicabilityDto | null>(
    null,
  );
  protected readonly meetingProviders = signal<readonly SafeMeetingProviderOptionDto[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly editingRowVersion = signal<string | null>(null);
  protected readonly confirmAction = signal<{ kind: ConfirmKind; item: SchoolInterviewAssessmentPolicyListItem } | null>(
    null,
  );
  protected readonly branches = signal<readonly { id: string; name: string }[]>([]);
  protected readonly stages = signal<TaxonomyItemDto[]>([]);
  protected readonly grades = signal<TaxonomyItemDto[]>([]);
  protected readonly academicYears = signal<TaxonomyItemDto[]>([]);
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly filterForm = this.formBuilder.nonNullable.group({
    branchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
    publicationStatus: [''],
    isActive: [''],
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    requirementMode: [InterviewAssessmentRequirementMode._2 as number, Validators.required],
    deliveryMode: [InterviewAssessmentDeliveryMode._1 as number],
    requiredParticipants: [InterviewAssessmentRequiredParticipants._1 as number],
    expectedDurationMinutes: [30 as number | null],
    bookingWindowOpensDaysBefore: [14 as number | null],
    bookingWindowClosesDaysBefore: [1 as number | null],
    minimumSchedulingLeadTimeHours: [24 as number | null],
    parentReschedulingAllowed: [true],
    maxParentRescheduleAttempts: [2],
    parentCancellationAllowed: [true],
    preparationNotesAr: [''],
    preparationNotesEn: [''],
    onSiteInstructionsAr: [''],
    onSiteInstructionsEn: [''],
    onlineInstructionsAr: [''],
    onlineInstructionsEn: [''],
    meetingProviderCode: [''],
    hybridSelectionAuthority: [HybridDeliverySelectionAuthority._1 as number],
    schoolBranchId: [''],
    educationalStageId: [''],
    gradeId: [''],
    academicYearId: [''],
  });

  protected readonly isEditMode = computed(() => !!this.editingId());

  protected isOperational(): boolean {
    return Number(this.form.controls.requirementMode.value) !== InterviewAssessmentRequirementMode._1;
  }

  protected showOnlineFields(): boolean {
    if (!this.isOperational()) {
      return false;
    }
    const mode = Number(this.form.controls.deliveryMode.value);
    return (
      mode === InterviewAssessmentDeliveryMode._1 || mode === InterviewAssessmentDeliveryMode._3
    );
  }

  protected showOnSiteFields(): boolean {
    if (!this.isOperational()) {
      return false;
    }
    const mode = Number(this.form.controls.deliveryMode.value);
    return (
      mode === InterviewAssessmentDeliveryMode._2 || mode === InterviewAssessmentDeliveryMode._3
    );
  }

  protected showHybridAuthority(): boolean {
    if (!this.isOperational()) {
      return false;
    }
    return Number(this.form.controls.deliveryMode.value) === InterviewAssessmentDeliveryMode._3;
  }

  ngOnInit(): void {
    this.loadLookups();
    this.loadMeetingProviders();
    this.loadItems();
    this.filterForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadItems();
    });
    this.form.controls.requirementMode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.syncOperationalValidators());
    this.form.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((stageId) => {
        this.form.controls.gradeId.setValue('');
        this.grades.set([]);
        if (stageId) {
          this.taxonomiesApi.getGradesByStage(stageId).subscribe((result) => {
            if (result.succeeded && result.data) {
              this.grades.set(result.data);
            }
          });
        }
      });
    this.syncOperationalValidators();
  }

  protected requirementModeLabel(mode: number | undefined): string {
    switch (mode) {
      case InterviewAssessmentRequirementMode._1:
        return this.transloco.translate('portal.interviewAssessmentPolicies.modes.notRequired');
      case InterviewAssessmentRequirementMode._2:
        return this.transloco.translate('portal.interviewAssessmentPolicies.modes.interviewOnly');
      case InterviewAssessmentRequirementMode._3:
        return this.transloco.translate('portal.interviewAssessmentPolicies.modes.assessmentOnly');
      case InterviewAssessmentRequirementMode._4:
        return this.transloco.translate(
          'portal.interviewAssessmentPolicies.modes.interviewAndAssessment',
        );
      default:
        return '—';
    }
  }

  protected deliveryModeLabel(mode: number | undefined): string {
    switch (mode) {
      case InterviewAssessmentDeliveryMode._1:
        return this.transloco.translate('portal.interviewAssessmentPolicies.delivery.online');
      case InterviewAssessmentDeliveryMode._2:
        return this.transloco.translate('portal.interviewAssessmentPolicies.delivery.onSite');
      case InterviewAssessmentDeliveryMode._3:
        return this.transloco.translate('portal.interviewAssessmentPolicies.delivery.hybrid');
      default:
        return '—';
    }
  }

  protected statusKey(item: SchoolInterviewAssessmentPolicyListItem): string {
    if (item.isActive === false) {
      return 'portal.interviewAssessmentPolicies.inactive';
    }
    return item.publicationStatus === InterviewAssessmentPolicyPublicationStatus._2
      ? 'portal.interviewAssessmentPolicies.published'
      : 'portal.interviewAssessmentPolicies.draft';
  }

  protected providerLabel(provider: SafeMeetingProviderOptionDto): string {
    return localizedBilingualName(
      provider.displayNameAr ?? '',
      provider.displayNameEn,
      this.activeLang(),
    );
  }

  protected openCreate(): void {
    this.editingId.set(null);
    this.editingRowVersion.set(null);
    this.form.reset({
      requirementMode: InterviewAssessmentRequirementMode._2,
      deliveryMode: InterviewAssessmentDeliveryMode._1,
      requiredParticipants: InterviewAssessmentRequiredParticipants._1,
      expectedDurationMinutes: 30,
      bookingWindowOpensDaysBefore: 14,
      bookingWindowClosesDaysBefore: 1,
      minimumSchedulingLeadTimeHours: 24,
      parentReschedulingAllowed: true,
      maxParentRescheduleAttempts: 2,
      parentCancellationAllowed: true,
      preparationNotesAr: '',
      preparationNotesEn: '',
      onSiteInstructionsAr: '',
      onSiteInstructionsEn: '',
      onlineInstructionsAr: '',
      onlineInstructionsEn: '',
      meetingProviderCode: '',
      hybridSelectionAuthority: HybridDeliverySelectionAuthority._1,
      schoolBranchId: '',
      educationalStageId: '',
      gradeId: '',
      academicYearId: '',
    });
    this.syncOperationalValidators();
    this.showForm.set(true);
    this.errorMessage.set(null);
  }

  protected openEdit(item: SchoolInterviewAssessmentPolicyListItem): void {
    const schoolId = this.schoolId();
    if (!schoolId || !item.id) {
      return;
    }
    this.api
      .getInterviewAssessmentPolicy(schoolId, item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.patchFormFromDetail(result.data);
        this.editingId.set(item.id!);
        this.editingRowVersion.set(result.data.rowVersion ?? null);
        this.showForm.set(true);
        this.errorMessage.set(null);
      });
  }

  protected cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
    this.editingRowVersion.set(null);
  }

  protected save(): void {
    this.syncOperationalValidators();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    const body = this.buildBodyFromForm();
    const editingId = this.editingId();
    this.saving.set(true);
    const request$ = editingId
      ? this.api.updateInterviewAssessmentPolicy(
          schoolId,
          editingId,
          body as UpdateSchoolInterviewAssessmentPolicyRequest,
        )
      : this.api.createInterviewAssessmentPolicy(
          schoolId,
          body as CreateSchoolInterviewAssessmentPolicyRequest,
        );
    request$
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.cancelForm();
          this.loadItems();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected publish(item: SchoolInterviewAssessmentPolicyListItem): void {
    this.runMutation((schoolId) => this.api.publishInterviewAssessmentPolicy(schoolId, item.id!));
  }

  protected unpublish(item: SchoolInterviewAssessmentPolicyListItem): void {
    this.runMutation((schoolId) => this.api.unpublishInterviewAssessmentPolicy(schoolId, item.id!));
  }

  protected requestDeactivate(item: SchoolInterviewAssessmentPolicyListItem): void {
    this.confirmAction.set({ kind: 'deactivate', item });
  }

  protected requestClone(item: SchoolInterviewAssessmentPolicyListItem): void {
    this.confirmAction.set({ kind: 'clone', item });
  }

  protected confirmDialogTitleKey(): string {
    return this.confirmAction()?.kind === 'clone'
      ? 'portal.interviewAssessmentPolicies.cloneConfirmTitle'
      : 'portal.interviewAssessmentPolicies.deactivateConfirmTitle';
  }

  protected confirmDialogMessageKey(): string {
    return this.confirmAction()?.kind === 'clone'
      ? 'portal.interviewAssessmentPolicies.cloneConfirmMessage'
      : 'portal.interviewAssessmentPolicies.deactivateConfirmMessage';
  }

  protected runConfirmedAction(): void {
    const action = this.confirmAction();
    if (!action?.item.id) {
      return;
    }
    const item = action.item;
    this.confirmAction.set(null);
    if (action.kind === 'clone') {
      this.runMutation((schoolId) => this.api.cloneInterviewAssessmentPolicy(schoolId, item.id!));
      return;
    }
    this.runMutation((schoolId) => this.api.deactivateInterviewAssessmentPolicy(schoolId, item.id!));
  }

  protected loadPreview(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    const filters = this.filterForm.getRawValue();
    this.previewLoading.set(true);
    this.api
      .previewInterviewAssessmentPolicyApplicability(schoolId, {
        schoolBranchId: filters.branchId || undefined,
        educationalStageId: filters.educationalStageId || undefined,
        gradeId: filters.gradeId || undefined,
        academicYearId: filters.academicYearId || undefined,
      })
      .pipe(
        finalize(() => this.previewLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.previewResult.set(result.data ?? null);
          return;
        }
        this.errorMessage.set(
          this.transloco.translate('portal.interviewAssessmentPolicies.previewFailed'),
        );
      });
  }

  protected retry(): void {
    this.errorMessage.set(null);
    this.loadItems();
  }

  private syncOperationalValidators(): void {
    const operational =
      Number(this.form.controls.requirementMode.value) !== InterviewAssessmentRequirementMode._1;
    const delivery = this.form.controls.deliveryMode;
    const participants = this.form.controls.requiredParticipants;
    const duration = this.form.controls.expectedDurationMinutes;
    const opens = this.form.controls.bookingWindowOpensDaysBefore;
    const closes = this.form.controls.bookingWindowClosesDaysBefore;
    const lead = this.form.controls.minimumSchedulingLeadTimeHours;
    const prepAr = this.form.controls.preparationNotesAr;
    const prepEn = this.form.controls.preparationNotesEn;

    if (operational) {
      delivery.setValidators([Validators.required]);
      participants.setValidators([Validators.required]);
      duration.setValidators([Validators.required, Validators.min(1)]);
      opens.setValidators([Validators.required, Validators.min(0)]);
      closes.setValidators([Validators.required, Validators.min(0)]);
      lead.setValidators([Validators.required, Validators.min(0)]);
      prepAr.setValidators([Validators.required]);
      prepEn.setValidators([Validators.required]);
    } else {
      delivery.clearValidators();
      participants.clearValidators();
      duration.clearValidators();
      opens.clearValidators();
      closes.clearValidators();
      lead.clearValidators();
      prepAr.clearValidators();
      prepEn.clearValidators();
    }
    delivery.updateValueAndValidity({ emitEvent: false });
    participants.updateValueAndValidity({ emitEvent: false });
    duration.updateValueAndValidity({ emitEvent: false });
    opens.updateValueAndValidity({ emitEvent: false });
    closes.updateValueAndValidity({ emitEvent: false });
    lead.updateValueAndValidity({ emitEvent: false });
    prepAr.updateValueAndValidity({ emitEvent: false });
    prepEn.updateValueAndValidity({ emitEvent: false });
  }

  private runMutation(
    factory: (
      schoolId: string,
    ) => Observable<{ succeeded?: boolean; errorCodes?: string[] | undefined }>,
  ): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    factory(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadItems();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private schoolId(): string | null {
    return this.route.parent?.snapshot.paramMap.get('schoolId') ?? null;
  }

  private loadLookups(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    this.api.listBranches(schoolId).subscribe((result) => {
      if (result.succeeded && result.data) {
        this.branches.set(
          result.data
            .filter((branch) => branch.id)
            .map((branch) => ({
              id: branch.id!,
              name: localizedBilingualName(branch.nameAr ?? '', branch.nameEn, this.activeLang()),
            })),
        );
      }
    });
    this.taxonomiesApi.getEducationalStages().subscribe((result) => {
      if (result.succeeded && result.data) {
        this.stages.set(result.data);
      }
    });
    this.taxonomiesApi.getAcademicYears().subscribe((result) => {
      if (result.succeeded && result.data) {
        this.academicYears.set(result.data);
      }
    });
  }

  private loadMeetingProviders(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      return;
    }
    this.api.listMeetingProviderOptions(schoolId).subscribe((result) => {
      if (result.succeeded && result.data) {
        this.meetingProviders.set(result.data);
      }
    });
  }

  private loadItems(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api
      .listInterviewAssessmentPolicies(schoolId, this.buildListParams())
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.items.set(result.data);
          this.errorMessage.set(null);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private buildListParams(): ListSchoolInterviewAssessmentPoliciesParams {
    const raw = this.filterForm.getRawValue();
    return {
      branchId: raw.branchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
      publicationStatus: raw.publicationStatus ? Number(raw.publicationStatus) : undefined,
      isActive: raw.isActive === '' ? undefined : raw.isActive === 'true',
    };
  }

  private patchFormFromDetail(detail: SchoolInterviewAssessmentPolicyDetail): void {
    if (detail.educationalStageId) {
      this.taxonomiesApi.getGradesByStage(detail.educationalStageId).subscribe((result) => {
        if (result.succeeded && result.data) {
          this.grades.set(result.data);
        }
      });
    }
    this.form.patchValue({
      requirementMode: detail.requirementMode ?? InterviewAssessmentRequirementMode._1,
      deliveryMode: detail.deliveryMode ?? InterviewAssessmentDeliveryMode._1,
      requiredParticipants:
        detail.requiredParticipants ?? InterviewAssessmentRequiredParticipants._1,
      expectedDurationMinutes: detail.expectedDurationMinutes ?? null,
      bookingWindowOpensDaysBefore: detail.bookingWindowOpensDaysBefore ?? null,
      bookingWindowClosesDaysBefore: detail.bookingWindowClosesDaysBefore ?? null,
      minimumSchedulingLeadTimeHours: detail.minimumSchedulingLeadTimeHours ?? null,
      parentReschedulingAllowed: detail.parentReschedulingAllowed ?? true,
      maxParentRescheduleAttempts: detail.maxParentRescheduleAttempts ?? 0,
      parentCancellationAllowed: detail.parentCancellationAllowed ?? true,
      preparationNotesAr: detail.preparationNotesAr ?? '',
      preparationNotesEn: detail.preparationNotesEn ?? '',
      onSiteInstructionsAr: detail.onSiteInstructionsAr ?? '',
      onSiteInstructionsEn: detail.onSiteInstructionsEn ?? '',
      onlineInstructionsAr: detail.onlineInstructionsAr ?? '',
      onlineInstructionsEn: detail.onlineInstructionsEn ?? '',
      meetingProviderCode: detail.meetingProviderCode ?? '',
      hybridSelectionAuthority:
        detail.hybridSelectionAuthority ?? HybridDeliverySelectionAuthority._1,
      schoolBranchId: detail.schoolBranchId ?? '',
      educationalStageId: detail.educationalStageId ?? '',
      gradeId: detail.gradeId ?? '',
      academicYearId: detail.academicYearId ?? '',
    });
    this.syncOperationalValidators();
  }

  private buildBodyFromForm():
    | CreateSchoolInterviewAssessmentPolicyRequest
    | UpdateSchoolInterviewAssessmentPolicyRequest {
    const raw = this.form.getRawValue();
    const requirementMode = Number(raw.requirementMode) as InterviewAssessmentRequirementMode;
    const notRequired = requirementMode === InterviewAssessmentRequirementMode._1;
    const deliveryMode = notRequired
      ? undefined
      : (Number(raw.deliveryMode) as InterviewAssessmentDeliveryMode);
    const isOnline =
      deliveryMode === InterviewAssessmentDeliveryMode._1 ||
      deliveryMode === InterviewAssessmentDeliveryMode._3;
    const isOnSite =
      deliveryMode === InterviewAssessmentDeliveryMode._2 ||
      deliveryMode === InterviewAssessmentDeliveryMode._3;
    const isHybrid = deliveryMode === InterviewAssessmentDeliveryMode._3;

    const shared = {
      requirementMode,
      deliveryMode,
      requiredParticipants: notRequired
        ? undefined
        : (Number(raw.requiredParticipants) as InterviewAssessmentRequiredParticipants),
      expectedDurationMinutes: notRequired ? undefined : (raw.expectedDurationMinutes ?? undefined),
      bookingWindowOpensDaysBefore: notRequired
        ? undefined
        : (raw.bookingWindowOpensDaysBefore ?? undefined),
      bookingWindowClosesDaysBefore: notRequired
        ? undefined
        : (raw.bookingWindowClosesDaysBefore ?? undefined),
      minimumSchedulingLeadTimeHours: notRequired
        ? undefined
        : (raw.minimumSchedulingLeadTimeHours ?? undefined),
      parentReschedulingAllowed: notRequired ? undefined : raw.parentReschedulingAllowed,
      maxParentRescheduleAttempts: notRequired
        ? undefined
        : Number(raw.maxParentRescheduleAttempts),
      parentCancellationAllowed: notRequired ? undefined : raw.parentCancellationAllowed,
      preparationNotesAr: notRequired
        ? undefined
        : raw.preparationNotesAr.trim() || undefined,
      preparationNotesEn: notRequired
        ? undefined
        : raw.preparationNotesEn.trim() || undefined,
      onSiteInstructionsAr: isOnSite ? raw.onSiteInstructionsAr.trim() || undefined : undefined,
      onSiteInstructionsEn: isOnSite ? raw.onSiteInstructionsEn.trim() || undefined : undefined,
      onlineInstructionsAr: isOnline ? raw.onlineInstructionsAr.trim() || undefined : undefined,
      onlineInstructionsEn: isOnline ? raw.onlineInstructionsEn.trim() || undefined : undefined,
      meetingProviderCode: isOnline ? raw.meetingProviderCode || undefined : undefined,
      hybridSelectionAuthority: isHybrid
        ? (Number(raw.hybridSelectionAuthority) as HybridDeliverySelectionAuthority)
        : undefined,
      schoolBranchId: raw.schoolBranchId || undefined,
      educationalStageId: raw.educationalStageId || undefined,
      gradeId: raw.gradeId || undefined,
      academicYearId: raw.academicYearId || undefined,
    };

    if (this.editingId()) {
      return {
        ...shared,
        rowVersion: this.editingRowVersion() ?? undefined,
      };
    }
    return shared;
  }
}
