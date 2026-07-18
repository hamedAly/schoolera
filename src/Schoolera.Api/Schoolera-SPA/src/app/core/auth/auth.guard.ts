import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { sanitizeReturnUrl } from './return-url';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ensureSession().pipe(
    map(() => {
      if (auth.isSignedIn()) {
        return true;
      }

      const returnUrl = sanitizeReturnUrl(state.url, '');
      return router.createUrlTree(['/auth/login'], {
        queryParams: returnUrl ? { returnUrl } : undefined,
      });
    }),
  );
};
