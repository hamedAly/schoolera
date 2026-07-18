import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';

export const adminRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['PlatformAdmin'] },
    loadComponent: () =>
      import('./layout/admin-layout/admin-layout').then((m) => m.AdminLayout),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/admin-dashboard-page/admin-dashboard-page').then(
            (m) => m.AdminDashboardPage,
          ),
        title: 'titles.adminDashboard',
      },
      {
        path: 'integrations',
        loadComponent: () =>
          import('./pages/admin-integrations-list-page/admin-integrations-list-page').then(
            (m) => m.AdminIntegrationsListPage,
          ),
        title: 'titles.adminIntegrations',
      },
      {
        path: 'integrations/new',
        loadComponent: () =>
          import('./pages/admin-integration-edit-page/admin-integration-edit-page').then(
            (m) => m.AdminIntegrationEditPage,
          ),
        title: 'titles.adminIntegrationNew',
      },
      {
        path: 'integrations/:id',
        loadComponent: () =>
          import('./pages/admin-integration-edit-page/admin-integration-edit-page').then(
            (m) => m.AdminIntegrationEditPage,
          ),
        title: 'titles.adminIntegrationEdit',
      },
      {
        path: 'notification-templates',
        loadComponent: () =>
          import('./pages/admin-notification-templates-page/admin-notification-templates-page').then(
            (m) => m.AdminNotificationTemplatesPage,
          ),
        title: 'titles.adminNotificationTemplates',
      },
      {
        path: 'notifications-ops',
        loadComponent: () =>
          import('./pages/admin-notifications-ops-page/admin-notifications-ops-page').then(
            (m) => m.AdminNotificationsOpsPage,
          ),
        title: 'titles.adminNotificationsOps',
      },
      {
        path: 'onboarding',
        loadComponent: () =>
          import('./pages/admin-onboarding-list-page/admin-onboarding-list-page').then(
            (m) => m.AdminOnboardingListPage,
          ),
        title: 'titles.adminOnboarding',
      },
      {
        path: 'onboarding/:applicationId',
        loadComponent: () =>
          import('./pages/admin-onboarding-detail-page/admin-onboarding-detail-page').then(
            (m) => m.AdminOnboardingDetailPage,
          ),
        title: 'titles.adminOnboardingDetail',
      },
      {
        path: 'applications',
        loadComponent: () =>
          import('./pages/admin-applications-list-page/admin-applications-list-page').then(
            (m) => m.AdminApplicationsListPage,
          ),
        title: 'titles.adminApplications',
      },
      {
        path: 'applications/:applicationId',
        loadComponent: () =>
          import('./pages/admin-application-detail-page/admin-application-detail-page').then(
            (m) => m.AdminApplicationDetailPage,
          ),
        title: 'titles.adminApplicationDetail',
      },
      {
        path: 'schools',
        loadComponent: () =>
          import('./pages/admin-schools-list-page/admin-schools-list-page').then(
            (m) => m.AdminSchoolsListPage,
          ),
        title: 'titles.adminSchools',
      },
      {
        path: 'schools/:schoolId',
        loadComponent: () =>
          import('./pages/admin-school-detail-page/admin-school-detail-page').then(
            (m) => m.AdminSchoolDetailPage,
          ),
        title: 'titles.adminSchoolDetail',
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./pages/admin-users-page/admin-users-page').then((m) => m.AdminUsersPage),
        title: 'titles.adminUsers',
      },
      {
        path: 'taxonomies',
        loadComponent: () =>
          import('./pages/admin-taxonomies-page/admin-taxonomies-page').then(
            (m) => m.AdminTaxonomiesPage,
          ),
        title: 'titles.adminTaxonomies',
      },
      {
        path: 'cms/pages',
        loadComponent: () =>
          import('./pages/admin-cms-pages-list-page/admin-cms-pages-list-page').then(
            (m) => m.AdminCmsPagesListPage,
          ),
        title: 'titles.adminCmsPages',
      },
      {
        path: 'cms/pages/new',
        loadComponent: () =>
          import('./pages/admin-cms-page-edit-page/admin-cms-page-edit-page').then(
            (m) => m.AdminCmsPageEditPage,
          ),
        title: 'titles.adminCmsPageNew',
      },
      {
        path: 'cms/pages/:pageId',
        loadComponent: () =>
          import('./pages/admin-cms-page-edit-page/admin-cms-page-edit-page').then(
            (m) => m.AdminCmsPageEditPage,
          ),
        title: 'titles.adminCmsPageEdit',
      },
      {
        path: 'cms/faq',
        loadComponent: () =>
          import('./pages/admin-cms-faq-page/admin-cms-faq-page').then((m) => m.AdminCmsFaqPage),
        title: 'titles.adminCmsFaq',
      },
      {
        path: 'cms/home',
        loadComponent: () =>
          import('./pages/admin-cms-home-page/admin-cms-home-page').then((m) => m.AdminCmsHomePage),
        title: 'titles.adminCmsHome',
      },
      {
        path: 'contact-requests',
        loadComponent: () =>
          import('./pages/admin-contact-requests-list-page/admin-contact-requests-list-page').then(
            (m) => m.AdminContactRequestsListPage,
          ),
        title: 'titles.adminContactRequests',
      },
      {
        path: 'contact-requests/:requestId',
        loadComponent: () =>
          import('./pages/admin-contact-request-detail-page/admin-contact-request-detail-page').then(
            (m) => m.AdminContactRequestDetailPage,
          ),
        title: 'titles.adminContactRequestDetail',
      },
      {
        path: 'support-tickets',
        loadComponent: () =>
          import('./pages/admin-support-tickets-list-page/admin-support-tickets-list-page').then(
            (m) => m.AdminSupportTicketsListPage,
          ),
        title: 'titles.adminSupportTickets',
      },
      {
        path: 'support-tickets/:ticketId',
        loadComponent: () =>
          import('./pages/admin-support-ticket-detail-page/admin-support-ticket-detail-page').then(
            (m) => m.AdminSupportTicketDetailPage,
          ),
        title: 'titles.adminSupportTicketDetail',
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./pages/admin-audit-page/admin-audit-page').then((m) => m.AdminAuditPage),
        title: 'titles.adminAudit',
      },
    ],
  },
];
