import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  SupportTicketAssignRequest,
  SupportTicketCategoryChangeRequest,
  SupportTicketListItemDtoPagedResultResult,
  SupportTicketPriorityChangeRequest,
  SupportTicketReplyRequest,
  SupportTicketStatusChangeRequest,
  SupportTicketSupportDetailDtoResult,
} from '../../../core/api-client/SwaggerClient.service';

import type { SupportTicketListFilters } from './support-ticket.models';

export type {
  SupportTicketAssignRequest,
  SupportTicketCategoryChangeRequest,
  SupportTicketPriorityChangeRequest,
  SupportTicketReplyRequest,
  SupportTicketStatusChangeRequest,
};

/**
 * SupportAgent / PlatformAdmin ticket queue facade over NSwag Client.
 */
@Injectable({
  providedIn: 'root',
})
export class SupportTicketsApi {
  private readonly client = inject(Client);

  list(filters?: SupportTicketListFilters): Observable<SupportTicketListItemDtoPagedResultResult> {
    return this.client.tickets(
      filters?.search,
      filters?.status,
      filters?.priority,
      filters?.category,
      filters?.assignedSupportAgentUserId,
      filters?.unassignedOnly,
      filters?.firstResponseOverdueOnly,
      filters?.resolutionOverdueOnly,
      filters?.pageNumber ?? 1,
      filters?.pageSize ?? 20,
    );
  }

  get(ticketId: string): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.tickets2(ticketId);
  }

  assignToMe(ticketId: string): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.assignToMe(ticketId);
  }

  reassign(
    ticketId: string,
    body: SupportTicketAssignRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.reassign(ticketId, body);
  }

  unassign(ticketId: string): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.unassign(ticketId);
  }

  reply(
    ticketId: string,
    body: SupportTicketReplyRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.replies2(ticketId, body);
  }

  addInternalNote(
    ticketId: string,
    body: SupportTicketReplyRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.internalNotes(ticketId, body);
  }

  changeStatus(
    ticketId: string,
    body: SupportTicketStatusChangeRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.status3(ticketId, body);
  }

  changePriority(
    ticketId: string,
    body: SupportTicketPriorityChangeRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.priority(ticketId, body);
  }

  changeCategory(
    ticketId: string,
    body: SupportTicketCategoryChangeRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.category(ticketId, body);
  }
}
