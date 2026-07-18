import { HttpClient, HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  AdmissionApplicationDetailDto,
  AdmissionApplicationDetailDtoResult,
  AdmissionApplicationListItemDtoPagedResultResult,
  BooleanResult,
  CancelAdmissionApplicationRequest,
  ChildDocumentDto,
  ChildDocumentDtoIReadOnlyListResult,
  ChildDocumentDtoResult,
  ChildProfileDetailDtoResult,
  ChildProfileListItemDtoIReadOnlyListResult,
  Client,
  CopyChildDocumentToAdmissionRequest,
  CreateAdmissionApplicationRequest,
  CreateChildProfileRequest,
  ParentDashboardDtoResult,
  ParentAvailableSlotDtoIReadOnlyListResult,
  AdmissionAppointmentDtoResult,
  ParentAppointmentMutationRequest,
  ParentCancelAppointmentRequest,
  ParentJoinAppointmentRequest,
  MeetingAccessActionDtoResult,
  ParentRequestAppointmentRescheduleRequest,
  ParentSelectAppointmentSlotRequest,
  ParentAgeEligibilityCheckRequest,
  AgeEligibilityResultDtoResult,
  ParentProfileDtoResult,
  SubmitAdmissionOutcomeDtoResult,
  UpdateAdmissionApplicationRequest,
  UpdateChildProfileRequest,
  UpdateParentProfileRequest,
  UpsertAdmissionAnswerRequest,
  AdmissionApplicationQuestionsDtoResult,
  ParentCourierAvailabilityDto,
  ParentCourierAvailabilityDtoResult,
  CourierAvailabilityOptionDto,
  CourierDestinationBranchDto,
} from '../../../core/api-client/SwaggerClient.service';

import type {
  AdmissionQuestionChecklistItem,
  MissingAdmissionQuestion,
} from './admission-questions.models';
import type {
  CorrectMissingSnapshotFieldRequest,
  ResubmitMissingDocumentsRequest,
} from '../../../core/api-client/SwaggerClient.service';

export type AdmissionUploadEvent =
  | { kind: 'progress'; percent: number }
  | {
      kind: 'complete';
      result: {
        succeeded: boolean;
        data: AdmissionApplicationDetailDto | null;
        errors: string[];
        errorCodes?: string[];
      };
    };

export type ChildDocumentUploadEvent =
  | { kind: 'progress'; percent: number }
  | {
      kind: 'complete';
      result: {
        succeeded: boolean;
        data: ChildDocumentDto | null;
        errors: string[];
        errorCodes?: string[];
      };
    };

export type SubmitAdmissionOutcomeResult = SubmitAdmissionOutcomeDtoResult & {
  data?: SubmitAdmissionOutcomeDtoResult['data'] & {
    missingQuestions?: MissingAdmissionQuestion[];
  };
};

export interface UpsertAdmissionAnswerBody {
  questionSnapshotId: string;
  textValue?: string | null;
  selectedOptionCodes?: string[] | null;
  dateValue?: string | null;
  booleanValue?: boolean | null;
  attachmentId?: string | null;
}

export interface AdmissionApplicationQuestionsResult {
  succeeded?: boolean;
  data?: {
    applicationId?: string;
    questions?: AdmissionQuestionChecklistItem[];
  };
  errors?: string[];
  errorCodes?: string[];
}

export type {
  CourierAvailabilityOptionDto,
  CourierDestinationBranchDto,
  ParentCourierAvailabilityDto,
};

/**
 * Feature facade over the NSwag-generated Parent API.
 * Components depend on this service, not on {@link Client} directly.
 *
 * Attachment upload/download use narrowly scoped HttpClient calls because the generated
 * NSwag methods do not expose upload progress or usable download blobs.
 */
@Injectable({
  providedIn: 'root',
})
export class ParentApi {
  private readonly client = inject(Client);
  private readonly http = inject(HttpClient);

  getDashboard(): Observable<ParentDashboardDtoResult> {
    return this.client.dashboard2();
  }

  checkAgeEligibility(body: ParentAgeEligibilityCheckRequest): Observable<AgeEligibilityResultDtoResult> {
    return this.client.ageEligibilityCheck(body);
  }

  getProfile(): Observable<ParentProfileDtoResult> {
    return this.client.profileGET();
  }

  updateProfile(body: UpdateParentProfileRequest): Observable<ParentProfileDtoResult> {
    return this.client.profilePUT2(body);
  }

  listChildren(): Observable<ChildProfileListItemDtoIReadOnlyListResult> {
    return this.client.childrenGET();
  }

  getChild(childId: string): Observable<ChildProfileDetailDtoResult> {
    return this.client.childrenGET2(childId);
  }

  createChild(body: CreateChildProfileRequest): Observable<ChildProfileDetailDtoResult> {
    return this.client.childrenPOST(body);
  }

  updateChild(childId: string, body: UpdateChildProfileRequest): Observable<ChildProfileDetailDtoResult> {
    return this.client.childrenPUT(childId, body);
  }

  deleteChild(childId: string): Observable<BooleanResult> {
    return this.client.childrenDELETE(childId);
  }

  listChildDocuments(childId: string): Observable<ChildDocumentDtoIReadOnlyListResult> {
    return this.client.documentsGET(childId);
  }

  deleteChildDocument(childId: string, documentId: string): Observable<BooleanResult> {
    return this.client.documentsDELETE(childId, documentId);
  }

  copyFromVault(
    applicationId: string,
    body: CopyChildDocumentToAdmissionRequest & { requirementSnapshotId?: string | null },
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.fromVault(applicationId, body);
  }

  uploadChildDocumentWithProgress(
    childId: string,
    file: File,
    documentType: number,
  ): Observable<ChildDocumentUploadEvent> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    formData.append('documentType', String(documentType));

    return this.http
      .post<{
        succeeded?: boolean;
        data?: ChildDocumentDto;
        errors?: string[];
        errorCodes?: string[];
      }>(`/api/parent/children/${childId}/documents`, formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(map((event) => this.mapChildDocumentUploadEvent(event)));
  }

  replaceChildDocumentWithProgress(
    childId: string,
    documentId: string,
    file: File,
    documentType: number,
  ): Observable<ChildDocumentUploadEvent> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    formData.append('documentType', String(documentType));

    return this.http
      .put<{
        succeeded?: boolean;
        data?: ChildDocumentDto;
        errors?: string[];
        errorCodes?: string[];
      }>(`/api/parent/children/${childId}/documents/${documentId}`, formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(map((event) => this.mapChildDocumentUploadEvent(event)));
  }

  downloadChildDocument(childId: string, documentId: string, fileName: string): Observable<void> {
    return this.http
      .get(`/api/parent/children/${childId}/documents/${documentId}/download`, {
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(
        map((response: HttpResponse<Blob>) => {
          const blob = response.body;
          if (!blob) {
            return;
          }

          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = fileName || 'document';
          anchor.rel = 'noopener';
          document.body.appendChild(anchor);
          anchor.click();
          anchor.remove();
          URL.revokeObjectURL(url);
        }),
      );
  }

  listAdmissionApplications(params?: {
    status?: number;
    childProfileId?: string;
    schoolId?: string;
    academicYearId?: string;
    search?: string;
    sort?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Observable<AdmissionApplicationListItemDtoPagedResultResult> {
    return this.client.admissionApplicationsGET3(
      params?.status,
      params?.childProfileId,
      params?.schoolId,
      params?.academicYearId,
      params?.search,
      params?.sort,
      params?.pageNumber,
      params?.pageSize,
    );
  }

  createAdmissionApplication(
    body: CreateAdmissionApplicationRequest,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.admissionApplicationsPOST(body);
  }

  getAdmissionApplication(applicationId: string): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.admissionApplicationsGET4(applicationId);
  }

  getOwnedApplicationCourierAvailability(
    applicationId: string,
    location: { countryId?: string; governorateId?: string; cityId?: string; districtId?: string },
  ): Observable<ParentCourierAvailabilityDtoResult> {
    return this.client.courierAvailability(
      applicationId,
      location.countryId,
      location.governorateId,
      location.cityId,
      location.districtId,
    );
  }

  updateAdmissionApplication(
    applicationId: string,
    body: UpdateAdmissionApplicationRequest & { confirmClearQuestionAnswers?: boolean },
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.admissionApplicationsPUT(applicationId, body);
  }

  submitAdmissionApplication(applicationId: string): Observable<SubmitAdmissionOutcomeDtoResult> {
    return this.client.submit(applicationId, { termsAccepted: true, privacyAccepted: true });
  }

  cancelAdmissionApplication(
    applicationId: string,
    body?: CancelAdmissionApplicationRequest,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.cancel(applicationId, body);
  }

  listAvailableInterviewAssessmentSlots(
    applicationId: string,
    kind?: number,
  ): Observable<ParentAvailableSlotDtoIReadOnlyListResult> {
    return this.client.availableSlots(applicationId, kind);
  }

  confirmAppointment(
    applicationId: string,
    kind: number,
    body: ParentAppointmentMutationRequest,
  ): Observable<AdmissionAppointmentDtoResult> {
    return this.client.confirm(applicationId, kind, body);
  }

  selectAppointmentSlot(
    applicationId: string,
    kind: number,
    body: ParentSelectAppointmentSlotRequest,
  ): Observable<AdmissionAppointmentDtoResult> {
    return this.client.selectSlot(applicationId, kind, body);
  }

  requestAppointmentReschedule(
    applicationId: string,
    kind: number,
    body: ParentRequestAppointmentRescheduleRequest,
  ): Observable<AdmissionAppointmentDtoResult> {
    return this.client.requestReschedule(applicationId, kind, body);
  }

  cancelAppointment(
    applicationId: string,
    kind: number,
    body: ParentCancelAppointmentRequest,
  ): Observable<AdmissionAppointmentDtoResult> {
    return this.client.cancel2(applicationId, kind, body);
  }

  joinAppointment(
    applicationId: string,
    kind: number,
    body: ParentJoinAppointmentRequest,
  ): Observable<MeetingAccessActionDtoResult> {
    return this.client.join(applicationId, kind, body);
  }

  removeAdmissionAttachment(
    applicationId: string,
    attachmentId: string,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.attachmentsDELETE(applicationId, attachmentId);
  }

  /** Parent responds to a MissingDocuments request and resubmits for review. */
  resubmitMissingDocuments(
    applicationId: string,
    body?: ResubmitMissingDocumentsRequest,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.resubmitMissingDocuments(applicationId, body);
  }

  /** Parent corrects a requested profile/child snapshot field (MissingDocuments flow). */
  correctMissingField(
    applicationId: string,
    body: CorrectMissingSnapshotFieldRequest,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.correctMissingField(applicationId, body);
  }

  /** Prefer NSwag Client for question snapshot/answer contracts. */
  ensureQuestionSnapshots(applicationId: string): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.ensure(applicationId);
  }

  getApplicationQuestions(applicationId: string): Observable<AdmissionApplicationQuestionsDtoResult> {
    return this.client.questions(applicationId);
  }

  upsertAdmissionAnswer(
    applicationId: string,
    body: UpsertAdmissionAnswerRequest,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.answersPUT(applicationId, body);
  }

  deleteAdmissionAnswer(
    applicationId: string,
    questionSnapshotId: string,
  ): Observable<AdmissionApplicationDetailDtoResult> {
    return this.client.answersDELETE(applicationId, questionSnapshotId);
  }

  uploadQuestionAttachmentWithProgress(
    applicationId: string,
    file: File,
    questionSnapshotId: string,
  ): Observable<AdmissionUploadEvent> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    formData.append('attachmentType', '1');
    formData.append('questionSnapshotId', questionSnapshotId);

    return this.http
      .post<{
        succeeded?: boolean;
        data?: AdmissionApplicationDetailDto;
        errors?: string[];
        errorCodes?: string[];
      }>(`/api/parent/admission-applications/${applicationId}/attachments`, formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(map((event) => this.mapUploadEvent(event)));
  }

  /**
   * Multipart upload with progress. Uses HttpClient because NSwag `attachmentsPOST`
   * does not expose upload progress events. CSRF and Accept-Language remain global.
   */
  uploadAdmissionAttachmentWithProgress(
    applicationId: string,
    file: File,
    attachmentType: number,
    requirementSnapshotId?: string,
    questionSnapshotId?: string,
  ): Observable<AdmissionUploadEvent> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    formData.append('attachmentType', String(attachmentType));
    if (requirementSnapshotId) {
      formData.append('requirementSnapshotId', requirementSnapshotId);
    }
    if (questionSnapshotId) {
      formData.append('questionSnapshotId', questionSnapshotId);
    }

    return this.http
      .post<{
        succeeded?: boolean;
        data?: AdmissionApplicationDetailDto;
        errors?: string[];
        errorCodes?: string[];
      }>(`/api/parent/admission-applications/${applicationId}/attachments`, formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(map((event) => this.mapUploadEvent(event)));
  }

  /**
   * Authenticated blob download. NSwag `download2` discards the response body.
   */
  downloadAdmissionAttachment(
    applicationId: string,
    attachmentId: string,
    fileName: string,
  ): Observable<void> {
    return this.http
      .get(`/api/parent/admission-applications/${applicationId}/attachments/${attachmentId}/download`, {
        observe: 'response',
        responseType: 'blob',
      })
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

  private mapUploadEvent(
    event: HttpEvent<{
      succeeded?: boolean;
      data?: AdmissionApplicationDetailDto;
      errors?: string[];
      errorCodes?: string[];
    }>,
  ): AdmissionUploadEvent {
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

  private mapChildDocumentUploadEvent(
    event: HttpEvent<{
      succeeded?: boolean;
      data?: ChildDocumentDto;
      errors?: string[];
      errorCodes?: string[];
    }>,
  ): ChildDocumentUploadEvent {
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
}
