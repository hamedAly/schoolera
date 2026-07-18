import { Routes } from '@angular/router';

import { StudentListPage } from './pages/student-list-page/student-list-page';

export const studentsRoutes: Routes = [
  {
    path: '',
    component: StudentListPage,
  },
];
