import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import type {
  PublicAdmissionRequirementSummaryDto,
  PublicInterviewFaqItemDto,
  InterviewFaqCategory,
} from '../../../core/api-client/SwaggerClient.service';

import {
  Client,
  GenderType,
  PublicMapConfigurationDtoResult,
  PublicSchoolListItemDto,
  PublicSchoolListItemDtoIReadOnlyListResult,
  PublicSchoolListItemDtoPagedResultResult,
  PublicSchoolMapPinsResultDtoResult,
  PublicSchoolProfileDtoResult,
  SchoolContactLeadRequest,
  SchoolContactLeadResultDtoResult,
  SchoolType,
} from '../../../core/api-client/SwaggerClient.service';
import { ApiResult } from '../../../core/http/api-result';
import type { SchoolsSearchQuery } from './schools-search-query';

export interface SchoolsListResult {
  succeeded: boolean;
  data: ReadonlyArray<PublicSchoolListItemDto> | null | undefined;
  errors?: string[] | null;
  errorCodes?: string[] | null;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** UI-facing search options mapped to NSwag `schools4` query parameters. */
export interface SchoolsSearchOptions {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  countryId?: string;
  governorateId?: string;
  cityId?: string;
  districtId?: string;
  stageId?: string;
  gradeId?: string;
  schoolType?: SchoolType;
  genderType?: GenderType;
  curriculumIds?: readonly string[];
  facilityIds?: readonly string[];
  minimumTuition?: number;
  maximumTuition?: number;
  academicYearId?: string;
  admissionOpen?: boolean;
  latitude?: number;
  longitude?: number;
  sort?: string;
}

export interface PublicAdmissionRequirementsParams {
  branchId?: string;
  educationalStageId?: string;
  gradeId?: string;
  academicYearId?: string;
}

export type PublicInterviewAssessmentPolicyParams = PublicAdmissionRequirementsParams;
export interface PublicAgeEligibilityParams extends PublicAdmissionRequirementsParams {
  childProfileId?: string;
}

export interface PublicInterviewFaqsParams extends PublicAdmissionRequirementsParams {
  category?: InterviewFaqCategory;
}

export type PublicAdmissionRequirementSummary = PublicAdmissionRequirementSummaryDto;
export type PublicInterviewFaqItem = PublicInterviewFaqItemDto;

/**
 * Feature facade over the NSwag-generated Schools API.
 * Components and stores depend on this service, not on {@link Client} directly.
 */
@Injectable({
  providedIn: 'root',
})
export class SchoolsApi {
  private readonly client = inject(Client);

  getSchools(options: SchoolsSearchOptions = {}): Observable<SchoolsListResult> {
    const pageNumber = options.pageNumber ?? 1;
    const pageSize = options.pageSize ?? 20;
    const curriculumIds = options.curriculumIds?.length ? [...options.curriculumIds] : undefined;
    const facilityIds = options.facilityIds?.length ? [...options.facilityIds] : undefined;

    return this.client
      .schools4(
        pageNumber,
        pageSize,
        options.search,
        options.countryId,
        options.governorateId,
        options.cityId,
        options.districtId,
        curriculumIds?.[0],
        curriculumIds,
        options.stageId,
        options.gradeId,
        options.schoolType,
        options.genderType,
        options.admissionOpen,
        facilityIds,
        options.minimumTuition,
        options.maximumTuition,
        options.academicYearId,
        options.latitude,
        options.longitude,
        options.sort,
      )
      .pipe(
        map((result: PublicSchoolListItemDtoPagedResultResult) => ({
          succeeded: !!result.succeeded,
          data: result.data?.items ?? [],
          errors: result.errors ?? [],
          errorCodes: result.errorCodes ?? [],
          totalCount: result.data?.totalCount ?? 0,
          pageNumber: result.data?.pageNumber ?? pageNumber,
          pageSize: result.data?.pageSize ?? pageSize,
          totalPages: result.data?.totalPages ?? 0,
          hasPreviousPage: !!result.data?.hasPreviousPage,
          hasNextPage: !!result.data?.hasNextPage,
        })),
      );
  }

  getFeaturedSchools(limit = 4): Observable<SchoolsListResult> {
    return this.getSchools({ pageNumber: 1, pageSize: limit });
  }

  getBySlug(slug: string): Observable<PublicSchoolProfileDtoResult> {
    return this.client.schools5(slug);
  }

  getRelated(slug: string, limit = 4): Observable<ApiResult<PublicSchoolListItemDto[]>> {
    return this.client.related(slug, limit).pipe(
      map((result: PublicSchoolListItemDtoIReadOnlyListResult) => ({
        succeeded: !!result.succeeded,
        data: result.data ?? [],
        errors: result.errors ?? [],
        errorCodes: result.errorCodes ?? [],
      })),
    );
  }

  submitContactLead(
    slug: string,
    body: SchoolContactLeadRequest,
  ): Observable<SchoolContactLeadResultDtoResult> {
    return this.client.contactLeads(slug, body);
  }

  getAdmissionRequirements(
    slug: string,
    params?: PublicAdmissionRequirementsParams,
  ): Observable<ApiResult<PublicAdmissionRequirementSummary[]>> {
    return this.client.admissionRequirementsGET3(
      slug,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
    ).pipe(
      map((result) => ({
        succeeded: !!result.succeeded,
        data: result.data ?? [],
        errors: result.errors ?? [],
        errorCodes: result.errorCodes ?? [],
      })),
    );
  }

  getInterviewAssessmentPolicy(
    slug: string,
    params?: PublicInterviewAssessmentPolicyParams,
  ) {
    return this.client.interviewAssessmentPolicy(
      slug,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
    );
  }

  getAgeEligibility(slug: string, params?: PublicAgeEligibilityParams) {
    return this.client.ageEligibility(
      slug,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
      params?.childProfileId,
    );
  }

  getInterviewFaqs(
    slug: string,
    params?: PublicInterviewFaqsParams,
  ): Observable<ApiResult<PublicInterviewFaqItem[]>> {
    return this.client
      .interviewFaqsGET3(
        slug,
        params?.branchId,
        params?.educationalStageId,
        params?.gradeId,
        params?.academicYearId,
        params?.category,
      )
      .pipe(
        map((result) => ({
          succeeded: !!result.succeeded,
          data: result.data ?? [],
          errors: result.errors ?? [],
          errorCodes: result.errorCodes ?? [],
        })),
      );
  }

  getMapConfiguration(): Observable<PublicMapConfigurationDtoResult> {
    return this.client.mapConfiguration();
  }

  getMapPins(query: SchoolsSearchQuery & {
    northLatitude?: number;
    southLatitude?: number;
    eastLongitude?: number;
    westLongitude?: number;
    centerLatitude?: number;
    centerLongitude?: number;
    radiusKm?: number;
    maxPins?: number;
  }): Observable<PublicSchoolMapPinsResultDtoResult> {
    const curriculumIds = query.curriculumIds?.length ? [...query.curriculumIds] : undefined;
    const facilityIds = query.facilityIds?.length ? [...query.facilityIds] : undefined;

    return this.client.mapPins(
      query.search,
      query.countryId,
      query.governorateId,
      query.cityId,
      query.districtId,
      curriculumIds?.[0],
      curriculumIds,
      query.stageId,
      query.gradeId,
      query.schoolType,
      query.genderType,
      query.admissionOpen,
      facilityIds,
      query.minimumTuition,
      query.maximumTuition,
      query.academicYearId,
      query.northLatitude,
      query.southLatitude,
      query.eastLongitude,
      query.westLongitude,
      query.centerLatitude,
      query.centerLongitude,
      query.radiusKm,
      query.maxPins,
    );
  }
}
