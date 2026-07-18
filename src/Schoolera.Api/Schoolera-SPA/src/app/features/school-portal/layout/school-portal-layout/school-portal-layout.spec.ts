import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../core/auth/auth.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { PortalContextService } from '../../data-access/portal-context.service';
import { SchoolPortalPermissionsDto } from '../../data-access/school-portal-permissions.models';
import { SchoolPortalLayout } from './school-portal-layout';

@Component({
  selector: 'se-layout-test-host',
  template: '',
})
class LayoutTestHost {}

describe('SchoolPortalLayout navigation permissions', () => {
  function createFixture(permissions: SchoolPortalPermissionsDto | null) {
    TestBed.configureTestingModule({
      imports: [
        SchoolPortalLayout,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                layout: {
                  brand: 'بوابة',
                  mainNav: 'تنقل',
                  openNav: 'فتح',
                  closeNav: 'إغلاق',
                  schoolSelector: 'مدرسة',
                  logout: 'خروج',
                },
                nav: {
                  overview: 'نظرة',
                  profile: 'ملف',
                  branches: 'فروع',
                  stages: 'مراحل',
                  fees: 'رسوم',
                  facilities: 'مرافق',
                  gallery: 'معرض',
                  services: 'خدمات',
                  team: 'فريق',
                  applications: 'طلبات',
                  admissionRequirements: 'متطلبات',
                  admissionQuestions: 'أسئلة',
                  ageEligibilityRules: 'العمر',
                  interviewAssessmentPolicies: 'مقابلة',
                },
                status: {
                  draft: 'مسودة',
                  published: 'منشور',
                  unpublished: 'غير منشور',
                  suspended: 'موقوف',
                },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideRouter([{ path: '**', component: LayoutTestHost }]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: () => 'school-1' },
              firstChild: { routeConfig: { path: 'overview' } },
            },
          },
        },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ displayName: 'User', roles: ['SchoolOwner'] }),
            logout: vi.fn(() => of(void 0)),
          },
        },
        {
          provide: DocumentLanguageService,
          useValue: { activeLang: signal('ar') },
        },
        {
          provide: PortalContextService,
          useValue: {
            currentDashboard: signal(null),
            accessibleSchools: signal([{ id: 'school-1', nameAr: 'مدرسة', nameEn: 'School' }]),
            currentSchool: signal({
              id: 'school-1',
              nameAr: 'مدرسة',
              nameEn: 'School',
              permissions,
            }),
            currentPermissions: signal(permissions),
            loadDashboard: vi.fn(() => of({ succeeded: true, data: null })),
          },
        },
      ],
    });

    const fixture = TestBed.createComponent(SchoolPortalLayout);
    fixture.detectChanges();
    return fixture;
  }

  function navTexts(fixture: ComponentFixture<SchoolPortalLayout>): string {
    return fixture.nativeElement.querySelector('.school-portal__nav')?.textContent ?? '';
  }

  it('shows finance-only nav for finance officer permissions', () => {
    const fixture = createFixture({
      canViewDashboard: true,
      canViewFees: true,
      canManageFees: true,
    });
    const text = navTexts(fixture);
    expect(text).toContain('نظرة');
    expect(text).toContain('رسوم');
    expect(text).not.toContain('فريق');
    expect(text).not.toContain('طلبات');
    expect(text).not.toContain('فروع');
  });

  it('shows admissions nav for admission officer permissions', () => {
    const fixture = createFixture({
      canViewDashboard: true,
      canViewApplications: true,
      canManageAdmissionRequirements: true,
      canManageAdmissionQuestions: true,
    });
    const text = navTexts(fixture);
    expect(text).toContain('طلبات');
    expect(text).toContain('متطلبات');
    expect(text).toContain('أسئلة');
    expect(text).not.toContain('رسوم');
    expect(text).not.toContain('فريق');
  });

  it('shows team link when canViewTeam is true', () => {
    const fixture = createFixture({
      canViewDashboard: true,
      canViewTeam: true,
    });
    expect(navTexts(fixture)).toContain('فريق');
  });
});
