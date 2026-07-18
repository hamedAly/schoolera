import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

import { Button } from '../../../../shared/ui/button/button';

@Component({
  selector: 'se-confirmation-dialog',
  imports: [Button, TranslocoPipe],
  templateUrl: './confirmation-dialog.html',
  styleUrl: './confirmation-dialog.scss',
})
export class ConfirmationDialog {
  readonly open = input(false);
  readonly titleKey = input.required<string>();
  readonly messageKey = input.required<string>();
  readonly confirmKey = input('portal.confirm.confirm');
  readonly cancelKey = input('portal.confirm.cancel');
  readonly confirming = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
