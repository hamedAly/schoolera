import { Routes } from '@angular/router';

import { SchoolPortalEntryRoles } from '../../core/auth/auth.models';
import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { portalPermissionGuard } from './guards/portal-permission.guard';
import { schoolPortalContextGuard } from './guards/school-portal-context.guard';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';

const portalRoles = [...SchoolPortalEntryRoles];

export const schoolPortalRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard],
    data: { roles: portalRoles },
    loadComponent: () =>
      import('./pages/portal-entry-page/portal-entry-page').then((m) => m.PortalEntryPage),
    title: 'titles.schoolPortal',
  },
  {
    path: ':schoolId',
    canActivate: [authGuard, roleGuard, schoolPortalContextGuard],
    data: { roles: portalRoles },
    loadComponent: () =>
      import('./layout/school-portal-layout/school-portal-layout').then((m) => m.SchoolPortalLayout),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canViewDashboard' },
        loadComponent: () =>
          import('./pages/portal-overview-page/portal-overview-page').then((m) => m.PortalOverviewPage),
        title: 'titles.portalOverview',
      },
      {
        path: 'profile',
        canActivate: [portalPermissionGuard],
        data: { portalPermissions: ['canViewProfile', 'canManageProfile', 'canManageContent'] },
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./pages/portal-profile-page/portal-profile-page').then((m) => m.PortalProfilePage),
        title: 'titles.portalProfile',
      },
      {
        path: 'branches',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageBranches' },
        loadComponent: () =>
          import('./pages/portal-branches-page/portal-branches-page').then((m) => m.PortalBranchesPage),
        title: 'titles.portalBranches',
      },
      {
        path: 'stages',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageOfferings' },
        loadComponent: () =>
          import('./pages/portal-stages-page/portal-stages-page').then((m) => m.PortalStagesPage),
        title: 'titles.portalStages',
      },
      {
        path: 'fees',
        canActivate: [portalPermissionGuard],
        data: { portalPermissions: ['canViewFees', 'canManageFees'] },
        loadComponent: () =>
          import('./pages/portal-fees-page/portal-fees-page').then((m) => m.PortalFeesPage),
        title: 'titles.portalFees',
      },
      {
        path: 'facilities',
        canActivate: [portalPermissionGuard],
        data: { portalPermissions: ['canManageFacilities', 'canManageContent'] },
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./pages/portal-facilities-page/portal-facilities-page').then((m) => m.PortalFacilitiesPage),
        title: 'titles.portalFacilities',
      },
      {
        path: 'gallery',
        canActivate: [portalPermissionGuard],
        data: { portalPermissions: ['canManageGallery', 'canManageContent'] },
        loadComponent: () =>
          import('./pages/portal-gallery-page/portal-gallery-page').then((m) => m.PortalGalleryPage),
        title: 'titles.portalGallery',
      },
      {
        path: 'services',
        canActivate: [portalPermissionGuard],
        data: { portalPermissions: ['canManageServices', 'canManageContent'] },
        loadComponent: () =>
          import('./pages/portal-services-page/portal-services-page').then((m) => m.PortalServicesPage),
        title: 'titles.portalServices',
      },
      {
        path: 'team',
        canActivate: [portalPermissionGuard],
        data: { portalPermissions: ['canViewTeam', 'canManageTeam'] },
        loadComponent: () =>
          import('./pages/portal-team-page/portal-team-page').then((m) => m.PortalTeamPage),
        title: 'titles.portalTeam',
      },
      {
        path: 'applications',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canViewApplications' },
        loadComponent: () =>
          import('./pages/portal-applications-list-page/portal-applications-list-page').then(
            (m) => m.PortalApplicationsListPage,
          ),
        title: 'titles.portalApplications',
      },
      {
        path: 'applications/:applicationId',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canViewApplications' },
        loadComponent: () =>
          import('./pages/portal-application-detail-page/portal-application-detail-page').then(
            (m) => m.PortalApplicationDetailPage,
          ),
        title: 'titles.portalApplicationDetail',
      },
      {
        path: 'admission-requirements',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageAdmissionRequirements' },
        loadComponent: () =>
          import('./pages/portal-admission-requirements-page/portal-admission-requirements-page').then(
            (m) => m.PortalAdmissionRequirementsPage,
          ),
        title: 'titles.portalAdmissionRequirements',
      },
      {
        path: 'admission-questions',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageAdmissionQuestions' },
        loadComponent: () =>
          import('./pages/portal-admission-questions-page/portal-admission-questions-page').then(
            (m) => m.PortalAdmissionQuestionsPage,
          ),
        title: 'titles.portalAdmissionQuestions',
      },
      {
        path: 'age-eligibility-rules',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageAdmissionRequirements' },
        loadComponent: () =>
          import('./pages/portal-age-eligibility-rules-page/portal-age-eligibility-rules-page').then(
            (m) => m.PortalAgeEligibilityRulesPage,
          ),
        title: 'titles.portalAgeEligibilityRules',
      },
      {
        path: 'interview-assessment-policies',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageAdmissionRequirements' },
        loadComponent: () =>
          import(
            './pages/portal-interview-assessment-policies-page/portal-interview-assessment-policies-page'
          ).then((m) => m.PortalInterviewAssessmentPoliciesPage),
        title: 'titles.portalInterviewAssessmentPolicies',
      },
      {
        path: 'interview-assessment-slots',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageAdmissionRequirements' },
        loadComponent: () =>
          import(
            './pages/portal-interview-assessment-slots-page/portal-interview-assessment-slots-page'
          ).then((m) => m.PortalInterviewAssessmentSlotsPage),
        title: 'titles.portalInterviewAssessmentSlots',
      },
      {
        path: 'interview-faqs',
        canActivate: [portalPermissionGuard],
        data: { portalPermission: 'canManageContent' },
        loadComponent: () =>
          import('./pages/portal-interview-faqs-page/portal-interview-faqs-page').then(
            (m) => m.PortalInterviewFaqsPage,
          ),
        title: 'titles.portalInterviewFaqs',
      },
    ],
  },
];
