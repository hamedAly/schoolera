import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';

import { componentHasUnsavedChildChanges } from './unsaved-child.models';

export const unsavedChildGuard: CanDeactivateFn<unknown> = (component) => {
  if (!componentHasUnsavedChildChanges(component) || !component.hasUnsavedChanges()) {
    return true;
  }

  return window.confirm(
    inject(TranslocoService).translate('parent.childForm.unsaved.confirmLeave'),
  );
};
