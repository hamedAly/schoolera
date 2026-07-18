import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AdminAdmissionApplicationDetailDto } from '../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { PortalAdmissionStatusBadge } from '../../../school-portal/components/portal-admission-status-badge/portal-admission-status-badge';
import { PortalApplicationTimeline } from '../../../school-portal/components/portal-application-timeline/portal-application-timeline';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { AttachmentList } from '../../../parent/applications/components/attachment-list/attachment-list';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

@Component({
  selector: 'se-admin-application-detail-page',
  imports: [
    AttachmentList,
    PortalAdmissionStatusBadge,
    PortalApplicationTimeline,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-application-detail-page.html',
  styleUrl: './admin-application-detail-page.scss',
})
export class AdminApplicationDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AdminPlatformApi);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<AdminAdmissionApplicationDetailDto | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected formatDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '—';
  }

  private load(): void {
    const applicationId = this.route.snapshot.paramMap.get('applicationId');
    if (!applicationId) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.api
      .getAdmissionApplication(applicationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.detail.set(result.data);
          return;
        }
        this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
