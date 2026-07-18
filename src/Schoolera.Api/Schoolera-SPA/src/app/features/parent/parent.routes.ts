import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';
import { unsavedApplicationGuard } from './applications/guards/unsaved-application.guard';
import { unsavedChildGuard } from './guards/unsaved-child.guard';

export const parentRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Parent'] },
    loadComponent: () =>
      import('./layout/parent-layout/parent-layout').then((m) => m.ParentLayout),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/parent-dashboard-page/parent-dashboard-page').then(
            (m) => m.ParentDashboardPage,
          ),
        title: 'titles.parentDashboard',
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./pages/parent-profile-page/parent-profile-page').then(
            (m) => m.ParentProfilePage,
          ),
        title: 'titles.parentProfile',
      },
      {
        path: 'children',
        loadComponent: () =>
          import('./pages/parent-children-list-page/parent-children-list-page').then(
            (m) => m.ParentChildrenListPage,
          ),
        title: 'titles.parentChildren',
      },
      {
        path: 'children/new',
        canDeactivate: [unsavedChildGuard],
        loadComponent: () =>
          import('./pages/parent-child-form-page/parent-child-form-page').then(
            (m) => m.ParentChildFormPage,
          ),
        title: 'titles.parentChildNew',
      },
      {
        path: 'children/:childId/edit',
        canDeactivate: [unsavedChildGuard],
        loadComponent: () =>
          import('./pages/parent-child-form-page/parent-child-form-page').then(
            (m) => m.ParentChildFormPage,
          ),
        title: 'titles.parentChildEdit',
      },
      {
        path: 'applications',
        loadComponent: () =>
          import('./applications/pages/applications-list-page/applications-list-page').then(
            (m) => m.ApplicationsListPage,
          ),
        title: 'titles.parentApplications',
      },
      {
        path: 'applications/new',
        canDeactivate: [unsavedApplicationGuard],
        loadComponent: () =>
          import('./applications/pages/application-wizard-page/application-wizard-page').then(
            (m) => m.ApplicationWizardPage,
          ),
        title: 'titles.parentApplicationNew',
      },
      {
        path: 'applications/:applicationId',
        loadComponent: () =>
          import('./applications/pages/application-detail-page/application-detail-page').then(
            (m) => m.ApplicationDetailPage,
          ),
        title: 'titles.parentApplicationDetail',
      },
      {
        path: 'applications/:applicationId/edit',
        canDeactivate: [unsavedApplicationGuard],
        loadComponent: () =>
          import('./applications/pages/application-wizard-page/application-wizard-page').then(
            (m) => m.ApplicationWizardPage,
          ),
        title: 'titles.parentApplicationEdit',
      },
      {
        path: 'applications/:applicationId/success',
        loadComponent: () =>
          import('./applications/pages/application-success-page/application-success-page').then(
            (m) => m.ApplicationSuccessPage,
          ),
        title: 'titles.parentApplicationSuccess',
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/parent-notifications-page/parent-notifications-page').then(
            (m) => m.ParentNotificationsPage,
          ),
        title: 'titles.parentNotifications',
      },
      {
        path: 'notification-preferences',
        loadComponent: () =>
          import(
            './pages/parent-notification-preferences-page/parent-notification-preferences-page'
          ).then((m) => m.ParentNotificationPreferencesPage),
        title: 'titles.parentNotificationPreferences',
      },
      {
        path: 'admission-subscriptions',
        loadComponent: () =>
          import(
            './pages/parent-admission-subscriptions-page/parent-admission-subscriptions-page'
          ).then((m) => m.ParentAdmissionSubscriptionsPage),
        title: 'titles.parentAdmissionSubscriptions',
      },
      {
        path: 'favorites',
        loadComponent: () =>
          import('./pages/parent-favorites-page/parent-favorites-page').then(
            (m) => m.ParentFavoritesPage,
          ),
        title: 'titles.parentFavorites',
      },
      {
        path: 'payments',
        loadComponent: () =>
          import('./pages/parent-payments-page/parent-payments-page').then(
            (m) => m.ParentPaymentsPage,
          ),
        title: 'titles.parentPayments',
      },
      {
        path: 'payments/return',
        loadComponent: () =>
          import('./pages/parent-payment-return-page/parent-payment-return-page').then(
            (m) => m.ParentPaymentReturnPage,
          ),
        title: 'titles.parentPaymentReturn',
      },
      {
        path: 'payments/:intentId',
        loadComponent: () =>
          import('./pages/parent-payments-page/parent-payments-page').then(
            (m) => m.ParentPaymentsPage,
          ),
        title: 'titles.parentPayments',
      },
      {
        path: 'support-tickets',
        loadComponent: () =>
          import(
            './pages/parent-support-tickets-list-page/parent-support-tickets-list-page'
          ).then((m) => m.ParentSupportTicketsListPage),
        title: 'titles.parentSupportTickets',
      },
      {
        path: 'support-tickets/new',
        loadComponent: () =>
          import(
            './pages/parent-support-ticket-create-page/parent-support-ticket-create-page'
          ).then((m) => m.ParentSupportTicketCreatePage),
        title: 'titles.parentSupportTicketNew',
      },
      {
        path: 'support-tickets/:ticketId',
        loadComponent: () =>
          import(
            './pages/parent-support-ticket-detail-page/parent-support-ticket-detail-page'
          ).then((m) => m.ParentSupportTicketDetailPage),
        title: 'titles.parentSupportTicketDetail',
      },
    ],
  },
];
