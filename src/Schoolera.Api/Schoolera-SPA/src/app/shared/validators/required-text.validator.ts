import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function requiredTextValidator(): ValidatorFn {
  return (control: AbstractControl<string | null>): ValidationErrors | null => {
    const value = control.value?.trim();

    return value ? null : { requiredText: true };
  };
}
