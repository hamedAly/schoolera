import { Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';

import { FormField } from '../form-field/form-field';

/**
 * Shows Arabic and English content fields together for management forms.
 * Does not hide either language based on the active UI language.
 */
@Component({
  selector: 'se-bilingual-field-group',
  imports: [FormField, ReactiveFormsModule, TranslocoPipe],
  templateUrl: './bilingual-field-group.html',
  styleUrl: './bilingual-field-group.scss',
})
export class BilingualFieldGroup {
  readonly formGroup = input.required<FormGroup>();
  readonly arabicControlName = input.required<string>();
  readonly englishControlName = input.required<string>();
  readonly arabicLabelKey = input('common.bilingual.arabicLabel');
  readonly englishLabelKey = input('common.bilingual.englishLabel');
  readonly multiline = input(false);
  readonly rows = input(4);
}
