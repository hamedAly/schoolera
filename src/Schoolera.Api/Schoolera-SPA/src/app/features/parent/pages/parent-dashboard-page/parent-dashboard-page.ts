import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AuthService } from '../../../../core/auth/auth.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ParentDashboardDto } from '../../../../core/api-client/SwaggerClient.service';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import { ParentApi } from '../../data-access/parent.api';

@Component({
  selector: 'se-parent-dashboard-page',
  imports: [
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-dashboard-page.html',
  styleUrl: './parent-dashboard-page.scss',
})
export class ParentDashboardPage implements OnInit {
  private readonly api = inject(ParentApi);
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly dashboard = signal<ParentDashboardDto | null>(null);
  protected readonly displayName = computed(() => this.auth.currentUser()?.displayName ?? '');

  protected readonly recentActivities = computed(
    () => this.dashboard()?.recentActivities ?? [],
  );

  ngOnInit(): void {
    this.loadDashboard();
  }

  protected retry(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.loadDashboard();
  }

  protected formatActivityDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '';
  }

  private loadDashboard(): void {
    this.api
      .getDashboard()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.dashboard.set(result.data);
          return;
        }
        this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
