import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export interface FormErrorSummaryItem {
  /** Already-translated message. */
  message: string;
  /** Optional control / field id to focus when the link is activated. */
  fieldId?: string;
}

@Component({
  selector: 'se-form-error-summary',
  imports: [TranslocoPipe],
  templateUrl: './form-error-summary.html',
  styleUrl: './form-error-summary.scss',
})
export class FormErrorSummary {
  readonly title = input<string>();
  readonly errors = input.required<FormErrorSummaryItem[]>();
  readonly fieldFocus = output<string>();

  protected onFieldActivate(fieldId: string | undefined, event: Event): void {
    if (!fieldId) {
      return;
    }

    event.preventDefault();
    this.fieldFocus.emit(fieldId);

    const element = document.getElementById(fieldId);
    if (element instanceof HTMLElement) {
      element.focus();
      element.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }
  }
}
