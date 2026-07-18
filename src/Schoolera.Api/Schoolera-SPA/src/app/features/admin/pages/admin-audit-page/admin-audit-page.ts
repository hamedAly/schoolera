import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AdminAuditEventDtoPagedResult } from '../../../../core/api-client/SwaggerClient.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-admin-audit-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    TranslocoPipe,
  ],
  templateUrl: './admin-audit-page.html',
  styleUrl: './admin-audit-page.scss',
})
export class AdminAuditPage implements OnInit {
  private readonly api = inject(AdminPlatformApi);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<AdminAuditEventDtoPagedResult | null>(null);
  protected readonly pageNumber = signal(1);

  protected actionFilter = '';
  protected entityTypeFilter = '';

  protected readonly items = computed(() => this.page()?.items ?? []);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .listAuditEvents(
        this.actionFilter.trim() || undefined,
        this.entityTypeFilter.trim() || undefined,
        this.pageNumber(),
        PAGE_SIZE,
      )
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
}
