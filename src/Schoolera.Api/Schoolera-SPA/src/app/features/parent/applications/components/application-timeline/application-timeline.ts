import { Component, inject, input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AdmissionHistoryDto } from '../../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { admissionStatusLabelKey } from '../../../data-access/admission-status';

@Component({
  selector: 'se-application-timeline',
  imports: [TranslocoPipe],
  templateUrl: './application-timeline.html',
  styleUrl: './application-timeline.scss',
})
export class ApplicationTimeline {
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly transloco = inject(TranslocoService);

  readonly items = input<ReadonlyArray<AdmissionHistoryDto>>([]);

  protected formatDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '';
  }

  protected actionLabel(action: string | undefined): string {
    if (!action) {
      return this.transloco.translate('parent.applications.timeline.actions.generic');
    }

    const key = `parent.applications.timeline.actions.${action}`;
    const translated = this.transloco.translate(key);
    return translated === key
      ? this.transloco.translate('parent.applications.timeline.actions.generic')
      : translated;
  }

  protected statusLabel(status: number | undefined): string {
    return this.transloco.translate(admissionStatusLabelKey(status));
  }
}
