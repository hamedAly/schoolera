import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { MyOnboardingApplicationDto } from '../../../../core/api-client/SwaggerClient.service';
import { Breadcrumbs } from '../../../../shared/ui/breadcrumbs/breadcrumbs';
import { Button } from '../../../../shared/ui/button/button';
import { PageHero } from '../../../../shared/ui/page-hero/page-hero';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { translateOnboardingErrorCodes } from '../../data-access/onboarding-errors';
import { SchoolOnboardingApi } from '../../data-access/school-onboarding.api';

@Component({
  selector: 'se-onboarding-status-page',
  imports: [Breadcrumbs, Button, PageHero, PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './onboarding-status-page.html',
  styleUrl: './onboarding-status-page.scss',
})
export class OnboardingStatusPage implements OnInit {
  private readonly api = inject(SchoolOnboardingApi);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly application = signal<MyOnboardingApplicationDto | null>(null);

  protected readonly status = computed(() => this.application()?.status ?? 'Draft');
  protected readonly canEdit = computed(() => {
    const status = this.status();
    return status === 'Draft' || status === 'ChangesRequested';
  });

  ngOnInit(): void {
    this.api
      .getMyApplication()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (!result.succeeded) {
            this.errorMessage.set(translateOnboardingErrorCodes(this.transloco, result.errorCodes));
            return;
          }
          this.application.set(result.data ?? null);
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('onboarding.errors.generic'));
        },
      });
  }

  protected continueOnboarding(): void {
    void this.router.navigate(['/school/onboarding']);
  }

  protected formatDate(value: string | undefined | null): string {
    if (!value) {
      return '';
    }
    try {
      return new Intl.DateTimeFormat(undefined, {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(new Date(value));
    } catch {
      return value;
    }
  }
}
