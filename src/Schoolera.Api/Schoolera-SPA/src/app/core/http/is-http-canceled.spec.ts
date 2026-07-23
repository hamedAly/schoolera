import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';

import { isHttpRequestCanceled } from './is-http-canceled';

describe('isHttpRequestCanceled', () => {
  it('classifies AbortError payloads as canceled', () => {
    expect(isHttpRequestCanceled(new DOMException('Aborted', 'AbortError'))).toBe(true);
    expect(
      isHttpRequestCanceled(
        new HttpErrorResponse({
          status: 0,
          error: new DOMException('Aborted', 'AbortError'),
          statusText: 'Unknown Error',
          url: '/api/schools',
        }),
      ),
    ).toBe(true);
  });

  it('classifies abort ProgressEvent as canceled', () => {
    const progress = new ProgressEvent('abort');
    expect(
      isHttpRequestCanceled(
        new HttpErrorResponse({
          status: 0,
          error: progress,
          url: '/api/schools',
        }),
      ),
    ).toBe(true);
  });

  it('does not treat generic network failures as canceled', () => {
    expect(
      isHttpRequestCanceled(
        new HttpErrorResponse({
          status: 0,
          statusText: 'Unknown Error',
          url: '/api/schools',
          error: new ProgressEvent('error'),
        }),
      ),
    ).toBe(false);
  });

  it('does not treat real HTTP errors as canceled', () => {
    expect(
      isHttpRequestCanceled(
        new HttpErrorResponse({
          status: 500,
          statusText: 'Internal Server Error',
          url: '/api/schools',
          error: { succeeded: false, errorCodes: ['error.unexpected'] },
        }),
      ),
    ).toBe(false);

    expect(
      isHttpRequestCanceled(
        new HttpErrorResponse({
          status: 400,
          statusText: 'Bad Request',
          url: '/api/schools',
        }),
      ),
    ).toBe(false);
  });
});
