import { AbstractControl, FormGroup } from '@angular/forms';

/** Applies translated server messages onto controls via a `server` error key. */
export function applyServerFieldErrors(
  form: FormGroup,
  fieldMessages: Record<string, string[]>,
): void {
  clearServerFieldErrors(form);

  for (const [field, messages] of Object.entries(fieldMessages)) {
    const control = form.get(field);
    if (!control || !messages.length) {
      continue;
    }

    control.setErrors({
      ...(control.errors ?? {}),
      server: messages[0],
    });
    control.markAsTouched();
  }
}

export function clearServerFieldErrors(form: FormGroup): void {
  for (const control of Object.values(form.controls)) {
    clearServerErrorOnControl(control);
  }
}

function clearServerErrorOnControl(control: AbstractControl): void {
  if (!control.errors?.['server']) {
    return;
  }

  const { server: _removed, ...rest } = control.errors;
  control.setErrors(Object.keys(rest).length ? rest : null);
}

export function serverOrClientFieldError(
  control: AbstractControl | null,
  clientMessage: string | undefined,
): string | undefined {
  if (!control?.touched) {
    return undefined;
  }

  const serverMessage = control.errors?.['server'];
  if (typeof serverMessage === 'string' && serverMessage) {
    return serverMessage;
  }

  return clientMessage;
}
