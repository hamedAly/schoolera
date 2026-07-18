import { HttpClient, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  Client,
  SupportTicketAssignRequest,
  SupportTicketListItemDtoPagedResultResult,
  SupportTicketSupportDetailDtoResult,
} from '../../../core/api-client/SwaggerClient.service';

import type { SupportTicketListFilters } from '../../support/data-access/support-ticket.models';

/**
 * Platform Admin support-ticket oversight facade.
 * List/detail/assign/convert use NSwag; CSV export uses HttpClient blob download
 * (NSwag export returns void without file handling).
 */
@Injectable({
  providedIn: 'root',
})
export class AdminSupportTicketsApi {
  private readonly client = inject(Client);
  private readonly http = inject(HttpClient);

  list(filters?: SupportTicketListFilters): Observable<SupportTicketListItemDtoPagedResultResult> {
    return this.client.supportTicketsGET(
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
    return this.client.supportTicketsGET2(ticketId);
  }

  assign(
    ticketId: string,
    body: SupportTicketAssignRequest,
  ): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.assign(ticketId, body);
  }

  convertContact(contactRequestId: string): Observable<SupportTicketSupportDetailDtoResult> {
    return this.client.convertContact(contactRequestId);
  }

  exportCsv(
    filters?: Omit<SupportTicketListFilters, 'pageNumber' | 'pageSize'>,
  ): Observable<void> {
    const query = new URLSearchParams();
    if (filters?.search) {
      query.set('search', filters.search);
    }
    if (filters?.status != null) {
      query.set('status', String(filters.status));
    }
    if (filters?.priority != null) {
      query.set('priority', String(filters.priority));
    }
    if (filters?.category != null) {
      query.set('category', String(filters.category));
    }
    if (filters?.assignedSupportAgentUserId) {
      query.set('assignedSupportAgentUserId', filters.assignedSupportAgentUserId);
    }
    if (filters?.unassignedOnly != null) {
      query.set('unassignedOnly', String(filters.unassignedOnly));
    }
    if (filters?.firstResponseOverdueOnly != null) {
      query.set('firstResponseOverdueOnly', String(filters.firstResponseOverdueOnly));
    }
    if (filters?.resolutionOverdueOnly != null) {
      query.set('resolutionOverdueOnly', String(filters.resolutionOverdueOnly));
    }
    const qs = query.toString();
    const url = `/api/admin/support-tickets/export${qs ? `?${qs}` : ''}`;
    return this.http.get(url, { observe: 'response', responseType: 'blob' }).pipe(
      map((response: HttpResponse<Blob>) => {
        const blob = response.body;
        if (!blob) {
          return;
        }
        const disposition = response.headers.get('content-disposition') ?? '';
        const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
        const fileName = match?.[1]
          ? decodeURIComponent(match[1].replace(/"/g, ''))
          : 'support-tickets.csv';
        const objectUrl = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = objectUrl;
        anchor.download = fileName;
        anchor.rel = 'noopener';
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(objectUrl);
      }),
    );
  }
}
