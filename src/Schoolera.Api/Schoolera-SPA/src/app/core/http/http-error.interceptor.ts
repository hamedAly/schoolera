import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { isHttpRequestCanceled } from './is-http-canceled';

export const httpErrorInterceptor: HttpInterceptorFn = (request, next) => {
  return next(request).pipe(
    catchError((error: unknown) => {
      if (isHttpRequestCanceled(error)) {
        // Expected abort (switchMap, navigation, tab teardown) — not an application fault.
        return throwError(() => error);
      }

      if (error instanceof HttpErrorResponse) {
        // Expected business/validation responses — page handlers already map errorCodes.
        if (error.status >= 400 && error.status < 500) {
          return throwError(() => error);
        }

        console.error('HTTP request failed', error.message);
      }

      return throwError(() => error);
    }),
  );
};
