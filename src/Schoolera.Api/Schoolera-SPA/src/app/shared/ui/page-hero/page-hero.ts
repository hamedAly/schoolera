import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface PageHeroAction {
  readonly label: string;
  readonly route: string;
  readonly variant?: 'primary' | 'secondary';
}

@Component({
  selector: 'se-page-hero',
  imports: [RouterLink],
  templateUrl: './page-hero.html',
  styleUrl: './page-hero.scss',
})
export class PageHero {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly primaryAction = input<PageHeroAction | null>(null);
  readonly secondaryAction = input<PageHeroAction | null>(null);
}
