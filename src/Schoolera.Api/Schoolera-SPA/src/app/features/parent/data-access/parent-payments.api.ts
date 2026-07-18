import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  CreateFinancingRequestBody,
  CreatePaymentIntentRequest,
  FinancingRequestDtoIReadOnlyListResult,
  FinancingRequestDtoResult,
  ParentPayableItemDtoIReadOnlyListResult,
  PaymentIntentDtoIReadOnlyListResult,
  PaymentIntentDtoResult,
  PaymentMethodKind,
  PaymentReceiptDtoResult,
  PaymentReturnRequest,
  PaymentSummaryDtoResult,
  SelectFinancingOfferRequest,
} from '../../../core/api-client/SwaggerClient.service';

export { PaymentMethodKind };
export type {
  CreateFinancingRequestBody,
  CreatePaymentIntentRequest,
  SelectFinancingOfferRequest,
};

/**
 * Parent payments / financing facade over regenerated NSwag Client.
 * Accept-Language and CSRF are handled by global interceptors.
 */
@Injectable({ providedIn: 'root' })
export class ParentPaymentsApi {
  private readonly client = inject(Client);

  listPayableItems(): Observable<ParentPayableItemDtoIReadOnlyListResult> {
    return this.client.payableItemsGET();
  }

  getSummary(
    payableItemId: string,
    admissionApplicationId?: string | null,
  ): Observable<PaymentSummaryDtoResult> {
    return this.client.summary2(payableItemId, admissionApplicationId ?? undefined);
  }

  listIntents(): Observable<PaymentIntentDtoIReadOnlyListResult> {
    return this.client.payments();
  }

  getIntent(intentId: string): Observable<PaymentIntentDtoResult> {
    return this.client.payments2(intentId);
  }

  getReceipt(intentId: string): Observable<PaymentReceiptDtoResult> {
    return this.client.receipt(intentId);
  }

  createIntent(body: CreatePaymentIntentRequest): Observable<PaymentIntentDtoResult> {
    return this.client.intents(body);
  }

  processReturn(reference: string): Observable<PaymentIntentDtoResult> {
    const body: PaymentReturnRequest = { reference };
    return this.client.return(body);
  }

  listFinancing(): Observable<FinancingRequestDtoIReadOnlyListResult> {
    return this.client.financingGET();
  }

  getFinancing(requestId: string): Observable<FinancingRequestDtoResult> {
    return this.client.financingGET2(requestId);
  }

  createFinancing(body: CreateFinancingRequestBody): Observable<FinancingRequestDtoResult> {
    return this.client.financingPOST(body);
  }

  selectOffer(requestId: string, body: SelectFinancingOfferRequest): Observable<FinancingRequestDtoResult> {
    return this.client.selectOffer(requestId, body);
  }
}
