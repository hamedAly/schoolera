import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type HomeIconName =
  | 'search'
  | 'compare'
  | 'shield'
  | 'chat'
  | 'clock'
  | 'cap'
  | 'sparkle'
  | 'pin'
  | 'arrow';

/**
 * Lightweight inline SVG icon set for the public homepage.
 * Decorative by default (aria-hidden); consistent 24px stroke icons.
 */
@Component({
  selector: 'se-home-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      viewBox="0 0 24 24"
      width="24"
      height="24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.8"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      @switch (name()) {
        @case ('search') {
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.2-3.2" />
        }
        @case ('compare') {
          <path d="M12 3v18M5 8l-3 6h6L5 8ZM19 8l-3 6h6l-3-6ZM4 21h16M8 6h8" />
        }
        @case ('shield') {
          <path d="M12 3l7 3v5c0 4.5-3 7.8-7 9-4-1.2-7-4.5-7-9V6l7-3Z" />
          <path d="m9 12 2 2 4-4" />
        }
        @case ('chat') {
          <path d="M4 5h16v11H8l-4 3V5Z" />
          <path d="M8 9h8M8 12h5" />
        }
        @case ('clock') {
          <circle cx="12" cy="12" r="9" />
          <path d="M12 7v5l3 2" />
        }
        @case ('cap') {
          <path d="M2 9l10-4 10 4-10 4L2 9Z" />
          <path d="M6 11v4c0 1.4 2.7 3 6 3s6-1.6 6-3v-4M22 9v5" />
        }
        @case ('pin') {
          <path d="M12 21c4-4.5 7-7.6 7-11a7 7 0 1 0-14 0c0 3.4 3 6.5 7 11Z" />
          <circle cx="12" cy="10" r="2.5" />
        }
        @case ('arrow') {
          <path d="M5 12h14M13 6l6 6-6 6" />
        }
        @default {
          <path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M18 6l-2.5 2.5M8.5 15.5 6 18" />
        }
      }
    </svg>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        line-height: 0;
      }
      svg {
        width: 100%;
        height: 100%;
      }
    `,
  ],
})
export class HomeIcon {
  readonly name = input.required<HomeIconName>();
}
