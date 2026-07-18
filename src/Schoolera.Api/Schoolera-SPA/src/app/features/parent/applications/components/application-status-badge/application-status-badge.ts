import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { AdmissionApplicationStatus } from '../../../../../core/api-client/SwaggerClient.service';
import {
  admissionStatusDescriptionKey,
  admissionStatusLabelKey,
  admissionStatusModifier,
} from '../../../data-access/admission-status';

@Component({
  selector: 'se-application-status-badge',
  imports: [TranslocoPipe],
  template: `
    <span
      class="parent-badge parent-badge--{{ modifier() }}"
      [attr.title]="descriptionKey() | transloco"
    >
      <span class="visually-hidden">{{ descriptionKey() | transloco }}</span>
      <span aria-hidden="true">{{ labelKey() | transloco }}</span>
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `,
})
export class ApplicationStatusBadge {
  readonly status = input<AdmissionApplicationStatus | number | null | undefined>();

  protected readonly modifier = computed(() => admissionStatusModifier(this.status()));
  protected readonly labelKey = computed(() => admissionStatusLabelKey(this.status()));
  protected readonly descriptionKey = computed(() => admissionStatusDescriptionKey(this.status()));
}
