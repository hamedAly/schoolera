import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/http/api-client';
import { ApiResult } from '../../../core/http/api-result';
import { CreateSchoolRequest, SchoolListItem } from './schools.models';

@Injectable({
  providedIn: 'root',
})
export class SchoolsApi {
  private readonly apiClient = inject(ApiClient);

  getSchools(): Observable<ApiResult<SchoolListItem[]>> {
    return this.apiClient.get<SchoolListItem[]>('/schools');
  }

  createSchool(request: CreateSchoolRequest): Observable<ApiResult<SchoolListItem>> {
    return this.apiClient.post<CreateSchoolRequest, SchoolListItem>('/schools', request);
  }
}
