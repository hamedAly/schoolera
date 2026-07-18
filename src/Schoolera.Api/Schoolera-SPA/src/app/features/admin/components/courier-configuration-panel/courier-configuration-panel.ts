import { Component, DestroyRef, Input, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize, Observable } from 'rxjs';

import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import {
  LocationSelection,
  LocationSelector,
} from '../../../../shared/ui/location-selector/location-selector';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import {
  AdminIntegrationsApi,
  CourierAdminDetailDto,
  CourierAvailabilityOptionDto,
  CourierCoverageDto,
  CourierCoverageResult,
  CourierServiceDto,
  CourierSlaAdminDto,
  CourierWindowDto,
  DayOfWeek,
} from '../../data-access/admin-integrations.api';

@Component({
  selector: 'se-courier-configuration-panel',
  imports: [FormsModule, LocationSelector, TranslocoPipe],
  templateUrl: './courier-configuration-panel.html',
  styleUrl: './courier-configuration-panel.scss',
})
export class CourierConfigurationPanel implements OnInit {
  @Input({ required: true }) integrationId!: string;

  private readonly api = inject(AdminIntegrationsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly locale = inject(LocaleFormatService);

  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly detail = signal<CourierAdminDetailDto | null>(null);
  protected readonly previewLocation = signal<LocationSelection>({});
  protected readonly coverageLocation = signal<LocationSelection>({});
  protected readonly preview = signal<readonly CourierAvailabilityOptionDto[]>([]);
  protected readonly previewLoaded = signal(false);

  protected profile = {
    descriptionAr: '',
    descriptionEn: '',
    logoReference: '',
    termsUrl: '',
    privacyUrl: '',
  };
  protected service: CourierServiceDto = this.emptyService();
  protected coverage: CourierCoverageDto = this.emptyCoverage();
  protected window: CourierWindowDto = this.emptyWindow();
  protected sla: CourierSlaAdminDto = this.emptySla();

  protected readonly coverageResults = [
    { value: CourierCoverageResult._1, key: 'admin.integrations.courier.coverage.covered' },
    { value: CourierCoverageResult._2, key: 'admin.integrations.courier.coverage.notCovered' },
  ];
  protected readonly days = Object.values(DayOfWeek)
    .filter((value): value is DayOfWeek => typeof value === 'number')
    .map((value) => ({ value, key: `admin.integrations.courier.days.${value}` }));

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getCourier(this.integrationId)
      .pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (!result.succeeded || !result.data) {
            this.error.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
            return;
          }
          this.applyDetail(result.data);
        },
        error: () => this.error.set(this.transloco.translate('admin.errors.generic')),
      });
  }

  protected saveProfile(): void {
    this.mutate(this.api.updateCourierProfile(
      this.integrationId,
      {
        descriptionAr: this.profile.descriptionAr.trim() || undefined,
        descriptionEn: this.profile.descriptionEn.trim() || undefined,
        logoReference: this.profile.logoReference.trim() || undefined,
        termsUrl: this.profile.termsUrl.trim() || undefined,
        privacyUrl: this.profile.privacyUrl.trim() || undefined,
      },
      this.concurrency(this.detail()?.profile?.rowVersion),
    ));
  }

  protected editService(value?: CourierServiceDto): void {
    this.service = value ? { ...value } : this.emptyService();
  }

  protected saveService(): void {
    this.service.supportsDropOffPoint = false;
    this.mutate(this.api.saveCourierService(
      this.integrationId,
      this.service,
      this.concurrency(this.service.rowVersion),
    ), () => this.editService());
  }

  protected toggleService(value: CourierServiceDto): void {
    this.mutate(this.api.setCourierServiceActive(
      this.integrationId,
      value,
      !value.isActive,
      this.concurrency(value.rowVersion),
    ));
  }

  protected editCoverage(value?: CourierCoverageDto): void {
    this.coverage = value ? { ...value } : this.emptyCoverage();
    this.coverageLocation.set({
      countryId: value?.countryId,
      governorateId: value?.governorateId,
      cityId: value?.cityId,
      districtId: value?.districtId,
    });
  }

  protected saveCoverage(): void {
    Object.assign(this.coverage, this.coverageLocation());
    this.mutate(this.api.saveCourierCoverage(
      this.integrationId,
      this.coverage,
      this.concurrency(this.coverage.rowVersion),
    ), () => this.editCoverage());
  }

  protected toggleCoverage(value: CourierCoverageDto): void {
    this.mutate(this.api.setCourierCoverageActive(
      this.integrationId,
      value,
      !value.isActive,
      this.concurrency(value.rowVersion),
    ));
  }

  protected editWindow(value?: CourierWindowDto): void {
    this.window = value ? { ...value } : this.emptyWindow();
  }

  protected saveWindow(): void {
    if ((this.window.localStartTime ?? '') >= (this.window.localEndTime ?? '')) {
      this.toast.error(this.transloco.translate('admin.integrations.courier.windows.noCrossMidnight'));
      return;
    }
    this.mutate(this.api.saveCourierWindow(
      this.integrationId,
      this.window,
      this.concurrency(this.window.rowVersion),
    ), () => this.editWindow());
  }

  protected toggleWindow(value: CourierWindowDto): void {
    this.mutate(this.api.setCourierWindowActive(
      this.integrationId,
      value,
      !value.isActive,
      this.concurrency(value.rowVersion),
    ));
  }

  protected editSla(value?: CourierSlaAdminDto): void {
    this.sla = value ? { ...value } : this.emptySla();
  }

  protected saveSla(): void {
    this.mutate(this.api.saveCourierSla(
      this.integrationId,
      this.sla,
      this.concurrency(this.sla.rowVersion),
    ), () => this.editSla());
  }

  protected toggleSla(value: CourierSlaAdminDto): void {
    this.mutate(this.api.setCourierSlaActive(
      this.integrationId,
      value,
      !value.isActive,
      this.concurrency(value.rowVersion),
    ));
  }

  protected previewAvailability(): void {
    this.busy.set(true);
    this.api.previewCourierAvailability(this.integrationId, this.previewLocation())
      .pipe(finalize(() => this.busy.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.previewLoaded.set(true);
          if (!result.succeeded) {
            this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
            return;
          }
          this.preview.set(result.data ?? []);
        },
        error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
      });
  }

  protected formatDate(value?: string): string {
    return value ? this.locale.formatDateTime(value) : '—';
  }

  protected localized(ar?: string, en?: string): string {
    return this.transloco.getActiveLang() === 'en' ? en || ar || '—' : ar || en || '—';
  }

  protected serviceName(id?: string): string {
    const item = this.detail()?.services?.find((value) => value.id === id);
    return item ? this.localized(item.nameAr, item.nameEn) : this.transloco.translate('admin.integrations.courier.allServices');
  }

  private mutate(
    request: Observable<import('../../data-access/admin-integrations.api').CourierAdminDetailDtoResult>,
    success?: () => void,
  ): void {
    this.busy.set(true);
    request.pipe(finalize(() => this.busy.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (!result.succeeded || !result.data) {
          if (result.errorCodes?.includes('courier.configurationConflict')) {
            this.toast.error(this.transloco.translate('admin.integrations.courier.concurrency'));
            this.load();
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
          return;
        }
        this.applyDetail(result.data);
        success?.();
        this.toast.success(this.transloco.translate('admin.integrations.courier.saved'));
      },
      error: () => this.toast.error(this.transloco.translate('admin.errors.generic')),
    });
  }

  private concurrency(rowVersion?: string) {
    return {
      expectedRowVersion: rowVersion ?? this.detail()?.concurrencyRowVersion,
      expectedConfigurationVersion: this.detail()?.configurationVersion,
    };
  }

  private applyDetail(value: CourierAdminDetailDto): void {
    this.detail.set(value);
    this.profile = {
      descriptionAr: value.profile?.descriptionAr ?? '',
      descriptionEn: value.profile?.descriptionEn ?? '',
      logoReference: value.profile?.logoReference ?? '',
      termsUrl: value.profile?.termsUrl ?? '',
      privacyUrl: value.profile?.privacyUrl ?? '',
    };
  }

  private emptyService(): CourierServiceDto {
    return {
      code: 'HomePickup',
      sortOrder: 0,
      minimumPickupLeadTimeMinutes: 60,
      maximumFuturePickupDays: 14,
      acceptanceWindowMinutes: 30,
      supportsScheduledPickup: true,
      supportsSameDayPickup: true,
      canCreatePickup: true,
      canQueryStatus: true,
      supportsPolling: true,
      supportsDropOffPoint: false,
    };
  }

  private emptyCoverage(): CourierCoverageDto {
    return { result: CourierCoverageResult._1 };
  }

  private emptyWindow(): CourierWindowDto {
    return { dayOfWeek: DayOfWeek._0, timeZoneId: 'Africa/Cairo' };
  }

  private emptySla(): CourierSlaAdminDto {
    return {
      acceptanceTargetMinutes: 30,
      pickupSchedulingTargetMinutes: 60,
      pickupCompletionTargetMinutes: 240,
      deliveryToSchoolTargetMinutes: 1440,
    };
  }
}
