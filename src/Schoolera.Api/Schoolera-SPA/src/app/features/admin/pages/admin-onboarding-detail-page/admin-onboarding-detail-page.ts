import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AdminOnboardingDetailDto } from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { localizedBilingualName } from '../../../school-portal/utils/localized-name';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

@Component({
  selector: 'se-admin-onboarding-detail-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-onboarding-detail-page.html',
  styleUrl: './admin-onboarding-detail-page.scss',
})
export class AdminOnboardingDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AdminPlatformApi);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly actionLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly actionIsError = signal(false);
  protected readonly detail = signal<AdminOnboardingDetailDto | null>(null);

  protected ownerVisibleReason = '';
  protected internalNote = '';

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());
  protected readonly application = computed(() => this.detail()?.application ?? null);
  protected readonly status = computed(() => this.application()?.status ?? '');

  protected readonly canStartReview = computed(() => this.status() === 'Submitted');
  protected readonly canReview = computed(() => this.status() === 'UnderReview');

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    const applicationId = this.route.snapshot.paramMap.get('applicationId');
    if (!applicationId) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getOnboardingApplication(applicationId)
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

  protected localizedName(nameAr?: string | null, nameEn?: string | null): string {
    return localizedBilingualName(nameAr, nameEn, this.activeLang());
  }

  protected statusKey(status: string | undefined): string {
    return status ? `admin.onboardingStatus.${status}` : 'admin.common.unknown';
  }

  protected startReview(): void {
    this.runAction((id) => this.api.startOnboardingReview(id));
  }

  protected requestChanges(): void {
    this.runAction((id) =>
      this.api.requestOnboardingChanges(id, {
        ownerVisibleReason: this.ownerVisibleReason,
        internalNote: this.internalNote || undefined,
      }),
    );
  }

  protected approve(): void {
    this.runAction((id) =>
      this.api.approveOnboardingApplication(id, {
        internalNote: this.internalNote || undefined,
      }),
    );
  }

  protected reject(): void {
    this.runAction((id) =>
      this.api.rejectOnboardingApplication(id, {
        ownerVisibleReason: this.ownerVisibleReason,
        internalNote: this.internalNote || undefined,
      }),
    );
  }

  private runAction(
    call: (applicationId: string) => ReturnType<AdminPlatformApi['startOnboardingReview']>,
  ): void {
    const applicationId = this.route.snapshot.paramMap.get('applicationId');
    if (!applicationId) {
      return;
    }

    this.actionLoading.set(true);
    this.actionMessage.set(null);
    this.actionIsError.set(false);

    call(applicationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.actionLoading.set(false);
        if (result.succeeded && result.data) {
          this.detail.set(result.data);
          this.actionMessage.set(this.transloco.translate('admin.onboardingDetail.actionSuccess'));
          this.actionIsError.set(false);
          this.ownerVisibleReason = '';
          this.internalNote = '';
          return;
        }
        this.actionMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        this.actionIsError.set(true);
      });
  }
}
