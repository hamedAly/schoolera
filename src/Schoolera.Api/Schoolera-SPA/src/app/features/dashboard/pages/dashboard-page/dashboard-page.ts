import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { PageHeader } from '../../../../core/layout/page-header/page-header';

@Component({
  selector: 'se-dashboard-page',
  imports: [PageHeader, RouterLink, TranslocoPipe],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  protected readonly modules = [
    {
      labelKey: 'ops.schools',
      valueKey: 'ops.moduleCountSchools',
      stateKey: 'ops.moduleActive',
      route: '/schools',
    },
    {
      labelKey: 'ops.students',
      valueKey: 'ops.moduleReady',
      stateKey: 'ops.modulePlaceholder',
      route: '/students',
    },
    {
      labelKey: 'ops.staff',
      valueKey: 'ops.moduleReady',
      stateKey: 'ops.modulePlaceholder',
      route: '/staff',
    },
    {
      labelKey: 'ops.classes',
      valueKey: 'ops.moduleReady',
      stateKey: 'ops.modulePlaceholder',
      route: '/classes',
    },
  ] as const;
}
