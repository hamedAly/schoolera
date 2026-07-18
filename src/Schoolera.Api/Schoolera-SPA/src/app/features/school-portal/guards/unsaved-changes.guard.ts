import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';

import { componentHasUnsavedChanges } from './unsaved-changes.models';

export const unsavedChangesGuard: CanDeactivateFn<unknown> = (component) => {
  if (!componentHasUnsavedChanges(component) || !component.hasUnsavedChanges()) {
    return true;
  }

  const transloco = inject(TranslocoService);
  return window.confirm(transloco.translate('portal.unsaved.confirmLeave'));
};
