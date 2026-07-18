import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, beforeEach, vi } from 'vitest';

import { Client } from '../../../core/api-client/SwaggerClient.service';
import { SchoolPortalApi } from './school-portal.api';

describe('SchoolPortalApi admission requirements', () => {
  let api: SchoolPortalApi;
  let client: {
    admissionRequirementsGET: ReturnType<typeof vi.fn>;
    reorder4: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    client = {
      admissionRequirementsGET: vi.fn(() => of({ succeeded: true, data: [] })),
      reorder4: vi.fn(() => of({ succeeded: true, data: [] })),
    };
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolPortalApi);
  });

  it('listAdmissionRequirements forwards filters to NSwag client', () => {
    api.listAdmissionRequirements('school-1', { kind: 4, publicationStatus: 2 }).subscribe();
    expect(client.admissionRequirementsGET).toHaveBeenCalledWith(
      'school-1',
      undefined,
      undefined,
      undefined,
      undefined,
      4,
      2,
      undefined,
    );
  });

  it('reorderAdmissionRequirements delegates to reorder4', () => {
    api.reorderAdmissionRequirements('school-1', { orderedRequirementIds: ['a', 'b'] }).subscribe();
    expect(client.reorder4).toHaveBeenCalledWith('school-1', { orderedRequirementIds: ['a', 'b'] });
  });
});

describe('SchoolPortalApi admission questions', () => {
  let api: SchoolPortalApi;
  let client: {
    admissionQuestionsGET: ReturnType<typeof vi.fn>;
    reorder3: ReturnType<typeof vi.fn>;
    publish10: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    client = {
      admissionQuestionsGET: vi.fn(() => of({ succeeded: true, data: [] })),
      reorder3: vi.fn(() => of({ succeeded: true, data: [] })),
      publish10: vi.fn(() => of({ succeeded: true, data: {} })),
    };
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolPortalApi);
  });

  it('listAdmissionQuestions forwards filters to NSwag client', () => {
    api
      .listAdmissionQuestions('school-1', { questionType: 1, publicationStatus: 2, isActive: true })
      .subscribe();
    expect(client.admissionQuestionsGET).toHaveBeenCalledWith(
      'school-1',
      undefined,
      undefined,
      undefined,
      undefined,
      1,
      2,
      true,
    );
  });

  it('reorderAdmissionQuestions delegates to reorder3', () => {
    api.reorderAdmissionQuestions('school-1', { orderedQuestionIds: ['a', 'b'] }).subscribe();
    expect(client.reorder3).toHaveBeenCalledWith('school-1', { orderedQuestionIds: ['a', 'b'] });
  });

  it('publishAdmissionQuestion delegates to publish10', () => {
    api.publishAdmissionQuestion('school-1', 'q-1').subscribe();
    expect(client.publish10).toHaveBeenCalledWith('school-1', 'q-1');
  });
});

describe('SchoolPortalApi age eligibility rules', () => {
  const client = {
    ageEligibilityRulesGET: vi.fn(() => of({ succeeded: true, data: [] })),
    publish12: vi.fn(() => of({ succeeded: true, data: {} })),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
  });

  it('forwards filters and delegates publishing to publish12', () => {
    const api = TestBed.inject(SchoolPortalApi);
    api.listAgeEligibilityRules('school-1', { publicationStatus: 2, isActive: true }).subscribe();
    expect(client.ageEligibilityRulesGET).toHaveBeenCalledWith(
      'school-1', undefined, undefined, undefined, undefined, 2, true,
    );
    api.publishAgeEligibilityRule('school-1', 'rule-1').subscribe();
    expect(client.publish12).toHaveBeenCalledWith('school-1', 'rule-1');
  });
});

describe('SchoolPortalApi interview assessment policies', () => {
  let api: SchoolPortalApi;
  let client: {
    interviewAssessmentPoliciesGET: ReturnType<typeof vi.fn>;
    publish13: ReturnType<typeof vi.fn>;
    unpublish12: ReturnType<typeof vi.fn>;
    deactivate22: ReturnType<typeof vi.fn>;
    clone2: ReturnType<typeof vi.fn>;
    previewApplicability: ReturnType<typeof vi.fn>;
    meetingProviders: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    client = {
      interviewAssessmentPoliciesGET: vi.fn(() => of({ succeeded: true, data: [] })),
      publish13: vi.fn(() => of({ succeeded: true, data: {} })),
      unpublish12: vi.fn(() => of({ succeeded: true, data: {} })),
      deactivate22: vi.fn(() => of({ succeeded: true, data: {} })),
      clone2: vi.fn(() => of({ succeeded: true, data: {} })),
      previewApplicability: vi.fn(() => of({ succeeded: true, data: {} })),
      meetingProviders: vi.fn(() => of({ succeeded: true, data: [] })),
    };
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolPortalApi);
  });

  it('listInterviewAssessmentPolicies forwards filters to NSwag client', () => {
    api
      .listInterviewAssessmentPolicies('school-1', {
        publicationStatus: 2,
        isActive: true,
      })
      .subscribe();
    expect(client.interviewAssessmentPoliciesGET).toHaveBeenCalledWith(
      'school-1',
      undefined,
      undefined,
      undefined,
      undefined,
      2,
      true,
    );
  });

  it('publishInterviewAssessmentPolicy delegates to publish13', () => {
    api.publishInterviewAssessmentPolicy('school-1', 'p-1').subscribe();
    expect(client.publish13).toHaveBeenCalledWith('school-1', 'p-1');
  });

  it('unpublishInterviewAssessmentPolicy delegates to unpublish11', () => {
    api.unpublishInterviewAssessmentPolicy('school-1', 'p-1').subscribe();
    expect(client.unpublish12).toHaveBeenCalledWith('school-1', 'p-1');
  });

  it('deactivateInterviewAssessmentPolicy delegates to deactivate21', () => {
    api.deactivateInterviewAssessmentPolicy('school-1', 'p-1').subscribe();
    expect(client.deactivate22).toHaveBeenCalledWith('school-1', 'p-1');
  });

  it('cloneInterviewAssessmentPolicy delegates to clone', () => {
    api.cloneInterviewAssessmentPolicy('school-1', 'p-1').subscribe();
    expect(client.clone2).toHaveBeenCalledWith('school-1', 'p-1', undefined);
  });

  it('previewInterviewAssessmentPolicyApplicability delegates to previewApplicability', () => {
    const body = { schoolBranchId: 'b1' };
    api.previewInterviewAssessmentPolicyApplicability('school-1', body).subscribe();
    expect(client.previewApplicability).toHaveBeenCalledWith('school-1', body);
  });

  it('listMeetingProviderOptions delegates to meetingProviders', () => {
    api.listMeetingProviderOptions('school-1').subscribe();
    expect(client.meetingProviders).toHaveBeenCalledWith('school-1');
  });
});

describe('SchoolPortalApi interview FAQs', () => {
  let api: SchoolPortalApi;
  let client: {
    interviewFaqsGET: ReturnType<typeof vi.fn>;
    interviewFaqsPOST: ReturnType<typeof vi.fn>;
    reorder5: ReturnType<typeof vi.fn>;
    publish14: ReturnType<typeof vi.fn>;
    unpublish13: ReturnType<typeof vi.fn>;
    activate10: ReturnType<typeof vi.fn>;
    deactivate23: ReturnType<typeof vi.fn>;
    activate9: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    client = {
      interviewFaqsGET: vi.fn(() => of({ succeeded: true, data: [] })),
      interviewFaqsPOST: vi.fn(() => of({ succeeded: true, data: {} })),
      reorder5: vi.fn(() => of({ succeeded: true, data: [] })),
      publish14: vi.fn(() => of({ succeeded: true, data: {} })),
      unpublish13: vi.fn(() => of({ succeeded: true, data: {} })),
      activate10: vi.fn(() => of({ succeeded: true, data: {} })),
      deactivate23: vi.fn(() => of({ succeeded: true, data: {} })),
      activate9: vi.fn(() => of({ succeeded: true, data: {} })),
    };
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolPortalApi);
  });

  it('listInterviewFaqs forwards filters to interviewFaqsGET', () => {
    api
      .listInterviewFaqs('school-1', {
        interviewCategory: 1,
        isPublished: true,
        isActive: true,
      })
      .subscribe();
    expect(client.interviewFaqsGET).toHaveBeenCalledWith(
      'school-1',
      1,
      undefined,
      undefined,
      undefined,
      undefined,
      true,
      true,
    );
  });

  it('reorderInterviewFaqs delegates to reorder5', () => {
    api.reorderInterviewFaqs('school-1', { orderedIds: ['a', 'b'] }).subscribe();
    expect(client.reorder5).toHaveBeenCalledWith('school-1', { orderedIds: ['a', 'b'] });
  });

  it('publishInterviewFaq delegates to publish14', () => {
    api.publishInterviewFaq('school-1', 'f-1').subscribe();
    expect(client.publish14).toHaveBeenCalledWith('school-1', 'f-1');
  });

  it('unpublishInterviewFaq delegates to unpublish12', () => {
    api.unpublishInterviewFaq('school-1', 'f-1').subscribe();
    expect(client.unpublish13).toHaveBeenCalledWith('school-1', 'f-1');
  });

  it('activateInterviewFaq delegates to activate10', () => {
    api.activateInterviewFaq('school-1', 'f-1').subscribe();
    expect(client.activate10).toHaveBeenCalledWith('school-1', 'f-1');
  });

  it('deactivateInterviewFaq delegates to deactivate22', () => {
    api.deactivateInterviewFaq('school-1', 'f-1').subscribe();
    expect(client.deactivate23).toHaveBeenCalledWith('school-1', 'f-1');
  });

  it('activateService still delegates to activate9 after FAQ regen', () => {
    api.activateService('school-1', 'svc-1').subscribe();
    expect(client.activate9).toHaveBeenCalledWith('school-1', 'svc-1');
  });
});

describe('SchoolPortalApi interview assessment slots', () => {
  const client = {
    interviewAssessmentSlotsGET: vi.fn(() => of({ succeeded: true, data: [] })),
    interviewAssessmentSlotsGET2: vi.fn(() => of({ succeeded: true, data: {} })),
    interviewAssessmentSlotsPOST: vi.fn(() => of({ succeeded: true, data: {} })),
    interviewAssessmentSlotsPUT: vi.fn(() => of({ succeeded: true, data: {} })),
    open: vi.fn(() => of({ succeeded: true, data: {} })),
    close2: vi.fn(() => of({ succeeded: true, data: {} })),
    reopen2: vi.fn(() => of({ succeeded: true, data: {} })),
    cancel3: vi.fn(() => of({ succeeded: true, data: {} })),
    preview2: vi.fn(() => of({ succeeded: true, data: {} })),
    generate: vi.fn(() => of({ succeeded: true, data: {} })),
    audit2: vi.fn(() => of({ succeeded: true, data: [] })),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
  });

  it('forwards slot filters in generated-client order', () => {
    TestBed.inject(SchoolPortalApi).listInterviewAssessmentSlots('school-1', {
      branchId: 'branch-1', stageId: 'stage-1', gradeId: 'grade-1', academicYearId: 'year-1',
      kind: 1, mode: 2, status: 3, resourceId: 'user-1',
      from: '2026-08-01', to: '2026-08-31',
    }).subscribe();
    expect(client.interviewAssessmentSlotsGET).toHaveBeenCalledWith(
      'school-1', 'branch-1', 'stage-1', 'grade-1', 'year-1', 1, 2, 3,
      'user-1', '2026-08-01', '2026-08-31',
    );
  });

  it('delegates CRUD, lifecycle, recurrence, and audit to exact generated methods', () => {
    const api = TestBed.inject(SchoolPortalApi);
    const body = { schoolBranchId: 'branch-1' };
    api.getInterviewAssessmentSlot('school-1', 'slot-1').subscribe();
    api.createInterviewAssessmentSlot('school-1', body).subscribe();
    api.updateInterviewAssessmentSlot('school-1', 'slot-1', body).subscribe();
    api.openInterviewAssessmentSlot('school-1', 'slot-1', 'rv').subscribe();
    api.closeInterviewAssessmentSlot('school-1', 'slot-1', 'rv').subscribe();
    api.reopenInterviewAssessmentSlot('school-1', 'slot-1', 'rv').subscribe();
    api.cancelInterviewAssessmentSlot('school-1', 'slot-1', { cancellationReasonAr: 'سبب' }).subscribe();
    api.previewInterviewAssessmentSlotRecurrence('school-1', body).subscribe();
    api.generateInterviewAssessmentSlotRecurrence('school-1', body).subscribe();
    api.getInterviewAssessmentSlotAudit('school-1', 'slot-1').subscribe();

    expect(client.interviewAssessmentSlotsGET2).toHaveBeenCalledWith('school-1', 'slot-1');
    expect(client.interviewAssessmentSlotsPOST).toHaveBeenCalledWith('school-1', body);
    expect(client.interviewAssessmentSlotsPUT).toHaveBeenCalledWith('school-1', 'slot-1', body);
    expect(client.open).toHaveBeenCalledWith('school-1', 'slot-1', 'rv');
    expect(client.close2).toHaveBeenCalledWith('school-1', 'slot-1', 'rv');
    expect(client.reopen2).toHaveBeenCalledWith('school-1', 'slot-1', 'rv');
    expect(client.cancel3).toHaveBeenCalled();
    expect(client.preview2).toHaveBeenCalledWith('school-1', body);
    expect(client.generate).toHaveBeenCalledWith('school-1', body);
    expect(client.audit2).toHaveBeenCalledWith('school-1', 'slot-1');
  });
});

describe('SchoolPortalApi appointment scheduling', () => {
  it('forwards generated slot and idempotency request fields unchanged', () => {
    const client = {
      scheduleInterview: vi.fn(() => of({ succeeded: true, data: {} })),
      rescheduleAssessment: vi.fn(() => of({ succeeded: true, data: {} })),
    };
    TestBed.configureTestingModule({
      providers: [SchoolPortalApi, { provide: Client, useValue: client }],
    });
    const api = TestBed.inject(SchoolPortalApi);
    const body = { slotId: 'slot-1', idempotencyKey: 'key-1', rowVersion: 'rv-1' };
    api.scheduleInterview('school-1', 'app-1', body).subscribe();
    api.rescheduleAssessment('school-1', 'app-1', body).subscribe();
    expect(client.scheduleInterview).toHaveBeenCalledWith('school-1', 'app-1', body);
    expect(client.rescheduleAssessment).toHaveBeenCalledWith('school-1', 'app-1', body);
  });
});
