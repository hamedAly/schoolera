import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { HttpLoadingService } from './http-loading.service';

/**
 * Tracks in-flight API HTTP calls for {@link HttpLoadingService}.
 * `finalize` runs on success, failure, and cancellation/unsubscribe.
 */
export const httpLoadingInterceptor: HttpInterceptorFn = (request, next) => {
  // Asset / i18n fetches are not application API work.
  if (!shouldTrack(request.url)) {
    return next(request);
  }

  const loading = inject(HttpLoadingService);
  loading.begin();

  return next(request).pipe(finalize(() => loading.end()));
};

function shouldTrack(url: string): boolean {
  try {
    const path = url.startsWith('http') ? new URL(url).pathname : url;
    return path.includes('/api/') || path.startsWith('/api') || path.startsWith('api/');
  } catch {
    return url.includes('/api');
  }
}
