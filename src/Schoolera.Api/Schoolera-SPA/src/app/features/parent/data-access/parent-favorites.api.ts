import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  BooleanResult,
  Client,
  FavoriteSchoolListItemDtoPagedResultResult,
} from '../../../core/api-client/SwaggerClient.service';

/**
 * Parent school favorites facade over NSwag Client.
 * Accept-Language and CSRF are handled by global interceptors.
 */
@Injectable({
  providedIn: 'root',
})
export class ParentFavoritesApi {
  private readonly client = inject(Client);

  list(pageNumber = 1, pageSize = 20): Observable<FavoriteSchoolListItemDtoPagedResultResult> {
    return this.client.favoritesGET(pageNumber, pageSize);
  }

  add(schoolId: string): Observable<BooleanResult> {
    return this.client.favoritesPOST(schoolId);
  }

  remove(schoolId: string): Observable<BooleanResult> {
    return this.client.favoritesDELETE(schoolId);
  }
}
