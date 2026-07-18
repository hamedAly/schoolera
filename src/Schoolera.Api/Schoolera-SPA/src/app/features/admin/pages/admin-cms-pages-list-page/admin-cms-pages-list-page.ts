import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  CmsPageListItemDto,
  CmsPageListItemDtoPagedResult,
  CmsPublicationStatus,
} from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { localizedBilingualName } from '../../../school-portal/utils/localized-name';
import { AdminCmsApi } from '../../data-access/admin-cms.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-admin-cms-pages-list-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-cms-pages-list-page.html',
  styleUrl: './admin-cms-pages-list-page.scss',
})
export class AdminCmsPagesListPage implements OnInit {
  private readonly api = inject(AdminCmsApi);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<CmsPageListItemDtoPagedResult | null>(null);
  protected readonly pageNumber = signal(1);

  protected searchTerm = '';
  protected statusFilter = '';

  protected readonly items = computed(() => this.page()?.items ?? []);
  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly statusOptions = ['', 'Draft', 'Published', 'Archived'] as const;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listPages(
        this.searchTerm.trim() || undefined,
        this.statusFilter || undefined,
        this.pageNumber(),
        PAGE_SIZE,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.page.set(result.data);
            return;
          }
          this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected applyFilters(): void {
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

  protected titleLabel(item: CmsPageListItemDto): string {
    return localizedBilingualName(item.titleAr, item.titleEn, this.activeLang());
  }

  protected statusKey(status: CmsPublicationStatus | undefined): string {
    switch (status) {
      case CmsPublicationStatus._1:
        return 'admin.cms.status.draft';
      case CmsPublicationStatus._2:
        return 'admin.cms.status.published';
      case CmsPublicationStatus._3:
        return 'admin.cms.status.archived';
      default:
        return 'admin.common.unknown';
    }
  }
}
