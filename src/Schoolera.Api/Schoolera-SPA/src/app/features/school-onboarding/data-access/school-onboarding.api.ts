import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  Client,
  MyOnboardingApplicationDtoResult,
  OnboardingDocumentTypesResponseDtoResult,
  SaveAuthorizedRepresentativeCommand,
  SaveOrganizationCommand,
  SavePrimaryBranchCommand,
  SaveSchoolDetailsCommand,
} from '../../../core/api-client/SwaggerClient.service';

export interface OnboardingUploadProgress {
  readonly kind: 'progress';
  readonly percent: number;
}

export interface OnboardingUploadComplete {
  readonly kind: 'complete';
  readonly result: MyOnboardingApplicationDtoResult;
}

export type OnboardingUploadEvent = OnboardingUploadProgress | OnboardingUploadComplete;

/**
 * Feature facade over NSwag school-onboarding endpoints.
 *
 * Upload uses a narrowly scoped HttpClient call so multipart progress events are available;
 * all other operations use the generated {@link Client}. Paths remain relative `/api/...`.
 * Accept-Language and CSRF are handled by global interceptors — do not set them here.
 */
@Injectable({
  providedIn: 'root',
})
export class SchoolOnboardingApi {
  private readonly client = inject(Client);
  private readonly http = inject(HttpClient);

  getMyApplication(): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.me2();
  }

  getDocumentTypes(): Observable<OnboardingDocumentTypesResponseDtoResult> {
    return this.client.documentTypes();
  }

  saveOrganization(body: SaveOrganizationCommand): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.organization(body);
  }

  saveAuthorizedRepresentative(
    body: SaveAuthorizedRepresentativeCommand,
  ): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.authorizedRepresentative(body);
  }

  saveSchoolDetails(body: SaveSchoolDetailsCommand): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.schoolDetails(body);
  }

  savePrimaryBranch(body: SavePrimaryBranchCommand): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.primaryBranch(body);
  }

  deleteDocument(documentId: string): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.documentsDELETE2(documentId);
  }

  submit(): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.submit2();
  }

  resubmit(): Observable<MyOnboardingApplicationDtoResult> {
    return this.client.resubmit();
  }

  /**
   * Multipart upload with progress. Uses HttpClient because the generated NSwag client
   * does not expose upload progress events.
   */
  uploadDocumentWithProgress(
    documentTypeId: string,
    file: File,
  ): Observable<OnboardingUploadEvent> {
    const formData = new FormData();
    formData.append('documentTypeId', documentTypeId);
    formData.append('file', file, file.name);

    return this.http
      .post<MyOnboardingApplicationDtoResult>('/api/school-onboarding/me/documents', formData, {
        reportProgress: true,
        observe: 'events',
      })
      .pipe(map((event) => this.mapUploadEvent(event)));
  }

  downloadDocument(documentId: string): Observable<Blob> {
    return this.http.get(`/api/school-onboarding/me/documents/${documentId}/download`, {
      responseType: 'blob',
    });
  }

  private mapUploadEvent(event: HttpEvent<MyOnboardingApplicationDtoResult>): OnboardingUploadEvent {
    if (event.type === HttpEventType.UploadProgress) {
      const total = event.total ?? 0;
      const percent = total > 0 ? Math.round((100 * event.loaded) / total) : 0;
      return { kind: 'progress', percent };
    }

    if (event.type === HttpEventType.Response) {
      return {
        kind: 'complete',
        result: event.body ?? { succeeded: false, errorCodes: ['onboarding.approvalFailed'] },
      };
    }

    return { kind: 'progress', percent: 0 };
  }
}
