import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AdmissionApplicationDetailDto } from '../../../../../core/api-client/SwaggerClient.service';
import { LocaleFormatService } from '../../../../../core/i18n/locale-format.service';
import { PortalErrorState } from '../../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdmissionErrorCodes } from '../../../data-access/admission-errors';
import { ParentApi } from '../../../data-access/parent.api';

@Component({
  selector: 'se-application-success-page',
  imports: [
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './application-success-page.html',
  styleUrl: './application-success-page.scss',
})
export class ApplicationSuccessPage implements OnInit {
  private readonly api = inject(ParentApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly detail = signal<AdmissionApplicationDetailDto | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('applicationId');
    if (!id) {
      void this.router.navigate(['/parent/applications']);
      return;
    }

    this.api
      .getAdmissionApplication(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translateAdmissionErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        this.detail.set(result.data);
      });
  }

  protected formatDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDateTime(value) : '—';
  }
}
