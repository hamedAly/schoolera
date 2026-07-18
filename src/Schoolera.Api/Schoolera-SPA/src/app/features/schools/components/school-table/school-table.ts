import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { PublicSchoolListItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { EmptyValuePipe } from '../../../../shared/pipes/empty-value.pipe';

@Component({
  selector: 'se-school-table',
  imports: [EmptyValuePipe, RouterLink, TranslocoPipe],
  templateUrl: './school-table.html',
  styleUrl: './school-table.scss',
})
export class SchoolTable {
  readonly schools = input<ReadonlyArray<PublicSchoolListItemDto>>([]);
}
