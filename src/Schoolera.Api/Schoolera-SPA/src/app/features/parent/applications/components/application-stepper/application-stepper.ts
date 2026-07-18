import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export interface ApplicationStepDef {
  readonly id: string;
  readonly labelKey: string;
}

@Component({
  selector: 'se-application-stepper',
  imports: [TranslocoPipe],
  templateUrl: './application-stepper.html',
  styleUrl: './application-stepper.scss',
})
export class ApplicationStepper {
  readonly steps = input.required<ReadonlyArray<ApplicationStepDef>>();
  readonly currentStep = input(0);
  readonly maxReachableStep = input(0);
  readonly stepSelect = output<number>();

  protected onSelect(index: number): void {
    if (index <= this.maxReachableStep()) {
      this.stepSelect.emit(index);
    }
  }

  protected onKeydown(event: KeyboardEvent, index: number): void {
    const steps = this.steps();
    if (!steps.length) {
      return;
    }

    let next = index;
    if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
      const rtl = document.documentElement.dir === 'rtl';
      const forward = event.key === 'ArrowRight' ? !rtl : rtl;
      next = forward ? Math.min(index + 1, steps.length - 1) : Math.max(index - 1, 0);
    } else if (event.key === 'Home') {
      next = 0;
    } else if (event.key === 'End') {
      next = steps.length - 1;
    } else {
      return;
    }

    event.preventDefault();
    if (next <= this.maxReachableStep()) {
      this.stepSelect.emit(next);
    }
  }
}
