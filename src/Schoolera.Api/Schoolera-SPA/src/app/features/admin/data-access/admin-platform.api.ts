import { HttpClient, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  AdminAdmissionApplicationDetailDtoResult,
  AdminAdmissionApplicationListItemDtoPagedResultResult,
  AdminAuditEventDtoPagedResultResult,
  AdminDashboardDtoResult,
  AdminOnboardingDetailDtoResult,
  AdminOnboardingListItemDtoPagedResultResult,
  AdminSchoolDetailDtoResult,
  AdminSchoolListItemDtoPagedResultResult,
  AdminUserListItemDtoPagedResultResult,
  AdminUserListItemDtoResult,
  CityAdminDtoResult,
  Client,
  CreateCityCommand,
  CreateCountryCommand,
  CreateCurriculumCommand,
  CreateGovernorateCommand,
  CountryAdminDtoResult,
  GovernorateAdminDtoResult,
  OnboardingApprovalInput,
  OnboardingReviewReasonInput,
  TaxonomyAdminDtoResult,
  UpdateAdminSchoolStatusRequest,
  UpdateAdminUserStatusRequest,
} from '../../../core/api-client/SwaggerClient.service';

/**
 * Feature facade over NSwag platform-admin endpoints.
 * Accept-Language and CSRF are handled by global interceptors — do not set them here.
 */
@Injectable({
  providedIn: 'root',
})
export class AdminPlatformApi {
  private readonly client = inject(Client);
  private readonly http = inject(HttpClient);

  getDashboard(): Observable<AdminDashboardDtoResult> {
    return this.client.dashboard();
  }

  listOnboardingApplications(
    status?: string,
    pageNumber?: number,
    pageSize?: number,
  ): Observable<AdminOnboardingListItemDtoPagedResultResult> {
    return this.client.schoolOnboarding(status, pageNumber, pageSize);
  }

  getOnboardingApplication(applicationId: string): Observable<AdminOnboardingDetailDtoResult> {
    return this.client.schoolOnboarding2(applicationId);
  }

  startOnboardingReview(applicationId: string): Observable<AdminOnboardingDetailDtoResult> {
    return this.client.startReview2(applicationId);
  }

  requestOnboardingChanges(
    applicationId: string,
    body: OnboardingReviewReasonInput,
  ): Observable<AdminOnboardingDetailDtoResult> {
    return this.client.requestChanges(applicationId, body);
  }

  approveOnboardingApplication(
    applicationId: string,
    body?: OnboardingApprovalInput,
  ): Observable<AdminOnboardingDetailDtoResult> {
    return this.client.approve(applicationId, body);
  }

  rejectOnboardingApplication(
    applicationId: string,
    body: OnboardingReviewReasonInput,
  ): Observable<AdminOnboardingDetailDtoResult> {
    return this.client.reject(applicationId, body);
  }

  listSchools(
    search?: string,
    status?: string,
    pageNumber?: number,
    pageSize?: number,
  ): Observable<AdminSchoolListItemDtoPagedResultResult> {
    return this.client.schools(search, status, pageNumber, pageSize);
  }

  getSchool(schoolId: string): Observable<AdminSchoolDetailDtoResult> {
    return this.client.schools2(schoolId);
  }

  updateSchoolStatus(
    schoolId: string,
    body: UpdateAdminSchoolStatusRequest,
  ): Observable<AdminSchoolDetailDtoResult> {
    return this.client.status(schoolId, body);
  }

  listUsers(
    search?: string,
    role?: string,
    accountStatus?: string,
    pageNumber?: number,
    pageSize?: number,
  ): Observable<AdminUserListItemDtoPagedResultResult> {
    return this.client.users(search, role, accountStatus, pageNumber, pageSize);
  }

  updateUserStatus(
    userId: string,
    body: UpdateAdminUserStatusRequest,
  ): Observable<AdminUserListItemDtoResult> {
    return this.client.status2(userId, body);
  }

  listAuditEvents(
    action?: string,
    entityType?: string,
    pageNumber?: number,
    pageSize?: number,
  ): Observable<AdminAuditEventDtoPagedResultResult> {
    return this.client.audit(action, entityType, pageNumber, pageSize);
  }

  createCountry(body: CreateCountryCommand): Observable<CountryAdminDtoResult> {
    return this.client.countriesPOST(body);
  }

  createGovernorate(body: CreateGovernorateCommand): Observable<GovernorateAdminDtoResult> {
    return this.client.governoratesPOST(body);
  }

  createCity(body: CreateCityCommand): Observable<CityAdminDtoResult> {
    return this.client.citiesPOST(body);
  }

  deactivateCity(id: string): Observable<CityAdminDtoResult> {
    return this.client.deactivate3(id);
  }

  createCurriculum(body: CreateCurriculumCommand): Observable<TaxonomyAdminDtoResult> {
    return this.client.curriculaPOST(body);
  }

  deactivateCurriculum(id: string): Observable<TaxonomyAdminDtoResult> {
    return this.client.deactivate3(id);
  }

  listAdmissionApplications(params?: {
    search?: string;
    schoolId?: string;
    cityId?: string;
    status?: number;
    branchId?: string;
    gradeId?: string;
    academicYearId?: string;
    dateFrom?: string;
    dateTo?: string;
    sort?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Observable<AdminAdmissionApplicationListItemDtoPagedResultResult> {
    return this.client.admissionApplicationsGET(
      params?.search,
      params?.schoolId,
      params?.cityId,
      params?.status,
      params?.branchId,
      params?.gradeId,
      params?.academicYearId,
      params?.dateFrom,
      params?.dateTo,
      params?.sort,
      params?.pageNumber,
      params?.pageSize,
    );
  }

  getAdmissionApplication(applicationId: string): Observable<AdminAdmissionApplicationDetailDtoResult> {
    return this.client.admissionApplicationsGET2(applicationId);
  }

  /**
   * CSV export with current filters. NSwag `export` discards the response body.
   */
  exportAdmissionApplications(params?: {
    search?: string;
    schoolId?: string;
    cityId?: string;
    status?: number;
    branchId?: string;
    gradeId?: string;
    academicYearId?: string;
    dateFrom?: string;
    dateTo?: string;
    sort?: string;
  }): Observable<void> {
    const query = new URLSearchParams();
    if (params?.search) {
      query.set('search', params.search);
    }
    if (params?.schoolId) {
      query.set('schoolId', params.schoolId);
    }
    if (params?.cityId) {
      query.set('cityId', params.cityId);
    }
    if (params?.status != null) {
      query.set('status', String(params.status));
    }
    if (params?.branchId) {
      query.set('branchId', params.branchId);
    }
    if (params?.gradeId) {
      query.set('gradeId', params.gradeId);
    }
    if (params?.academicYearId) {
      query.set('academicYearId', params.academicYearId);
    }
    if (params?.dateFrom) {
      query.set('dateFrom', params.dateFrom);
    }
    if (params?.dateTo) {
      query.set('dateTo', params.dateTo);
    }
    if (params?.sort) {
      query.set('sort', params.sort);
    }

    const qs = query.toString();
    const url = `/api/admin/admission-applications/export${qs ? `?${qs}` : ''}`;

    return this.http.get(url, { observe: 'response', responseType: 'blob' }).pipe(
      map((response: HttpResponse<Blob>) => {
        const blob = response.body;
        if (!blob) {
          return;
        }

        const disposition = response.headers.get('content-disposition') ?? '';
        const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
        const fileName = match?.[1] ? decodeURIComponent(match[1].replace(/"/g, '')) : 'admission-applications.csv';

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
