import { Component, input } from '@angular/core';

type ButtonType = 'button' | 'submit' | 'reset';
type ButtonVariant = 'primary' | 'secondary';

@Component({
  selector: 'se-button',
  templateUrl: './button.html',
  styleUrl: './button.scss',
})
export class Button {
  readonly type = input<ButtonType>('button');
  readonly variant = input<ButtonVariant>('primary');
  readonly disabled = input(false);
}
