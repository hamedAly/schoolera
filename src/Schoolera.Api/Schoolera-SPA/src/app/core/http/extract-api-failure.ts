import { HttpErrorResponse } from '@angular/common/http';

import { ApiResult } from './api-result';

/** Normalized failure payload extracted from an API `Result<T>` body or HTTP error. */
export interface ApiFailure {
  errorCodes: string[];
  errors: string[];
  status: number | null;
}

export function emptyApiFailure(): ApiFailure {
  return { errorCodes: [], errors: [], status: null };
}

/**
 * Reads stable `errorCodes` from an `ApiResult`, `HttpErrorResponse` body, or unknown error.
 * Never parse localized `errors` text for branching.
 */
export function extractApiFailure(source: unknown): ApiFailure {
  if (!source) {
    return emptyApiFailure();
  }

  if (source instanceof HttpErrorResponse) {
    return {
      ...readResultBody(source.error),
      status: source.status,
    };
  }

  if (isApiResultLike(source)) {
    return {
      ...readResultBody(source),
      status: null,
    };
  }

  return emptyApiFailure();
}

function readResultBody(body: unknown): Pick<ApiFailure, 'errorCodes' | 'errors'> {
  if (!body || typeof body !== 'object') {
    return { errorCodes: [], errors: [] };
  }

  const record = body as Record<string, unknown>;
  const errorCodes = Array.isArray(record['errorCodes'])
    ? record['errorCodes'].filter((code): code is string => typeof code === 'string')
    : [];
  const errors = Array.isArray(record['errors'])
    ? record['errors'].filter((message): message is string => typeof message === 'string')
    : [];

  return { errorCodes, errors };
}

function isApiResultLike(value: unknown): value is ApiResult<unknown> {
  return (
    !!value &&
    typeof value === 'object' &&
    'succeeded' in value &&
    typeof (value as ApiResult<unknown>).succeeded === 'boolean'
  );
}
