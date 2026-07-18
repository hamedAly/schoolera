import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Sends cookies (session + XSRF) for same-origin API calls.
 */
export const apiCredentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/')) {
    return next(req);
  }

  return next(
    req.clone({
      withCredentials: true,
    }),
  );
};
