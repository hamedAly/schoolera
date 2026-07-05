import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';
import { SchoolTable } from '../../components/school-table/school-table';
import { SchoolsStore } from '../../data-access/schools.store';

@Component({
  selector: 'se-school-list-page',
  imports: [EmptyState, PageHeader, RouterLink, SchoolTable],
  templateUrl: './school-list-page.html',
  styleUrl: './school-list-page.scss',
})
export class SchoolListPage {
  private readonly schoolsStore = inject(SchoolsStore);

  protected readonly schools = this.schoolsStore.schools;
  protected readonly loading = this.schoolsStore.loading;
  protected readonly errors = this.schoolsStore.errors;

  constructor() {
    this.schoolsStore.load();
  }
}
