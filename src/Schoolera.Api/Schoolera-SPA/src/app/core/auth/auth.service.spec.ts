import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { AuthApi } from '../../features/auth/data-access/auth.api';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  const meMock = vi.fn();
  const loginMock = vi.fn();
  const logoutMock = vi.fn();

  beforeEach(() => {
    meMock.mockReset();
    loginMock.mockReset();
    logoutMock.mockReset();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        AuthService,
        {
          provide: AuthApi,
          useValue: {
            me: meMock,
            login: loginMock,
            logout: logoutMock,
          },
        },
      ],
    });
  });

  it('initializes session from /me', () => {
    meMock.mockReturnValue(
      of({
        succeeded: true,
        data: {
          id: 'user-1',
          displayName: 'Test User',
          email: 'test@example.com',
          roles: ['Parent'],
          accountStatus: 'Active',
          preferredLanguage: 'ar',
          postLoginDestination: '/parent',
        },
        errors: [],
        errorCodes: [],
      }),
    );

    const service = TestBed.inject(AuthService);

    service.initSession().subscribe();

    expect(service.isSignedIn()).toBe(true);
    expect(service.currentUser()?.displayName).toBe('Test User');
    expect(service.sessionReady()).toBe(true);
  });

  it('clears user when /me fails', () => {
    meMock.mockReturnValue(throwError(() => new Error('network')));

    const service = TestBed.inject(AuthService);

    service.initSession().subscribe();

    expect(service.isSignedIn()).toBe(false);
    expect(service.sessionReady()).toBe(true);
  });

  it('sets user on successful login', () => {
    loginMock.mockReturnValue(
      of({
        succeeded: true,
        data: {
          id: 'user-2',
          displayName: 'Parent User',
          email: 'parent@example.com',
          roles: ['Parent'],
          accountStatus: 'Active',
          preferredLanguage: 'en',
          postLoginDestination: '/parent',
        },
        errors: [],
        errorCodes: [],
      }),
    );

    const service = TestBed.inject(AuthService);

    service
      .login({ email: 'parent@example.com', password: 'password123' })
      .subscribe((result) => {
        expect(result.succeeded).toBe(true);
        expect(result.data?.email).toBe('parent@example.com');
      });

    expect(service.isSignedIn()).toBe(true);
  });

  it('clears user on logout', () => {
    logoutMock.mockReturnValue(
      of({
        succeeded: true,
        data: { message: 'ok' },
        errors: [],
        errorCodes: [],
      }),
    );

    const service = TestBed.inject(AuthService);
    service['user'].set({
      id: 'user-1',
      displayName: 'Test',
      email: 'test@example.com',
      phoneNumber: null,
      roles: ['Parent'],
      accountStatus: 'Active',
      preferredLanguage: 'ar',
      postLoginDestination: '/parent',
    });

    service.logout().subscribe();

    expect(service.isSignedIn()).toBe(false);
  });
});
