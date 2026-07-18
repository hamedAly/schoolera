import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

import { schooleraAcceptLanguage } from '../i18n/schoolera-lang';

/**
 * Adds Accept-Language for all HttpClient traffic, including NSwag-generated clients.
 * Do not set this header inside individual feature services.
 */
export const acceptLanguageInterceptor: HttpInterceptorFn = (req, next) => {
  const value = schooleraAcceptLanguage(inject(TranslocoService).getActiveLang());

  return next(
    req.clone({
      setHeaders: {
        'Accept-Language': value,
      },
    }),
  );
};
