import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';

import { componentHasUnsavedApplicationChanges } from './unsaved-application.models';

export const unsavedApplicationGuard: CanDeactivateFn<unknown> = (component) => {
  if (
    !componentHasUnsavedApplicationChanges(component) ||
    !component.hasUnsavedChanges()
  ) {
    return true;
  }

  const transloco = inject(TranslocoService);
  return window.confirm(transloco.translate('parent.applications.unsaved.confirmLeave'));
};
