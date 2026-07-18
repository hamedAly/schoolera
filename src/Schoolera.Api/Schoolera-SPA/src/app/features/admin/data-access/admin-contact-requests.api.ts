import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  ContactAdminNoteBody,
  ContactRequestDetailDtoResult,
  ContactRequestListItemDtoPagedResultResult,
} from '../../../core/api-client/SwaggerClient.service';

/**
 * Feature facade over `/api/admin/contact-requests/**`.
 */
@Injectable({
  providedIn: 'root',
})
export class AdminContactRequestsApi {
  private readonly client = inject(Client);

  list(
    search?: string,
    status?: string,
    category?: string,
    pageNumber?: number,
    pageSize?: number,
  ): Observable<ContactRequestListItemDtoPagedResultResult> {
    return this.client.contactRequests(search, status, category, pageNumber, pageSize);
  }

  get(id: string): Observable<ContactRequestDetailDtoResult> {
    return this.client.contactRequests2(id);
  }

  startReview(id: string, body?: ContactAdminNoteBody): Observable<ContactRequestDetailDtoResult> {
    return this.client.startReview(id, body);
  }

  resolve(id: string, body?: ContactAdminNoteBody): Observable<ContactRequestDetailDtoResult> {
    return this.client.resolve(id, body);
  }

  close(id: string, body?: ContactAdminNoteBody): Observable<ContactRequestDetailDtoResult> {
    return this.client.close(id, body);
  }
}
