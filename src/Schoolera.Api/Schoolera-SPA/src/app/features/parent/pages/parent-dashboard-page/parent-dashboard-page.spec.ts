import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthService } from '../../../../core/auth/auth.service';
import { ParentApi } from '../../data-access/parent.api';
import { ParentDashboardPage } from './parent-dashboard-page';

describe('ParentDashboardPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ParentDashboardPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              parent: {
                dashboard: {
                  title: 'لوحة',
                  subtitle: 'وصف',
                  welcome: 'مرحبًا، {{name}}.',
                  childrenSection: 'أبناء',
                  totalChildren: 'الإجمالي',
                  activeChildren: 'نشط',
                  viewChildren: 'عرض',
                  quickActions: 'إجراءات',
                  addChild: 'إضافة',
                  profileIncomplete: 'ملف غير مكتمل',
                  completeProfile: 'إكمال',
                  applicationsTitle: 'طلبات',
                  applicationsUnavailable: 'غير متاح',
                  recentActivityTitle: 'نشاط',
                  recentActivityUnavailable: 'لا نشاط',
                },
                common: { retry: 'إعادة' },
              },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ id: 'u1', displayName: 'Parent User', email: 'p@test.com', roles: ['Parent'] }),
          },
        },
        {
          provide: ParentApi,
          useValue: {
            getDashboard: vi.fn(() =>
              of({
                succeeded: true,
                data: {
                  childCount: 2,
                  activeChildCount: 2,
                  applicationsAvailable: false,
                  recentActivityAvailable: false,
                  profileIncomplete: true,
                },
              }),
            ),
          },
        },
      ],
    }).compileComponents();
  });

  it('shows profile incomplete prompt and applications unavailable', () => {
    const fixture = TestBed.createComponent(ParentDashboardPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('غير متاح');
    expect(element.textContent).toContain('Parent User');
  });
});
