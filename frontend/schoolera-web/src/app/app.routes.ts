import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.dashboardRoutes),
  },
  {
    path: 'schools',
    loadChildren: () => import('./features/schools/schools.routes').then((m) => m.schoolsRoutes),
  },
  {
    path: 'students',
    loadChildren: () => import('./features/students/students.routes').then((m) => m.studentsRoutes),
  },
  {
    path: 'staff',
    loadChildren: () => import('./features/staff/staff.routes').then((m) => m.staffRoutes),
  },
  {
    path: 'classes',
    loadChildren: () => import('./features/classes/classes.routes').then((m) => m.classesRoutes),
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
