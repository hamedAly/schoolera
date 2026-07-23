import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, of, shareReplay, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { ApiResult } from '../http/api-result';
import { CurrentUserDtoResult } from '../api-client/SwaggerClient.service';

import {
  AuthApi,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterParentCommand,
  RegisterRequest,
  ResendVerificationRequest,
  ResetPasswordRequest,
  VerifyRequest,
} from '../../features/auth/data-access/auth.api';
import { AuthUser, mapCurrentUserDto } from './auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly authApi = inject(AuthApi);

  private readonly user = signal<AuthUser | null>(null);
  private readonly sessionInitialized = signal(false);
  private sessionInit$: Observable<void> | null = null;

  readonly currentUser = this.user.asReadonly();
  readonly isSignedIn = computed(() => this.user() !== null);
  readonly sessionReady = this.sessionInitialized.asReadonly();

  initSession(): Observable<void> {
    return this.authApi.me().pipe(
      tap((result) => this.applyMeResult(result)),
      map(() => undefined),
      catchError(() => {
        this.user.set(null);
        this.sessionInitialized.set(true);
        return of(undefined);
      }),
    );
  }

  ensureSession(): Observable<void> {
    if (this.sessionInitialized()) {
      return of(undefined);
    }

    if (!this.sessionInit$) {
      this.sessionInit$ = this.initSession().pipe(shareReplay(1));
    }

    return this.sessionInit$;
  }

  login(request: LoginRequest): Observable<ApiResult<AuthUser>> {
    return this.authApi.login(request).pipe(
      map((result) => {
        if (result.succeeded && result.data) {
          const user = mapCurrentUserDto(result.data);
          if (user) {
            this.user.set(user);
            return { succeeded: true, data: user, errors: [], errorCodes: [] };
          }
        }

        return {
          succeeded: false,
          data: null,
          errors: result.errors ?? [],
          errorCodes: result.errorCodes ?? [],
        };
      }),
    );
  }

  logout(): Observable<void> {
    return this.authApi.logout().pipe(
      tap(() => this.user.set(null)),
      map(() => undefined),
      catchError(() => {
        this.user.set(null);
        return of(undefined);
      }),
    );
  }

  registerParent(request: RegisterParentCommand) {
    return this.authApi.registerParent(request);
  }

  registerSchoolOwner(request: RegisterRequest) {
    return this.authApi.registerSchoolOwner(request);
  }

  verify(request: VerifyRequest) {
    return this.authApi.verify(request);
  }

  resendVerification(request: ResendVerificationRequest) {
    return this.authApi.resendVerification(request);
  }

  forgotPassword(request: ForgotPasswordRequest) {
    return this.authApi.forgotPassword(request);
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.authApi.resetPassword(request);
  }

  private applyMeResult(result: CurrentUserDtoResult): void {
    if (result.succeeded && result.data) {
      this.user.set(mapCurrentUserDto(result.data));
    } else {
      this.user.set(null);
    }

    this.sessionInitialized.set(true);
  }
}
