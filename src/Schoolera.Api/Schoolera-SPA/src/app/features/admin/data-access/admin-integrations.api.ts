import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  CreatePlatformIntegrationRequest,
  CreateTemplateVersionRequest,
  IntegrationHealthStatus,
  IntegrationType,
  NotificationChannel,
  NotificationEventType,
  NotificationOpsSummaryDto,
  NotificationOpsSummaryDtoResult,
  NotificationTemplateListItemDto,
  NotificationTemplateListItemDtoIReadOnlyListResult,
  NotificationTemplateVersionDto,
  NotificationTemplateVersionDtoResult,
  PlatformIntegrationDetailDto,
  PlatformIntegrationDetailDtoResult,
  PlatformIntegrationSummaryDto,
  PlatformIntegrationSummaryDtoIReadOnlyListResult,
  StringIReadOnlyListResult,
  TestConnectionResultDto,
  TestConnectionResultDtoResult,
  UpdatePlatformIntegrationRequest,
  ValidateIntegrationRequest,
  FailedMeetingSessionListItemDtoIReadOnlyListResult,
  RetryMeetingSessionRequest,
  BooleanResult,
  FailedMeetingSessionListItemDto,
  CourierAdminDetailDto,
  CourierAdminDetailDtoResult,
  CourierAvailabilityOptionDto,
  CourierAvailabilityOptionDtoIReadOnlyListResult,
  CourierCoverageDto,
  CourierCoverageResult,
  CourierHealthDto,
  CourierHealthDtoIReadOnlyListResult,
  CourierProviderProfileDto,
  CourierServiceDto,
  CourierSlaAdminDto,
  CourierWindowDto,
  DayOfWeek,
} from '../../../core/api-client/SwaggerClient.service';

export {
  IntegrationType,
  IntegrationHealthStatus,
  NotificationChannel,
  NotificationEventType,
  CourierCoverageResult,
  DayOfWeek,
};
export type {
  CreatePlatformIntegrationRequest,
  CreateTemplateVersionRequest,
  NotificationOpsSummaryDto,
  NotificationTemplateListItemDto,
  NotificationTemplateVersionDto,
  PlatformIntegrationDetailDto,
  PlatformIntegrationSummaryDto,
  TestConnectionResultDto,
  UpdatePlatformIntegrationRequest,
  ValidateIntegrationRequest,
  FailedMeetingSessionListItemDto,
  CourierAdminDetailDto,
  CourierAdminDetailDtoResult,
  CourierAvailabilityOptionDto,
  CourierCoverageDto,
  CourierHealthDto,
  CourierProviderProfileDto,
  CourierServiceDto,
  CourierSlaAdminDto,
  CourierWindowDto,
};

export type CourierConcurrency = {
  expectedRowVersion?: string;
  expectedConfigurationVersion?: number;
};

/**
 * Feature facade over NSwag Platform Admin integration and notification-template endpoints.
 * Accept-Language and CSRF are handled by global interceptors — do not set them here.
 * Never log SettingsJson or secret values.
 */
@Injectable({
  providedIn: 'root',
})
export class AdminIntegrationsApi {
  private readonly client = inject(Client);

  listIntegrations(filters?: {
    type?: IntegrationType;
    providerCode?: string;
    isActive?: boolean;
    healthStatus?: IntegrationHealthStatus;
  }): Observable<PlatformIntegrationSummaryDtoIReadOnlyListResult> {
    return this.client.integrationsGET(
      filters?.type,
      filters?.providerCode,
      filters?.isActive,
      filters?.healthStatus,
    );
  }

  getIntegration(id: string): Observable<PlatformIntegrationDetailDtoResult> {
    return this.client.integrationsGET2(id);
  }

  createIntegration(
    body: CreatePlatformIntegrationRequest,
  ): Observable<PlatformIntegrationDetailDtoResult> {
    return this.client.integrationsPOST(body);
  }

  updateIntegration(
    id: string,
    body: UpdatePlatformIntegrationRequest,
  ): Observable<PlatformIntegrationDetailDtoResult> {
    return this.client.integrationsPUT(id, body);
  }

  activate(id: string): Observable<PlatformIntegrationDetailDtoResult> {
    return this.client.activate(id);
  }

  deactivate(id: string): Observable<PlatformIntegrationDetailDtoResult> {
    return this.client.deactivate(id);
  }

  setDefault(id: string): Observable<PlatformIntegrationDetailDtoResult> {
    return this.client.setDefault(id);
  }

  validate(
    body: ValidateIntegrationRequest,
    id?: string,
  ): Observable<StringIReadOnlyListResult> {
    return id ? this.client.validate2(id, body) : this.client.validate(body);
  }

  testConnection(id: string): Observable<TestConnectionResultDtoResult> {
    return this.client.testConnection(id);
  }

  listTemplates(): Observable<NotificationTemplateListItemDtoIReadOnlyListResult> {
    return this.client.notificationTemplates();
  }

  createTemplateVersion(
    body: CreateTemplateVersionRequest,
  ): Observable<NotificationTemplateVersionDtoResult> {
    return this.client.versions(body);
  }

  publishTemplateVersion(versionId: string): Observable<NotificationTemplateVersionDtoResult> {
    return this.client.publish5(versionId);
  }

  getOpsSummary(): Observable<NotificationOpsSummaryDtoResult> {
    return this.client.summary();
  }

  listFailedMeetingSessions(take = 50): Observable<FailedMeetingSessionListItemDtoIReadOnlyListResult> {
    return this.client.failed(take);
  }

  retryMeetingSession(id: string, body: RetryMeetingSessionRequest): Observable<BooleanResult> {
    return this.client.retry(id, body);
  }

  getCourier(integrationId: string): Observable<CourierAdminDetailDtoResult> {
    return this.client.courier(integrationId);
  }

  updateCourierProfile(
    integrationId: string,
    profile: CourierProviderProfileDto,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    return this.client.profilePUT(
      integrationId,
      profile.descriptionAr,
      profile.descriptionEn,
      profile.logoReference,
      profile.termsUrl,
      profile.privacyUrl,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    );
  }

  saveCourierService(
    integrationId: string,
    service: CourierServiceDto,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    const args = [
      service.code,
      service.nameAr,
      service.nameEn,
      service.descriptionAr,
      service.descriptionEn,
      service.sortOrder,
      service.minimumPickupLeadTimeMinutes,
      service.dailyCutoffLocalTime,
      service.maximumFuturePickupDays,
      service.acceptanceWindowMinutes,
      service.supportsScheduledPickup,
      service.supportsSameDayPickup,
      service.canCreatePickup,
      service.canQueryStatus,
      service.supportsWebhook,
      service.supportsPolling,
      service.supportsManualUpdates,
      service.supportsCancellationBeforePickup,
      service.supportsCourierAssignment,
      service.supportsProofOfPickup,
      service.supportsProofOfDelivery,
      false,
      service.maximumEnvelopeWeightGrams,
      service.maximumEnvelopeLengthCm,
      service.maximumEnvelopeWidthCm,
      service.maximumEnvelopeHeightCm,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    ] as const;
    return service.id
      ? this.client.servicesPUT(integrationId, service.id, ...args)
      : this.client.servicesPOST(integrationId, ...args);
  }

  setCourierServiceActive(
    integrationId: string,
    service: CourierServiceDto,
    isActive: boolean,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    return this.client.active(
      integrationId,
      service.id!,
      isActive,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    );
  }

  saveCourierCoverage(
    integrationId: string,
    coverage: CourierCoverageDto,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    const args = [
      coverage.serviceId,
      coverage.countryId,
      coverage.governorateId,
      coverage.cityId,
      coverage.districtId,
      coverage.result,
      coverage.notesAr,
      coverage.notesEn,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    ] as const;
    return coverage.id
      ? this.client.coveragePUT(integrationId, coverage.id, ...args)
      : this.client.coveragePOST(integrationId, ...args);
  }

  setCourierCoverageActive(
    integrationId: string,
    coverage: CourierCoverageDto,
    isActive: boolean,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    return this.client.active2(
      integrationId,
      coverage.id!,
      isActive,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    );
  }

  saveCourierWindow(
    integrationId: string,
    window: CourierWindowDto,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    const args = [
      window.serviceId,
      window.coverageRuleId,
      window.scopeKey,
      window.timeZoneId,
      window.dayOfWeek,
      window.localStartTime,
      window.localEndTime,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    ] as const;
    return window.id
      ? this.client.windowsPUT(integrationId, window.id, ...args)
      : this.client.windowsPOST(integrationId, ...args);
  }

  setCourierWindowActive(
    integrationId: string,
    window: CourierWindowDto,
    isActive: boolean,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    return this.client.active3(
      integrationId,
      window.id!,
      isActive,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    );
  }

  saveCourierSla(
    integrationId: string,
    sla: CourierSlaAdminDto,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    const args = [
      sla.serviceId,
      sla.coverageRuleId,
      sla.scopeKey,
      sla.acceptanceTargetMinutes,
      sla.pickupSchedulingTargetMinutes,
      sla.pickupCompletionTargetMinutes,
      sla.deliveryToSchoolTargetMinutes,
      sla.receiptConfirmationTargetMinutes,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    ] as const;
    return sla.id
      ? this.client.slasPUT(integrationId, sla.id, ...args)
      : this.client.slasPOST(integrationId, ...args);
  }

  setCourierSlaActive(
    integrationId: string,
    sla: CourierSlaAdminDto,
    isActive: boolean,
    concurrency: CourierConcurrency,
  ): Observable<CourierAdminDetailDtoResult> {
    return this.client.active4(
      integrationId,
      sla.id!,
      isActive,
      concurrency.expectedRowVersion,
      concurrency.expectedConfigurationVersion,
    );
  }

  previewCourierAvailability(
    integrationId: string,
    location: { countryId?: string; governorateId?: string; cityId?: string; districtId?: string },
  ): Observable<CourierAvailabilityOptionDtoIReadOnlyListResult> {
    return this.client.availabilityPreview(
      integrationId,
      location.countryId,
      location.governorateId,
      location.cityId,
      location.districtId,
    );
  }

  getCourierHealthHistory(
    integrationId: string,
    take = 20,
  ): Observable<CourierHealthDtoIReadOnlyListResult> {
    return this.client.healthHistory(integrationId, take);
  }
}
