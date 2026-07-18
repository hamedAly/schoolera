import { Injectable, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';

/**
 * Treats Angular route `title` values as Transloco keys (e.g. `titles.home`).
 */
@Injectable()
export class TranslocoTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private titleSubscription?: Subscription;

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const key = this.buildTitle(snapshot);
    this.titleSubscription?.unsubscribe();

    if (!key) {
      return;
    }

    this.titleSubscription = this.transloco.langChanges$
      .pipe(
        switchMap(() => this.transloco.selectTranslate(key)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => this.title.setTitle(value));

    // Ensure the first emission happens even before the next lang change.
    this.transloco
      .selectTranslate(key)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.title.setTitle(value));
  }
}
