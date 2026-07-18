/**
 * Prompt 7 admission lifecycle helpers.
 *
 * Request/result DTOs come from the regenerated NSwag client.
 * Named mode/kind constants remain for templates (generated enums use numeric `_1`/`_2` labels).
 */

export type {
  AdmissionAppointmentDto,
  AdmissionApplicationCapabilitiesDto,
  AdmissionApplicationDetailDto,
  AdmissionApplicationDetailDtoResult,
  AdmissionMissingItemDto,
  AdmissionMissingItemRefDto,
  AdmissionMissingItemsRequestDto,
  AdmissionWaitingListDto,
  CancelAdmissionAppointmentRequest,
  CompleteAdmissionAppointmentRequest,
  CorrectMissingSnapshotFieldRequest,
  MarkRegisteredRequest,
  MoveToWaitingListRequest,
  RequestMissingDocumentsRequest,
  ResubmitMissingDocumentsRequest,
  ReturnFromWaitingListRequest,
  ScheduleAdmissionAppointmentRequest,
  SchoolAdmissionApplicationDetailDto,
  SchoolAdmissionApplicationDetailDtoResult,
  SchoolAdmissionReviewCapabilitiesDto,
} from '../../../core/api-client/SwaggerClient.service';

/** Named appointment modes (Online=1, InPerson=2). */
export const AdmissionAppointmentMode = {
  Online: 1,
  InPerson: 2,
} as const;

/** Named appointment lifecycle matching the authoritative backend enum values. */
export const AdmissionAppointmentLifecycle = {
  Proposed: 1,
  Completed: 2,
  Cancelled: 3,
  RescheduleRequested: 4,
  Confirmed: 5,
  NoShow: 6,
  InProgress: 7,
} as const;

/** Named missing-item kinds matching backend enum values. */
export const AdmissionMissingItemKind = {
  RequirementSnapshot: 1,
  QuestionSnapshot: 2,
  ParentSnapshotField: 3,
  ChildSnapshotField: 4,
} as const;

export type ParentAdmissionLifecycleDetail =
  import('../../../core/api-client/SwaggerClient.service').AdmissionApplicationDetailDto;
export type SchoolAdmissionLifecycleDetail =
  import('../../../core/api-client/SwaggerClient.service').SchoolAdmissionApplicationDetailDto;
export type SchoolAdmissionLifecycleCapabilities =
  import('../../../core/api-client/SwaggerClient.service').SchoolAdmissionReviewCapabilitiesDto;
export type ParentAdmissionLifecycleResult =
  import('../../../core/api-client/SwaggerClient.service').AdmissionApplicationDetailDtoResult;
export type SchoolAdmissionLifecycleResult =
  import('../../../core/api-client/SwaggerClient.service').SchoolAdmissionApplicationDetailDtoResult;

/** @deprecated Prefer generated RequestMissingDocumentsRequest */
export type RequestMissingDocumentsBody =
  import('../../../core/api-client/SwaggerClient.service').RequestMissingDocumentsRequest;
/** @deprecated Prefer generated ScheduleAdmissionAppointmentRequest */
export type ScheduleAdmissionAppointmentBody =
  import('../../../core/api-client/SwaggerClient.service').ScheduleAdmissionAppointmentRequest;
/** @deprecated Prefer generated CompleteAdmissionAppointmentRequest */
export type CompleteAdmissionAppointmentBody =
  import('../../../core/api-client/SwaggerClient.service').CompleteAdmissionAppointmentRequest;
/** @deprecated Prefer generated CancelAdmissionAppointmentRequest */
export type CancelAdmissionAppointmentBody =
  import('../../../core/api-client/SwaggerClient.service').CancelAdmissionAppointmentRequest;
/** @deprecated Prefer generated MoveToWaitingListRequest */
export type MoveToWaitingListBody =
  import('../../../core/api-client/SwaggerClient.service').MoveToWaitingListRequest;
/** @deprecated Prefer generated ReturnFromWaitingListRequest */
export type ReturnFromWaitingListBody =
  import('../../../core/api-client/SwaggerClient.service').ReturnFromWaitingListRequest;
/** @deprecated Prefer generated MarkRegisteredRequest */
export type MarkRegisteredBody =
  import('../../../core/api-client/SwaggerClient.service').MarkRegisteredRequest;
/** @deprecated Prefer generated ResubmitMissingDocumentsRequest */
export type ResubmitMissingDocumentsBody =
  import('../../../core/api-client/SwaggerClient.service').ResubmitMissingDocumentsRequest;
/** @deprecated Prefer generated CorrectMissingSnapshotFieldRequest */
export type CorrectMissingSnapshotFieldBody =
  import('../../../core/api-client/SwaggerClient.service').CorrectMissingSnapshotFieldRequest;
