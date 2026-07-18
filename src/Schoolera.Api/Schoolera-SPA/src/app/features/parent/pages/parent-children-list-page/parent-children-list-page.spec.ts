import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ChildGender, ChildIdentityType } from '../../../../core/api-client/SwaggerClient.service';
import { ParentApi } from '../../data-access/parent.api';
import { ParentChildrenListPage } from './parent-children-list-page';

describe('ParentChildrenListPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ParentChildrenListPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                children: {
                  title: 'أبنائي',
                  subtitle: 'وصف',
                  addChild: 'إضافة',
                  emptyTitle: 'فارغ',
                  emptyMessage: 'لا أبناء',
                  specialNeedsYes: 'نعم',
                  specialNeedsNo: 'لا',
                  deleteConfirmTitle: 'حذف؟',
                  deleteConfirmMessage: 'تأكيد',
                  columns: {
                    name: 'اسم',
                    identity: 'هوية',
                    age: 'عمر',
                    gender: 'جنس',
                    grade: 'صف',
                    specialNeeds: 'احتياجات',
                    actions: 'إجراءات',
                  },
                },
                enums: { gender: { male: 'ذكر', female: 'أنثى' } },
                confirm: { confirm: 'نعم', cancel: 'لا' },
                common: { edit: 'تعديل', delete: 'حذف', retry: 'إعادة' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideRouter([]),
        {
          provide: ParentApi,
          useValue: {
            listChildren: vi.fn(() => of({ succeeded: true, data: [] })),
            deleteChild: vi.fn(() => of({ succeeded: true, data: true })),
          },
        },
      ],
    }).compileComponents();
  });

  it('shows empty state when there are no children', () => {
    const fixture = TestBed.createComponent(ParentChildrenListPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('فارغ');
    expect(fixture.nativeElement.textContent).toContain('لا أبناء');
  });

  it('masks identity in the list', async () => {
    TestBed.overrideProvider(ParentApi, {
      useValue: {
        listChildren: vi.fn(() =>
          of({
            succeeded: true,
            data: [
              {
                id: 'c1',
                fullName: 'Sara',
                maskedIdentity: '***1234',
                identityType: ChildIdentityType._1,
                birthDate: '2015-01-01',
                ageYears: 10,
                gender: ChildGender._2,
                currentGradeId: 'g1',
                currentGradeName: 'Grade 4',
                hasSpecialNeeds: false,
                isActive: true,
              },
            ],
          }),
        ),
        deleteChild: vi.fn(() => of({ succeeded: true, data: true })),
      },
    });

    const fixture = TestBed.createComponent(ParentChildrenListPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('***1234');
    expect(fixture.nativeElement.textContent).not.toContain('1234567890');
  });

  it('opens delete confirmation dialog', async () => {
    TestBed.overrideProvider(ParentApi, {
      useValue: {
        listChildren: vi.fn(() =>
          of({
            succeeded: true,
            data: [
              {
                id: 'c1',
                fullName: 'Sara',
                maskedIdentity: '***1234',
                identityType: ChildIdentityType._1,
                birthDate: '2015-01-01',
                ageYears: 10,
                gender: ChildGender._2,
                currentGradeId: 'g1',
                currentGradeName: 'Grade 4',
                hasSpecialNeeds: false,
                isActive: true,
              },
            ],
          }),
        ),
        deleteChild: vi.fn(() => of({ succeeded: true, data: true })),
      },
    });

    const fixture = TestBed.createComponent(ParentChildrenListPage);
    fixture.detectChanges();

    const deleteButton = fixture.nativeElement.querySelector('button.danger') as HTMLButtonElement;
    deleteButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('حذف؟');
  });
});
