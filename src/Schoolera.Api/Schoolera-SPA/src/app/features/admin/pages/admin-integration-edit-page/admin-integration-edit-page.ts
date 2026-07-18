import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import {
  AdminIntegrationsApi,
  IntegrationType,
  PlatformIntegrationDetailDto,
} from '../../data-access/admin-integrations.api';
import { CourierConfigurationPanel } from '../../components/courier-configuration-panel/courier-configuration-panel';

type ConfirmAction = 'deactivate' | 'setDefault' | null;

@Component({
  selector: 'se-admin-integration-edit-page',
  imports: [
    ConfirmationDialog,
    CourierConfigurationPanel,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-integration-edit-page.html',
  styleUrl: './admin-integration-edit-page.scss',
})
export class AdminIntegrationEditPage implements OnInit {
  private readonly api = inject(AdminIntegrationsApi);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isCreate = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly acting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<PlatformIntegrationDetailDto | null>(null);
  protected readonly confirmAction = signal<ConfirmAction>(null);
  protected readonly confirming = signal(false);
  protected readonly validationMessages = signal<string[]>([]);

  protected readonly typeOptions = [
    { value: IntegrationType._1, labelKey: 'admin.integrations.types.email' },
    { value: IntegrationType._2, labelKey: 'admin.integrations.types.sms' },
    { value: IntegrationType._3, labelKey: 'admin.integrations.types.whatsApp' },
    { value: IntegrationType._4, labelKey: 'admin.integrations.types.courier' },
    { value: IntegrationType._9, labelKey: 'admin.integrations.types.meeting' },
  ] as const;

  protected readonly form = this.fb.group({
    integrationType: [IntegrationType._1 as IntegrationType, Validators.required],
    providerCode: ['', Validators.required],
    displayNameAr: ['', Validators.required],
    displayNameEn: [''],
    settingsJson: ['{}', Validators.required],
    settingsSchemaVersion: [1, Validators.required],
    sortOrder: [0, Validators.required],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || id === 'new') {
      this.isCreate.set(true);
      this.loading.set(false);
      return;
    }
    this.load(id);
  }

  protected load(id: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getIntegration(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.applyDetail(result.data);
            return;
          }
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.validationMessages.set([]);

    if (this.isCreate()) {
      this.api
        .createIntegration({
          integrationType: value.integrationType!,
          providerCode: value.providerCode!.trim(),
          displayNameAr: value.displayNameAr!.trim(),
          displayNameEn: value.displayNameEn?.trim() || undefined,
          settingsJson: value.settingsJson!.trim(),
          settingsSchemaVersion: Number(value.settingsSchemaVersion) || 1,
          sortOrder: Number(value.sortOrder) || 0,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (result) => {
            this.saving.set(false);
            if (result.succeeded && result.data?.id) {
              this.toast.success(this.transloco.translate('admin.integrations.edit.saved'));
              void this.router.navigate(['/admin/integrations', result.data.id]);
              return;
            }
            this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
          },
          error: () => {
            this.saving.set(false);
            this.toast.error(this.transloco.translate('admin.errors.generic'));
          },
        });
      return;
    }

    const current = this.detail();
    if (!current?.id) {
      this.saving.set(false);
      return;
    }

    this.api
      .updateIntegration(current.id, {
        providerCode: value.providerCode!.trim(),
        displayNameAr: value.displayNameAr!.trim(),
        displayNameEn: value.displayNameEn?.trim() || undefined,
        settingsJson: value.settingsJson!.trim(),
        settingsSchemaVersion: Number(value.settingsSchemaVersion) || 1,
        sortOrder: Number(value.sortOrder) || 0,
        rowVersion: current.rowVersion,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          if (result.succeeded && result.data) {
            this.applyDetail(result.data);
            this.toast.success(this.transloco.translate('admin.integrations.edit.saved'));
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.saving.set(false);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected validateSettings(): void {
    const value = this.form.getRawValue();
    const current = this.detail();
    this.acting.set(true);
    this.validationMessages.set([]);

    this.api
      .validate(
        {
          integrationType: this.isCreate() ? (value.integrationType ?? undefined) : undefined,
          providerCode: value.providerCode?.trim() || undefined,
          settingsJson: value.settingsJson?.trim() ?? '{}',
          settingsSchemaVersion: Number(value.settingsSchemaVersion) || 1,
        },
        current?.id,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded) {
            this.validationMessages.set(result.data?.length ? result.data : []);
            this.toast.success(this.transloco.translate('admin.integrations.edit.validateOk'));
            return;
          }
          this.validationMessages.set(result.data ?? result.errors ?? []);
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected activate(): void {
    this.runLifecycle('activate');
  }

  protected isMeeting(): boolean {
    return this.form.controls.integrationType.value === IntegrationType._9;
  }

  protected isCourier(): boolean {
    return this.form.controls.integrationType.value === IntegrationType._4;
  }

  protected applySimulatedCourierTemplate(): void {
    this.form.patchValue({
      integrationType: IntegrationType._4,
      providerCode: 'Simulated',
      displayNameAr: this.transloco.translate('admin.integrations.courier.template.displayNameAr'),
      displayNameEn: this.transloco.translate('admin.integrations.courier.template.displayNameEn'),
      settingsSchemaVersion: 1,
      settingsJson: JSON.stringify({
        environment: 'Development',
        publicNameAr: this.transloco.translate('admin.integrations.courier.template.publicNameAr'),
        publicNameEn: this.transloco.translate('admin.integrations.courier.template.publicNameEn'),
        requestTimeoutSeconds: 30,
        maxRetryAttempts: 3,
        retryDelaySeconds: 10,
        supportsHomePickup: true,
        supportsDropOffPoint: false,
        simulatedScenario: 'Success',
      }, null, 2),
    });
  }

  protected applySimulatedMeetingTemplate(): void {
    this.form.patchValue({
      integrationType: IntegrationType._9,
      providerCode: 'Simulated',
      displayNameAr: 'اجتماع تجريبي (للتطوير فقط)',
      displayNameEn: 'Simulated Meeting (Development only)',
      settingsSchemaVersion: 1,
      settingsJson: JSON.stringify({
        environment: 'Development',
        publicNameAr: 'اجتماع تجريبي',
        publicNameEn: 'Simulated meeting',
        maximumDurationMinutes: 180,
        joinBeforeMinutes: 15,
        joinAfterMinutes: 0,
        hostBeforeMinutes: 30,
        requestTimeoutSeconds: 30,
        maxRetryAttempts: 3,
        retryDelaySeconds: 30,
        supportsMeetingCreation: true,
        supportsParentAccess: true,
        supportsHostAccess: true,
        supportsCancellation: true,
        supportsStatusQuery: true,
        simulatedScenario: 'Success',
      }, null, 2),
    });
  }

  protected testConnection(): void {
    const current = this.detail();
    if (!current?.id) {
      return;
    }
    this.acting.set(true);
    this.api
      .testConnection(current.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded && result.data) {
            this.toast.success(
              this.transloco.translate(
                result.data.succeeded
                  ? 'admin.integrations.edit.testOk'
                  : 'admin.integrations.edit.testFailed',
              ),
            );
            this.load(current.id!);
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected requestConfirm(action: ConfirmAction): void {
    this.confirmAction.set(action);
  }

  protected cancelConfirm(): void {
    this.confirmAction.set(null);
  }

  protected confirmPending(): void {
    const action = this.confirmAction();
    if (!action) {
      return;
    }
    this.confirming.set(true);
    this.runLifecycle(action, () => {
      this.confirming.set(false);
      this.confirmAction.set(null);
    });
  }

  protected confirmTitleKey(): string {
    return this.confirmAction() === 'setDefault'
      ? 'admin.integrations.edit.confirm.setDefaultTitle'
      : 'admin.integrations.edit.confirm.deactivateTitle';
  }

  protected confirmMessageKey(): string {
    return this.confirmAction() === 'setDefault'
      ? 'admin.integrations.edit.confirm.setDefaultMessage'
      : 'admin.integrations.edit.confirm.deactivateMessage';
  }

  private runLifecycle(
    action: 'activate' | 'deactivate' | 'setDefault',
    onDone?: () => void,
  ): void {
    const current = this.detail();
    if (!current?.id) {
      onDone?.();
      return;
    }

    this.acting.set(true);
    const request =
      action === 'activate'
        ? this.api.activate(current.id)
        : action === 'deactivate'
          ? this.api.deactivate(current.id)
          : this.api.setDefault(current.id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.acting.set(false);
        onDone?.();
        if (result.succeeded && result.data) {
          this.applyDetail(result.data);
          this.toast.success(this.transloco.translate(`admin.integrations.edit.${action}Ok`));
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => {
        this.acting.set(false);
        onDone?.();
        this.toast.error(this.transloco.translate('admin.errors.generic'));
      },
    });
  }

  private applyDetail(data: PlatformIntegrationDetailDto): void {
    this.detail.set(data);
    this.form.patchValue({
      integrationType: data.integrationType,
      providerCode: data.providerCode,
      displayNameAr: data.displayNameAr,
      displayNameEn: data.displayNameEn ?? '',
      settingsJson: data.maskedSettingsJson,
      settingsSchemaVersion: data.settingsSchemaVersion,
      sortOrder: data.sortOrder,
    });
    this.form.controls.integrationType.disable();
  }
}
