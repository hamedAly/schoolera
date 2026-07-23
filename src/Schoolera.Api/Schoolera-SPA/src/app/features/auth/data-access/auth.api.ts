import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  API_BASE_URL,
  CurrentUserDtoResult,
  MessageResultDtoResult,
  RegisterParentCommand,
} from '../../../core/api-client/SwaggerClient.service';
import { ApiResult } from '../../../core/http/api-result';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterResultData {
  userId?: string;
  email?: string;
  accountStatus?: string;
  requiresVerification?: boolean;
  verificationDeliverySucceeded?: boolean;
  verificationDeliveryMode?: string;
  codeExpiresInMinutes?: number;
}

/** School-owner registration body (privacy consent is Parent-only on the API). */
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
  termsAccepted: boolean;
  preferredLanguage: string;
}

export type { RegisterParentCommand };

export interface VerifyRequest {
  email: string;
  code: string;
}

export interface ResendVerificationRequest {
  email: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

/**
 * Feature facade for auth endpoints.
 * Uses JSON POST bodies (backend contract) instead of NSwag query-param stubs.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  me(): Observable<CurrentUserDtoResult> {
    return this.http.get<CurrentUserDtoResult>(this.url('/api/auth/me'));
  }

  login(body: LoginRequest): Observable<CurrentUserDtoResult> {
    return this.http.post<CurrentUserDtoResult>(this.url('/api/auth/login'), body);
  }

  logout(): Observable<MessageResultDtoResult> {
    return this.http.post<MessageResultDtoResult>(this.url('/api/auth/logout'), {});
  }

  registerParent(body: RegisterParentCommand): Observable<ApiResult<RegisterResultData>> {
    return this.http.post<ApiResult<RegisterResultData>>(this.url('/api/auth/register/parent'), body);
  }

  registerSchoolOwner(body: RegisterRequest): Observable<ApiResult<RegisterResultData>> {
    return this.http.post<ApiResult<RegisterResultData>>(this.url('/api/auth/register/school-owner'), body);
  }

  verify(body: VerifyRequest): Observable<MessageResultDtoResult> {
    return this.http.post<MessageResultDtoResult>(this.url('/api/auth/verify'), body);
  }

  resendVerification(body: ResendVerificationRequest): Observable<MessageResultDtoResult> {
    return this.http.post<MessageResultDtoResult>(this.url('/api/auth/resend-verification'), body);
  }

  forgotPassword(body: ForgotPasswordRequest): Observable<MessageResultDtoResult> {
    return this.http.post<MessageResultDtoResult>(this.url('/api/auth/forgot-password'), body);
  }

  resetPassword(body: ResetPasswordRequest): Observable<MessageResultDtoResult> {
    return this.http.post<MessageResultDtoResult>(this.url('/api/auth/reset-password'), body);
  }

  private url(path: string): string {
    return `${this.baseUrl}${path}`;
  }
}
