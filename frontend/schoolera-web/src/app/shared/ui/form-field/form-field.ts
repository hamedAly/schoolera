import { Component, input } from '@angular/core';

@Component({
  selector: 'se-form-field',
  templateUrl: './form-field.html',
  styleUrl: './form-field.scss',
})
export class FormField {
  readonly label = input.required<string>();
  readonly error = input<string>();
}
