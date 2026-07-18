import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';

import { BreadcrumbItem, Breadcrumbs } from '../breadcrumbs/breadcrumbs';
import { PageHero, PageHeroAction } from '../page-hero/page-hero';
import { PublicPageContainer } from '../public-page-container/public-page-container';
import { PublicPlaceholderContentKeys } from './public-placeholder.models';

@Component({
  selector: 'se-public-placeholder',
  imports: [Breadcrumbs, PageHero, PublicPageContainer, RouterLink],
  templateUrl: './public-placeholder.html',
  styleUrl: './public-placeholder.scss',
})
export class PublicPlaceholder {
  private readonly transloco = inject(TranslocoService);

  readonly content = input.required<PublicPlaceholderContentKeys>();

  private readonly lang = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  protected readonly title = computed(() => {
    this.lang();
    return this.transloco.translate(this.content().titleKey);
  });

  protected readonly description = computed(() => {
    this.lang();
    return this.transloco.translate(this.content().descriptionKey);
  });

  protected readonly note = computed(() => {
    this.lang();
    return this.transloco.translate('pages.placeholderNote');
  });

  protected readonly backHome = computed(() => {
    this.lang();
    return this.transloco.translate('pages.backHome');
  });

  protected readonly breadcrumbs = computed<readonly BreadcrumbItem[]>(() => {
    this.lang();
    return (this.content().breadcrumbKeys ?? []).map((item) => ({
      label: this.transloco.translate(item.labelKey),
      route: item.route,
    }));
  });

  protected readonly primaryAction = computed<PageHeroAction | null>(() => {
    this.lang();
    const action = this.content().primaryAction;
    return action
      ? { label: this.transloco.translate(action.labelKey), route: action.route, variant: 'primary' }
      : null;
  });

  protected readonly secondaryAction = computed<PageHeroAction | null>(() => {
    this.lang();
    const action = this.content().secondaryAction;
    return action
      ? { label: this.transloco.translate(action.labelKey), route: action.route, variant: 'secondary' }
      : null;
  });
}
