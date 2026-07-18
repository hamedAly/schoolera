import { Component, DestroyRef, Input, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs';

import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import {
  LocationSelection,
  LocationSelector,
} from '../../../../../shared/ui/location-selector/location-selector';
import { translateAdmissionErrorCodes } from '../../../data-access/admission-errors';
import {
  CourierAvailabilityOptionDto,
  CourierDestinationBranchDto,
  ParentApi,
} from '../../../data-access/parent.api';

@Component({
  selector: 'se-courier-availability',
  imports: [LocationSelector, TranslocoPipe],
  templateUrl: './courier-availability.html',
  styleUrl: './courier-availability.scss',
})
export class CourierAvailability {
  @Input({ required: true }) applicationId!: string;

  private readonly api = inject(ParentApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly transloco = inject(TranslocoService);
  private readonly locale = inject(LocaleFormatService);

  protected readonly location = signal<LocationSelection>({});
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly branch = signal<CourierDestinationBranchDto | null>(null);
  protected readonly options = signal<readonly CourierAvailabilityOptionDto[]>([]);
  protected readonly reasonCodes = signal<readonly string[]>([]);

  protected preview(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getOwnedApplicationCourierAvailability(this.applicationId, this.location())
      .pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loaded.set(true);
          if (!result.succeeded || !result.data) {
            this.error.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
            return;
          }
          this.branch.set(result.data.destinationBranch ?? null);
          this.options.set(result.data.options ?? []);
          this.reasonCodes.set(result.data.reasonCodes ?? []);
        },
        error: () => this.error.set(this.transloco.translate('parent.errors.generic')),
      });
  }

  protected localized(ar?: string, en?: string): string {
    return this.transloco.getActiveLang() === 'en' ? en || ar || '—' : ar || en || '—';
  }

  protected formatDate(value?: string): string {
    return value ? this.locale.formatDateTime(value) : '—';
  }

  protected reasonKey(code: string): string {
    const keys: Record<string, string> = {
      'courier.noAvailableOptions': 'parent.applications.courier.reasons.noAvailableOptions',
      'courier.applicationCancelled': 'parent.applications.courier.reasons.applicationCancelled',
      'courier.destinationBranchInactive': 'parent.applications.courier.reasons.destinationBranchInactive',
      'courier.invalidLocation': 'parent.applications.courier.reasons.invalidLocation',
    };
    return keys[code] ?? 'parent.applications.courier.reasons.unavailable';
  }
}
