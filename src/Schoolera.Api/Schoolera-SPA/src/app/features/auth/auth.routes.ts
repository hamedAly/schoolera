import { Routes } from '@angular/router';

import { guestGuard } from '../../core/auth/guest.guard';
import { AccountTypePage } from './pages/account-type-page/account-type-page';
import { ForgotPasswordPage } from './pages/forgot-password-page/forgot-password-page';
import { LoginPage } from './pages/login-page/login-page';
import { RegisterParentPage } from './pages/register-parent-page/register-parent-page';
import { RegisterSchoolOwnerPage } from './pages/register-school-owner-page/register-school-owner-page';
import { ResetPasswordPage } from './pages/reset-password-page/reset-password-page';
import { VerifyPage } from './pages/verify-page/verify-page';

export const authRoutes: Routes = [
  {
    path: 'account-type',
    component: AccountTypePage,
    canActivate: [guestGuard],
    title: 'titles.accountType',
  },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'titles.login',
  },
  {
    path: 'register',
    redirectTo: 'account-type',
    pathMatch: 'full',
  },
  {
    path: 'register/parent',
    component: RegisterParentPage,
    canActivate: [guestGuard],
    title: 'titles.registerParent',
  },
  {
    path: 'register/school-owner',
    component: RegisterSchoolOwnerPage,
    canActivate: [guestGuard],
    title: 'titles.registerSchoolOwner',
  },
  {
    path: 'verify',
    component: VerifyPage,
    canActivate: [guestGuard],
    title: 'titles.verify',
  },
  {
    path: 'forgot-password',
    component: ForgotPasswordPage,
    canActivate: [guestGuard],
    title: 'titles.forgotPassword',
  },
  {
    path: 'reset-password',
    component: ResetPasswordPage,
    canActivate: [guestGuard],
    title: 'titles.resetPassword',
  },
];

export const protectedPortalRoutes: Routes = [
  {
    path: 'parent',
    loadChildren: () => import('../parent/parent.routes').then((m) => m.parentRoutes),
  },
  {
    path: 'admin',
    loadChildren: () => import('../admin/admin.routes').then((m) => m.adminRoutes),
  },
  {
    path: 'support',
    loadChildren: () => import('../support/support.routes').then((m) => m.supportRoutes),
  },
];
