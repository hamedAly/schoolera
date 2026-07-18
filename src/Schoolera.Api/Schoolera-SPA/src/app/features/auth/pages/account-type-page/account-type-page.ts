import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';

@Component({
  selector: 'se-account-type-page',
  imports: [Breadcrumbs, PageHero, PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './account-type-page.html',
  styleUrl: './account-type-page.scss',
})
export class AccountTypePage {}
