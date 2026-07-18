import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';

export const schoolOnboardingRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SchoolOwner'] },
    loadComponent: () =>
      import('./pages/onboarding-wizard-page/onboarding-wizard-page').then(
        (m) => m.OnboardingWizardPage,
      ),
    title: 'titles.schoolOnboarding',
  },
  {
    path: 'status',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SchoolOwner'] },
    loadComponent: () =>
      import('./pages/onboarding-status-page/onboarding-status-page').then(
        (m) => m.OnboardingStatusPage,
      ),
    title: 'titles.schoolOnboardingStatus',
  },
];
