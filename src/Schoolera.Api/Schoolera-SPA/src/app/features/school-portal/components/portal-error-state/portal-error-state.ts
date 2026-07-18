import { Component, input } from '@angular/core';

@Component({
  selector: 'se-portal-error-state',
  templateUrl: './portal-error-state.html',
  styleUrl: './portal-error-state.scss',
})
export class PortalErrorState {
  readonly message = input.required<string>();
}
