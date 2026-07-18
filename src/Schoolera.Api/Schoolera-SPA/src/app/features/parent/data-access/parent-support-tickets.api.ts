import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  CreateSupportTicketRequest,
  SupportTicketHistoryDtoIReadOnlyListResult,
  SupportTicketListItemDtoPagedResultResult,
  SupportTicketParentDetailDtoResult,
  SupportTicketReplyRequest,
} from '../../../core/api-client/SwaggerClient.service';

export type {
  CreateSupportTicketRequest,
  SupportTicketReplyRequest,
};

/**
 * Parent support-ticket facade over NSwag Client.
 */
@Injectable({
  providedIn: 'root',
})
export class ParentSupportTicketsApi {
  private readonly client = inject(Client);

  list(filters?: {
    status?: number;
    priority?: number;
    pageNumber?: number;
    pageSize?: number;
  }): Observable<SupportTicketListItemDtoPagedResultResult> {
    return this.client.supportTicketsGET3(
      filters?.status,
      filters?.priority,
      filters?.pageNumber ?? 1,
      filters?.pageSize ?? 20,
    );
  }

  create(body: CreateSupportTicketRequest): Observable<SupportTicketParentDetailDtoResult> {
    return this.client.supportTicketsPOST(body);
  }

  get(ticketId: string): Observable<SupportTicketParentDetailDtoResult> {
    return this.client.supportTicketsGET4(ticketId);
  }

  history(ticketId: string): Observable<SupportTicketHistoryDtoIReadOnlyListResult> {
    return this.client.history(ticketId);
  }

  reply(
    ticketId: string,
    body: SupportTicketReplyRequest,
  ): Observable<SupportTicketParentDetailDtoResult> {
    return this.client.replies(ticketId, body);
  }

  reopen(ticketId: string): Observable<SupportTicketParentDetailDtoResult> {
    return this.client.reopen(ticketId);
  }
}
