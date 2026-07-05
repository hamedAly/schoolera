import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PageHeader } from '../../../../core/layout/page-header/page-header';

@Component({
  selector: 'se-dashboard-page',
  imports: [PageHeader, RouterLink],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  protected readonly modules = [
    { label: 'Schools', value: '0', state: 'Ready', route: '/schools' },
    { label: 'Students', value: '0', state: 'Ready', route: '/students' },
    { label: 'Staff', value: '0', state: 'Ready', route: '/staff' },
    { label: 'Classes', value: '0', state: 'Ready', route: '/classes' },
  ] as const;
}
