import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalApplicationDetailPage } from './portal-application-detail-page';

describe('PortalApplicationDetailPage slot scheduling', () => {
  let fixture: ComponentFixture<PortalApplicationDetailPage>;
  const api = {
    getAdmissionApplication: vi.fn(() => of({
      succeeded: true,
      data: {
        id: 'app-1', schoolBranchId: 'branch-1', educationalStageId: 'stage-1',
        gradeId: 'grade-1', academicYearId: 'year-1', rowVersion: 'rv-1',
        capabilities: { canScheduleInterview: true }, attachments: [], timeline: [],
      },
    })),
    listInterviewAssessmentSlots: vi.fn(() => of({
      succeeded: true,
      data: [{
        id: 'slot-1', startAtUtc: '2026-08-01T08:00:00Z', endAtUtc: '2026-08-01T08:30:00Z',
        timeZoneId: 'Africa/Cairo', deliveryMode: 2, capacity: 3, activeAppointmentCount: 1,
        instructionsEn: 'Bring ID',
      }],
    })),
    scheduleInterview: vi.fn(() => of({ succeeded: true, data: { id: 'app-1', capabilities: {} } })),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [
        PortalApplicationDetailPage,
        TranslocoTestingModule.forRoot({
          langs: { en: { portal: { applications: { detail: {}, timeline: {} }, confirm: {}, errors: {} } } },
          translocoConfig: { defaultLang: 'en', availableLangs: ['en'] },
        }),
      ],
      providers: [
        provideRouter([]),
        { provide: SchoolPortalApi, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => 'app-1' } },
            parent: { snapshot: { paramMap: { get: () => 'school-1' } } },
          },
        },
        { provide: LocaleFormatService, useValue: { formatDateTime: (value: string) => value } },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(PortalApplicationDetailPage);
    fixture.detectChanges();
  });

  it('loads matching open slots and forwards the selected slot', () => {
    (fixture.componentInstance as any).openDialog('scheduleInterview');
    fixture.detectChanges();
    expect(api.listInterviewAssessmentSlots).toHaveBeenCalledWith('school-1', {
      branchId: 'branch-1', stageId: 'stage-1', gradeId: 'grade-1',
      academicYearId: 'year-1', kind: 1, status: 2,
    });
    (fixture.componentInstance as any).selectedScheduleSlotId.set('slot-1');
    (fixture.componentInstance as any).submitSchedule();
    expect(api.scheduleInterview).toHaveBeenCalledWith(
      'school-1',
      'app-1',
      expect.objectContaining({ slotId: 'slot-1', rowVersion: 'rv-1', idempotencyKey: expect.any(String) }),
    );
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
      'portal.applications.detail.lifecycle.statusProposed',
      'portal.applications.detail.lifecycle.statusCompleted',
      'portal.applications.detail.lifecycle.statusCancelled',
      'portal.applications.detail.lifecycle.statusRescheduleRequested',
      'portal.applications.detail.lifecycle.statusConfirmed',
      'portal.applications.detail.lifecycle.statusNoShow',
    ]);
  });
});
