import { Component, input } from '@angular/core';

@Component({
  selector: 'se-empty-state',
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input<string>();
}
