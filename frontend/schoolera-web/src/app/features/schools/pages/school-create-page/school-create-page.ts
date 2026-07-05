import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { PageHeader } from '../../../../core/layout/page-header/page-header';
import { SchoolForm } from '../../components/school-form/school-form';
import { SchoolsApi } from '../../data-access/schools.api';
import { CreateSchoolRequest } from '../../data-access/schools.models';

@Component({
  selector: 'se-school-create-page',
  imports: [PageHeader, SchoolForm],
  templateUrl: './school-create-page.html',
  styleUrl: './school-create-page.scss',
})
export class SchoolCreatePage {
  private readonly router = inject(Router);
  private readonly schoolsApi = inject(SchoolsApi);

  protected readonly errors = signal<string[]>([]);
  protected readonly saving = signal(false);

  protected saveSchool(request: CreateSchoolRequest): void {
    this.saving.set(true);
    this.errors.set([]);

    this.schoolsApi.createSchool(request).subscribe({
      next: (result) => {
        this.saving.set(false);

        if (result.succeeded) {
          void this.router.navigate(['/schools']);
          return;
        }

        this.errors.set(result.errors);
      },
      error: () => {
        this.saving.set(false);
        this.errors.set(['Unable to create school.']);
      },
    });
  }
}
