import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  DayOfWeek,
  SlotDeliveryMode,
  SlotRecurrenceFrequency,
} from '../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalInterviewAssessmentSlotsPage } from './portal-interview-assessment-slots-page';

describe('PortalInterviewAssessmentSlotsPage', () => {
  let fixture: ComponentFixture<PortalInterviewAssessmentSlotsPage>;
  let api: Record<string, ReturnType<typeof vi.fn>>;

  beforeEach(async () => {
    api = {
      listInterviewAssessmentSlots: vi.fn(() => of({
        succeeded: true,
        data: [{
          id: 'slot-1', capacity: 8, activeAppointmentCount: 3,
          affectedApplicationCount: 2, startAtUtc: '2026-08-01T08:00:00Z',
          capabilities: { canEdit: true, canOpen: false, canClose: true, canCancel: true },
        }],
      })),
      listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
      listMeetingProviderOptions: vi.fn(() => of({ succeeded: true, data: [] })),
      getTeam: vi.fn(() => of({ succeeded: true, data: [] })),
      previewInterviewAssessmentSlotRecurrence: vi.fn(() => of({
        succeeded: true,
        data: { occurrenceCount: 2, occurrences: [{ startAtUtc: '2026-08-01T08:00:00Z', hasConflict: true }] },
      })),
      generateInterviewAssessmentSlotRecurrence: vi.fn(() => of({ succeeded: true, data: { slots: [] } })),
      getInterviewAssessmentSlot: vi.fn(() => of({ succeeded: true, data: {} })),
      cancelInterviewAssessmentSlot: vi.fn(() => of({ succeeded: true, data: {} })),
    };

    await TestBed.configureTestingModule({
      imports: [
        PortalInterviewAssessmentSlotsPage,
        TranslocoTestingModule.forRoot({
          langs: { en: { portal: { slots: {}, confirm: {}, errors: { generic: 'Error' } }, common: {} } },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
      providers: [
        { provide: SchoolPortalApi, useValue: api },
        {
          provide: TaxonomiesApi,
          useValue: {
            getEducationalStages: vi.fn(() => of({ succeeded: true, data: [] })),
            getAcademicYears: vi.fn(() => of({ succeeded: true, data: [] })),
            getGradesByStage: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
        { provide: LocaleFormatService, useValue: { formatDateTime: (value: string) => value } },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
        {
          provide: ActivatedRoute,
          useValue: { parent: { snapshot: { paramMap: { get: () => 'school-1' } } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalInterviewAssessmentSlotsPage);
    fixture.detectChanges();
  });

  it('renders capacity, affected count, and capability-driven actions', () => {
    expect(fixture.nativeElement.textContent).toContain('3/8');
    expect(fixture.nativeElement.textContent).toContain('2');
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('portal.slots.actions.edit');
    expect(text).toContain('portal.slots.actions.close');
    expect(text).not.toContain('portal.slots.actions.open');
  });

  it('forwards filters and toggles the bounded calendar presentation', () => {
    const page = fixture.componentInstance as any;
    api['listInterviewAssessmentSlots'].mockClear();
    page.filterForm.patchValue({ branchId: 'b1', kind: '2' });
    expect(api['listInterviewAssessmentSlots']).toHaveBeenLastCalledWith(
      'school-1',
      expect.objectContaining({ branchId: 'b1', kind: 2 }),
    );
    page.setView('calendar');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.slot-calendar')).toBeTruthy();
  });

  it('requires a provider online, clears it on site, and exposes no Hybrid option', () => {
    const page = fixture.componentInstance as any;
    page.openCreate();
    page.slotForm.controls.deliveryMode.setValue(SlotDeliveryMode._1);
    page.slotForm.controls.meetingProviderCode.setValue('');
    expect(page.slotForm.controls.meetingProviderCode.invalid).toBe(true);
    page.slotForm.controls.meetingProviderCode.setValue('safe-provider');
    page.slotForm.controls.deliveryMode.setValue(SlotDeliveryMode._2);
    expect(page.slotForm.controls.meetingProviderCode.value).toBe('');
    fixture.detectChanges();
    const modeOptions = [...fixture.nativeElement.querySelectorAll('[data-testid="slot-mode"] option')];
    expect(modeOptions).toHaveLength(2);
  });

  it('requires weekdays for weekly recurrence and blocks generation on conflicts', () => {
    const page = fixture.componentInstance as any;
    page.openRecurrence();
    page.recurrenceForm.patchValue({
      schoolBranchId: 'b1', educationalStageId: 's1', academicYearId: 'y1',
      localStartDate: '2026-08-01', localEndDate: '2026-08-08', localStartTime: '08:00',
      frequency: SlotRecurrenceFrequency._2,
    });
    page.previewRecurrence();
    expect(api['previewInterviewAssessmentSlotRecurrence']).not.toHaveBeenCalled();
    page.toggleWeekday(DayOfWeek._1, true);
    page.previewRecurrence();
    expect(api['previewInterviewAssessmentSlotRecurrence']).toHaveBeenCalled();
    expect(page.previewCount()).toBe(2);
    page.generateRecurrence();
    expect(api['generateInterviewAssessmentSlotRecurrence']).not.toHaveBeenCalled();
  });

  it('requires both cancellation reasons', () => {
    const page = fixture.componentInstance as any;
    page.openCancel({ id: 'slot-1', capabilities: { canCancel: true } });
    page.cancelForm.patchValue({ cancellationReasonAr: 'سبب', cancellationReasonEn: '' });
    page.confirmCancel();
    expect(api['cancelInterviewAssessmentSlot']).not.toHaveBeenCalled();
    page.cancelForm.patchValue({ cancellationReasonEn: 'Reason' });
    page.confirmCancel();
    expect(api['cancelInterviewAssessmentSlot']).toHaveBeenCalled();
  });
});
