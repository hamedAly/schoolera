import { HttpEventType } from '@angular/common/http';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Client } from '../../../core/api-client/SwaggerClient.service';
import { ParentApi } from './parent.api';

describe('ParentApi child documents', () => {
  let api: ParentApi;
  let http: HttpTestingController;
  let client: {
    documentsGET: ReturnType<typeof vi.fn>;
    documentsDELETE: ReturnType<typeof vi.fn>;
    fromVault: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    client = {
      documentsGET: vi.fn(() => of({ succeeded: true, data: [] })),
      documentsDELETE: vi.fn(() => of({ succeeded: true, data: true })),
      fromVault: vi.fn(() => of({ succeeded: true, data: { id: 'app-1' } })),
    };
    TestBed.configureTestingModule({
      providers: [
        ParentApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Client, useValue: client },
      ],
    });
    api = TestBed.inject(ParentApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('wraps list, delete, and copy-from-vault generated methods', () => {
    api.listChildDocuments('child-1').subscribe();
    api.deleteChildDocument('child-1', 'doc-1').subscribe();
    api.copyFromVault('app-1', { childDocumentId: 'doc-1' }).subscribe();

    expect(client.documentsGET).toHaveBeenCalledWith('child-1');
    expect(client.documentsDELETE).toHaveBeenCalledWith('child-1', 'doc-1');
    expect(client.fromVault).toHaveBeenCalledWith('app-1', { childDocumentId: 'doc-1' });
  });

  it('uploads a vault document as multipart and reports completion', () => {
    const events: unknown[] = [];
    api
      .uploadChildDocumentWithProgress(
        'child-1',
        new File(['pdf'], 'birth.pdf', { type: 'application/pdf' }),
        1,
      )
      .subscribe((event) => events.push(event));

    const request = http.expectOne('/api/parent/children/child-1/documents');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    request.event({ type: HttpEventType.UploadProgress, loaded: 5, total: 10 });
    request.flush({ succeeded: true, data: { id: 'doc-1' }, errors: [] });

    expect(events).toContainEqual({ kind: 'progress', percent: 50 });
    expect(events).toContainEqual(
      expect.objectContaining({ kind: 'complete', result: expect.objectContaining({ succeeded: true }) }),
    );
  });

  it('uses the authorized blob endpoint for downloads and revokes the URL', () => {
    const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    api.downloadChildDocument('child-1', 'doc-1', 'birth.pdf').subscribe();
    http
      .expectOne('/api/parent/children/child-1/documents/doc-1/download')
      .flush(new Blob(['pdf'], { type: 'application/pdf' }));

    expect(createUrl).toHaveBeenCalled();
    expect(revokeUrl).toHaveBeenCalledWith('blob:test');
  });
});

describe('ParentApi admission questions', () => {
  let api: ParentApi;
  let http: HttpTestingController;
  let client: {
    ensure: ReturnType<typeof vi.fn>;
    answersPUT: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    client = {
      ensure: vi.fn(() => of({ succeeded: true, data: { id: 'app-1' } })),
      answersPUT: vi.fn(() => of({ succeeded: true, data: { id: 'app-1' } })),
    };
    TestBed.configureTestingModule({
      providers: [
        ParentApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Client, useValue: client },
      ],
    });
    api = TestBed.inject(ParentApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('ensureQuestionSnapshots delegates to the generated client', () => {
    api.ensureQuestionSnapshots('app-1').subscribe();
    expect(client.ensure).toHaveBeenCalledWith('app-1');
  });

  it('upsertAdmissionAnswer delegates to the generated client', () => {
    const body = {
      questionSnapshotId: 'snap-1',
      textValue: 'hello',
    };
    api
      .upsertAdmissionAnswer('app-1', body)
      .subscribe();
    expect(client.answersPUT).toHaveBeenCalledWith('app-1', body);
  });
});

describe('ParentApi available interview assessment slots', () => {
  it('delegates read-only availability to the generated client', () => {
    const client = {
      availableSlots: vi.fn(() => of({ succeeded: true, data: [] })),
    };
    TestBed.configureTestingModule({
      providers: [
        ParentApi,
        provideHttpClient(),
        { provide: Client, useValue: client },
      ],
    });
    TestBed.inject(ParentApi).listAvailableInterviewAssessmentSlots('app-1', 2).subscribe();
    expect(client.availableSlots).toHaveBeenCalledWith('app-1', 2);
  });
});

describe('ParentApi appointment journey mutations', () => {
  it('delegates to the exact generated appointment methods', () => {
    const client = {
      confirm: vi.fn(() => of({ succeeded: true, data: {} })),
      selectSlot: vi.fn(() => of({ succeeded: true, data: {} })),
      requestReschedule: vi.fn(() => of({ succeeded: true, data: {} })),
      cancel2: vi.fn(() => of({ succeeded: true, data: {} })),
      join: vi.fn(() => of({ succeeded: true, data: {} })),
    };
    TestBed.configureTestingModule({
      providers: [
        ParentApi,
        provideHttpClient(),
        { provide: Client, useValue: client },
      ],
    });
    const api = TestBed.inject(ParentApi);
    const mutation = { rowVersion: 'rv-1', idempotencyKey: 'key-1' };

    api.confirmAppointment('app-1', 1, mutation).subscribe();
    api.selectAppointmentSlot('app-1', 2, { ...mutation, slotId: 'slot-2' }).subscribe();
    api.requestAppointmentReschedule('app-1', 1, { ...mutation, reason: 'reason' }).subscribe();
    api.cancelAppointment('app-1', 2, { ...mutation, reason: 'cancel' }).subscribe();
    api.joinAppointment('app-1', 1, { rowVersion: 'rv-1' }).subscribe();

    expect(client.confirm).toHaveBeenCalledWith('app-1', 1, mutation);
    expect(client.selectSlot).toHaveBeenCalledWith('app-1', 2, { ...mutation, slotId: 'slot-2' });
    expect(client.requestReschedule).toHaveBeenCalledWith('app-1', 1, { ...mutation, reason: 'reason' });
    expect(client.cancel2).toHaveBeenCalledWith('app-1', 2, { ...mutation, reason: 'cancel' });
    expect(client.join).toHaveBeenCalledWith('app-1', 1, { rowVersion: 'rv-1' });
  });
});
