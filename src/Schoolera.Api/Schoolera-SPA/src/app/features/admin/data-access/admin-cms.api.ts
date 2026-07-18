import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  CmsPageAdminDtoResult,
  CmsPageListItemDtoPagedResultResult,
  CreateCmsPageCommand,
  CreateFaqCategoryCommand,
  CreateFaqItemCommand,
  FaqCategoryAdminDtoIReadOnlyListResult,
  FaqCategoryAdminDtoResult,
  FaqItemAdminDtoIReadOnlyListResult,
  FaqItemAdminDtoResult,
  HomepageAdminDtoResult,
  InterviewFaqCategory,
  ReorderFaqCategoriesCommand,
  ReorderFaqItemsCommand,
  UpdateCmsPageBody,
  UpdateFaqCategoryBody,
  UpdateFaqItemBody,
  UpdateHomepageContentBody,
} from '../../../core/api-client/SwaggerClient.service';

/**
 * Feature facade over `/api/admin/cms/**`.
 * NSwag method names are opaque (publish4, homeGET, …) — keep that mapping here.
 */
@Injectable({
  providedIn: 'root',
})
export class AdminCmsApi {
  private readonly client = inject(Client);

  // —— Pages ——
  listPages(
    search?: string,
    status?: string,
    pageNumber?: number,
    pageSize?: number,
  ): Observable<CmsPageListItemDtoPagedResultResult> {
    return this.client.pagesGET(search, status, pageNumber, pageSize);
  }

  getPage(id: string): Observable<CmsPageAdminDtoResult> {
    return this.client.pagesGET2(id);
  }

  createPage(body: CreateCmsPageCommand): Observable<CmsPageAdminDtoResult> {
    return this.client.pagesPOST(body);
  }

  updatePage(id: string, body: UpdateCmsPageBody): Observable<CmsPageAdminDtoResult> {
    return this.client.pagesPUT(id, body);
  }

  publishPage(id: string): Observable<CmsPageAdminDtoResult> {
    return this.client.publish4(id);
  }

  unpublishPage(id: string): Observable<CmsPageAdminDtoResult> {
    return this.client.unpublish4(id);
  }

  archivePage(id: string): Observable<CmsPageAdminDtoResult> {
    return this.client.archive(id);
  }

  // —— FAQ categories ——
  listFaqCategories(): Observable<FaqCategoryAdminDtoIReadOnlyListResult> {
    return this.client.categoriesGET();
  }

  createFaqCategory(body: CreateFaqCategoryCommand): Observable<FaqCategoryAdminDtoResult> {
    return this.client.categoriesPOST(body);
  }

  updateFaqCategory(id: string, body: UpdateFaqCategoryBody): Observable<FaqCategoryAdminDtoResult> {
    return this.client.categoriesPUT(id, body);
  }

  publishFaqCategory(id: string): Observable<FaqCategoryAdminDtoResult> {
    return this.client.publish(id);
  }

  unpublishFaqCategory(id: string): Observable<FaqCategoryAdminDtoResult> {
    return this.client.unpublish(id);
  }

  reorderFaqCategories(body: ReorderFaqCategoriesCommand): Observable<FaqCategoryAdminDtoIReadOnlyListResult> {
    return this.client.reorder(body);
  }

  // —— FAQ items ——
  listInterviewFaqItems(
    category?: InterviewFaqCategory,
    isPublished?: boolean,
    isActive?: boolean,
    search?: string,
  ): Observable<FaqItemAdminDtoIReadOnlyListResult> {
    return this.client.interviewItems(category, isPublished, isActive, search);
  }

  createFaqItem(body: CreateFaqItemCommand): Observable<FaqItemAdminDtoResult> {
    return this.client.itemsPOST(body);
  }

  updateFaqItem(id: string, body: UpdateFaqItemBody): Observable<FaqItemAdminDtoResult> {
    return this.client.itemsPUT(id, body);
  }

  publishFaqItem(id: string): Observable<FaqItemAdminDtoResult> {
    return this.client.publish2(id);
  }

  unpublishFaqItem(id: string): Observable<FaqItemAdminDtoResult> {
    return this.client.unpublish2(id);
  }

  activateFaqItem(id: string): Observable<FaqItemAdminDtoResult> {
    return this.client.activate(id);
  }

  deactivateFaqItem(id: string): Observable<FaqItemAdminDtoResult> {
    return this.client.deactivate(id);
  }

  reorderFaqItems(body: ReorderFaqItemsCommand): Observable<FaqItemAdminDtoIReadOnlyListResult> {
    return this.client.reorder2(body);
  }

  // —— Homepage ——
  getHomepage(): Observable<HomepageAdminDtoResult> {
    return this.client.homeGET();
  }

  updateHomepage(body: UpdateHomepageContentBody): Observable<HomepageAdminDtoResult> {
    return this.client.homePUT(body);
  }

  publishHomepage(id: string): Observable<HomepageAdminDtoResult> {
    return this.client.publish3(id);
  }

  unpublishHomepage(id: string): Observable<HomepageAdminDtoResult> {
    return this.client.unpublish3(id);
  }
}
