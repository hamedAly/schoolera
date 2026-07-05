import { Component } from '@angular/core';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'se-student-list-page',
  imports: [EmptyState, PageHeader],
  templateUrl: './student-list-page.html',
  styleUrl: './student-list-page.scss',
})
export class StudentListPage {}
