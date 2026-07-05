import { Component, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AutofocusDirective } from '../../../../shared/directives/autofocus.directive';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { requiredTextValidator } from '../../../../shared/validators/required-text.validator';
import { CreateSchoolRequest } from '../../data-access/schools.models';

@Component({
  selector: 'se-school-form',
  imports: [AutofocusDirective, Button, FormField, ReactiveFormsModule],
  templateUrl: './school-form.html',
  styleUrl: './school-form.scss',
})
export class SchoolForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly saving = input(false);
  readonly save = output<CreateSchoolRequest>();

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [requiredTextValidator(), Validators.maxLength(160)]],
    code: ['', [requiredTextValidator(), Validators.maxLength(32)]],
    city: ['', [Validators.maxLength(120)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    this.save.emit({
      name: value.name,
      code: value.code,
      city: value.city || undefined,
    });
  }
}
