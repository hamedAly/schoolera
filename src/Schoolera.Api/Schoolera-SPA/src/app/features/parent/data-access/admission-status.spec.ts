import {
  AdmissionLifecycleStatus,
  admissionStatusKey,
  admissionStatusLabelKey,
  admissionStatusModifier,
} from './admission-status';

describe('admission status mapping', () => {
  it('maps base statuses (1-6) to keys', () => {
    expect(admissionStatusKey(AdmissionLifecycleStatus.Draft)).toBe('draft');
    expect(admissionStatusKey(AdmissionLifecycleStatus.Submitted)).toBe('submitted');
    expect(admissionStatusKey(AdmissionLifecycleStatus.UnderReview)).toBe('underReview');
    expect(admissionStatusKey(AdmissionLifecycleStatus.Accepted)).toBe('accepted');
    expect(admissionStatusKey(AdmissionLifecycleStatus.Rejected)).toBe('rejected');
    expect(admissionStatusKey(AdmissionLifecycleStatus.Cancelled)).toBe('cancelled');
  });

  it('maps Prompt 7 lifecycle statuses (7-11) to keys', () => {
    expect(admissionStatusKey(AdmissionLifecycleStatus.MissingDocuments)).toBe('missingDocuments');
    expect(admissionStatusKey(AdmissionLifecycleStatus.InterviewRequired)).toBe('interviewRequired');
    expect(admissionStatusKey(AdmissionLifecycleStatus.AssessmentRequired)).toBe('assessmentRequired');
    expect(admissionStatusKey(AdmissionLifecycleStatus.WaitingList)).toBe('waitingList');
    expect(admissionStatusKey(AdmissionLifecycleStatus.Registered)).toBe('registered');
  });

  it('maps lifecycle statuses to badge modifiers', () => {
    expect(admissionStatusModifier(AdmissionLifecycleStatus.MissingDocuments)).toBe('warning');
    expect(admissionStatusModifier(AdmissionLifecycleStatus.InterviewRequired)).toBe('info');
    expect(admissionStatusModifier(AdmissionLifecycleStatus.AssessmentRequired)).toBe('info');
    expect(admissionStatusModifier(AdmissionLifecycleStatus.WaitingList)).toBe('warning');
    expect(admissionStatusModifier(AdmissionLifecycleStatus.Registered)).toBe('success');
  });

  it('falls back to unknown for unmapped or null values', () => {
    expect(admissionStatusKey(null)).toBe('unknown');
    expect(admissionStatusKey(undefined)).toBe('unknown');
    expect(admissionStatusKey(99)).toBe('unknown');
    expect(admissionStatusModifier(99)).toBe('unknown');
  });

  it('builds parent-scoped label keys for lifecycle statuses', () => {
    expect(admissionStatusLabelKey(AdmissionLifecycleStatus.MissingDocuments)).toBe(
      'parent.applications.status.missingDocuments',
    );
    expect(admissionStatusLabelKey(AdmissionLifecycleStatus.Registered)).toBe(
      'parent.applications.status.registered',
    );
  });
});
