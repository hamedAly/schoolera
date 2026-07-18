import { isDevMode } from '@angular/core';
import { Routes } from '@angular/router';

import { PublicLayout } from './core/layout/public-layout/public-layout';
import { AppShell } from './core/layout/app-shell/app-shell';
import { protectedPortalRoutes } from './features/auth/auth.routes';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: PublicLayout,
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/home/pages/home-page/home-page').then((m) => m.HomePage),
        title: 'titles.home',
      },
      {
        path: 'schools',
        loadChildren: () => import('./features/schools/schools.routes').then((m) => m.schoolsRoutes),
      },
      {
        path: 'about',
        loadComponent: () =>
          import('./features/public/pages/cms-static-page/cms-static-page').then(
            (m) => m.CmsStaticPage,
          ),
        data: { slug: 'about' },
        title: 'titles.about',
      },
      {
        path: 'how-it-works',
        loadComponent: () =>
          import('./features/public/pages/cms-static-page/cms-static-page').then(
            (m) => m.CmsStaticPage,
          ),
        data: { slug: 'how-it-works' },
        title: 'titles.howItWorks',
      },
      {
        path: 'privacy',
        loadComponent: () =>
          import('./features/public/pages/cms-static-page/cms-static-page').then(
            (m) => m.CmsStaticPage,
          ),
        data: { slug: 'privacy' },
        title: 'titles.privacy',
      },
      {
        path: 'terms',
        loadComponent: () =>
          import('./features/public/pages/cms-static-page/cms-static-page').then(
            (m) => m.CmsStaticPage,
          ),
        data: { slug: 'terms' },
        title: 'titles.terms',
      },
      {
        path: 'sla',
        loadComponent: () =>
          import('./features/public/pages/cms-static-page/cms-static-page').then(
            (m) => m.CmsStaticPage,
          ),
        data: { slug: 'sla' },
        title: 'titles.sla',
      },
      {
        path: 'faq',
        loadComponent: () =>
          import('./features/public/pages/faq-page/faq-page').then((m) => m.FaqPage),
        title: 'titles.faq',
      },
      {
        path: 'contact',
        loadComponent: () =>
          import('./features/public/pages/contact-page/contact-page').then((m) => m.ContactPage),
        title: 'titles.contact',
      },
      {
        path: 'auth',
        loadChildren: () => import('./features/auth/auth.routes').then((m) => m.authRoutes),
      },
      {
        path: 'school/onboarding',
        loadChildren: () =>
          import('./features/school-onboarding/school-onboarding.routes').then(
            (m) => m.schoolOnboardingRoutes,
          ),
      },
      {
        path: 'school',
        loadChildren: () =>
          import('./features/school-portal/school-portal.routes').then((m) => m.schoolPortalRoutes),
      },
      ...protectedPortalRoutes,
      {
        path: 'unauthorized',
        loadComponent: () =>
          import('./features/auth/pages/unauthorized-page/unauthorized-page').then(
            (m) => m.UnauthorizedPage,
          ),
        title: 'titles.unauthorized',
      },
      {
        path: 'dev/bilingual-fields',
        loadComponent: () =>
          import('./features/dev/pages/bilingual-field-showcase-page/bilingual-field-showcase-page').then(
            (m) => m.BilingualFieldShowcasePage,
          ),
        title: 'titles.bilingualShowcase',
      },
      {
        path: 'dev/simulated-meeting/:sessionId/:role',
        canMatch: [() => isDevMode(), authGuard],
        loadComponent: () =>
          import('./features/dev/pages/simulated-meeting-page/simulated-meeting-page').then(
            (m) => m.SimulatedMeetingPage,
          ),
        title: 'common.simulatedMeeting.title',
      },
    ],
  },
  {
    path: '',
    component: AppShell,
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
        title: 'titles.dashboard',
      },
      {
        path: 'students',
        loadChildren: () =>
          import('./features/students/students.routes').then((m) => m.studentsRoutes),
        title: 'titles.students',
      },
      {
        path: 'staff',
        loadChildren: () => import('./features/staff/staff.routes').then((m) => m.staffRoutes),
        title: 'titles.staff',
      },
      {
        path: 'classes',
        loadChildren: () =>
          import('./features/classes/classes.routes').then((m) => m.classesRoutes),
        title: 'titles.classes',
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/not-found-page').then((m) => m.NotFoundPage),
    title: 'titles.notFound',
  },
];
