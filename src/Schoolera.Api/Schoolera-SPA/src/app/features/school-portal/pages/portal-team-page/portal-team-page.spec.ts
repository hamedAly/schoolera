import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { PortalContextService } from '../../data-access/portal-context.service';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { SchoolTeamRole } from '../../data-access/school-portal-permissions.models';
import { PortalTeamPage } from './portal-team-page';

const teamLang = {
  portal: {
    team: {
      title: 'الفريق',
      subtitle: 'وصف',
      addMember: 'إضافة عضو',
      saveMember: 'حفظ',
      transferOwnership: 'نقل الملكية',
      transferTitle: 'نقل',
      transferSubtitle: 'وصف النقل',
      transferSubmit: 'متابعة',
      transferConfirmTitle: 'تأكيد النقل',
      transferConfirmMessage: 'رسالة النقل',
      editConfirmTitle: 'تأكيد التعديل',
      editConfirmMessage: 'رسالة التعديل',
      activateConfirmTitle: 'تفعيل',
      activateConfirmMessage: 'رسالة تفعيل',
      deactivateConfirmTitle: 'إلغاء تفعيل',
      deactivateConfirmMessage: 'رسالة إلغاء',
      ownerBadge: 'مالك',
      emptyTitle: 'فارغ',
      emptyMessage: 'رسالة',
      retry: 'إعادة',
      roles: {
        schoolAdmin: 'مسؤول',
        admissionOfficer: 'قبول',
        financeOfficer: 'مالي',
        contentModerator: 'محتوى',
        unknown: 'عضو',
      },
      scope: {
        all: 'كل الفروع',
        selected: 'فروع محددة',
        selectedCount: '{{count}} فروع',
        notApplicable: 'على مستوى المدرسة',
      },
      status: { active: 'نشط', inactive: 'غير نشط' },
      fields: {
        email: 'بريد',
        role: 'دور',
        branchScope: 'نطاق',
        branches: 'فروع',
        isActive: 'نشط',
        newOwnerEmail: 'بريد المالك',
      },
    },
    confirm: { confirm: 'نعم', cancel: 'لا' },
    common: { loading: 'تحميل', edit: 'تعديل', activate: 'تفعيل', deactivate: 'إلغاء' },
  },
  common: { save: 'حفظ' },
};

describe('PortalTeamPage', () => {
  function setup(options: {
    canManageTeam: boolean;
    canTransferOwnership?: boolean;
  }) {
    return TestBed.configureTestingModule({
      imports: [
        PortalTeamPage,
        TranslocoTestingModule.forRoot({
          langs: { ar: teamLang },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { parent: { snapshot: { paramMap: { get: () => 'school-1' } } } },
        },
        {
          provide: DocumentLanguageService,
          useValue: { activeLang: signal('ar') },
        },
        {
          provide: PortalContextService,
          useValue: {
            canManageTeam: signal(options.canManageTeam),
            canTransferOwnership: signal(options.canTransferOwnership ?? options.canManageTeam),
            canViewTeam: signal(true),
            loadAccessibleSchools: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
        {
          provide: SchoolPortalApi,
          useValue: {
            listBranches: vi.fn(() => of({ succeeded: true, data: [] })),
            getTeam: vi.fn(() =>
              of({
                succeeded: true,
                data: [
                  {
                    userId: 'owner-1',
                    displayName: 'Owner',
                    email: 'owner@test.com',
                    isOwner: true,
                    isActive: true,
                  },
                  {
                    membershipId: 'mem-1',
                    userId: 'admin-1',
                    displayName: 'Admin',
                    email: 'admin@test.com',
                    isOwner: false,
                    isActive: true,
                    role: SchoolTeamRole.SchoolAdmin,
                  },
                ],
              }),
            ),
            addTeamMember: vi.fn(() => of({ succeeded: true, data: {} })),
            updateTeamMember: vi.fn(() => of({ succeeded: true, data: {} })),
            transferOwnership: vi.fn(() => of({ succeeded: true, data: {} })),
          },
        },
      ],
    }).compileComponents();
  }

  it('shows team management actions when canManageTeam is true', async () => {
    await setup({ canManageTeam: true });
    const fixture = TestBed.createComponent(PortalTeamPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.portal-team__add')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('نقل الملكية');
    expect(fixture.nativeElement.textContent).toContain('مسؤول');
  });

  it('hides team management actions when canManageTeam is false', async () => {
    await setup({ canManageTeam: false, canTransferOwnership: false });
    const fixture = TestBed.createComponent(PortalTeamPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.portal-team__add')).toBeFalsy();
    expect(fixture.nativeElement.textContent).not.toContain('نقل الملكية');
    expect(fixture.nativeElement.textContent).not.toContain('تعديل');
  });
});
