import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';

@Component({
  selector: 'se-unauthorized-page',
  imports: [Breadcrumbs, PageHero, PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './unauthorized-page.html',
  styleUrl: './unauthorized-page.scss',
})
export class UnauthorizedPage {
  protected readonly auth = inject(AuthService);
}
