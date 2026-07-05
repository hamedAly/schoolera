import { Component } from '@angular/core';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'se-staff-list-page',
  imports: [EmptyState, PageHeader],
  templateUrl: './staff-list-page.html',
  styleUrl: './staff-list-page.scss',
})
export class StaffListPage {}
