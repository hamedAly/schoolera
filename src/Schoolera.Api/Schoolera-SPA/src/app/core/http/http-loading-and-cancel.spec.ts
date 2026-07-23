import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { httpErrorInterceptor } from './http-error.interceptor';
import { httpLoadingInterceptor } from './http-loading.interceptor';
import { HttpLoadingService } from './http-loading.service';
import { switchMapLatestLoading } from './switch-map-latest-loading';

describe('http error and loading cancellation', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let loading: HttpLoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([httpLoadingInterceptor, httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    loading = TestBed.inject(HttpLoadingService);
    loading.reset();
  });

  afterEach(() => {
    httpTesting.verify();
    vi.restoreAllMocks();
  });

  it('does not console.error canceled HTTP requests', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    http.get('/api/schools').subscribe({
      error: () => undefined,
    });

    const request = httpTesting.expectOne('/api/schools');
    request.error(new ProgressEvent('abort'), { status: 0, statusText: 'Unknown Error' });

    expect(consoleError).not.toHaveBeenCalled();
  });

  it('still console.errors real HTTP failures', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    http.get('/api/schools').subscribe({
      error: () => undefined,
    });

    const request = httpTesting.expectOne('/api/schools');
    request.flush(
      { succeeded: false, errors: ['boom'], errorCodes: ['error.unexpected'] },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(consoleError).toHaveBeenCalled();
  });

  it('does not console.error expected HTTP 400 validation responses', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    http.post('/api/auth/register/parent', {}).subscribe({
      error: () => undefined,
    });

    const request = httpTesting.expectOne('/api/auth/register/parent');
    request.flush(
      {
        succeeded: false,
        errors: ['Privacy acceptance is required.'],
        errorCodes: ['legal.privacy_required'],
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(consoleError).not.toHaveBeenCalled();
  });

  it('ref-counts loading so ending a stale request leaves a newer request loading', () => {
    loading.begin();
    loading.begin();
    expect(loading.isLoading()).toBe(true);
    expect(loading.activeRequests()).toBe(2);

    loading.end();
    expect(loading.isLoading()).toBe(true);
    expect(loading.activeRequests()).toBe(1);

    loading.end();
    expect(loading.isLoading()).toBe(false);
  });

  it('tracks API requests through the loading interceptor', () => {
    http.get('/api/schools').subscribe();
    expect(loading.isLoading()).toBe(true);

    const request = httpTesting.expectOne('/api/schools');
    request.flush({ succeeded: true, data: [] });
    expect(loading.isLoading()).toBe(false);
  });
});

describe('switchMapLatestLoading', () => {
  it('does not clear loading when a newer request replaced a stale one', () => {
    const loading = signal(false);
    const source$ = new Subject<string>();
    const results: string[] = [];

    const stale$ = new Subject<string>();
    const latest$ = new Subject<string>();
    let calls = 0;

    source$
      .pipe(
        switchMapLatestLoading(loading, () => {
          calls += 1;
          return calls === 1 ? stale$ : latest$;
        }),
      )
      .subscribe((value) => results.push(value));

    source$.next('a');
    expect(loading()).toBe(true);

    source$.next('b');
    expect(loading()).toBe(true);

    stale$.error(new DOMException('Aborted', 'AbortError'));
    expect(loading()).toBe(true);

    latest$.next('ok');
    latest$.complete();
    expect(results).toEqual(['ok']);
    expect(loading()).toBe(false);
  });

  it('clears loading on real failure of the latest request', () => {
    const loading = signal(false);
    const source$ = new Subject<number>();

    source$
      .pipe(switchMapLatestLoading(loading, () => throwError(() => new Error('fail'))))
      .subscribe({ error: () => undefined });

    source$.next(1);
    expect(loading()).toBe(false);
  });

  it('clears loading on success', () => {
    const loading = signal(false);
    const source$ = new Subject<number>();

    source$.pipe(switchMapLatestLoading(loading, () => of('done'))).subscribe();

    source$.next(1);
    expect(loading()).toBe(false);
  });
});
