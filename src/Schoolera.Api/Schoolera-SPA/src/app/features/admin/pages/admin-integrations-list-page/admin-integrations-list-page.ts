import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import {
  AdminIntegrationsApi,
  IntegrationHealthStatus,
  IntegrationType,
  PlatformIntegrationSummaryDto,
  FailedMeetingSessionListItemDto,
} from '../../data-access/admin-integrations.api';

@Component({
  selector: 'se-admin-integrations-list-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-integrations-list-page.html',
  styleUrl: './admin-integrations-list-page.scss',
})
export class AdminIntegrationsListPage implements OnInit {
  private readonly api = inject(AdminIntegrationsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly items = signal<PlatformIntegrationSummaryDto[]>([]);
  protected readonly failedMeetings = signal<FailedMeetingSessionListItemDto[]>([]);
  protected readonly retryingMeetingId = signal<string | null>(null);

  protected typeFilter = '';
  protected providerFilter = '';
  protected activeFilter = '';

  protected readonly typeOptions = [
    { value: '', labelKey: 'admin.common.all' },
    { value: String(IntegrationType._1), labelKey: 'admin.integrations.types.email' },
    { value: String(IntegrationType._2), labelKey: 'admin.integrations.types.sms' },
    { value: String(IntegrationType._3), labelKey: 'admin.integrations.types.whatsApp' },
    { value: String(IntegrationType._6), labelKey: 'admin.integrations.types.map' },
    { value: '9', labelKey: 'admin.integrations.types.meeting' },
  ] as const;

  protected readonly filteredItems = computed(() => this.items());

  ngOnInit(): void {
    this.load();
    this.loadFailedMeetings();
  }

  protected loadFailedMeetings(): void {
    this.api.listFailedMeetingSessions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        if (result.succeeded) this.failedMeetings.set(result.data ?? []);
      },
    });
  }

  protected retryMeeting(item: FailedMeetingSessionListItemDto): void {
    if (!item.meetingSessionId || !item.rowVersion) return;
    this.retryingMeetingId.set(item.meetingSessionId);
    this.api.retryMeetingSession(item.meetingSessionId, {
      rowVersion: item.rowVersion,
      idempotencyKey: crypto.randomUUID(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.retryingMeetingId.set(null);
        this.loadFailedMeetings();
      },
      error: () => this.retryingMeetingId.set(null),
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listIntegrations({
        type: this.typeFilter ? (Number(this.typeFilter) as IntegrationType) : undefined,
        providerCode: this.providerFilter.trim() || undefined,
        isActive:
          this.activeFilter === ''
            ? undefined
            : this.activeFilter === 'true',
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.items.set(result.data);
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

  protected applyFilters(): void {
    this.load();
  }

  protected typeKey(type: IntegrationType | undefined): string {
    switch (type) {
      case IntegrationType._1:
        return 'admin.integrations.types.email';
      case IntegrationType._2:
        return 'admin.integrations.types.sms';
      case IntegrationType._3:
        return 'admin.integrations.types.whatsApp';
      case IntegrationType._4:
        return 'admin.integrations.types.courier';
      case IntegrationType._6:
        return 'admin.integrations.types.map';
      case 9 as IntegrationType:
        return 'admin.integrations.types.meeting';
      default:
        return 'admin.integrations.types.other';
    }
  }

  protected healthKey(status: IntegrationHealthStatus | undefined): string {
    switch (status) {
      case IntegrationHealthStatus._1:
        return 'admin.integrations.health.healthy';
      case IntegrationHealthStatus._2:
        return 'admin.integrations.health.degraded';
      case IntegrationHealthStatus._3:
        return 'admin.integrations.health.unhealthy';
      default:
        return 'admin.integrations.health.unknown';
    }
  }

  protected displayName(item: PlatformIntegrationSummaryDto): string {
    const lang = this.transloco.getActiveLang();
    if (lang === 'en' && item.displayNameEn?.trim()) {
      return item.displayNameEn;
    }
    return item.displayNameAr ?? item.providerCode ?? '—';
  }
}
