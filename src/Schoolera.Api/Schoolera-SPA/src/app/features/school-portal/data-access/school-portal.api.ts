import { HttpClient, HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  AcceptSchoolAdmissionApplicationRequest,
  Client,
  AccessibleSchoolDtoIReadOnlyListResult,
  AddSchoolAdminRequest,
  CreateSchoolAdditionalServiceRequest,
  CreateSchoolAdmissionRequirementRequest,
  CreateSchoolAdmissionQuestionRequest,
  CreateSchoolInterviewAssessmentPolicyRequest,
  CreateSchoolInterviewFaqRequest,
  CancelInterviewAssessmentSlotRequest,
  CloneSchoolInterviewAssessmentPolicyRequest,
  CloneSchoolChildAgeEligibilityRuleRequest,
  CreateSchoolChildAgeEligibilityRuleRequest,
  CreateSchoolBranchRequest,
  PreviewSchoolInterviewAssessmentPolicyApplicabilityRequest,
  PreviewSchoolChildAgeEligibilityRequest,
  CreateSchoolStageOfferingRequest,
  CreateTuitionFeeRequest,
  ReplaceSchoolFacilitiesRequest,
  ReorderSchoolAdditionalServicesRequest,
  ReorderSchoolAdmissionRequirementsRequest,
  ReorderSchoolAdmissionQuestionsRequest,
  ReorderSchoolInterviewFaqsRequest,
  RejectSchoolAdmissionApplicationRequest,
  GrantAdmissionAgeEligibilityExceptionRequest,
  ReorderSchoolGalleryImagesRequest,
  CancelAdmissionAppointmentRequest,
  CompleteAdmissionAppointmentRequest,
  MarkRegisteredRequest,
  MoveToWaitingListRequest,
  RequestMissingDocumentsRequest,
  ReturnFromWaitingListRequest,
  ScheduleAdmissionAppointmentRequest,
  SchoolAdditionalServiceDtoIReadOnlyListResult,
  SchoolAdmissionApplicationDetailDtoResult,
  SchoolAdmissionApplicationListItemDtoPagedResultResult,
  StartSchoolAdmissionReviewRequest,
  SchoolAdditionalServiceDtoResult,
  SchoolBranchDtoIReadOnlyListResult,
  SchoolBranchDtoResult,
  SchoolDashboardDtoResult,
  SchoolFacilityListItemDtoIReadOnlyListResult,
  SchoolGalleryImageDtoIReadOnlyListResult,
  SchoolGalleryImageDtoResult,
  SchoolMediaDtoResult,
  SchoolPortalProfileDtoResult,
  SchoolStageOfferingDtoIReadOnlyListResult,
  SchoolStageOfferingDtoResult,
  SchoolTeamMemberDtoIReadOnlyListResult,
  SchoolTeamMemberDtoResult,
  TransferSchoolOwnershipRequest,
  TuitionFeeDtoIReadOnlyListResult,
  TuitionFeeDtoResult,
  UpdateSchoolAdditionalServiceRequest,
  UpdateSchoolAdmissionRequirementRequest,
  UpdateSchoolAdmissionQuestionRequest,
  UpdateSchoolInterviewAssessmentPolicyRequest,
  UpdateSchoolInterviewFaqRequest,
  UpdateSchoolChildAgeEligibilityRuleRequest,
  UpdateSchoolBranchRequest,
  UpdateSchoolFeeVisibilityRequest,
  UpdateSchoolGalleryImageRequest,
  UpdateSchoolPortalProfileRequest,
  UpdateSchoolStageOfferingRequest,
  UpdateSchoolTeamMemberRequest,
  UpdateTuitionFeeRequest,
  UpsertSchoolTeamMemberRequest,
  SchoolFeeVisibilityDtoResult,
  InterviewAssessmentSlotAuditDtoIReadOnlyListResult,
  InterviewAssessmentSlotDtoIReadOnlyListResult,
  InterviewAssessmentSlotDtoResult,
  SlotGenerationResultDtoResult,
  SlotRecurrencePreviewDtoResult,
  SlotRecurrenceRequest,
  UpsertInterviewAssessmentSlotRequest,
  SchoolMeetingSessionContextDtoResult,
  MeetingAccessActionDtoResult,
  SlotKind,
} from '../../../core/api-client/SwaggerClient.service';

import {
  PortalUploadEvent,
  SchoolGalleryImageDto,
  SchoolMediaDto,
} from './school-portal.models';
/**
 * Feature facade over NSwag school-portal endpoints.
 *
 * Logo, cover, and gallery uploads use a narrowly scoped HttpClient call so multipart
 * progress events are available; all other operations use the generated {@link Client}.
 * Paths remain relative `/api/...`. Accept-Language and CSRF are handled by global
 * interceptors — do not set them here.
 */
@Injectable({
  providedIn: 'root',
})
export class SchoolPortalApi {
  private readonly client = inject(Client);
  private readonly http = inject(HttpClient);

  getMeetingSession(
    schoolId: string,
    appointmentId: string,
    kind: SlotKind,
  ): Observable<SchoolMeetingSessionContextDtoResult> {
    return this.client.appointments(schoolId, appointmentId, kind);
  }

  resolveMeetingHost(
    schoolId: string,
    appointmentId: string,
    kind: SlotKind,
  ): Observable<MeetingAccessActionDtoResult> {
    return this.client.host(schoolId, appointmentId, kind);
  }

  listAccessibleSchools(): Observable<AccessibleSchoolDtoIReadOnlyListResult> {
    return this.client.schools3();
  }

  getDashboard(schoolId: string): Observable<SchoolDashboardDtoResult> {
    return this.client.dashboard3(schoolId);
  }

  getProfile(schoolId: string): Observable<SchoolPortalProfileDtoResult> {
    return this.client.profileGET2(schoolId);
  }

  updateProfile(
    schoolId: string,
    body: UpdateSchoolPortalProfileRequest,
  ): Observable<SchoolPortalProfileDtoResult> {
    return this.client.profilePUT3(schoolId, body);
  }

  listBranches(schoolId: string): Observable<SchoolBranchDtoIReadOnlyListResult> {
    return this.client.branchesGET(schoolId);
  }

  createBranch(schoolId: string, body: CreateSchoolBranchRequest): Observable<SchoolBranchDtoResult> {
    return this.client.branchesPOST(schoolId, body);
  }

  updateBranch(
    schoolId: string,
    branchId: string,
    body: UpdateSchoolBranchRequest,
  ): Observable<SchoolBranchDtoResult> {
    return this.client.branchesPUT(schoolId, branchId, body);
  }

  activateBranch(schoolId: string, branchId: string): Observable<SchoolBranchDtoResult> {
    return this.client.activate3(schoolId, branchId);
  }

  deactivateBranch(schoolId: string, branchId: string): Observable<SchoolBranchDtoResult> {
    return this.client.deactivate12(schoolId, branchId);
  }

  listOfferings(schoolId: string, branchId?: string): Observable<SchoolStageOfferingDtoIReadOnlyListResult> {
    return this.client.offeringsGET(schoolId, branchId);
  }

  createOffering(
    schoolId: string,
    body: CreateSchoolStageOfferingRequest,
  ): Observable<SchoolStageOfferingDtoResult> {
    return this.client.offeringsPOST(schoolId, body);
  }

  updateOffering(
    schoolId: string,
    offeringId: string,
    body: UpdateSchoolStageOfferingRequest,
  ): Observable<SchoolStageOfferingDtoResult> {
    return this.client.offeringsPUT(schoolId, offeringId, body);
  }

  activateOffering(schoolId: string, offeringId: string): Observable<SchoolStageOfferingDtoResult> {
    return this.client.activate4(schoolId, offeringId);
  }

  deactivateOffering(schoolId: string, offeringId: string): Observable<SchoolStageOfferingDtoResult> {
    return this.client.deactivate13(schoolId, offeringId);
  }

  listTuitionFees(schoolId: string, branchId?: string): Observable<TuitionFeeDtoIReadOnlyListResult> {
    return this.client.tuitionFeesGET(schoolId, branchId);
  }

  createTuitionFee(schoolId: string, body: CreateTuitionFeeRequest): Observable<TuitionFeeDtoResult> {
    return this.client.tuitionFeesPOST(schoolId, body);
  }

  updateTuitionFee(
    schoolId: string,
    feeId: string,
    body: UpdateTuitionFeeRequest,
  ): Observable<TuitionFeeDtoResult> {
    return this.client.tuitionFeesPUT(schoolId, feeId, body);
  }

  activateTuitionFee(schoolId: string, feeId: string): Observable<TuitionFeeDtoResult> {
    return this.client.activate5(schoolId, feeId);
  }

  deactivateTuitionFee(schoolId: string, feeId: string): Observable<TuitionFeeDtoResult> {
    return this.client.deactivate14(schoolId, feeId);
  }

  publishTuitionFee(schoolId: string, feeId: string): Observable<TuitionFeeDtoResult> {
    return this.client.publish6(schoolId, feeId);
  }

  unpublishTuitionFee(schoolId: string, feeId: string): Observable<TuitionFeeDtoResult> {
    return this.client.unpublish5(schoolId, feeId);
  }

  updateFeeVisibility(
    schoolId: string,
    body: UpdateSchoolFeeVisibilityRequest,
  ): Observable<SchoolFeeVisibilityDtoResult> {
    return this.client.feeVisibility(schoolId, body);
  }

  getFacilities(schoolId: string): Observable<SchoolFacilityListItemDtoIReadOnlyListResult> {
    return this.client.facilitiesGET(schoolId);
  }

  replaceFacilities(
    schoolId: string,
    body: ReplaceSchoolFacilitiesRequest,
  ): Observable<SchoolFacilityListItemDtoIReadOnlyListResult> {
    return this.client.facilitiesPUT2(schoolId, body);
  }

  getMedia(schoolId: string): Observable<SchoolMediaDtoResult> {
    return this.client.media(schoolId);
  }

  uploadLogoWithProgress(schoolId: string, file: File): Observable<PortalUploadEvent<SchoolMediaDto>> {
    return this.uploadMedia(`/api/school-portal/schools/${schoolId}/media/logo`, file);
  }

  uploadCoverWithProgress(schoolId: string, file: File): Observable<PortalUploadEvent<SchoolMediaDto>> {
    return this.uploadMedia(`/api/school-portal/schools/${schoolId}/media/cover`, file);
  }

  uploadGalleryImageWithProgress(
    schoolId: string,
    file: File,
    stageId?: string | null,
  ): Observable<PortalUploadEvent<SchoolGalleryImageDto>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    if (stageId) {
      formData.append('stageId', stageId);
    }
    return this.uploadMedia(`/api/school-portal/schools/${schoolId}/media/gallery`, file, formData);
  }

  deleteLogo(schoolId: string): Observable<SchoolMediaDtoResult> {
    return this.client.logoDELETE(schoolId);
  }

  deleteCover(schoolId: string): Observable<SchoolMediaDtoResult> {
    return this.client.coverDELETE(schoolId);
  }

  updateGalleryImage(
    schoolId: string,
    imageId: string,
    body: UpdateSchoolGalleryImageRequest,
  ): Observable<SchoolGalleryImageDtoResult> {
    return this.client.galleryPUT(schoolId, imageId, body);
  }

  reorderGallery(
    schoolId: string,
    body: ReorderSchoolGalleryImagesRequest,
  ): Observable<SchoolGalleryImageDtoIReadOnlyListResult> {
    return this.client.order2(schoolId, body);
  }

  deleteGalleryImage(schoolId: string, imageId: string): Observable<SchoolGalleryImageDtoResult> {
    return this.client.galleryDELETE(schoolId, imageId);
  }

  listServices(schoolId: string): Observable<SchoolAdditionalServiceDtoIReadOnlyListResult> {
    return this.client.servicesGET(schoolId);
  }

  createService(
    schoolId: string,
    body: CreateSchoolAdditionalServiceRequest,
  ): Observable<SchoolAdditionalServiceDtoResult> {
    return this.client.servicesPOST2(schoolId, body);
  }

  updateService(
    schoolId: string,
    serviceId: string,
    body: UpdateSchoolAdditionalServiceRequest,
  ): Observable<SchoolAdditionalServiceDtoResult> {
    return this.client.servicesPUT2(schoolId, serviceId, body);
  }

  activateService(schoolId: string, serviceId: string): Observable<SchoolAdditionalServiceDtoResult> {
    return this.client.activate9(schoolId, serviceId);
  }

  deactivateService(schoolId: string, serviceId: string): Observable<SchoolAdditionalServiceDtoResult> {
    return this.client.deactivate18(schoolId, serviceId);
  }

  reorderServices(
    schoolId: string,
    body: ReorderSchoolAdditionalServicesRequest,
  ): Observable<SchoolAdditionalServiceDtoIReadOnlyListResult> {
    return this.client.order(schoolId, body);
  }

  getTeam(schoolId: string): Observable<SchoolTeamMemberDtoIReadOnlyListResult> {
    return this.client.team(schoolId);
  }

  addSchoolAdmin(schoolId: string, body: AddSchoolAdminRequest): Observable<SchoolTeamMemberDtoResult> {
    return this.client.schoolAdminsPOST(schoolId, body);
  }

  removeSchoolAdmin(schoolId: string, membershipId: string): Observable<SchoolTeamMemberDtoResult> {
    return this.client.schoolAdminsDELETE(schoolId, membershipId);
  }

  addTeamMember(
    schoolId: string,
    body: UpsertSchoolTeamMemberRequest,
  ): Observable<SchoolTeamMemberDtoResult> {
    return this.client.membersPOST(schoolId, body);
  }

  updateTeamMember(
    schoolId: string,
    membershipId: string,
    body: UpdateSchoolTeamMemberRequest,
  ): Observable<SchoolTeamMemberDtoResult> {
    return this.client.membersPUT(schoolId, membershipId, body);
  }

  transferOwnership(
    schoolId: string,
    body: TransferSchoolOwnershipRequest,
  ): Observable<SchoolTeamMemberDtoResult> {
    return this.client.transferOwnership(schoolId, body);
  }

  listAdmissionApplications(
    schoolId: string,
    params?: {
      status?: number;
      branchId?: string;
      gradeId?: string;
      educationalStageId?: string;
      academicYearId?: string;
      search?: string;
      dateFrom?: string;
      dateTo?: string;
      sort?: string;
      pageNumber?: number;
      pageSize?: number;
    },
  ): Observable<SchoolAdmissionApplicationListItemDtoPagedResultResult> {
    return this.client.applications(
      schoolId,
      params?.status,
      params?.branchId,
      params?.gradeId,
      params?.educationalStageId,
      params?.academicYearId,
      params?.search,
      params?.dateFrom,
      params?.dateTo,
      params?.sort,
      params?.pageNumber,
      params?.pageSize,
    );
  }

  getAdmissionApplication(
    schoolId: string,
    applicationId: string,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.applications2(schoolId, applicationId);
  }

  startAdmissionReview(
    schoolId: string,
    applicationId: string,
    body?: StartSchoolAdmissionReviewRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.startReview3(schoolId, applicationId, body);
  }

  acceptAdmissionApplication(
    schoolId: string,
    applicationId: string,
    body?: AcceptSchoolAdmissionApplicationRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.accept(schoolId, applicationId, body);
  }

  rejectAdmissionApplication(
    schoolId: string,
    applicationId: string,
    body: RejectSchoolAdmissionApplicationRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.reject2(schoolId, applicationId, body);
  }

  // ---- Prompt 7 admission lifecycle actions (generated NSwag Client) ----

  requestMissingDocuments(
    schoolId: string,
    applicationId: string,
    body: RequestMissingDocumentsRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.requestMissingDocuments(schoolId, applicationId, body);
  }

  scheduleInterview(
    schoolId: string,
    applicationId: string,
    body: ScheduleAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.scheduleInterview(schoolId, applicationId, body);
  }

  rescheduleInterview(
    schoolId: string,
    applicationId: string,
    body: ScheduleAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.rescheduleInterview(schoolId, applicationId, body);
  }

  cancelInterview(
    schoolId: string,
    applicationId: string,
    body?: CancelAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.cancelInterview(schoolId, applicationId, body);
  }

  completeInterview(
    schoolId: string,
    applicationId: string,
    body: CompleteAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.completeInterview(schoolId, applicationId, body);
  }

  scheduleAssessment(
    schoolId: string,
    applicationId: string,
    body: ScheduleAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.scheduleAssessment(schoolId, applicationId, body);
  }

  rescheduleAssessment(
    schoolId: string,
    applicationId: string,
    body: ScheduleAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.rescheduleAssessment(schoolId, applicationId, body);
  }

  cancelAssessment(
    schoolId: string,
    applicationId: string,
    body?: CancelAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.cancelAssessment(schoolId, applicationId, body);
  }

  completeAssessment(
    schoolId: string,
    applicationId: string,
    body: CompleteAdmissionAppointmentRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.completeAssessment(schoolId, applicationId, body);
  }

  moveToWaitingList(
    schoolId: string,
    applicationId: string,
    body?: MoveToWaitingListRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.moveToWaitingList(schoolId, applicationId, body);
  }

  returnFromWaitingList(
    schoolId: string,
    applicationId: string,
    body?: ReturnFromWaitingListRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.returnFromWaitingList(schoolId, applicationId, body);
  }

  markRegistered(
    schoolId: string,
    applicationId: string,
    body?: MarkRegisteredRequest,
  ): Observable<SchoolAdmissionApplicationDetailDtoResult> {
    return this.client.markRegistered(schoolId, applicationId, body);
  }

  /**
   * Authenticated blob download. NSwag `download4` discards the response body.
   */
  downloadAdmissionAttachment(
    schoolId: string,
    applicationId: string,
    attachmentId: string,
    fileName: string,
  ): Observable<void> {
    return this.http
      .get(
        `/api/school-portal/schools/${schoolId}/applications/${applicationId}/attachments/${attachmentId}/download`,
        {
          observe: 'response',
          responseType: 'blob',
        },
      )
      .pipe(
        map((response: HttpResponse<Blob>) => {
          const blob = response.body;
          if (!blob) {
            return;
          }

          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = fileName || 'attachment';
          anchor.rel = 'noopener';
          document.body.appendChild(anchor);
          anchor.click();
          anchor.remove();
          URL.revokeObjectURL(url);
        }),
      );
  }

  /**
   * Multipart upload with progress. Uses HttpClient because the generated NSwag client
   * does not expose upload progress events.
   */
  private uploadMedia<T>(
    url: string,
    file: File,
    formData = (() => {
      const fd = new FormData();
      fd.append('file', file, file.name);
      return fd;
    })(),
  ): Observable<PortalUploadEvent<T>> {
    return this.http
      .post<{ succeeded?: boolean; data?: T; errors?: string[]; errorCodes?: string[] }>(url, formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(map((event) => this.mapUploadEvent(event)));
  }

  private mapUploadEvent<T>(
    event: HttpEvent<{ succeeded?: boolean; data?: T; errors?: string[]; errorCodes?: string[] }>,
  ): PortalUploadEvent<T> {
    if (event.type === HttpEventType.UploadProgress) {
      const total = event.total ?? 0;
      const percent = total > 0 ? Math.round((100 * event.loaded) / total) : 0;
      return { kind: 'progress', percent };
    }

    if (event.type === HttpEventType.Response) {
      const body = event.body;
      return {
        kind: 'complete',
        result: {
          succeeded: !!body?.succeeded,
          data: body?.data ?? null,
          errors: body?.errors ?? [],
          errorCodes: body?.errorCodes,
        },
      };
    }

    return { kind: 'progress', percent: 0 };
  }

  listAdmissionRequirements(
    schoolId: string,
    params?: import('./school-portal.models').ListSchoolAdmissionRequirementsParams,
  ) {
    return this.client.admissionRequirementsGET(
      schoolId,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
      params?.kind,
      params?.publicationStatus,
      params?.isActive,
    );
  }

  getAdmissionRequirement(schoolId: string, requirementId: string) {
    return this.client.admissionRequirementsGET2(schoolId, requirementId);
  }

  createAdmissionRequirement(schoolId: string, body: CreateSchoolAdmissionRequirementRequest) {
    return this.client.admissionRequirementsPOST(schoolId, body);
  }

  updateAdmissionRequirement(
    schoolId: string,
    requirementId: string,
    body: UpdateSchoolAdmissionRequirementRequest,
  ) {
    return this.client.admissionRequirementsPUT(schoolId, requirementId, body);
  }

  reorderAdmissionRequirements(schoolId: string, body: ReorderSchoolAdmissionRequirementsRequest) {
    return this.client.reorder4(schoolId, body);
  }

  publishAdmissionRequirement(schoolId: string, requirementId: string) {
    return this.client.publish11(schoolId, requirementId);
  }

  unpublishAdmissionRequirement(schoolId: string, requirementId: string) {
    return this.client.unpublish10(schoolId, requirementId);
  }

  deactivateAdmissionRequirement(schoolId: string, requirementId: string) {
    return this.client.deactivate20(schoolId, requirementId);
  }

  listAdmissionQuestions(
    schoolId: string,
    params?: import('./school-portal.models').ListSchoolAdmissionQuestionsParams,
  ) {
    return this.client.admissionQuestionsGET(
      schoolId,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
      params?.questionType,
      params?.publicationStatus,
      params?.isActive,
    );
  }

  getAdmissionQuestion(schoolId: string, questionId: string) {
    return this.client.admissionQuestionsGET2(schoolId, questionId);
  }

  createAdmissionQuestion(
    schoolId: string,
    body: CreateSchoolAdmissionQuestionRequest,
  ) {
    return this.client.admissionQuestionsPOST(schoolId, body);
  }

  updateAdmissionQuestion(
    schoolId: string,
    questionId: string,
    body: UpdateSchoolAdmissionQuestionRequest,
  ) {
    return this.client.admissionQuestionsPUT(schoolId, questionId, body);
  }

  reorderAdmissionQuestions(schoolId: string, body: ReorderSchoolAdmissionQuestionsRequest) {
    return this.client.reorder3(schoolId, body);
  }

  publishAdmissionQuestion(schoolId: string, questionId: string) {
    return this.client.publish10(schoolId, questionId);
  }

  unpublishAdmissionQuestion(schoolId: string, questionId: string) {
    return this.client.unpublish9(schoolId, questionId);
  }

  deactivateAdmissionQuestion(schoolId: string, questionId: string) {
    return this.client.deactivate19(schoolId, questionId);
  }

  listAgeEligibilityRules(
    schoolId: string,
    params?: import('./school-portal.models').ListSchoolChildAgeEligibilityRulesParams,
  ) {
    return this.client.ageEligibilityRulesGET(
      schoolId,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
      params?.publicationStatus,
      params?.isActive,
    );
  }

  getAgeEligibilityRule(schoolId: string, ruleId: string) {
    return this.client.ageEligibilityRulesGET2(schoolId, ruleId);
  }

  createAgeEligibilityRule(schoolId: string, body: CreateSchoolChildAgeEligibilityRuleRequest) {
    return this.client.ageEligibilityRulesPOST(schoolId, body);
  }

  updateAgeEligibilityRule(
    schoolId: string,
    ruleId: string,
    body: UpdateSchoolChildAgeEligibilityRuleRequest,
  ) {
    return this.client.ageEligibilityRulesPUT(schoolId, ruleId, body);
  }

  publishAgeEligibilityRule(schoolId: string, ruleId: string) {
    return this.client.publish12(schoolId, ruleId);
  }

  unpublishAgeEligibilityRule(schoolId: string, ruleId: string) {
    return this.client.unpublish11(schoolId, ruleId);
  }

  deactivateAgeEligibilityRule(schoolId: string, ruleId: string) {
    return this.client.deactivate21(schoolId, ruleId);
  }

  cloneAgeEligibilityRule(
    schoolId: string,
    ruleId: string,
    body?: CloneSchoolChildAgeEligibilityRuleRequest,
  ) {
    return this.client.clone(schoolId, ruleId, body);
  }

  previewAgeEligibility(schoolId: string, body: PreviewSchoolChildAgeEligibilityRequest) {
    return this.client.preview(schoolId, body);
  }

  grantAgeEligibilityException(
    schoolId: string,
    applicationId: string,
    body: GrantAdmissionAgeEligibilityExceptionRequest,
  ) {
    return this.client.ageEligibilityException(schoolId, applicationId, body);
  }

  listInterviewAssessmentPolicies(
    schoolId: string,
    params?: import('./school-portal.models').ListSchoolInterviewAssessmentPoliciesParams,
  ) {
    return this.client.interviewAssessmentPoliciesGET(
      schoolId,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
      params?.publicationStatus,
      params?.isActive,
    );
  }

  getInterviewAssessmentPolicy(schoolId: string, policyId: string) {
    return this.client.interviewAssessmentPoliciesGET2(schoolId, policyId);
  }

  createInterviewAssessmentPolicy(
    schoolId: string,
    body: CreateSchoolInterviewAssessmentPolicyRequest,
  ) {
    return this.client.interviewAssessmentPoliciesPOST(schoolId, body);
  }

  updateInterviewAssessmentPolicy(
    schoolId: string,
    policyId: string,
    body: UpdateSchoolInterviewAssessmentPolicyRequest,
  ) {
    return this.client.interviewAssessmentPoliciesPUT(schoolId, policyId, body);
  }

  publishInterviewAssessmentPolicy(schoolId: string, policyId: string) {
    return this.client.publish13(schoolId, policyId);
  }

  unpublishInterviewAssessmentPolicy(schoolId: string, policyId: string) {
    return this.client.unpublish12(schoolId, policyId);
  }

  deactivateInterviewAssessmentPolicy(schoolId: string, policyId: string) {
    return this.client.deactivate22(schoolId, policyId);
  }

  cloneInterviewAssessmentPolicy(
    schoolId: string,
    policyId: string,
    body?: CloneSchoolInterviewAssessmentPolicyRequest,
  ) {
    return this.client.clone2(schoolId, policyId, body);
  }

  previewInterviewAssessmentPolicyApplicability(
    schoolId: string,
    body: PreviewSchoolInterviewAssessmentPolicyApplicabilityRequest,
  ) {
    return this.client.previewApplicability(schoolId, body);
  }

  listMeetingProviderOptions(schoolId: string) {
    return this.client.meetingProviders(schoolId);
  }

  listInterviewFaqs(
    schoolId: string,
    params?: import('./school-portal.models').ListSchoolInterviewFaqsParams,
  ) {
    return this.client.interviewFaqsGET(
      schoolId,
      params?.interviewCategory,
      params?.branchId,
      params?.educationalStageId,
      params?.gradeId,
      params?.academicYearId,
      params?.isPublished,
      params?.isActive,
    );
  }

  getInterviewFaq(schoolId: string, id: string) {
    return this.client.interviewFaqsGET2(schoolId, id);
  }

  createInterviewFaq(schoolId: string, body: CreateSchoolInterviewFaqRequest) {
    return this.client.interviewFaqsPOST(schoolId, body);
  }

  updateInterviewFaq(schoolId: string, id: string, body: UpdateSchoolInterviewFaqRequest) {
    return this.client.interviewFaqsPUT(schoolId, id, body);
  }

  reorderInterviewFaqs(schoolId: string, body: ReorderSchoolInterviewFaqsRequest) {
    return this.client.reorder5(schoolId, body);
  }

  publishInterviewFaq(schoolId: string, id: string) {
    return this.client.publish14(schoolId, id);
  }

  unpublishInterviewFaq(schoolId: string, id: string) {
    return this.client.unpublish13(schoolId, id);
  }

  activateInterviewFaq(schoolId: string, id: string) {
    return this.client.activate10(schoolId, id);
  }

  deactivateInterviewFaq(schoolId: string, id: string) {
    return this.client.deactivate23(schoolId, id);
  }

  listInterviewAssessmentSlots(
    schoolId: string,
    params?: {
      branchId?: string;
      stageId?: string;
      gradeId?: string;
      academicYearId?: string;
      kind?: number;
      mode?: number;
      status?: number;
      resourceId?: string;
      from?: string;
      to?: string;
    },
  ): Observable<InterviewAssessmentSlotDtoIReadOnlyListResult> {
    return this.client.interviewAssessmentSlotsGET(
      schoolId,
      params?.branchId,
      params?.stageId,
      params?.gradeId,
      params?.academicYearId,
      params?.kind,
      params?.mode,
      params?.status,
      params?.resourceId,
      params?.from,
      params?.to,
    );
  }

  getInterviewAssessmentSlot(
    schoolId: string,
    slotId: string,
  ): Observable<InterviewAssessmentSlotDtoResult> {
    return this.client.interviewAssessmentSlotsGET2(schoolId, slotId);
  }

  createInterviewAssessmentSlot(
    schoolId: string,
    body: UpsertInterviewAssessmentSlotRequest,
  ): Observable<InterviewAssessmentSlotDtoResult> {
    return this.client.interviewAssessmentSlotsPOST(schoolId, body);
  }

  updateInterviewAssessmentSlot(
    schoolId: string,
    slotId: string,
    body: UpsertInterviewAssessmentSlotRequest,
  ): Observable<InterviewAssessmentSlotDtoResult> {
    return this.client.interviewAssessmentSlotsPUT(schoolId, slotId, body);
  }

  openInterviewAssessmentSlot(schoolId: string, slotId: string, rowVersion?: string) {
    return this.client.open(schoolId, slotId, rowVersion);
  }

  closeInterviewAssessmentSlot(schoolId: string, slotId: string, rowVersion?: string) {
    return this.client.close2(schoolId, slotId, rowVersion);
  }

  reopenInterviewAssessmentSlot(schoolId: string, slotId: string, rowVersion?: string) {
    return this.client.reopen2(schoolId, slotId, rowVersion);
  }

  cancelInterviewAssessmentSlot(
    schoolId: string,
    slotId: string,
    body: CancelInterviewAssessmentSlotRequest,
  ) {
    return this.client.cancel3(schoolId, slotId, body);
  }

  previewInterviewAssessmentSlotRecurrence(
    schoolId: string,
    body: SlotRecurrenceRequest,
  ): Observable<SlotRecurrencePreviewDtoResult> {
    return this.client.preview2(schoolId, body);
  }

  generateInterviewAssessmentSlotRecurrence(
    schoolId: string,
    body: SlotRecurrenceRequest,
  ): Observable<SlotGenerationResultDtoResult> {
    return this.client.generate(schoolId, body);
  }

  getInterviewAssessmentSlotAudit(
    schoolId: string,
    slotId: string,
  ): Observable<InterviewAssessmentSlotAuditDtoIReadOnlyListResult> {
    return this.client.audit2(schoolId, slotId);
  }
}
