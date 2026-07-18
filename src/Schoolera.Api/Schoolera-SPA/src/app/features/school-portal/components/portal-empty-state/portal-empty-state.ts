import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { EmptyState } from '../../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'se-portal-empty-state',
  imports: [EmptyState, TranslocoPipe],
  templateUrl: './portal-empty-state.html',
  styleUrl: './portal-empty-state.scss',
})
export class PortalEmptyState {
  readonly titleKey = input.required<string>();
  readonly messageKey = input<string>();
}
