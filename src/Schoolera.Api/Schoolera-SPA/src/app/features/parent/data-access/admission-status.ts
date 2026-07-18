import { AdmissionApplicationStatus } from '../../../core/api-client/SwaggerClient.service';

export type AdmissionStatusModifier =
  | 'draft'
  | 'info'
  | 'warning'
  | 'success'
  | 'error'
  | 'muted'
  | 'unknown';

/**
 * Backend admission lifecycle status values (Prompt 7 extends the generated NSwag enum,
 * which currently only exposes 1–6). These numeric constants stay valid once NSwag
 * regenerates `AdmissionApplicationStatus` with the 7–11 members.
 */
export const AdmissionLifecycleStatus = {
  Draft: 1,
  Submitted: 2,
  UnderReview: 3,
  Accepted: 4,
  Rejected: 5,
  Cancelled: 6,
  MissingDocuments: 7,
  InterviewRequired: 8,
  AssessmentRequired: 9,
  WaitingList: 10,
  Registered: 11,
} as const;

const STATUS_KEYS: Record<number, string> = {
  [AdmissionLifecycleStatus.Draft]: 'draft',
  [AdmissionLifecycleStatus.Submitted]: 'submitted',
  [AdmissionLifecycleStatus.UnderReview]: 'underReview',
  [AdmissionLifecycleStatus.Accepted]: 'accepted',
  [AdmissionLifecycleStatus.Rejected]: 'rejected',
  [AdmissionLifecycleStatus.Cancelled]: 'cancelled',
  [AdmissionLifecycleStatus.MissingDocuments]: 'missingDocuments',
  [AdmissionLifecycleStatus.InterviewRequired]: 'interviewRequired',
  [AdmissionLifecycleStatus.AssessmentRequired]: 'assessmentRequired',
  [AdmissionLifecycleStatus.WaitingList]: 'waitingList',
  [AdmissionLifecycleStatus.Registered]: 'registered',
};

const STATUS_MODIFIERS: Record<number, AdmissionStatusModifier> = {
  [AdmissionLifecycleStatus.Draft]: 'draft',
  [AdmissionLifecycleStatus.Submitted]: 'info',
  [AdmissionLifecycleStatus.UnderReview]: 'warning',
  [AdmissionLifecycleStatus.Accepted]: 'success',
  [AdmissionLifecycleStatus.Rejected]: 'error',
  [AdmissionLifecycleStatus.Cancelled]: 'muted',
  [AdmissionLifecycleStatus.MissingDocuments]: 'warning',
  [AdmissionLifecycleStatus.InterviewRequired]: 'info',
  [AdmissionLifecycleStatus.AssessmentRequired]: 'info',
  [AdmissionLifecycleStatus.WaitingList]: 'warning',
  [AdmissionLifecycleStatus.Registered]: 'success',
};

export function admissionStatusKey(status: AdmissionApplicationStatus | number | null | undefined): string {
  if (status == null) {
    return 'unknown';
  }

  return STATUS_KEYS[status as number] ?? 'unknown';
}

export function admissionStatusLabelKey(
  status: AdmissionApplicationStatus | number | null | undefined,
): string {
  return `parent.applications.status.${admissionStatusKey(status)}`;
}

export function admissionStatusDescriptionKey(
  status: AdmissionApplicationStatus | number | null | undefined,
): string {
  return `parent.applications.status.descriptions.${admissionStatusKey(status)}`;
}

export function admissionStatusModifier(
  status: AdmissionApplicationStatus | number | null | undefined,
): AdmissionStatusModifier {
  if (status == null) {
    return 'unknown';
  }

  return STATUS_MODIFIERS[status as number] ?? 'unknown';
}

export function isDraftStatus(status: AdmissionApplicationStatus | number | null | undefined): boolean {
  return status === AdmissionApplicationStatus._1;
}
