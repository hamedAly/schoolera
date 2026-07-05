import { Routes } from '@angular/router';

import { SchoolCreatePage } from './pages/school-create-page/school-create-page';
import { SchoolDetailsPage } from './pages/school-details-page/school-details-page';
import { SchoolListPage } from './pages/school-list-page/school-list-page';

export const schoolsRoutes: Routes = [
  {
    path: '',
    component: SchoolListPage,
  },
  {
    path: 'new',
    component: SchoolCreatePage,
  },
  {
    path: ':id',
    component: SchoolDetailsPage,
  },
];
