import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'se-staff-list-page',
  imports: [EmptyState, PageHeader, TranslocoPipe],
  templateUrl: './staff-list-page.html',
  styleUrl: './staff-list-page.scss',
})
export class StaffListPage {}
