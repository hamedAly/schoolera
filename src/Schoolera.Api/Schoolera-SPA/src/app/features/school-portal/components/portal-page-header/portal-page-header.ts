import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'se-portal-page-header',
  imports: [TranslocoPipe],
  templateUrl: './portal-page-header.html',
  styleUrl: './portal-page-header.scss',
})
export class PortalPageHeader {
  readonly titleKey = input.required<string>();
  readonly subtitleKey = input<string>();
}
