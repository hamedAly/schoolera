import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { ParentFavoritesApi } from '../../data-access/parent-favorites.api';
import {
  FavoriteSchoolListItem,
  FavoriteSchoolPagedResult,
} from '../../data-access/parent-favorites.models';
import { translateParentErrorCodes } from '../../data-access/parent-errors';

const PAGE_SIZE = 20;

@Component({
  selector: 'se-parent-favorites-page',
  imports: [
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-favorites-page.html',
  styleUrl: './parent-favorites-page.scss',
})
export class ParentFavoritesPage implements OnInit {
  private readonly api = inject(ParentFavoritesApi);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly acting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<FavoriteSchoolPagedResult | null>(null);
  protected readonly pageNumber = signal(1);

  protected readonly items = computed(() => this.page()?.items ?? []);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .list(this.pageNumber(), PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.page.set(result.data);
            return;
          }
          this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set(this.transloco.translate('parent.errors.generic'));
        },
      });
  }

  protected displayName(item: FavoriteSchoolListItem): string {
    if (item.isAvailable) {
      return item.school?.name ?? this.transloco.translate('parent.favorites.unnamed');
    }
    return (
      item.unavailable?.displayName ?? this.transloco.translate('parent.favorites.unavailable.title')
    );
  }

  protected schoolLink(item: FavoriteSchoolListItem): string[] | null {
    if (!item.isAvailable || !item.school?.slug) {
      return null;
    }
    return ['/schools', item.school.slug];
  }

  protected remove(item: FavoriteSchoolListItem): void {
    if (!item.schoolId || this.acting()) {
      return;
    }
    this.acting.set(true);
    this.api
      .remove(item.schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.acting.set(false);
          if (result.succeeded) {
            this.toast.success(this.transloco.translate('parent.favorites.removed'));
            this.load();
            return;
          }
          this.toast.error(translateParentErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.acting.set(false);
          this.toast.error(this.transloco.translate('parent.errors.generic'));
        },
      });
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
