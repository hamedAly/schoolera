import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import {
  DayOfWeek,
  InterviewAssessmentSlotAuditDto,
  InterviewAssessmentSlotDto,
  SafeMeetingProviderOptionDto,
  SchoolTeamMemberDto,
  SlotDeliveryMode,
  SlotKind,
  SlotOccurrenceDto,
  SlotRecurrenceFrequency,
  SlotRecurrenceRequest,
  SlotResourceKind,
  SlotStatus,
  TaxonomyItemDto,
  UpsertInterviewAssessmentSlotRequest,
} from '../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { ConfirmationDialog } from '../../components/confirmation-dialog/confirmation-dialog';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';

type ViewMode = 'list' | 'calendar';
type LifecycleAction = 'open' | 'close' | 'reopen';

@Component({
  selector: 'se-portal-interview-assessment-slots-page',
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
  templateUrl: './portal-interview-assessment-slots-page.html',
  styleUrl: './portal-interview-assessment-slots-page.scss',
})
export class PortalInterviewAssessmentSlotsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly locale = inject(LocaleFormatService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly Kind = SlotKind;
  protected readonly Mode = SlotDeliveryMode;
  protected readonly Status = SlotStatus;
  protected readonly Frequency = SlotRecurrenceFrequency;
  protected readonly weekdays = [
    DayOfWeek._0, DayOfWeek._1, DayOfWeek._2, DayOfWeek._3,
    DayOfWeek._4, DayOfWeek._5, DayOfWeek._6,
  ] as const;

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly slots = signal<readonly InterviewAssessmentSlotDto[]>([]);
  protected readonly viewMode = signal<ViewMode>('list');
  protected readonly showSlotForm = signal(false);
  protected readonly showRecurrenceForm = signal(false);
  protected readonly editingSlot = signal<InterviewAssessmentSlotDto | null>(null);
  protected readonly confirmAction = signal<{ action: LifecycleAction; slot: InterviewAssessmentSlotDto } | null>(null);
  protected readonly cancellingSlot = signal<InterviewAssessmentSlotDto | null>(null);
  protected readonly auditSlot = signal<InterviewAssessmentSlotDto | null>(null);
  protected readonly auditLoading = signal(false);
  protected readonly auditItems = signal<readonly InterviewAssessmentSlotAuditDto[]>([]);
  protected readonly previewLoading = signal(false);
  protected readonly generating = signal(false);
  protected readonly previewOccurrences = signal<readonly SlotOccurrenceDto[]>([]);
  protected readonly previewCount = signal(0);
  protected readonly previewReady = signal(false);

  protected readonly branches = signal<readonly { id: string; name: string }[]>([]);
  protected readonly stages = signal<readonly TaxonomyItemDto[]>([]);
  protected readonly grades = signal<readonly TaxonomyItemDto[]>([]);
  protected readonly years = signal<readonly TaxonomyItemDto[]>([]);
  protected readonly providers = signal<readonly SafeMeetingProviderOptionDto[]>([]);
  protected readonly team = signal<readonly SchoolTeamMemberDto[]>([]);

  protected readonly filterForm = this.fb.nonNullable.group({
    branchId: [''],
    kind: [''],
    mode: [''],
    status: [''],
    resourceId: [''],
    from: [''],
    to: [''],
  });

  protected readonly slotForm = this.fb.nonNullable.group({
    schoolBranchId: ['', Validators.required],
    educationalStageId: ['', Validators.required],
    gradeId: [''],
    academicYearId: ['', Validators.required],
    kind: [SlotKind._1 as number, Validators.required],
    deliveryMode: [SlotDeliveryMode._2 as number, Validators.required],
    localDate: ['', Validators.required],
    localStartTime: ['', Validators.required],
    localEndTime: ['', Validators.required],
    timeZoneId: ['Africa/Cairo', Validators.required],
    capacity: [1, [Validators.required, Validators.min(1)]],
    resourceReferenceId: [''],
    instructionsAr: [''],
    instructionsEn: [''],
    meetingProviderCode: [''],
  });

  protected readonly recurrenceForm = this.fb.nonNullable.group({
    schoolBranchId: ['', Validators.required],
    educationalStageId: ['', Validators.required],
    gradeId: [''],
    academicYearId: ['', Validators.required],
    kind: [SlotKind._1 as number, Validators.required],
    deliveryMode: [SlotDeliveryMode._2 as number, Validators.required],
    localStartDate: ['', Validators.required],
    localEndDate: ['', Validators.required],
    localStartTime: ['', Validators.required],
    durationMinutes: [30, [Validators.required, Validators.min(1)]],
    timeZoneId: ['Africa/Cairo', Validators.required],
    capacity: [1, [Validators.required, Validators.min(1)]],
    resourceReferenceId: [''],
    instructionsAr: [''],
    instructionsEn: [''],
    meetingProviderCode: [''],
    frequency: [SlotRecurrenceFrequency._1 as number, Validators.required],
    selectedWeekdays: this.fb.nonNullable.control<number[]>([]),
    requestKey: [crypto.randomUUID()],
  });

  protected readonly calendarGroups = computed(() => {
    const groups = new Map<string, InterviewAssessmentSlotDto[]>();
    for (const slot of this.slots()) {
      const key = slot.startAtUtc?.slice(0, 10) ?? '';
      const group = groups.get(key) ?? [];
      group.push(slot);
      groups.set(key, group);
    }
    return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
  });

  protected readonly previewHasConflicts = computed(() =>
    this.previewOccurrences().some((occurrence) => occurrence.hasConflict),
  );

  ngOnInit(): void {
    this.loadLookups();
    this.loadSlots();
    this.filterForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.loadSlots());
    this.slotForm.controls.deliveryMode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.syncMode(this.slotForm, Number(value)));
    this.recurrenceForm.controls.deliveryMode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.syncMode(this.recurrenceForm, Number(value)));
    this.recurrenceForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.previewReady.set(false);
      this.previewOccurrences.set([]);
      this.previewCount.set(0);
    });
    this.slotForm.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((id) => this.loadGrades(id));
    this.recurrenceForm.controls.educationalStageId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((id) => this.loadGrades(id));
    this.syncMode(this.slotForm, SlotDeliveryMode._2);
    this.syncMode(this.recurrenceForm, SlotDeliveryMode._2);
  }

  protected setView(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  protected retry(): void {
    this.loadSlots();
  }

  protected openCreate(): void {
    this.editingSlot.set(null);
    this.slotForm.reset({
      kind: SlotKind._1,
      deliveryMode: SlotDeliveryMode._2,
      timeZoneId: 'Africa/Cairo',
      capacity: 1,
    });
    this.showRecurrenceForm.set(false);
    this.showSlotForm.set(true);
  }

  protected openEdit(slot: InterviewAssessmentSlotDto): void {
    if (!slot.id || !slot.capabilities?.canEdit) return;
    this.api.getInterviewAssessmentSlot(this.schoolId(), slot.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.showError(result.errorCodes);
          return;
        }
        const item = result.data;
        const local = this.localParts(item.startAtUtc, item.endAtUtc);
        this.editingSlot.set(item);
        this.slotForm.reset({
          schoolBranchId: item.schoolBranchId ?? '',
          educationalStageId: item.educationalStageId ?? '',
          gradeId: item.gradeId ?? '',
          academicYearId: item.academicYearId ?? '',
          kind: item.kind ?? SlotKind._1,
          deliveryMode: item.deliveryMode ?? SlotDeliveryMode._2,
          localDate: local.date,
          localStartTime: local.start,
          localEndTime: local.end,
          timeZoneId: item.timeZoneId ?? 'Africa/Cairo',
          capacity: item.capacity ?? 1,
          resourceReferenceId: item.resourceReferenceId ?? '',
          instructionsAr: item.instructionsAr ?? '',
          instructionsEn: item.instructionsEn ?? '',
          meetingProviderCode: item.meetingProviderCode ?? '',
        });
        this.showRecurrenceForm.set(false);
        this.showSlotForm.set(true);
      });
  }

  protected saveSlot(): void {
    if (this.slotForm.invalid) {
      this.slotForm.markAllAsTouched();
      return;
    }
    const body = this.slotBody();
    const editing = this.editingSlot();
    this.saving.set(true);
    const request = editing?.id
      ? this.api.updateInterviewAssessmentSlot(this.schoolId(), editing.id, {
          ...body,
          rowVersion: editing.rowVersion,
        })
      : this.api.createInterviewAssessmentSlot(this.schoolId(), body);
    request.pipe(
      finalize(() => this.saving.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((result) => {
      if (!result.succeeded || !result.data) {
        this.handleMutationError(result.errorCodes, editing?.id);
        return;
      }
      this.showSlotForm.set(false);
      this.toast.success(this.transloco.translate('portal.slots.messages.saved'));
      this.loadSlots();
    });
  }

  protected openRecurrence(): void {
    this.showSlotForm.set(false);
    this.recurrenceForm.controls.requestKey.setValue(crypto.randomUUID(), { emitEvent: false });
    this.previewReady.set(false);
    this.showRecurrenceForm.set(true);
  }

  protected toggleWeekday(day: DayOfWeek, checked: boolean): void {
    const current = new Set(this.recurrenceForm.controls.selectedWeekdays.value);
    checked ? current.add(day) : current.delete(day);
    this.recurrenceForm.controls.selectedWeekdays.setValue([...current]);
  }

  protected weekdaySelected(day: DayOfWeek): boolean {
    return this.recurrenceForm.controls.selectedWeekdays.value.includes(day);
  }

  protected previewRecurrence(): void {
    if (!this.recurrenceValid()) return;
    this.previewLoading.set(true);
    this.api.previewInterviewAssessmentSlotRecurrence(this.schoolId(), this.recurrenceBody())
      .pipe(finalize(() => this.previewLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded || !result.data) {
          this.showError(result.errorCodes);
          return;
        }
        this.previewCount.set(result.data.occurrenceCount ?? 0);
        this.previewOccurrences.set(result.data.occurrences ?? []);
        this.previewReady.set(true);
      });
  }

  protected generateRecurrence(): void {
    if (!this.previewReady() || this.previewHasConflicts()) return;
    this.generating.set(true);
    this.api.generateInterviewAssessmentSlotRecurrence(this.schoolId(), this.recurrenceBody())
      .pipe(finalize(() => this.generating.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result.succeeded) {
          this.handleMutationError(result.errorCodes);
          return;
        }
        this.toast.success(this.transloco.translate('portal.slots.messages.generated'));
        this.showRecurrenceForm.set(false);
        this.loadSlots();
      });
  }

  protected confirmLifecycle(): void {
    const pending = this.confirmAction();
    if (!pending?.slot.id) return;
    const calls = {
      open: () => this.api.openInterviewAssessmentSlot(this.schoolId(), pending.slot.id!, pending.slot.rowVersion),
      close: () => this.api.closeInterviewAssessmentSlot(this.schoolId(), pending.slot.id!, pending.slot.rowVersion),
      reopen: () => this.api.reopenInterviewAssessmentSlot(this.schoolId(), pending.slot.id!, pending.slot.rowVersion),
    };
    calls[pending.action]().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.confirmAction.set(null);
      if (!result.succeeded) {
        this.handleMutationError(result.errorCodes, pending.slot.id);
        return;
      }
      this.loadSlots();
    });
  }

  protected readonly cancelForm = this.fb.nonNullable.group({
    cancellationReasonAr: ['', Validators.required],
    cancellationReasonEn: ['', Validators.required],
  });

  protected openCancel(slot: InterviewAssessmentSlotDto): void {
    if (!slot.capabilities?.canCancel) return;
    this.cancelForm.reset();
    this.cancellingSlot.set(slot);
  }

  protected confirmCancel(): void {
    const slot = this.cancellingSlot();
    if (!slot?.id || this.cancelForm.invalid) {
      this.cancelForm.markAllAsTouched();
      return;
    }
    this.api.cancelInterviewAssessmentSlot(this.schoolId(), slot.id, {
      ...this.cancelForm.getRawValue(),
      rowVersion: slot.rowVersion,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.cancellingSlot.set(null);
      if (!result.succeeded) {
        this.handleMutationError(result.errorCodes, slot.id);
        return;
      }
      this.loadSlots();
    });
  }

  protected showAudit(slot: InterviewAssessmentSlotDto): void {
    if (!slot.id) return;
    this.auditSlot.set(slot);
    this.auditItems.set([]);
    this.auditLoading.set(true);
    this.api.getInterviewAssessmentSlotAudit(this.schoolId(), slot.id)
      .pipe(finalize(() => this.auditLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) this.auditItems.set((result.data ?? []).slice(0, 50));
        else this.showError(result.errorCodes);
      });
  }

  protected formatDate(value?: string): string {
    return value ? this.locale.formatDateTime(value) : '—';
  }

  protected kindKey(kind?: SlotKind): string {
    return kind === SlotKind._2 ? 'portal.slots.kinds.assessment' : 'portal.slots.kinds.interview';
  }

  protected modeKey(mode?: SlotDeliveryMode): string {
    return mode === SlotDeliveryMode._1 ? 'portal.slots.modes.online' : 'portal.slots.modes.onSite';
  }

  protected statusKey(status?: SlotStatus): string {
    return `portal.slots.status.${({ 1: 'draft', 2: 'open', 3: 'closed', 4: 'cancelled' } as Record<number, string>)[status ?? 1]}`;
  }

  protected weekdayKey(day: DayOfWeek): string {
    return `portal.slots.recurrence.weekdays.${day}`;
  }

  protected safeMetadata(value?: string): string {
    if (!value) return '—';
    try {
      const parsed = JSON.parse(value) as Record<string, unknown>;
      return Object.entries(parsed).slice(0, 8).map(([key, item]) => `${key}: ${String(item).slice(0, 120)}`).join(' · ');
    } catch {
      return value.slice(0, 500);
    }
  }

  private schoolId(): string {
    return this.route.parent?.snapshot.paramMap.get('schoolId') ?? '';
  }

  private loadSlots(): void {
    const value = this.filterForm.getRawValue();
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.listInterviewAssessmentSlots(this.schoolId(), {
      branchId: value.branchId || undefined,
      kind: value.kind ? Number(value.kind) : undefined,
      mode: value.mode ? Number(value.mode) : undefined,
      status: value.status ? Number(value.status) : undefined,
      resourceId: value.resourceId || undefined,
      from: value.from || undefined,
      to: value.to || undefined,
    }).pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded) this.slots.set(result.data ?? []);
          else this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => this.errorMessage.set(this.transloco.translate('portal.errors.generic')),
      });
  }

  private loadLookups(): void {
    this.api.listBranches(this.schoolId()).subscribe((result) => {
      if (result.succeeded) {
        this.branches.set((result.data ?? []).map((branch) => ({
          id: branch.id!,
          name: branch.nameEn || branch.nameAr || '',
        })));
      }
    });
    this.taxonomiesApi.getEducationalStages().subscribe((result) => {
      if (result.succeeded) this.stages.set(result.data ?? []);
    });
    this.taxonomiesApi.getAcademicYears().subscribe((result) => {
      if (result.succeeded) this.years.set(result.data ?? []);
    });
    this.api.listMeetingProviderOptions(this.schoolId()).subscribe((result) => {
      if (result.succeeded) this.providers.set(result.data ?? []);
    });
    this.api.getTeam(this.schoolId()).subscribe((result) => {
      if (result.succeeded) this.team.set((result.data ?? []).filter((member) => member.isActive !== false));
    });
  }

  private loadGrades(stageId: string): void {
    if (!stageId) {
      this.grades.set([]);
      return;
    }
    this.taxonomiesApi.getGradesByStage(stageId).subscribe((result) => {
      if (result.succeeded) this.grades.set(result.data ?? []);
    });
  }

  private syncMode(form: typeof this.slotForm | typeof this.recurrenceForm, mode: number): void {
    const provider = form.controls.meetingProviderCode;
    if (mode === SlotDeliveryMode._1) {
      provider.addValidators(Validators.required);
    } else {
      provider.clearValidators();
      provider.setValue('', { emitEvent: false });
    }
    provider.updateValueAndValidity({ emitEvent: false });
  }

  private slotBody(): UpsertInterviewAssessmentSlotRequest {
    const value = this.slotForm.getRawValue();
    return {
      ...value,
      kind: Number(value.kind),
      deliveryMode: Number(value.deliveryMode),
      gradeId: value.gradeId || undefined,
      resourceKind: value.resourceReferenceId ? SlotResourceKind._1 : undefined,
      resourceReferenceId: value.resourceReferenceId || undefined,
      instructionsAr: value.instructionsAr || undefined,
      instructionsEn: value.instructionsEn || undefined,
      meetingProviderCode: value.meetingProviderCode || undefined,
    };
  }

  private recurrenceBody(): SlotRecurrenceRequest {
    const value = this.recurrenceForm.getRawValue();
    return {
      ...value,
      kind: Number(value.kind),
      deliveryMode: Number(value.deliveryMode),
      frequency: Number(value.frequency),
      selectedWeekdays: value.selectedWeekdays as DayOfWeek[],
      gradeId: value.gradeId || undefined,
      resourceKind: value.resourceReferenceId ? SlotResourceKind._1 : undefined,
      resourceReferenceId: value.resourceReferenceId || undefined,
      instructionsAr: value.instructionsAr || undefined,
      instructionsEn: value.instructionsEn || undefined,
      meetingProviderCode: value.meetingProviderCode || undefined,
    };
  }

  private recurrenceValid(): boolean {
    const weekly = Number(this.recurrenceForm.controls.frequency.value) === SlotRecurrenceFrequency._2;
    if (this.recurrenceForm.invalid || (weekly && !this.recurrenceForm.controls.selectedWeekdays.value.length)) {
      this.recurrenceForm.markAllAsTouched();
      return false;
    }
    return true;
  }

  private handleMutationError(errorCodes?: string[], slotId?: string): void {
    if (errorCodes?.includes('schoolPortal.concurrentUpdate')) {
      this.toast.error(this.transloco.translate('portal.slots.messages.concurrentUpdate'));
      this.loadSlots();
      if (slotId) {
        this.api.getInterviewAssessmentSlot(this.schoolId(), slotId).subscribe();
      }
      return;
    }
    this.showError(errorCodes);
  }

  private showError(errorCodes?: string[]): void {
    this.toast.error(translatePortalErrorCodes(this.transloco, errorCodes));
  }

  private localParts(start?: string, end?: string): { date: string; start: string; end: string } {
    return {
      date: start?.slice(0, 10) ?? '',
      start: start?.slice(11, 16) ?? '',
      end: end?.slice(11, 16) ?? '',
    };
  }
}
