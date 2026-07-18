import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  AdminOnboardingListItemDto,
  AdminOnboardingListItemDtoPagedResult,
} from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { localizedBilingualName } from '../../../school-portal/utils/localized-name';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-admin-onboarding-list-page',
  imports: [
    DatePipe,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-onboarding-list-page.html',
  styleUrl: './admin-onboarding-list-page.scss',
})
export class AdminOnboardingListPage implements OnInit {
  private readonly api = inject(AdminPlatformApi);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<AdminOnboardingListItemDtoPagedResult | null>(null);
  protected readonly statusFilter = signal('');
  protected readonly pageNumber = signal(1);

  protected readonly items = computed(() => this.page()?.items ?? []);
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly statusOptions = [
    '',
    'Submitted',
    'UnderReview',
    'ChangesRequested',
    'Approved',
    'Rejected',
    'Draft',
  ] as const;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const status = this.statusFilter() || undefined;
    this.api
      .listOnboardingApplications(status, this.pageNumber(), PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.page.set(result.data);
          return;
        }
        this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected onStatusChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.statusFilter.set(value);
    this.pageNumber.set(1);
    this.load();
  }

  protected previousPage(): void {
    if (this.page()?.hasPreviousPage) {
      this.pageNumber.update((n) => n - 1);
      this.load();
    }
  }

  protected nextPage(): void {
    if (this.page()?.hasNextPage) {
      this.pageNumber.update((n) => n + 1);
      this.load();
    }
  }

  protected schoolLabel(item: AdminOnboardingListItemDto): string {
    return localizedBilingualName(
      item.schoolNameAr ?? item.organizationNameAr,
      null,
      this.activeLang(),
    );
  }

  protected statusKey(status: string | undefined): string {
    return status ? `admin.onboardingStatus.${status}` : 'admin.common.unknown';
  }
}
