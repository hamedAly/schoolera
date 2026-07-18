import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { hasAnyRole } from '../../../../core/auth/auth.models';
import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { Button } from '../../../../shared/ui/button/button';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';

@Component({
  selector: 'se-portal-placeholder-page',
  imports: [Breadcrumbs, Button, PageHero, PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './portal-placeholder-page.html',
  styleUrl: './portal-placeholder-page.scss',
})
export class PortalPlaceholderPage {
  protected readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly titleKey = this.route.snapshot.data['titleKey'] as string;
  protected readonly subtitleKey = this.route.snapshot.data['subtitleKey'] as string;
  protected readonly breadcrumbKey = this.route.snapshot.data['breadcrumbKey'] as string;

  protected readonly showSchoolOnboardingCta = computed(() => {
    const user = this.auth.currentUser();
    return !!user && hasAnyRole(user, ['SchoolOwner']) && this.titleKey === 'auth.portals.school.title';
  });
}
