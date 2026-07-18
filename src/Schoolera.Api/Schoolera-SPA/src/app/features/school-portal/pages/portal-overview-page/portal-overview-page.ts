import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalAdmissionStatusBadge } from '../../components/portal-admission-status-badge/portal-admission-status-badge';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { SchoolDashboardDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';

@Component({
  selector: 'se-portal-overview-page',
  imports: [PortalAdmissionStatusBadge, PortalPageHeader, PortalErrorState, PortalLoadingSkeleton, RouterLink, TranslocoPipe],
  templateUrl: './portal-overview-page.html',
  styleUrl: './portal-overview-page.scss',
})
export class PortalOverviewPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly dashboard = signal<SchoolDashboardDto | null>(null);

  protected readonly schoolId = (): string => this.route.parent?.snapshot.paramMap.get('schoolId') ?? '';

  ngOnInit(): void {
    const schoolId = this.schoolId();
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api
      .getDashboard(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.dashboard.set(result.data);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
