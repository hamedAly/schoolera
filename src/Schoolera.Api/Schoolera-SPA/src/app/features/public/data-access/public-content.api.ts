import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  ContactRequestBody,
  ContactRequestResultDtoResult,
  PublicCmsPageDtoResult,
  PublicFaqCategoryDtoIReadOnlyListResult,
  PublicHomepageDtoResult,
} from '../../../core/api-client/SwaggerClient.service';

/**
 * Feature facade over public CMS / FAQ / contact endpoints.
 * Components depend on this service, not on {@link Client} directly.
 */
@Injectable({
  providedIn: 'root',
})
export class PublicContentApi {
  private readonly client = inject(Client);

  getPage(slug: string): Observable<PublicCmsPageDtoResult> {
    return this.client.pagesGET3(slug);
  }

  getFaqs(): Observable<PublicFaqCategoryDtoIReadOnlyListResult> {
    return this.client.faqs();
  }

  getHome(): Observable<PublicHomepageDtoResult> {
    return this.client.homeGET2();
  }

  submitContact(body: ContactRequestBody): Observable<ContactRequestResultDtoResult> {
    return this.client.contact(body);
  }
}
