import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { hasAnyRole } from './auth.models';
import { sanitizeReturnUrl } from './return-url';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];

  return auth.ensureSession().pipe(
    map(() => {
      const user = auth.currentUser();

      if (!user) {
        const returnUrl = sanitizeReturnUrl(router.url, '');
        return router.createUrlTree(['/auth/login'], {
          queryParams: returnUrl ? { returnUrl } : undefined,
        });
      }

      if (!requiredRoles.length || hasAnyRole(user, requiredRoles)) {
        return true;
      }

      return router.createUrlTree(['/unauthorized']);
    }),
  );
};
