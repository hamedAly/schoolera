import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { sanitizeReturnUrl } from './return-url';

export const guestGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ensureSession().pipe(
    map(() => {
      if (!auth.isSignedIn()) {
        return true;
      }

      const returnUrl = route.queryParamMap.get('returnUrl');
      const destination = sanitizeReturnUrl(
        returnUrl,
        auth.currentUser()?.postLoginDestination ?? '/',
      );

      return router.createUrlTree([destination]);
    }),
  );
};
