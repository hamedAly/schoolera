import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { AdmissionApplicationStatus } from '../../../../core/api-client/SwaggerClient.service';
import {
  admissionStatusDescriptionKey,
  admissionStatusLabelKey,
  admissionStatusModifier,
} from '../../../parent/data-access/admission-status';

@Component({
  selector: 'se-portal-admission-status-badge',
  imports: [TranslocoPipe],
  template: `
    <span
      class="portal-admission-badge portal-admission-badge--{{ modifier() }}"
      [attr.title]="descriptionKey() | transloco"
    >
      <span class="visually-hidden">{{ descriptionKey() | transloco }}</span>
      <span aria-hidden="true">{{ labelKey() | transloco }}</span>
    </span>
  `,
  styleUrl: './portal-admission-status-badge.scss',
})
export class PortalAdmissionStatusBadge {
  readonly status = input<AdmissionApplicationStatus | number | null | undefined>();

  protected readonly modifier = computed(() => admissionStatusModifier(this.status()));
  protected readonly labelKey = computed(() => admissionStatusLabelKey(this.status()));
  protected readonly descriptionKey = computed(() => admissionStatusDescriptionKey(this.status()));
}
