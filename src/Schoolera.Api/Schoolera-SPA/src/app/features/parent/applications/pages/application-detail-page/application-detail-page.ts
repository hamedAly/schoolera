import { Component, DestroyRef, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';
import { forkJoin } from 'rxjs';

import { AdmissionApplicationWithRequirements } from '../../../data-access/admission-requirements.models';
import {
  AdmissionMissingItemDto,
  AdmissionMissingItemKind,
  ParentAdmissionLifecycleDetail,
} from '../../../data-access/admission-lifecycle.models';
import {
  AdmissionAppointmentDto,
  AdmissionAppointmentLifecycle,
  AdmissionAppointmentMode,
  InterviewAssessmentDeliveryMode,
  InterviewAssessmentRequiredParticipants,
  InterviewAssessmentRequirementMode,
  ChildAgeEligibilityResultCode,
  PublicInterviewFaqItemDto,
  ParentAvailableSlotDto,
  SlotDeliveryMode,
  SlotKind,
  MeetingSessionStatus,
  OnSiteAttendanceDetailsDto,
  SafeMeetingProviderMetadataDto,
} from '../../../../../core/api-client/SwaggerClient.service';
import { ApplicationQuestionsForm } from '../../components/application-questions-form/application-questions-form';
import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { ConfirmationDialog } from '../../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../../shared/ui/toast/toast.service';
import { sanitizeHtml } from '../../../../../shared/utils/sanitize-html';
import { translateAdmissionErrorCodes } from '../../../data-access/admission-errors';
import { ParentApi } from '../../../data-access/parent.api';
import { ApplicationStatusBadge } from '../../components/application-status-badge/application-status-badge';
import { ApplicationSummary } from '../../components/application-summary/application-summary';
import { ApplicationTimeline } from '../../components/application-timeline/application-timeline';
import { AttachmentList } from '../../components/attachment-list/attachment-list';
import { CourierAvailability } from '../../components/courier-availability/courier-availability';

type ParentDetail = AdmissionApplicationWithRequirements & ParentAdmissionLifecycleDetail;

@Component({
  selector: 'se-application-detail-page',
  imports: [
    ApplicationQuestionsForm,
    ApplicationStatusBadge,
    ApplicationSummary,
    ApplicationTimeline,
    AttachmentList,
    ConfirmationDialog,
    CourierAvailability,
    FormsModule,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './application-detail-page.html',
  styleUrl: './application-detail-page.scss',
})
export class ApplicationDetailPage implements OnInit {
  private readonly api = inject(ParentApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly MissingItemKind = AdmissionMissingItemKind;

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<ParentDetail | null>(null);
  protected readonly showCancelDialog = signal(false);
  protected readonly cancelling = signal(false);
  protected readonly cancelReason = signal('');
  protected readonly downloading = signal(false);
  protected readonly openInterviewFaqIds = signal<ReadonlySet<string>>(new Set());
  protected readonly availableSlots = signal<readonly ParentAvailableSlotDto[]>([]);
  protected readonly availableSlotsLoading = signal(false);
  protected readonly availableSlotsError = signal<string | null>(null);
  protected readonly appointmentDialog = signal<'confirm' | 'slots' | 'reschedule' | 'cancel' | null>(null);
  protected readonly appointmentDialogKind = signal<SlotKind | null>(null);
  protected readonly appointmentMutating = signal(false);
  protected readonly appointmentAlert = signal<string | null>(null);
  protected readonly selectedSlotId = signal<string | null>(null);
  protected readonly appointmentReason = signal('');

  // Missing-items flow state.
  protected readonly correctingItemId = signal<string | null>(null);
  protected readonly correctionValue = signal('');
  protected readonly savingCorrection = signal(false);
  protected readonly uploadingItemId = signal<string | null>(null);
  protected readonly showResubmitDialog = signal(false);
  protected readonly resubmitting = signal(false);

  protected readonly missingRequest = computed(() => this.detail()?.activeMissingItemsRequest ?? null);

  protected readonly interviewFaqs = computed(
    () => this.detail()?.interviewFaqs ?? ([] as PublicInterviewFaqItemDto[]),
  );

  protected readonly hasInterviewFaqs = computed(() => this.interviewFaqs().length > 0);

  ngOnInit(): void {
    this.load();
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

  protected formatDate(value: string | null | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '—';
  }

  protected policyRequirementModeLabel(mode: number | undefined): string {
    switch (mode) {
      case InterviewAssessmentRequirementMode._1:
        return this.transloco.translate('parent.applications.detail.policyModes.notRequired');
      case InterviewAssessmentRequirementMode._2:
        return this.transloco.translate('parent.applications.detail.policyModes.interviewOnly');
      case InterviewAssessmentRequirementMode._3:
        return this.transloco.translate('parent.applications.detail.policyModes.assessmentOnly');
      case InterviewAssessmentRequirementMode._4:
        return this.transloco.translate(
          'parent.applications.detail.policyModes.interviewAndAssessment',
        );
      default:
        return '—';
    }
  }

  protected policyDeliveryModeLabel(mode: number | undefined): string {
    switch (mode) {
      case InterviewAssessmentDeliveryMode._1:
        return this.transloco.translate('parent.applications.detail.policyDelivery.online');
      case InterviewAssessmentDeliveryMode._2:
        return this.transloco.translate('parent.applications.detail.policyDelivery.onSite');
      case InterviewAssessmentDeliveryMode._3:
        return this.transloco.translate('parent.applications.detail.policyDelivery.hybrid');
      default:
        return '—';
    }
  }

  protected ageEligibilityKey(code: ChildAgeEligibilityResultCode | undefined): string {
    const keys: Record<number, string> = {
      1: 'eligible', 2: 'belowMin', 3: 'aboveMax', 4: 'birthDateRequired',
      5: 'invalidBirthDate', 6: 'ruleNotConfigured', 7: 'manualExceptionApproved',
    };
    return `parent.applications.ageEligibility.${keys[code ?? 6] ?? 'ruleNotConfigured'}`;
  }

  protected retry(): void {
    this.load();
  }

  protected openCancel(): void {
    this.cancelReason.set('');
    this.showCancelDialog.set(true);
  }

  protected confirmCancel(): void {
    const app = this.detail();
    if (!app?.id || !app.capabilities?.canCancel) {
      this.showCancelDialog.set(false);
      return;
    }

    this.cancelling.set(true);
    this.api
      .cancelAdmissionApplication(app.id, {
        reason: this.cancelReason().trim() || undefined,
      })
      .pipe(
        finalize(() => this.cancelling.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.showCancelDialog.set(false);
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        this.detail.set(result.data as ParentDetail);
        this.toast.success(this.transloco.translate('parent.applications.detail.cancelSuccess'));
      });
  }

  protected onDownload(attachment: { id?: string; originalFileName?: string | undefined }): void {
    const app = this.detail();
    if (!app?.id || !attachment.id) {
      return;
    }

    this.downloading.set(true);
    this.api
      .downloadAdmissionAttachment(app.id, attachment.id, attachment.originalFileName ?? 'attachment')
      .pipe(
        finalize(() => this.downloading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: () => this.toast.error(this.transloco.translate('parent.errors.generic')),
      });
  }

  // ---- Missing items ----

  protected isCorrectableField(item: AdmissionMissingItemDto): boolean {
    return (
      item.kind === AdmissionMissingItemKind.ParentSnapshotField ||
      item.kind === AdmissionMissingItemKind.ChildSnapshotField
    );
  }

  protected isDocumentItem(item: AdmissionMissingItemDto): boolean {
    return item.kind === AdmissionMissingItemKind.RequirementSnapshot;
  }

  protected isQuestionItem(item: AdmissionMissingItemDto): boolean {
    return item.kind === AdmissionMissingItemKind.QuestionSnapshot;
  }

  protected openCorrection(item: AdmissionMissingItemDto): void {
    if (!item.id) {
      return;
    }
    this.correctionValue.set('');
    this.correctingItemId.set(item.id);
  }

  protected cancelCorrection(): void {
    this.correctingItemId.set(null);
    this.correctionValue.set('');
  }

  protected saveCorrection(item: AdmissionMissingItemDto): void {
    const app = this.detail();
    const value = this.correctionValue().trim();
    if (!app?.id || !item.id || !value) {
      return;
    }

    this.savingCorrection.set(true);
    this.api
      .correctMissingField(app.id, {
        missingItemId: item.id,
        textValue: value,
        rowVersion: app.rowVersion,
      })
      .pipe(
        finalize(() => this.savingCorrection.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.detail.set(result.data as ParentDetail);
        this.correctingItemId.set(null);
        this.correctionValue.set('');
        this.toast.success(this.transloco.translate('parent.applications.lifecycle.missingItems.correctSuccess'));
      });
  }

  protected onMissingDocumentSelected(event: Event, item: AdmissionMissingItemDto): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = '';
    const app = this.detail();
    const snapshotId = item.requirementSnapshotId;
    if (!file || !app?.id || !snapshotId || this.uploadingItemId()) {
      return;
    }

    this.uploadingItemId.set(item.id ?? snapshotId);
    this.api
      .uploadAdmissionAttachmentWithProgress(app.id, file, 1, snapshotId)
      .pipe(
        finalize(() => this.uploadingItemId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => {
        if (event.kind !== 'complete') {
          return;
        }
        if (!event.result.succeeded || !event.result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, event.result.errorCodes));
          return;
        }
        this.toast.success(this.transloco.translate('parent.applications.wizard.uploadSuccess'));
        this.load();
      });
  }

  protected openResubmit(): void {
    this.showResubmitDialog.set(true);
  }

  protected confirmResubmit(): void {
    const app = this.detail();
    if (!app?.id || !app.capabilities?.canResubmitMissingItems) {
      this.showResubmitDialog.set(false);
      return;
    }

    this.resubmitting.set(true);
    this.api
      .resubmitMissingDocuments(app.id, { rowVersion: app.rowVersion })
      .pipe(
        finalize(() => {
          this.resubmitting.set(false);
          this.showResubmitDialog.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.toast.error(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.detail.set(result.data as ParentDetail);
        this.toast.success(this.transloco.translate('parent.applications.lifecycle.missingItems.resubmitSuccess'));
      });
  }

  // ---- Appointment helpers ----

  protected appointmentModeKey(mode: number | undefined): string {
    return mode === AdmissionAppointmentMode._1
      ? 'parent.applications.lifecycle.interview.online'
      : 'parent.applications.lifecycle.interview.inPerson';
  }

  protected meetingStatusKey(status?: MeetingSessionStatus): string {
    const value = Number(status);
    return value === 1 ? 'parent.applications.appointment.meeting.pending'
      : value === 2 ? 'parent.applications.appointment.meeting.ready'
        : value === 3 ? 'parent.applications.appointment.meeting.failed'
          : value === 4 ? 'parent.applications.appointment.meeting.cancellationPending'
            : value === 5 ? 'parent.applications.appointment.meeting.cancelled'
              : 'parent.applications.appointment.meeting.expired';
  }

  protected meetingUnavailableKey(code?: string): string {
    const known: Record<string, string> = {
      'meeting.pending': 'parent.applications.appointment.meeting.pending',
      'meeting.failed': 'parent.applications.appointment.meeting.failed',
      'meeting.cancelled': 'parent.applications.appointment.meeting.cancelled',
      'meeting.expired': 'parent.applications.appointment.meeting.expired',
      'meeting.join.tooEarly': 'parent.applications.appointment.meeting.tooEarly',
      'meeting.join.expired': 'parent.applications.appointment.meeting.windowExpired',
      'meeting.provider.disabled': 'parent.applications.appointment.meeting.providerUnavailable',
      'meeting.provider.notConfigured': 'parent.applications.appointment.meeting.providerUnavailable',
      'meeting.notProvisioned': 'parent.applications.appointment.meeting.notProvisioned',
    };
    return known[code ?? ''] ?? 'parent.applications.appointment.meeting.unavailable';
  }

  protected localizedBranchName(details: OnSiteAttendanceDetailsDto): string {
    return this.transloco.getActiveLang() === 'en'
      ? details.branchNameEn || details.branchNameAr || ''
      : details.branchNameAr || details.branchNameEn || '';
  }

  protected localizedBranchAddress(details: OnSiteAttendanceDetailsDto): string {
    return this.transloco.getActiveLang() === 'en'
      ? details.publicAddressEn || details.publicAddressAr || ''
      : details.publicAddressAr || details.publicAddressEn || '';
  }

  protected localizedProviderName(provider: SafeMeetingProviderMetadataDto): string {
    return this.transloco.getActiveLang() === 'en'
      ? provider.publicNameEn || provider.publicNameAr || provider.providerCode || ''
      : provider.publicNameAr || provider.publicNameEn || provider.providerCode || '';
  }

  protected appointmentStatusKey(lifecycle: number | undefined): string {
    switch (lifecycle) {
      case AdmissionAppointmentLifecycle._1: return 'parent.applications.appointment.lifecycle.proposed';
      case AdmissionAppointmentLifecycle._2: return 'parent.applications.appointment.lifecycle.completed';
      case AdmissionAppointmentLifecycle._3: return 'parent.applications.appointment.lifecycle.cancelled';
      case AdmissionAppointmentLifecycle._4: return 'parent.applications.appointment.lifecycle.rescheduleRequested';
      case AdmissionAppointmentLifecycle._5: return 'parent.applications.appointment.lifecycle.confirmed';
      case AdmissionAppointmentLifecycle._6: return 'parent.applications.appointment.lifecycle.noShow';
      case 7: return 'parent.applications.appointment.lifecycle.inProgress';
      default:
        return 'parent.applications.appointment.lifecycle.unknown';
    }
  }

  protected appointmentKindKey(kind: SlotKind): string {
    return kind === SlotKind._2
      ? 'parent.applications.appointment.kinds.assessment'
      : 'parent.applications.appointment.kinds.interview';
  }

  protected participantKey(value: InterviewAssessmentRequiredParticipants | undefined): string {
    switch (value) {
      case InterviewAssessmentRequiredParticipants._1:
        return 'parent.applications.appointment.participants.child';
      case InterviewAssessmentRequiredParticipants._2:
        return 'parent.applications.appointment.participants.parent';
      default:
        return 'parent.applications.appointment.participants.both';
    }
  }

  protected appointmentFor(kind: SlotKind): AdmissionAppointmentDto | undefined {
    return kind === SlotKind._2 ? this.detail()?.activeAssessment : this.detail()?.activeInterview;
  }

  protected appointments(): readonly { kind: SlotKind; value: AdmissionAppointmentDto }[] {
    const app = this.detail();
    return [
      ...(app?.activeInterview ? [{ kind: SlotKind._1, value: app.activeInterview }] : []),
      ...(app?.activeAssessment ? [{ kind: SlotKind._2, value: app.activeAssessment }] : []),
    ];
  }

  protected slotAddress(appointment: AdmissionAppointmentDto): string | undefined {
    if (appointment.location) return appointment.location;
    return this.availableSlots().find((slot) => slot.slotId === appointment.slotId)?.branchAddress;
  }

  protected alternateSlots(): readonly ParentAvailableSlotDto[] {
    const kind = this.appointmentDialogKind();
    const current = kind ? this.appointmentFor(kind)?.slotId : undefined;
    return this.availableSlots().filter((slot) => slot.kind === kind && slot.slotId !== current);
  }

  protected openAppointmentDialog(
    dialog: 'confirm' | 'slots' | 'reschedule' | 'cancel',
    kind: SlotKind,
  ): void {
    this.appointmentReason.set('');
    this.selectedSlotId.set(null);
    this.appointmentAlert.set(null);
    this.appointmentDialogKind.set(kind);
    this.appointmentDialog.set(dialog);
  }

  protected closeAppointmentDialog(): void {
    if (this.appointmentMutating()) return;
    this.appointmentDialog.set(null);
    this.appointmentDialogKind.set(null);
  }

  protected confirmAppointmentAction(): void {
    const kind = this.appointmentDialogKind();
    const appointment = kind ? this.appointmentFor(kind) : undefined;
    const app = this.detail();
    if (!kind || !appointment || !app?.id) return;

    const key = this.createIdempotencyKey();
    const dialog = this.appointmentDialog();
    if (dialog === 'confirm') {
      this.runAppointmentMutation(
        kind,
        this.api.confirmAppointment(app.id, kind, { rowVersion: appointment.rowVersion, idempotencyKey: key }),
      );
    } else if (dialog === 'slots' && this.selectedSlotId()) {
      this.runAppointmentMutation(
        kind,
        this.api.selectAppointmentSlot(app.id, kind, {
          slotId: this.selectedSlotId()!,
          rowVersion: appointment.rowVersion,
          idempotencyKey: key,
        }),
      );
    } else if (dialog === 'reschedule' && this.appointmentReason().trim()) {
      this.runAppointmentMutation(
        kind,
        this.api.requestAppointmentReschedule(app.id, kind, {
          reason: this.appointmentReason().trim(),
          rowVersion: appointment.rowVersion,
          idempotencyKey: key,
        }),
      );
    } else if (dialog === 'cancel') {
      this.runAppointmentMutation(
        kind,
        this.api.cancelAppointment(app.id, kind, {
          reason: this.appointmentReason().trim() || undefined,
          rowVersion: appointment.rowVersion,
          idempotencyKey: key,
        }),
      );
    }
  }

  protected joinAppointment(kind: SlotKind): void {
    const appointment = this.appointmentFor(kind);
    const app = this.detail();
    if (!app?.id || !appointment?.capabilities?.canJoinOnlineAppointment) return;
    this.appointmentMutating.set(true);
    this.appointmentAlert.set(null);
    this.api.joinAppointment(app.id, kind, { rowVersion: appointment.rowVersion })
      .pipe(finalize(() => this.appointmentMutating.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded && result.data?.actionPath) {
            void this.router.navigateByUrl(result.data.actionPath);
            return;
          }
          this.appointmentAlert.set(
            translateAdmissionErrorCodes(this.transloco, result.errorCodes),
          );
        },
        error: () => this.appointmentAlert.set(this.transloco.translate('parent.applications.appointment.joinUnavailable')),
      });
  }

  protected retryAvailableSlots(): void {
    const app = this.detail();
    if (app?.id) {
      this.loadAvailableSlots(app.id, app.policySummary?.requirementMode);
    }
  }

  protected availableSlotKindKey(kind: SlotKind | undefined): string {
    return kind === SlotKind._2
      ? 'parent.applications.availableSlots.kinds.assessment'
      : 'parent.applications.availableSlots.kinds.interview';
  }

  protected availableSlotModeKey(mode: SlotDeliveryMode | undefined): string {
    return mode === SlotDeliveryMode._1
      ? 'parent.applications.availableSlots.modes.online'
      : 'parent.applications.availableSlots.modes.onSite';
  }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('applicationId');
    if (!id) {
      void this.router.navigate(['/parent/applications']);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.api
      .getAdmissionApplication(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        this.detail.set(result.data as ParentDetail);
        this.loadAvailableSlots(id, result.data.policySummary?.requirementMode);
      });
  }

  private loadAvailableSlots(applicationId: string, requirementMode: number | undefined): void {
    const kinds =
      requirementMode === InterviewAssessmentRequirementMode._2
        ? [SlotKind._1]
        : requirementMode === InterviewAssessmentRequirementMode._3
          ? [SlotKind._2]
          : requirementMode === InterviewAssessmentRequirementMode._4
            ? [SlotKind._1, SlotKind._2]
            : [];
    if (!kinds.length) {
      this.availableSlots.set([]);
      return;
    }

    this.availableSlotsLoading.set(true);
    this.availableSlotsError.set(null);
    forkJoin(kinds.map((kind) => this.api.listAvailableInterviewAssessmentSlots(applicationId, kind)))
      .pipe(
        finalize(() => this.availableSlotsLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (results) => {
          const failed = results.find((result) => !result.succeeded);
          if (failed) {
            this.availableSlotsError.set(translateAdmissionErrorCodes(this.transloco, failed.errorCodes));
            return;
          }
          this.availableSlots.set(results.flatMap((result) => result.data ?? []));
        },
        error: () => this.availableSlotsError.set(this.transloco.translate('parent.errors.generic')),
      });
  }

  private runAppointmentMutation(
    kind: SlotKind,
    request$: import('rxjs').Observable<import('../../../../../core/api-client/SwaggerClient.service').AdmissionAppointmentDtoResult>,
  ): void {
    this.appointmentMutating.set(true);
    this.appointmentAlert.set(null);
    request$.pipe(
      finalize(() => this.appointmentMutating.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (result) => {
        if (!result.succeeded || !result.data) {
          this.handleAppointmentFailure(result.errorCodes);
          return;
        }
        this.detail.update((current) => current
          ? ({ ...current, [kind === SlotKind._2 ? 'activeAssessment' : 'activeInterview']: result.data } as ParentDetail)
          : current);
        this.appointmentDialog.set(null);
        this.appointmentDialogKind.set(null);
        this.appointmentAlert.set(this.transloco.translate('parent.applications.appointment.updated'));
      },
      error: () => {
        this.appointmentAlert.set(this.transloco.translate('parent.errors.generic'));
      },
    });
  }

  private handleAppointmentFailure(errorCodes: readonly string[] | undefined): void {
    if (errorCodes?.includes('admission.appointment.concurrencyConflict')) {
      this.appointmentDialog.set(null);
      this.appointmentAlert.set(this.transloco.translate('parent.applications.appointment.concurrency'));
      this.load();
      return;
    }
    this.appointmentAlert.set(translateAdmissionErrorCodes(this.transloco, errorCodes as string[] | undefined));
  }

  private createIdempotencyKey(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }
    return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
  }
}
