import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of, switchMap } from 'rxjs';

import { PortalContextService } from '../data-access/portal-context.service';

export const schoolPortalContextGuard: CanActivateFn = (route) => {
  const context = inject(PortalContextService);
  const router = inject(Router);
  const schoolId = route.paramMap.get('schoolId');

  if (!schoolId) {
    return router.createUrlTree(['/school']);
  }

  if (context.schoolsReady() && context.isSchoolAccessible(schoolId)) {
    context.setActiveSchoolId(schoolId);
    return true;
  }

  return context.loadAccessibleSchools().pipe(
    switchMap((result) => {
      if (!result.succeeded || !context.isSchoolAccessible(schoolId)) {
        return of(router.createUrlTree(['/school']));
      }
      context.setActiveSchoolId(schoolId);
      return of(true);
    }),
    map((value) => value),
  );
};
