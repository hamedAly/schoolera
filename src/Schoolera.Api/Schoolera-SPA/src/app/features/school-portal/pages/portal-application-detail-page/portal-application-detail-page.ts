import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  ChildStudyLanguage,
  ChildAgeEligibilityExceptionReasonCode,
  ChildAgeEligibilityResultCode,
  InterviewAssessmentSlotDto,
  SchoolAdmissionApplicationDetailDto,
  SlotKind,
  SlotStatus,
  SchoolMeetingSessionContextDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { AdmissionRequirementChecklistItem } from '../../../parent/data-access/admission-requirements.models';
import { AdmissionQuestionChecklistItem } from '../../../parent/data-access/admission-questions.models';
import {
  AdmissionAppointmentLifecycle,
  AdmissionAppointmentMode,
  AdmissionMissingItemKind,
  SchoolAdmissionLifecycleCapabilities,
  SchoolAdmissionLifecycleResult,
} from '../../../parent/data-access/admission-lifecycle.models';
import { ApplicationRequirementsChecklist } from '../../../parent/applications/components/application-requirements-checklist/application-requirements-checklist';
import { ApplicationQuestionsForm } from '../../../parent/applications/components/application-questions-form/application-questions-form';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { AttachmentList } from '../../../parent/applications/components/attachment-list/attachment-list';
import { translateAdmissionErrorCodes } from '../../../parent/data-access/admission-errors';
import { ConfirmationDialog } from '../../components/confirmation-dialog/confirmation-dialog';
import { PortalAdmissionStatusBadge } from '../../components/portal-admission-status-badge/portal-admission-status-badge';
import { PortalApplicationTimeline } from '../../components/portal-application-timeline/portal-application-timeline';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { SchoolPortalApi } from '../../data-access/school-portal.api';

type ConfirmAction = 'startReview' | 'accept';

type LifecycleDialog =
  | 'requestMissing'
  | 'scheduleInterview'
  | 'rescheduleInterview'
  | 'scheduleAssessment'
  | 'rescheduleAssessment'
  | 'completeInterview'
  | 'completeAssessment'
  | 'cancelInterview'
  | 'cancelAssessment'
  | 'moveToWaitingList'
  | 'returnToUnderReview'
  | 'markRegistered';

type SchoolApplicationDetail = SchoolAdmissionApplicationDetailDto & {
  readonly capabilities?: SchoolAdmissionLifecycleCapabilities;
  readonly requirements?: readonly AdmissionRequirementChecklistItem[];
  readonly questions?: readonly AdmissionQuestionChecklistItem[];
};

interface SelectableMissingItem {
  kind: number;
  snapshotId: string;
  label: string;
  selected: boolean;
  mandatory: boolean;
}

@Component({
  selector: 'se-portal-application-detail-page',
  imports: [
    ApplicationRequirementsChecklist,
    ApplicationQuestionsForm,
    AttachmentList,
    ConfirmationDialog,
    FormsModule,
    PortalAdmissionStatusBadge,
    PortalApplicationTimeline,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './portal-application-detail-page.html',
  styleUrl: './portal-application-detail-page.scss',
})
export class PortalApplicationDetailPage implements OnInit {
  protected readonly SlotKind = SlotKind;
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(SchoolPortalApi);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly actionLoading = signal(false);
  protected readonly downloading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<SchoolApplicationDetail | null>(null);
  protected readonly interviewMeeting = signal<SchoolMeetingSessionContextDto | null>(null);
  protected readonly assessmentMeeting = signal<SchoolMeetingSessionContextDto | null>(null);

  protected readonly showConfirmDialog = signal(false);
  protected readonly showRejectDialog = signal(false);
  protected readonly showAgeExceptionDialog = signal(false);
  protected readonly pendingAction = signal<ConfirmAction | null>(null);
  protected readonly activeDialog = signal<LifecycleDialog | null>(null);

  protected internalReviewNote = '';
  protected parentVisibleRejectionReason = '';
  protected ageExceptionReason: number = ChildAgeEligibilityExceptionReasonCode._1;
  protected ageExceptionNote = '';
  protected readonly AgeExceptionReason = ChildAgeEligibilityExceptionReasonCode;

  // Request-missing-items form state.
  protected missingReason = '';
  protected missingInstructions = '';
  protected missingDeadline = '';
  protected readonly selectableItems = signal<SelectableMissingItem[]>([]);

  // Appointment schedule form state.
  protected scheduleDateTime = '';
  protected scheduleTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
  protected scheduleMode: number = AdmissionAppointmentMode.InPerson;
  protected scheduleLocation = '';
  protected scheduleOnlineInstructions = '';
  protected scheduleParentNotes = '';
  protected schedulePreparation = '';
  protected readonly scheduleSlots = signal<readonly InterviewAssessmentSlotDto[]>([]);
  protected readonly scheduleSlotsLoading = signal(false);
  protected readonly scheduleSlotsError = signal<string | null>(null);
  protected readonly selectedScheduleSlotId = signal<string | null>(null);
  protected useManualScheduleFallback = false;

  // Complete appointment form state.
  protected completeNextStatus = 3;
  protected completeOutcomeNotes = '';
  protected completeWaitingListReason = '';
  protected completeWaitingListPosition: number | null = null;
  protected completeWaitingListReviewDate = '';
  protected completeRejectionReason = '';

  // Move-to-waiting-list form state.
  protected waitingReason = '';
  protected waitingPosition: number | null = null;
  protected waitingReviewDate = '';

  protected readonly AppointmentMode = AdmissionAppointmentMode;

  protected readonly schoolId = computed(() => this.route.parent?.snapshot.paramMap.get('schoolId') ?? '');
  protected readonly applicationId = computed(() => this.route.snapshot.paramMap.get('applicationId') ?? '');
  protected readonly capabilities = computed(() => this.detail()?.capabilities ?? null);
  protected readonly missingRequest = computed(() => this.detail()?.activeMissingItemsRequest ?? null);

  protected readonly nextStatusOptions = [
    { value: 3, labelKey: 'portal.applications.detail.dialogs.nextStatusUnderReview' },
    { value: 10, labelKey: 'portal.applications.detail.dialogs.nextStatusWaitingList' },
    { value: 4, labelKey: 'portal.applications.detail.dialogs.nextStatusAccepted' },
    { value: 5, labelKey: 'portal.applications.detail.dialogs.nextStatusRejected' },
  ] as const;

  ngOnInit(): void {
    this.load();
  }

  protected formatDate(value: string | null | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '—';
  }

  protected studyLanguageKey(value: ChildStudyLanguage | undefined): string {
    switch (value) {
      case ChildStudyLanguage._1:
        return 'portal.applications.detail.studyLanguages.arabic';
      case ChildStudyLanguage._2:
        return 'portal.applications.detail.studyLanguages.english';
      case ChildStudyLanguage._3:
        return 'portal.applications.detail.studyLanguages.french';
      case ChildStudyLanguage._4:
        return 'portal.applications.detail.studyLanguages.german';
      default:
        return 'portal.applications.detail.studyLanguages.other';
    }
  }

  protected appointmentModeKey(mode: number | undefined): string {
    return mode === AdmissionAppointmentMode.Online
      ? 'portal.applications.detail.lifecycle.online'
      : 'portal.applications.detail.lifecycle.inPerson';
  }

  protected appointmentStatusKey(lifecycle: number | undefined): string {
    switch (lifecycle) {
      case AdmissionAppointmentLifecycle.Proposed:
        return 'portal.applications.detail.lifecycle.statusProposed';
      case AdmissionAppointmentLifecycle.Completed:
        return 'portal.applications.detail.lifecycle.statusCompleted';
      case AdmissionAppointmentLifecycle.Cancelled:
        return 'portal.applications.detail.lifecycle.statusCancelled';
      case AdmissionAppointmentLifecycle.RescheduleRequested:
        return 'portal.applications.detail.lifecycle.statusRescheduleRequested';
      case AdmissionAppointmentLifecycle.Confirmed:
        return 'portal.applications.detail.lifecycle.statusConfirmed';
      case AdmissionAppointmentLifecycle.NoShow:
        return 'portal.applications.detail.lifecycle.statusNoShow';
      case AdmissionAppointmentLifecycle.InProgress:
        return 'portal.applications.detail.lifecycle.statusInProgress';
      default:
        return 'portal.applications.detail.lifecycle.statusUnknown';
    }
  }

  protected meetingFor(kind: SlotKind): SchoolMeetingSessionContextDto | null {
    return kind === SlotKind._1 ? this.interviewMeeting() : this.assessmentMeeting();
  }

  protected hostMeeting(kind: SlotKind): void {
    const schoolId = this.schoolId();
    const appointment = kind === SlotKind._1
      ? this.detail()?.activeInterview
      : this.detail()?.activeAssessment;
    if (!schoolId || !appointment?.id || !this.meetingFor(kind)?.summary?.canSchoolHost) return;
    this.actionLoading.set(true);
    this.api.resolveMeetingHost(schoolId, appointment.id, kind)
      .pipe(finalize(() => this.actionLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded && result.data?.actionPath) {
            void this.router.navigateByUrl(result.data.actionPath);
          } else {
            this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          }
        },
        error: () => this.toast.error(this.transloco.translate('portal.errors.generic')),
      });
  }

  protected meetingStatusKey(status: number | undefined): string {
    return status === 1 ? 'portal.applications.meeting.pending'
      : status === 2 ? 'portal.applications.meeting.ready'
        : status === 3 ? 'portal.applications.meeting.failed'
          : status === 4 ? 'portal.applications.meeting.cancellationPending'
            : status === 5 ? 'portal.applications.meeting.cancelled'
              : 'portal.applications.meeting.expired';
  }

  protected ageEligibilityKey(code: ChildAgeEligibilityResultCode | undefined): string {
    const keys: Record<number, string> = {
      1: 'eligible', 2: 'belowMin', 3: 'aboveMax', 4: 'birthDateRequired',
      5: 'invalidBirthDate', 6: 'ruleNotConfigured', 7: 'manualExceptionApproved',
    };
    return `portal.applications.detail.ageEligibility.results.${keys[code ?? 6] ?? 'ruleNotConfigured'}`;
  }

  protected openAgeException(): void {
    this.ageExceptionReason = ChildAgeEligibilityExceptionReasonCode._1;
    this.ageExceptionNote = '';
    this.showAgeExceptionDialog.set(true);
  }

  protected grantAgeException(): void {
    const app = this.detail();
    if (!app || !this.capabilities()?.canGrantAgeException) return;
    this.actionLoading.set(true);
    this.api.grantAgeEligibilityException(this.schoolId(), this.applicationId(), {
      reasonCode: this.ageExceptionReason as ChildAgeEligibilityExceptionReasonCode,
      reasonNote: this.ageExceptionNote.trim() || undefined,
      rowVersion: app.rowVersion,
    }).pipe(finalize(() => this.actionLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.showAgeExceptionDialog.set(false);
        this.applyResult(result);
      });
  }

  // ---- Confirm dialogs (start-review / accept) ----

  protected openStartReview(): void {
    this.pendingAction.set('startReview');
    this.showConfirmDialog.set(true);
  }

  protected openAccept(): void {
    this.pendingAction.set('accept');
    this.showConfirmDialog.set(true);
  }

  protected openReject(): void {
    this.parentVisibleRejectionReason = '';
    this.internalReviewNote = '';
    this.showRejectDialog.set(true);
  }

  protected confirmAction(): void {
    const action = this.pendingAction();
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    if (!action || !schoolId || !applicationId || !app) {
      this.showConfirmDialog.set(false);
      return;
    }

    const rowVersion = app.rowVersion;
    const note = this.internalReviewNote.trim() || undefined;

    const request$ =
      action === 'startReview'
        ? this.api.startAdmissionReview(schoolId, applicationId, { internalReviewNote: note, rowVersion })
        : this.api.acceptAdmissionApplication(schoolId, applicationId, { internalReviewNote: note, rowVersion });

    this.actionLoading.set(true);
    request$
      .pipe(
        finalize(() => {
          this.actionLoading.set(false);
          this.showConfirmDialog.set(false);
          this.pendingAction.set(null);
          this.internalReviewNote = '';
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => this.applyResult(result));
  }

  protected confirmReject(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    const reason = this.parentVisibleRejectionReason.trim();
    if (!schoolId || !applicationId || !app || !reason) {
      return;
    }

    this.actionLoading.set(true);
    this.api
      .rejectAdmissionApplication(schoolId, applicationId, {
        parentVisibleRejectionReason: reason,
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      })
      .pipe(
        finalize(() => {
          this.actionLoading.set(false);
          this.showRejectDialog.set(false);
          this.parentVisibleRejectionReason = '';
          this.internalReviewNote = '';
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => this.applyResult(result));
  }

  // ---- Lifecycle dialogs ----

  protected openDialog(dialog: LifecycleDialog): void {
    this.internalReviewNote = '';
    if (dialog === 'requestMissing') {
      this.missingReason = '';
      this.missingInstructions = '';
      this.missingDeadline = '';
      this.selectableItems.set(this.buildSelectableItems());
    } else if (dialog.startsWith('schedule') || dialog.startsWith('reschedule')) {
      this.resetScheduleForm(dialog);
      this.loadScheduleSlots(dialog);
    } else if (dialog === 'completeInterview' || dialog === 'completeAssessment') {
      this.completeNextStatus = 3;
      this.completeOutcomeNotes = '';
      this.completeWaitingListReason = '';
      this.completeWaitingListPosition = null;
      this.completeWaitingListReviewDate = '';
      this.completeRejectionReason = '';
    } else if (dialog === 'moveToWaitingList') {
      this.waitingReason = '';
      this.waitingPosition = null;
      this.waitingReviewDate = '';
    }
    this.activeDialog.set(dialog);
  }

  protected closeDialog(): void {
    this.activeDialog.set(null);
  }

  protected toggleItemSelected(item: SelectableMissingItem): void {
    this.selectableItems.update((items) =>
      items.map((i) => (i === item ? { ...i, selected: !i.selected } : i)),
    );
  }

  protected toggleItemMandatory(item: SelectableMissingItem): void {
    this.selectableItems.update((items) =>
      items.map((i) => (i === item ? { ...i, mandatory: !i.mandatory } : i)),
    );
  }

  protected submitRequestMissing(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    const reason = this.missingReason.trim();
    const selected = this.selectableItems().filter((i) => i.selected);
    if (!schoolId || !applicationId || !app || !reason || selected.length === 0) {
      return;
    }

    const items = selected.map((i) => ({
      kind: i.kind,
      requirementSnapshotId:
        i.kind === AdmissionMissingItemKind.RequirementSnapshot ? i.snapshotId : undefined,
      questionSnapshotId: i.kind === AdmissionMissingItemKind.QuestionSnapshot ? i.snapshotId : undefined,
      isMandatory: i.mandatory,
    }));

    this.runAction(
      this.api.requestMissingDocuments(schoolId, applicationId, {
        parentVisibleReason: reason,
        instructions: this.missingInstructions.trim() || undefined,
        responseDeadlineUtc: this.missingDeadline
          ? new Date(this.missingDeadline).toISOString()
          : undefined,
        items,
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      }),
    );
  }

  protected submitSchedule(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    const dialog = this.activeDialog();
    const selectedSlot = this.scheduleSlots().find((slot) => slot.id === this.selectedScheduleSlotId());
    if (!schoolId || !applicationId || !app || !dialog || (!selectedSlot && !this.useManualScheduleFallback)) {
      return;
    }

    const body = {
      scheduledAtUtc: selectedSlot?.startAtUtc ?? (this.scheduleDateTime ? new Date(this.scheduleDateTime).toISOString() : undefined),
      timeZoneId: selectedSlot?.timeZoneId ?? (this.scheduleTimeZone || 'UTC'),
      mode: selectedSlot ? Number(selectedSlot.deliveryMode) : Number(this.scheduleMode),
      location: selectedSlot ? undefined : this.scheduleLocation.trim() || undefined,
      onlineInstructions: selectedSlot ? undefined : this.scheduleOnlineInstructions.trim() || undefined,
      parentVisibleNotes: this.scheduleParentNotes.trim() || undefined,
      preparationInstructions: this.schedulePreparation.trim() || undefined,
      internalReviewNote: this.internalReviewNote.trim() || undefined,
      rowVersion: app.rowVersion,
      slotId: selectedSlot?.id,
      idempotencyKey: this.createIdempotencyKey(),
    };

    let request$: Observable<SchoolAdmissionLifecycleResult>;
    switch (dialog) {
      case 'scheduleInterview':
        request$ = this.api.scheduleInterview(schoolId, applicationId, body);
        break;
      case 'rescheduleInterview':
        request$ = this.api.rescheduleInterview(schoolId, applicationId, body);
        break;
      case 'scheduleAssessment':
        request$ = this.api.scheduleAssessment(schoolId, applicationId, body);
        break;
      case 'rescheduleAssessment':
        request$ = this.api.rescheduleAssessment(schoolId, applicationId, body);
        break;
      default:
        return;
    }

    this.runAction(request$);
  }

  protected submitComplete(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    const dialog = this.activeDialog();
    if (!schoolId || !applicationId || !app || !dialog) {
      return;
    }

    const nextStatus = Number(this.completeNextStatus);
    if (nextStatus === 5 && !this.completeRejectionReason.trim()) {
      return;
    }

    const body = {
      parentVisibleOutcomeNotes: this.completeOutcomeNotes.trim() || undefined,
      internalReviewNote: this.internalReviewNote.trim() || undefined,
      nextStatus,
      waitingListReason: nextStatus === 10 ? this.completeWaitingListReason.trim() || undefined : undefined,
      waitingListPosition:
        nextStatus === 10 && this.completeWaitingListPosition != null
          ? this.completeWaitingListPosition
          : undefined,
      waitingListReviewDate:
        nextStatus === 10 && this.completeWaitingListReviewDate ? this.completeWaitingListReviewDate : undefined,
      parentVisibleRejectionReason: nextStatus === 5 ? this.completeRejectionReason.trim() : undefined,
      rowVersion: app.rowVersion,
    };

    const request$ =
      dialog === 'completeInterview'
        ? this.api.completeInterview(schoolId, applicationId, body)
        : this.api.completeAssessment(schoolId, applicationId, body);

    this.runAction(request$);
  }

  protected submitMoveToWaitingList(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    if (!schoolId || !applicationId || !app) {
      return;
    }

    this.runAction(
      this.api.moveToWaitingList(schoolId, applicationId, {
        parentVisibleReason: this.waitingReason.trim() || undefined,
        position: this.waitingPosition ?? undefined,
        reviewDate: this.waitingReviewDate || undefined,
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      }),
    );
  }

  protected confirmCancelInterview(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    if (!schoolId || !applicationId || !app) {
      return;
    }
    this.runAction(
      this.api.cancelInterview(schoolId, applicationId, {
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      }),
    );
  }

  protected confirmCancelAssessment(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    if (!schoolId || !applicationId || !app) {
      return;
    }
    this.runAction(
      this.api.cancelAssessment(schoolId, applicationId, {
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      }),
    );
  }

  protected confirmReturnToUnderReview(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    if (!schoolId || !applicationId || !app) {
      return;
    }
    this.runAction(
      this.api.returnFromWaitingList(schoolId, applicationId, {
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      }),
    );
  }

  protected confirmMarkRegistered(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    const app = this.detail();
    if (!schoolId || !applicationId || !app) {
      return;
    }
    this.runAction(
      this.api.markRegistered(schoolId, applicationId, {
        internalReviewNote: this.internalReviewNote.trim() || undefined,
        rowVersion: app.rowVersion,
      }),
    );
  }

  protected onDownload(attachment: { id?: string; originalFileName?: string | undefined }): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    if (!schoolId || !applicationId || !attachment.id) {
      return;
    }

    this.downloading.set(true);
    this.api
      .downloadAdmissionAttachment(
        schoolId,
        applicationId,
        attachment.id,
        attachment.originalFileName ?? 'attachment',
      )
      .pipe(
        finalize(() => this.downloading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () => this.toast.error(this.transloco.translate('portal.errors.generic')),
      });
  }

  protected confirmTitleKey(): string {
    return this.pendingAction() === 'accept'
      ? 'portal.applications.detail.acceptConfirmTitle'
      : 'portal.applications.detail.startReviewConfirmTitle';
  }

  protected confirmMessageKey(): string {
    return this.pendingAction() === 'accept'
      ? 'portal.applications.detail.acceptConfirmMessage'
      : 'portal.applications.detail.startReviewConfirmMessage';
  }

  protected confirmButtonKey(): string {
    return this.pendingAction() === 'accept'
      ? 'portal.applications.detail.accept'
      : 'portal.applications.detail.startReview';
  }

  private runAction(request$: Observable<SchoolAdmissionLifecycleResult>): void {
    this.actionLoading.set(true);
    request$
      .pipe(
        finalize(() => this.actionLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => this.applyResult(result));
  }

  private applyResult(result: SchoolAdmissionLifecycleResult): void {
    if (result.succeeded && result.data) {
      this.detail.set(result.data as SchoolApplicationDetail);
      this.activeDialog.set(null);
      this.internalReviewNote = '';
      this.toast.success(this.transloco.translate('portal.applications.detail.actionSuccess'));
      return;
    }
    const reloadCodes = [
      'admission.appointment.slotFull',
      'admission.appointment.concurrencyConflict',
      'admission.appointment.idempotencyConflict',
      'schoolPortal.idempotencyConflict',
    ];
    if (result.errorCodes?.some((code) => reloadCodes.includes(code))) {
      this.toast.error(this.transloco.translate(
        result.errorCodes.includes('admission.appointment.slotFull')
          ? 'portal.applications.detail.dialogs.slotFull'
          : result.errorCodes.includes('admission.appointment.concurrencyConflict')
            ? 'portal.applications.detail.dialogs.scheduleConcurrency'
            : 'portal.applications.detail.dialogs.idempotencyConflict',
      ));
      this.load();
      const dialog = this.activeDialog();
      if (dialog) this.loadScheduleSlots(dialog);
      return;
    }
    this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
  }

  private resetScheduleForm(dialog: LifecycleDialog): void {
    const app = this.detail();
    const existing =
      dialog === 'rescheduleInterview'
        ? app?.activeInterview
        : dialog === 'rescheduleAssessment'
          ? app?.activeAssessment
          : null;
    this.scheduleDateTime = existing?.scheduledAtUtc ? this.toLocalInput(existing.scheduledAtUtc) : '';
    this.scheduleTimeZone = existing?.timeZoneId || Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    this.scheduleMode = existing?.mode ?? AdmissionAppointmentMode.InPerson;
    this.scheduleLocation = existing?.location ?? '';
    this.scheduleOnlineInstructions = existing?.onlineInstructions ?? '';
    this.scheduleParentNotes = existing?.parentVisibleNotes ?? '';
    this.schedulePreparation = existing?.preparationInstructions ?? '';
    this.scheduleSlots.set([]);
    this.scheduleSlotsError.set(null);
    this.selectedScheduleSlotId.set(null);
    this.useManualScheduleFallback = false;
  }

  protected scheduleSlotInstruction(slot: InterviewAssessmentSlotDto): string | undefined {
    return this.transloco.getActiveLang() === 'en'
      ? slot.instructionsEn ?? slot.instructionsAr
      : slot.instructionsAr ?? slot.instructionsEn;
  }

  protected remainingCapacity(slot: InterviewAssessmentSlotDto): number {
    return Math.max(0, (slot.capacity ?? 0) - (slot.activeAppointmentCount ?? 0));
  }

  protected retryScheduleSlots(): void {
    const dialog = this.activeDialog();
    if (dialog) this.loadScheduleSlots(dialog);
  }

  private loadScheduleSlots(dialog: LifecycleDialog): void {
    const app = this.detail();
    if (!app) return;
    const kind = dialog.endsWith('Assessment') ? SlotKind._2 : SlotKind._1;
    this.scheduleSlotsLoading.set(true);
    this.scheduleSlotsError.set(null);
    this.api.listInterviewAssessmentSlots(this.schoolId(), {
      branchId: app.schoolBranchId,
      stageId: app.educationalStageId,
      gradeId: app.gradeId,
      academicYearId: app.academicYearId,
      kind,
      status: SlotStatus._2,
    }).pipe(
      finalize(() => this.scheduleSlotsLoading.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => {
        if (!result.succeeded) {
          this.scheduleSlotsError.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.scheduleSlots.set((result.data ?? []).filter((slot) => this.remainingCapacity(slot) > 0));
      },
      error: () => this.scheduleSlotsError.set(this.transloco.translate('portal.errors.generic')),
    });
  }

  private createIdempotencyKey(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }
    return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
  }

  private toLocalInput(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  private buildSelectableItems(): SelectableMissingItem[] {
    const app = this.detail();
    if (!app) {
      return [];
    }

    const requirementItems = (app.requirements ?? [])
      .filter((r) => !!r.snapshotId)
      .map((r) => ({
        kind: AdmissionMissingItemKind.RequirementSnapshot,
        snapshotId: r.snapshotId!,
        label: r.name ?? r.requirementCode ?? r.snapshotId!,
        selected: false,
        mandatory: r.isRequired ?? true,
      }));

    const questionItems = (app.questions ?? [])
      .filter((q) => !!q.snapshotId)
      .map((q) => ({
        kind: AdmissionMissingItemKind.QuestionSnapshot,
        snapshotId: q.snapshotId!,
        label: q.label ?? q.questionCode ?? q.snapshotId!,
        selected: false,
        mandatory: q.isRequired ?? true,
      }));

    return [...requirementItems, ...questionItems];
  }

  private load(): void {
    const schoolId = this.schoolId();
    const applicationId = this.applicationId();
    if (!schoolId || !applicationId) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('portal.errors.generic'));
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.api
      .getAdmissionApplication(schoolId, applicationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.detail.set(result.data as SchoolApplicationDetail);
          this.loadMeetingSession(result.data.activeInterview?.id, SlotKind._1);
          this.loadMeetingSession(result.data.activeAssessment?.id, SlotKind._2);
          return;
        }
        this.errorMessage.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private loadMeetingSession(appointmentId: string | undefined, kind: SlotKind): void {
    const schoolId = this.schoolId();
    const target = kind === SlotKind._1 ? this.interviewMeeting : this.assessmentMeeting;
    target.set(null);
    if (!schoolId || !appointmentId) return;
    this.api.getMeetingSession(schoolId, appointmentId, kind)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => target.set(result.succeeded ? result.data ?? null : null),
        error: () => target.set(null),
      });
  }
}
