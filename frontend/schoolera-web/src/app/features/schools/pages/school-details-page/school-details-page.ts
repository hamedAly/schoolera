import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { PageHeader } from '../../../../core/layout/page-header/page-header';

@Component({
  selector: 'se-school-details-page',
  imports: [PageHeader, RouterLink],
  templateUrl: './school-details-page.html',
  styleUrl: './school-details-page.scss',
})
export class SchoolDetailsPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly schoolId = this.route.snapshot.paramMap.get('id') ?? '';
}
