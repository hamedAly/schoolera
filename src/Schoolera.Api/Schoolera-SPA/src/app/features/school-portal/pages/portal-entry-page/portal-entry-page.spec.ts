import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthService } from '../../../../core/auth/auth.service';
import { PortalContextService } from '../../data-access/portal-context.service';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { PortalEntryPage } from './portal-entry-page';

describe('PortalEntryPage', () => {
  let router: Router;
  let portalApi: { listAccessibleSchools: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    portalApi = {
      listAccessibleSchools: vi.fn(() =>
        of({
          succeeded: true,
          data: [{ id: 'school-1', nameAr: 'مدرسة', slug: 'school', status: 1, isOwner: true }],
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [
        PortalEntryPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              portal: {
                entry: { title: 'مدارسك', subtitle: 'اختر', noSchoolsTitle: 'لا مدارس', noSchoolsMessage: 'رسالة', onboardingCta: 'تسجيل' },
                common: { loading: 'تحميل' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: SchoolPortalApi, useValue: portalApi },
        PortalContextService,
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ id: 'u1', displayName: 'Owner', email: 'o@test.com', roles: ['SchoolOwner'] }),
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  it('redirects to overview when only one school is accessible', () => {
    const fixture = TestBed.createComponent(PortalEntryPage);
    fixture.detectChanges();

    expect(router.navigate).toHaveBeenCalledWith(['/school', 'school-1', 'overview']);
  });

  it('shows selector when multiple schools are accessible', () => {
    portalApi.listAccessibleSchools.mockReturnValue(
      of({
        succeeded: true,
        data: [
          { id: 'school-1', nameAr: 'أ', slug: 'a', status: 1, isOwner: true },
          { id: 'school-2', nameAr: 'ب', slug: 'b', status: 1, isOwner: true },
        ],
      }),
    );

    const fixture = TestBed.createComponent(PortalEntryPage);
    fixture.detectChanges();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelectorAll('.portal-entry__card').length).toBe(2);
  });
});
