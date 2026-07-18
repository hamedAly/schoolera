import { Routes } from '@angular/router';

import { SchoolDetailsPage } from './pages/school-details-page/school-details-page';
import { SchoolListPage } from './pages/school-list-page/school-list-page';

export const schoolsRoutes: Routes = [
  {
    path: '',
    component: SchoolListPage,
    title: 'titles.schools',
  },
  {
    path: ':slug',
    component: SchoolDetailsPage,
    title: 'titles.schoolDetails',
  },
];
