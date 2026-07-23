import { HttpErrorResponse } from '@angular/common/http';

/**
 * True when an HTTP observable failed because the browser aborted the request
 * (unsubscribe via switchMap/navigation, explicit AbortSignal, etc.).
 * These are expected and must not surface as application errors.
 *
 * Does not treat generic offline/network failures (also status 0) as cancellations.
 */
export function isHttpRequestCanceled(error: unknown): boolean {
  if (error instanceof HttpErrorResponse) {
    return isAbortPayload(error.error) || isAbortMessage(error.message);
  }

  return isAbortPayload(error) || isAbortNamed(error);
}

function isAbortPayload(payload: unknown): boolean {
  if (!payload) {
    return false;
  }

  if (typeof ProgressEvent !== 'undefined' && payload instanceof ProgressEvent) {
    return payload.type === 'abort';
  }

  if (typeof DOMException !== 'undefined' && payload instanceof DOMException) {
    return payload.name === 'AbortError';
  }

  return isAbortNamed(payload);
}

function isAbortNamed(value: unknown): boolean {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const record = value as { name?: unknown; code?: unknown };
  const name = typeof record.name === 'string' ? record.name : '';
  const code = typeof record.code === 'string' ? record.code : '';

  return name === 'AbortError' || code === 'ERR_CANCELED';
}

function isAbortMessage(message: string | null | undefined): boolean {
  if (!message) {
    return false;
  }

  const normalized = message.toLowerCase();
  return (
    normalized.includes('aborterror') ||
    normalized.includes('the user aborted') ||
    normalized.includes('request aborted') ||
    normalized.includes('http request canceled') ||
    normalized.includes('http request cancelled')
  );
}
