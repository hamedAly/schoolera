import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { Client } from '../../../core/api-client/SwaggerClient.service';
import { SchoolsApi } from './schools.api';

describe('SchoolsApi admission requirements', () => {
  let api: SchoolsApi;
  let client: { admissionRequirementsGET3: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    client = {
      admissionRequirementsGET3: vi.fn(() =>
        of({
          succeeded: true,
          data: [{ requirementCode: 'birth-cert', name: 'Birth certificate' }],
        }),
      ),
    };
    TestBed.configureTestingModule({
      providers: [SchoolsApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolsApi);
  });

  it('getAdmissionRequirements calls NSwag public endpoint', async () => {
    const result = await firstValueFrom(
      api.getAdmissionRequirements('demo-school', { branchId: 'b1' }),
    );

    expect(client.admissionRequirementsGET3).toHaveBeenCalledWith(
      'demo-school',
      'b1',
      undefined,
      undefined,
      undefined,
    );
    expect(result.succeeded).toBe(true);
    expect(result.data?.length).toBe(1);
  });
});

describe('SchoolsApi interview assessment policy', () => {
  let api: SchoolsApi;
  let client: { interviewAssessmentPolicy: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    client = {
      interviewAssessmentPolicy: vi.fn(() =>
        of({ succeeded: true, data: { requirementMode: 2 } }),
      ),
    };
    TestBed.configureTestingModule({
      providers: [SchoolsApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolsApi);
  });

  it('getInterviewAssessmentPolicy calls NSwag public endpoint', async () => {
    await firstValueFrom(api.getInterviewAssessmentPolicy('demo-school', { gradeId: 'g1' }));
    expect(client.interviewAssessmentPolicy).toHaveBeenCalledWith(
      'demo-school',
      undefined,
      undefined,
      'g1',
      undefined,
    );
  });
});

describe('SchoolsApi interview FAQs', () => {
  let api: SchoolsApi;
  let client: { interviewFaqsGET3: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    client = {
      interviewFaqsGET3: vi.fn(() =>
        of({
          succeeded: true,
          data: [{ id: 'f1', question: 'Q?', answer: '<p>A</p>' }],
        }),
      ),
    };
    TestBed.configureTestingModule({
      providers: [SchoolsApi, { provide: Client, useValue: client }],
    });
    api = TestBed.inject(SchoolsApi);
  });

  it('getInterviewFaqs calls interviewFaqsGET3', async () => {
    const result = await firstValueFrom(
      api.getInterviewFaqs('demo-school', { category: 1 as never, branchId: 'b1' }),
    );

    expect(client.interviewFaqsGET3).toHaveBeenCalledWith(
      'demo-school',
      'b1',
      undefined,
      undefined,
      undefined,
      1,
    );
    expect(result.succeeded).toBe(true);
    expect(result.data?.length).toBe(1);
  });
});
