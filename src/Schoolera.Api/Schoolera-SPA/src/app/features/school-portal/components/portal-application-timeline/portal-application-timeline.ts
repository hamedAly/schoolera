import { Component, inject, input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { SchoolAdmissionHistoryDto } from '../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { admissionStatusLabelKey } from '../../../parent/data-access/admission-status';

@Component({
  selector: 'se-portal-application-timeline',
  imports: [TranslocoPipe],
  templateUrl: './portal-application-timeline.html',
  styleUrl: './portal-application-timeline.scss',
})
export class PortalApplicationTimeline {
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly transloco = inject(TranslocoService);

  readonly items = input<ReadonlyArray<SchoolAdmissionHistoryDto>>([]);

  protected formatDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '';
  }

  protected actionLabel(action: string | undefined): string {
    if (!action) {
      return this.transloco.translate('portal.applications.timeline.actions.generic');
    }

    const key = `portal.applications.timeline.actions.${action}`;
    const translated = this.transloco.translate(key);
    return translated === key
      ? this.transloco.translate('portal.applications.timeline.actions.generic')
      : translated;
  }

  protected statusLabel(status: number | undefined): string {
    return this.transloco.translate(admissionStatusLabelKey(status));
  }
}
