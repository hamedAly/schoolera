import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';
import { roleGuard } from '../../core/auth/role.guard';

export const supportRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SupportAgent', 'PlatformAdmin'] },
    loadComponent: () =>
      import('./layout/support-layout/support-layout').then((m) => m.SupportLayout),
    children: [
      { path: '', redirectTo: 'tickets', pathMatch: 'full' },
      {
        path: 'tickets',
        loadComponent: () =>
          import('./pages/support-tickets-list-page/support-tickets-list-page').then(
            (m) => m.SupportTicketsListPage,
          ),
        title: 'titles.supportTickets',
      },
      {
        path: 'tickets/:ticketId',
        loadComponent: () =>
          import('./pages/support-ticket-detail-page/support-ticket-detail-page').then(
            (m) => m.SupportTicketDetailPage,
          ),
        title: 'titles.supportTicketDetail',
      },
    ],
  },
];
