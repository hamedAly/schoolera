import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { EmptyValuePipe } from '../../../../shared/pipes/empty-value.pipe';
import { SchoolListItem } from '../../data-access/schools.models';

@Component({
  selector: 'se-school-table',
  imports: [EmptyValuePipe, RouterLink],
  templateUrl: './school-table.html',
  styleUrl: './school-table.scss',
})
export class SchoolTable {
  readonly schools = input<ReadonlyArray<SchoolListItem>>([]);
}
