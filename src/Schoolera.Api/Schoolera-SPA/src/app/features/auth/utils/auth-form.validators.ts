import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

import { requiredTextValidator } from '../../../shared/validators/required-text.validator';

export function passwordMatchValidator(
  passwordKey = 'password',
  confirmKey = 'confirmPassword',
): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const password = control.get(passwordKey)?.value;
    const confirmPassword = control.get(confirmKey)?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}

export const registerFormValidators = {
  firstName: [requiredTextValidator(), Validators.maxLength(100)],
  lastName: [requiredTextValidator(), Validators.maxLength(100)],
  email: [requiredTextValidator(), Validators.email, Validators.maxLength(256)],
  phoneNumber: [requiredTextValidator(), Validators.maxLength(30)],
  password: [requiredTextValidator(), Validators.minLength(8), Validators.maxLength(128)],
  confirmPassword: [requiredTextValidator()],
  termsAccepted: [Validators.requiredTrue],
  privacyAccepted: [Validators.requiredTrue],
};
