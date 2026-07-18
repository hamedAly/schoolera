import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { GenderType, SchoolType } from '../../../../core/api-client/SwaggerClient.service';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalProfilePage } from './portal-profile-page';

describe('PortalProfilePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PortalProfilePage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                profile: {
                  title: 'ملف',
                  subtitle: 'وصف',
                  seoSection: 'SEO',
                  mediaSection: 'وسائط',
                  uploadLogo: 'شعار',
                  uploadCover: 'غلاف',
                  fields: {
                    nameAr: 'اسم ع',
                    nameEn: 'اسم En',
                    shortDescriptionAr: 'قصير ع',
                    shortDescriptionEn: 'قصير En',
                    fullDescriptionAr: 'كامل ع',
                    fullDescriptionEn: 'كامل En',
                    schoolType: 'نوع',
                    genderType: 'جنس',
                    foundedYear: 'سنة',
                    studentCount: 'عدد',
                    publicPhone: 'هاتف',
                    publicEmail: 'بريد',
                    websiteUrl: 'موقع',
                    whatsAppNumber: 'واتس',
                    seoTitleAr: 'seo ع',
                    seoTitleEn: 'seo En',
                    seoDescriptionAr: 'desc ع',
                    seoDescriptionEn: 'desc En',
                  },
                },
                enums: {
                  schoolType: { private: 'خ', international: 'د', national: 'و', language: 'ل' },
                  genderType: { boys: 'ب', girls: 'بن', mixed: 'م' },
                },
                common: { loading: 'تحميل' },
              },
              common: {
                save: 'حفظ',
                saving: 'حفظ...',
                bilingual: { arabicLabel: 'عربي', englishLabel: 'English' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { parent: { snapshot: { paramMap: { get: () => 'school-1' } } } },
        },
        {
          provide: SchoolPortalApi,
          useValue: {
            getProfile: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  id: 'school-1',
                  nameAr: 'مدرسة',
                  slug: 'school',
                  schoolType: SchoolType._1,
                  genderType: GenderType._3,
                  status: 1,
                  updatedAtUtc: '2026-01-01T00:00:00Z',
                },
              }),
            ),
            updateProfile: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  });

  it('requires Arabic school name before save', () => {
    const fixture = TestBed.createComponent(PortalProfilePage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component['form'].controls.nameAr.setValue('');
    component['save']();

    expect(component['form'].controls.nameAr.invalid).toBe(true);
  });

  it('reports unsaved changes when form is dirty', () => {
    const fixture = TestBed.createComponent(PortalProfilePage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.hasUnsavedChanges()).toBe(false);
    component['form'].controls.nameEn.setValue('Updated');
    component['form'].markAsDirty();
    expect(component.hasUnsavedChanges()).toBe(true);
  });
});
