import { Component, input } from '@angular/core';

@Component({
  selector: 'se-page-header',
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
})
export class PageHeader {
  readonly eyebrow = input('Schoolera');
  readonly title = input.required<string>();
  readonly description = input<string>();
}
