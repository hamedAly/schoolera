import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ChannelAvailabilityDto,
  ChannelAvailabilityDtoResult,
  Client,
  CreateParentAdmissionOpenSubscriptionRequest,
  Int32Result,
  NotificationChannel,
  ParentAdmissionOpenSubscriptionDto,
  ParentAdmissionOpenSubscriptionDtoIReadOnlyListResult,
  ParentAdmissionOpenSubscriptionDtoResult,
  ParentInAppNotificationDto,
  ParentInAppNotificationDtoPagedResult,
  ParentInAppNotificationDtoPagedResultResult,
  ParentInAppNotificationDtoResult,
  ParentNotificationPreferenceDto,
  ParentNotificationPreferenceDtoResult,
  UpdateParentNotificationPreferencesRequest,
} from '../../../core/api-client/SwaggerClient.service';

export { NotificationChannel };
export type {
  ChannelAvailabilityDto,
  CreateParentAdmissionOpenSubscriptionRequest,
  ParentAdmissionOpenSubscriptionDto,
  ParentInAppNotificationDto,
  ParentInAppNotificationDtoPagedResult,
  ParentNotificationPreferenceDto,
  UpdateParentNotificationPreferencesRequest,
};

/** @deprecated Use ParentInAppNotificationDtoPagedResult from NSwag. */
export type ParentNotificationPagedResult = ParentInAppNotificationDtoPagedResult;

/**
 * Feature facade over NSwag Parent notification endpoints.
 * Accept-Language and CSRF are handled by global interceptors.
 * Never selects ProviderCode or IntegrationConfigurationId for delivery.
 */
@Injectable({
  providedIn: 'root',
})
export class ParentNotificationsApi {
  private readonly client = inject(Client);

  listNotifications(
    pageNumber = 1,
    pageSize = 20,
  ): Observable<ParentInAppNotificationDtoPagedResultResult> {
    return this.client.notifications(pageNumber, pageSize);
  }

  unreadCount(): Observable<Int32Result> {
    return this.client.unreadCount();
  }

  markRead(notificationId: string): Observable<ParentInAppNotificationDtoResult> {
    return this.client.read(notificationId);
  }

  markAllRead(): Observable<Int32Result> {
    return this.client.readAll();
  }

  getPreferences(): Observable<ParentNotificationPreferenceDtoResult> {
    return this.client.notificationPreferencesGET();
  }

  updatePreferences(
    body: UpdateParentNotificationPreferencesRequest,
  ): Observable<ParentNotificationPreferenceDtoResult> {
    return this.client.notificationPreferencesPUT(body);
  }

  channelAvailability(): Observable<ChannelAvailabilityDtoResult> {
    return this.client.channelAvailability();
  }

  listSubscriptions(): Observable<ParentAdmissionOpenSubscriptionDtoIReadOnlyListResult> {
    return this.client.admissionOpenSubscriptionsGET();
  }

  createSubscription(
    body: CreateParentAdmissionOpenSubscriptionRequest,
  ): Observable<ParentAdmissionOpenSubscriptionDtoResult> {
    return this.client.admissionOpenSubscriptionsPOST(body);
  }

  unsubscribe(subscriptionId: string): Observable<ParentAdmissionOpenSubscriptionDtoResult> {
    return this.client.unsubscribe(subscriptionId);
  }
}
