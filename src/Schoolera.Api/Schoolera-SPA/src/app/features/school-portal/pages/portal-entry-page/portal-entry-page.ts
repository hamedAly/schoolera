import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { hasAnyRole } from '../../../../core/auth/auth.models';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { Button } from '../../../../shared/ui/button/button';
import { PortalContextService } from '../../data-access/portal-context.service';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { localizedBilingualName } from '../../utils/localized-name';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'se-portal-entry-page',
  imports: [
    Button,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './portal-entry-page.html',
  styleUrl: './portal-entry-page.scss',
})
export class PortalEntryPage implements OnInit {
  private readonly context = inject(PortalContextService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly schools = this.context.accessibleSchools;

  protected readonly showOnboardingCta = computed(() => {
    const user = this.auth.currentUser();
    return !!user && hasAnyRole(user, ['SchoolOwner']);
  });

  ngOnInit(): void {
    this.context
      .loadAccessibleSchools()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded) {
          this.errorMessage.set(
            translatePortalErrorCodes(this.transloco, result.errorCodes),
          );
          return;
        }

        const schools = result.data ?? [];
        if (schools.length === 1 && schools[0].id) {
          void this.router.navigate(['/school', schools[0].id, 'overview']);
        }
      });
  }

  protected schoolLabel(nameAr?: string | null, nameEn?: string | null): string {
    return localizedBilingualName(nameAr, nameEn, this.documentLanguage.activeLang());
  }

  protected openSchool(schoolId?: string | null): void {
    if (!schoolId) {
      return;
    }
    void this.router.navigate(['/school', schoolId, 'overview']);
  }
}
