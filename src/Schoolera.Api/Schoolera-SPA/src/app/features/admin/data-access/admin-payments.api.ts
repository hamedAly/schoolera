import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AdminPaymentMonitoringDtoIReadOnlyListResult,
  Client,
  PaymentIntentDtoResult,
  ReconciliationRecordDtoIReadOnlyListResult,
  ReconciliationRecordDtoResult,
  RequestPaymentRefundRequest,
  ResolveReconciliationRequest,
  SchoolPayableItemAdminDtoIReadOnlyListResult,
  SchoolPayableItemAdminDtoResult,
  SchoolSettlementPaymentDtoIReadOnlyListResult,
  UpsertSchoolPayableItemRequest,
} from '../../../core/api-client/SwaggerClient.service';

/**
 * Admin / school-portal payment facades over NSwag Client.
 */
@Injectable({ providedIn: 'root' })
export class AdminPaymentsApi {
  private readonly client = inject(Client);

  monitoring(status?: number, take = 100): Observable<AdminPaymentMonitoringDtoIReadOnlyListResult> {
    return this.client.monitoring(status, take);
  }

  reconciliation(status?: number): Observable<ReconciliationRecordDtoIReadOnlyListResult> {
    return this.client.reconciliation(status);
  }

  investigate(id: string, note?: string): Observable<ReconciliationRecordDtoResult> {
    return this.client.investigate(id, { note });
  }

  resolve(id: string, body: ResolveReconciliationRequest): Observable<ReconciliationRecordDtoResult> {
    return this.client.resolve2(id, body);
  }

  refund(intentId: string, body: RequestPaymentRefundRequest): Observable<PaymentIntentDtoResult> {
    return this.client.refund(intentId, body);
  }

  requery(intentId: string): Observable<PaymentIntentDtoResult> {
    return this.client.requery(intentId);
  }
}

@Injectable({ providedIn: 'root' })
export class SchoolPortalPaymentsApi {
  private readonly client = inject(Client);

  listPayableItems(schoolId: string): Observable<SchoolPayableItemAdminDtoIReadOnlyListResult> {
    return this.client.payableItemsGET2(schoolId);
  }

  upsertPayableItem(
    schoolId: string,
    body: UpsertSchoolPayableItemRequest,
  ): Observable<SchoolPayableItemAdminDtoResult> {
    return this.client.payableItemsPUT(schoolId, body);
  }

  settlements(schoolId: string): Observable<SchoolSettlementPaymentDtoIReadOnlyListResult> {
    return this.client.settlements(schoolId);
  }
}
