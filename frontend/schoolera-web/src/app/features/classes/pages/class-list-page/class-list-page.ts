import { Component } from '@angular/core';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'se-class-list-page',
  imports: [EmptyState, PageHeader],
  templateUrl: './class-list-page.html',
  styleUrl: './class-list-page.scss',
})
export class ClassListPage {}
