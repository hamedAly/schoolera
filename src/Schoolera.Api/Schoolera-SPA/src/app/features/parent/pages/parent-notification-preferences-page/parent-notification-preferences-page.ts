import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import {
  ChannelAvailabilityDto,
  ParentNotificationPreferenceDto,
  ParentNotificationsApi,
} from '../../data-access/parent-notifications.api';

@Component({
  selector: 'se-parent-notification-preferences-page',
  imports: [
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './parent-notification-preferences-page.html',
  styleUrl: './parent-notification-preferences-page.scss',
})
export class ParentNotificationPreferencesPage implements OnInit {
  private readonly api = inject(ParentNotificationsApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly preferences = signal<ParentNotificationPreferenceDto | null>(null);
  protected readonly availability = signal<ChannelAvailabilityDto | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    inAppEnabled: [true],
    emailEnabled: [false],
    smsEnabled: [false],
    whatsAppEnabled: [false],
    optionalAdmissionsOpenEnabled: [true],
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .channelAvailability()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.availability.set(result.data);
          this.applyChannelAvailability(result.data);
        }
      });

    this.api
      .getPreferences()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.preferences.set(result.data);
            this.form.patchValue({
              inAppEnabled: result.data.inAppEnabled,
              emailEnabled: result.data.emailEnabled,
              smsEnabled: result.data.smsEnabled,
              whatsAppEnabled: result.data.whatsAppEnabled,
              optionalAdmissionsOpenEnabled: result.data.optionalAdmissionsOpenEnabled,
            });
            this.applyChannelAvailability(this.availability());
            return;
          }
          this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  protected save(): void {
    const current = this.preferences();
    if (!current) {
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);

    this.api
      .updatePreferences({
        inAppEnabled: value.inAppEnabled,
        emailEnabled: value.emailEnabled,
        smsEnabled: value.smsEnabled,
        whatsAppEnabled: value.whatsAppEnabled,
        optionalAdmissionsOpenEnabled: value.optionalAdmissionsOpenEnabled,
        consentSource: 'parent-preferences-ui',
        rowVersion: current.rowVersion,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          if (result.succeeded && result.data) {
            this.preferences.set(result.data);
            this.toast.success(this.transloco.translate('parent.preferences.saved'));
            return;
          }
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.saving.set(false);
          this.toast.error(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  private applyChannelAvailability(avail: ChannelAvailabilityDto | null): void {
    if (!avail) {
      return;
    }
    if (!avail.emailConfigured) {
      this.form.controls.emailEnabled.disable({ emitEvent: false });
    } else {
      this.form.controls.emailEnabled.enable({ emitEvent: false });
    }
    if (!avail.smsConfigured) {
      this.form.controls.smsEnabled.disable({ emitEvent: false });
    } else {
      this.form.controls.smsEnabled.enable({ emitEvent: false });
    }
    if (!avail.whatsAppConfigured) {
      this.form.controls.whatsAppEnabled.disable({ emitEvent: false });
    } else {
      this.form.controls.whatsAppEnabled.enable({ emitEvent: false });
    }
  }
}
