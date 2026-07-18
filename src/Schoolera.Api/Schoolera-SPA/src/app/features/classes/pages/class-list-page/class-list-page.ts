import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'se-class-list-page',
  imports: [EmptyState, PageHeader, TranslocoPipe],
  templateUrl: './class-list-page.html',
  styleUrl: './class-list-page.scss',
})
export class ClassListPage {}
