import { afterNextRender, Component, DestroyRef, ElementRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { filter } from 'rxjs/operators';

import { PublicFooter } from '../public-footer/public-footer';
import { PublicHeader } from '../public-header/public-header';

@Component({
  selector: 'se-public-layout',
  imports: [PublicHeader, PublicFooter, RouterOutlet, TranslocoPipe],
  templateUrl: './public-layout.html',
  styleUrl: './public-layout.scss',
})
export class PublicLayout {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly main = viewChild<ElementRef<HTMLElement>>('mainContent');

  constructor() {
    afterNextRender(() => {
      this.router.events
        .pipe(
          filter((event) => event instanceof NavigationEnd),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe(() => {
          const main = this.main()?.nativeElement;
          if (!main) {
            return;
          }

          main.focus({ preventScroll: true });
          if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            window.scrollTo({ top: 0, behavior: 'smooth' });
          } else {
            window.scrollTo(0, 0);
          }
        });
    });
  }
}
