import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { SchoolStatus } from '../../data-access/school-portal.models';
import { schoolStatusBadgeVariant, schoolStatusTranslationKey } from '../../utils/school-status';

@Component({
  selector: 'se-status-badge',
  imports: [TranslocoPipe],
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss',
})
export class StatusBadge {
  readonly status = input.required<SchoolStatus>();

  protected labelKey(status: SchoolStatus): string {
    return schoolStatusTranslationKey(status);
  }

  protected variant(status: SchoolStatus): string {
    return schoolStatusBadgeVariant(status);
  }
}
