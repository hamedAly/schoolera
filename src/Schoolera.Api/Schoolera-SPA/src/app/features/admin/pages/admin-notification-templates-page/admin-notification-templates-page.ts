import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import {
  AdminIntegrationsApi,
  NotificationChannel,
  NotificationEventType,
  NotificationTemplateListItemDto,
  NotificationTemplateVersionDto,
} from '../../data-access/admin-integrations.api';

@Component({
  selector: 'se-admin-notification-templates-page',
  imports: [
    ConfirmationDialog,
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './admin-notification-templates-page.html',
  styleUrl: './admin-notification-templates-page.scss',
})
export class AdminNotificationTemplatesPage implements OnInit {
  private readonly api = inject(AdminIntegrationsApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly templates = signal<NotificationTemplateListItemDto[]>([]);
  protected readonly lastCreatedVersion = signal<NotificationTemplateVersionDto | null>(null);
  protected readonly publishVersionId = signal<string | null>(null);
  protected readonly confirming = signal(false);

  protected readonly eventOptions = [
    { value: NotificationEventType._1, labelKey: 'admin.templates.events.accountVerification' },
    {
      value: NotificationEventType._2,
      labelKey: 'admin.templates.events.registrationConfirmation',
    },
    {
      value: NotificationEventType._10,
      labelKey: 'admin.templates.events.admissionSubmitted',
    },
    {
      value: NotificationEventType._11,
      labelKey: 'admin.templates.events.admissionStatusChanged',
    },
    { value: NotificationEventType._50, labelKey: 'admin.templates.events.admissionsOpened' },
  ] as const;

  protected readonly channelOptions = [
    { value: NotificationChannel._1, labelKey: 'admin.templates.channels.inApp' },
    { value: NotificationChannel._2, labelKey: 'admin.templates.channels.email' },
    { value: NotificationChannel._3, labelKey: 'admin.templates.channels.sms' },
    { value: NotificationChannel._4, labelKey: 'admin.templates.channels.whatsApp' },
  ] as const;

  protected readonly form = this.fb.group({
    eventType: [NotificationEventType._50 as NotificationEventType, Validators.required],
    channel: [NotificationChannel._1 as NotificationChannel, Validators.required],
    culture: ['ar', Validators.required],
    code: [''],
    subject: [''],
    body: ['', Validators.required],
    allowedVariablesCsv: [''],
    providerTemplateId: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listTemplates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.templates.set(result.data);
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

  protected createVersion(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);

    this.api
      .createTemplateVersion({
        eventType: value.eventType!,
        channel: value.channel!,
        culture: value.culture!.trim(),
        code: value.code?.trim() || undefined,
        subject: value.subject?.trim() || undefined,
        body: value.body!.trim(),
        allowedVariablesCsv: value.allowedVariablesCsv?.trim() ?? '',
        providerTemplateId: value.providerTemplateId?.trim() || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          if (result.succeeded && result.data) {
            this.lastCreatedVersion.set(result.data ?? null);
            this.toast.success(this.transloco.translate('admin.templates.versionCreated'));
            this.load();
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

  protected requestPublish(versionId: string | undefined): void {
    if (!versionId) {
      return;
    }
    this.publishVersionId.set(versionId);
  }

  protected cancelPublish(): void {
    this.publishVersionId.set(null);
  }

  protected confirmPublish(): void {
    const versionId = this.publishVersionId();
    if (!versionId) {
      return;
    }

    this.confirming.set(true);
    this.api
      .publishTemplateVersion(versionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.confirming.set(false);
          this.publishVersionId.set(null);
          if (result.succeeded) {
            this.toast.success(this.transloco.translate('admin.templates.published'));
            this.lastCreatedVersion.set(result.data ?? null);
            this.load();
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.confirming.set(false);
          this.publishVersionId.set(null);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected eventKey(type: NotificationEventType | undefined): string {
    const match = this.eventOptions.find((o) => o.value === type);
    return match?.labelKey ?? 'admin.common.unknown';
  }

  protected channelKey(channel: NotificationChannel | undefined): string {
    const match = this.channelOptions.find((o) => o.value === channel);
    return match?.labelKey ?? 'admin.common.unknown';
  }
}
