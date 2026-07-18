import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { ToastService } from '../../../../../shared/ui/toast/toast.service';
import { ParentApi } from '../../../data-access/parent.api';
import { ApplicationDetailPage } from './application-detail-page';

describe('ApplicationDetailPage available slots', () => {
  let fixture: ComponentFixture<ApplicationDetailPage>;
  let api: {
    getAdmissionApplication: ReturnType<typeof vi.fn>;
    listAvailableInterviewAssessmentSlots: ReturnType<typeof vi.fn>;
    confirmAppointment: ReturnType<typeof vi.fn>;
    selectAppointmentSlot: ReturnType<typeof vi.fn>;
    requestAppointmentReschedule: ReturnType<typeof vi.fn>;
    cancelAppointment: ReturnType<typeof vi.fn>;
    joinAppointment: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    api = {
      getAdmissionApplication: vi.fn(() => of({
        succeeded: true,
        data: {
          id: 'app-1',
          status: 2,
          applicationNumber: 'APP-1',
          capabilities: { canViewInterview: true },
          policySummary: { requirementMode: 2, requiredParticipants: 3 },
          activeInterview: {
            id: 'appointment-1', slotId: 'current-slot', lifecycle: 1, mode: 2,
            scheduledAtUtc: '2026-08-01T07:00:00Z', timeZoneId: 'Africa/Cairo',
            rowVersion: 'rv-1', location: 'Current safe address',
            capabilities: { canConfirmAppointment: true, canSelectAlternateSlot: true },
          },
          attachments: [],
          timeline: [],
        },
      })),
      listAvailableInterviewAssessmentSlots: vi.fn(() => of({
        succeeded: true,
        data: [{
          slotId: 'slot-1',
          kind: 1,
          deliveryMode: 2,
          startAtUtc: '2026-08-01T08:00:00Z',
          endAtUtc: '2026-08-01T08:30:00Z',
          timeZoneId: 'Africa/Cairo',
          remainingSeats: 4,
          branchName: 'Main Branch',
          branchAddress: 'Safe address',
          instructions: 'Bring identification',
        }],
      })),
      confirmAppointment: vi.fn(() => of({ succeeded: true, data: { id: 'appointment-1', lifecycle: 2 } })),
      selectAppointmentSlot: vi.fn(() => of({ succeeded: false, errorCodes: ['admission.appointment.slotFull'] })),
      requestAppointmentReschedule: vi.fn(() => of({ succeeded: true, data: {} })),
      cancelAppointment: vi.fn(() => of({ succeeded: true, data: {} })),
      joinAppointment: vi.fn(() => of({ succeeded: false, errorCodes: ['admission.appointment.joinTooEarly'] })),
    };

    await TestBed.configureTestingModule({
      imports: [
        ApplicationDetailPage,
        TranslocoTestingModule.forRoot({
          langs: {
            en: {
              parent: {
                applications: {
                  availableSlots: {
                    title: 'Available slots', subtitle: 'Read only', loading: 'Loading',
                    retry: 'Retry', empty: 'None', date: 'Date', timezone: 'Time zone',
                    remainingSeats: 'Remaining seats', branch: 'Branch', address: 'Address',
                    instructions: 'Instructions',
                    kinds: { interview: 'Interview', assessment: 'Assessment' },
                    modes: { online: 'Online', onSite: 'On site' },
                  },
                  detail: { title: 'Application', subtitle: 'Details', backToList: 'Back', datesTitle: 'Dates' },
                  timeline: { title: 'Timeline' },
                  wizard: { attachmentsTitle: 'Attachments' },
                },
                common: { retry: 'Retry' },
              },
            },
          },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
      providers: [
        provideRouter([]),
        { provide: ParentApi, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'app-1' } } } },
        { provide: LocaleFormatService, useValue: { formatDateTime: (value: string) => value } },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicationDetailPage);
    fixture.detectChanges();
  });

  it('loads relevant availability and renders current safe appointment fields', () => {
    expect(api.listAvailableInterviewAssessmentSlots).toHaveBeenCalledWith('app-1', 1);
    const card = fixture.nativeElement.querySelector('.application-detail__appointment');
    expect(card).toBeTruthy();
    expect(card.textContent).toContain('Current safe address');
  });

  it('shows only capability-driven actions and excludes the current slot', () => {
    const card = fixture.nativeElement.querySelector('.application-detail__appointment');
    const buttons = [...card.querySelectorAll('button')].map((button: HTMLButtonElement) => button.textContent.trim());
    expect(buttons).toContain('parent.applications.appointment.actions.confirm');
    expect(buttons).toContain('parent.applications.appointment.actions.alternate');
    expect(buttons).not.toContain('parent.applications.appointment.actions.reschedule');
    expect(buttons).not.toContain('parent.applications.appointment.actions.cancel');

    (card.querySelectorAll('button')[1] as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Safe address');
    expect(fixture.nativeElement.textContent).not.toContain('current-slot');
  });

  it('maps all backend appointment lifecycle values to the correct labels', () => {
    const appointmentStatusKey = (lifecycle: number) =>
      (fixture.componentInstance as any).appointmentStatusKey(lifecycle);

    expect([
      appointmentStatusKey(1),
      appointmentStatusKey(2),
      appointmentStatusKey(3),
      appointmentStatusKey(4),
      appointmentStatusKey(5),
      appointmentStatusKey(6),
    ]).toEqual([
      'parent.applications.appointment.lifecycle.proposed',
      'parent.applications.appointment.lifecycle.completed',
      'parent.applications.appointment.lifecycle.cancelled',
      'parent.applications.appointment.lifecycle.rescheduleRequested',
      'parent.applications.appointment.lifecycle.confirmed',
      'parent.applications.appointment.lifecycle.noShow',
    ]);
  });
});
