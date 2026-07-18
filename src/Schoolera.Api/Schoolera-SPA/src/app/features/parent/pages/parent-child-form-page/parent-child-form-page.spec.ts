import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import {
  ChildGender,
  ChildIdentityType,
  ChildStudyLanguage,
} from '../../../../core/api-client/SwaggerClient.service';
import { ParentApi } from '../../data-access/parent.api';
import { ParentChildFormPage } from './parent-child-form-page';

describe('ParentChildFormPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ParentChildFormPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                childForm: {
                  editTitle: 'تعديل',
                  editSubtitle: 'وصف',
                  addTitle: 'إضافة',
                  addSubtitle: 'وصف',
                  replaceIdentity: 'استبدال',
                  fields: {
                    fullName: 'اسم',
                    identityType: 'نوع',
                    identityValue: 'رقم',
                    maskedIdentity: 'مقنّع',
                    birthDate: 'ميلاد',
                    gender: 'جنس',
                    educationalStage: 'مرحلة',
                    currentGrade: 'صف',
                    currentSchoolName: 'المدرسة',
                    preferredStudyLanguage: 'اللغة',
                    skills: 'المهارات',
                    hobbies: 'الهوايات',
                    strengths: 'القوة',
                    improvementAreas: 'التحسين',
                    hasSpecialNeeds: 'احتياجات',
                    specialNeedsNotes: 'ملاحظات',
                    healthNotes: 'الصحة',
                  },
                  specialNeedsPrivacyNote: 'خاص',
                  healthPrivacyNote: 'خاص',
                },
                documentVault: {
                  title: 'الخزنة',
                  description: 'وصف',
                  loading: 'تحميل',
                  emptyType: 'فارغ',
                  upload: 'رفع',
                  types: {
                    birthCertificate: 'ميلاد',
                    childPhoto: 'صورة',
                    previousSchoolCertificate: 'شهادة',
                    medicalReport: 'تقرير',
                    otherApproved: 'آخر',
                  },
                },
                enums: {
                  gender: { male: 'ذكر', female: 'أنثى' },
                  identityType: { nationalId: 'وطنية', residencyId: 'إقامة' },
                  studyLanguage: {
                    arabic: 'العربية',
                    english: 'الإنجليزية',
                    french: 'الفرنسية',
                    german: 'الألمانية',
                    other: 'أخرى',
                  },
                },
                common: { save: 'حفظ', saving: '...', cancel: 'إلغاء', select: 'اختر' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'child-1' } } },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn() },
        },
        {
          provide: ParentApi,
          useValue: {
            getChild: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'child-1',
                  fullName: 'Omar',
                  maskedIdentity: '***5678',
                  identityType: ChildIdentityType._1,
                  birthDate: '2014-05-01',
                  ageYears: 11,
                  gender: ChildGender._1,
                  currentGradeId: 'g1',
                  currentGradeName: 'Grade 5',
                  currentSchoolName: 'Demo School',
                  preferredStudyLanguage: ChildStudyLanguage._2,
                  skills: 'Drawing',
                  hobbies: 'Reading',
                  strengths: 'Teamwork',
                  improvementAreas: 'Writing',
                  hasSpecialNeeds: false,
                  healthNotes: 'Allergy',
                  isActive: true,
                },
              }),
            ),
            updateChild: vi.fn(() => of({ succeeded: true, data: {} })),
            createChild: vi.fn(() => of({ succeeded: true, data: {} })),
            listChildDocuments: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
        {
          provide: TaxonomiesApi,
          useValue: {
            getEducationalStages: vi.fn(() => of({ succeeded: true, data: [] })),
            getGradesByStage: vi.fn(() => of({ succeeded: true, data: [] })),
          },
        },
        {
          provide: ToastService,
          useValue: { success: vi.fn() },
        },
      ],
    }).compileComponents();
  });

  it('shows masked identity in edit mode and hides full identity input', () => {
    const fixture = TestBed.createComponent(ParentChildFormPage);
    fixture.detectChanges();

    const html = fixture.nativeElement as HTMLElement;
    expect(html.textContent).toContain('***5678');
    expect(html.querySelector('#parent-child-identityValue')).toBeNull();
  });

  it('reveals replace identity fields when toggled', () => {
    const fixture = TestBed.createComponent(ParentChildFormPage);
    fixture.detectChanges();

    const checkbox = fixture.nativeElement.querySelector('.parent-replace-identity input[type="checkbox"]') as HTMLInputElement;
    checkbox.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('#parent-child-replaceIdentityValue')).not.toBeNull();
  });

  it('loads the extended child profile fields and study-language select', () => {
    const fixture = TestBed.createComponent(ParentChildFormPage);
    fixture.detectChanges();

    const html = fixture.nativeElement as HTMLElement;
    expect((html.querySelector('#parent-child-currentSchoolName') as HTMLInputElement).value).toBe('Demo School');
    expect((html.querySelector('#parent-child-preferredStudyLanguage') as HTMLSelectElement).selectedIndex).toBe(2);
    expect((html.querySelector('#parent-child-skills') as HTMLTextAreaElement).value).toBe('Drawing');
    expect((html.querySelector('#parent-child-healthNotes') as HTMLTextAreaElement).value).toBe('Allergy');
    expect(html.querySelector('se-child-document-vault')).not.toBeNull();
  });
});
