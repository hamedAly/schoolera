import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';
import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalEmptyState } from '../../../school-portal/components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import {
  ChildGender,
  ChildProfileListItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { translateParentErrorCodes } from '../../data-access/parent-errors';
import { ParentApi } from '../../data-access/parent.api';

@Component({
  selector: 'se-parent-children-list-page',
  imports: [
    ConfirmationDialog,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './parent-children-list-page.html',
  styleUrl: './parent-children-list-page.scss',
})
export class ParentChildrenListPage implements OnInit {
  private readonly api = inject(ParentApi);
  private readonly transloco = inject(TranslocoService);
  private readonly localeFormat = inject(LocaleFormatService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly children = signal<ChildProfileListItemDto[]>([]);
  protected readonly deleteTarget = signal<ChildProfileListItemDto | null>(null);
  protected readonly showDeleteDialog = signal(false);

  ngOnInit(): void {
    this.loadChildren();
  }

  protected retry(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.loadChildren();
  }

  protected genderLabel(gender: ChildGender | undefined): string {
    const key =
      gender === ChildGender._1
        ? 'parent.enums.gender.male'
        : 'parent.enums.gender.female';
    return this.transloco.translate(key);
  }

  protected formatBirthDate(value: string | undefined): string {
    return value ? this.localeFormat.formatDate(value) : '';
  }

  protected openDeleteDialog(child: ChildProfileListItemDto): void {
    this.deleteTarget.set(child);
    this.showDeleteDialog.set(true);
  }

  protected cancelDelete(): void {
    this.showDeleteDialog.set(false);
    this.deleteTarget.set(null);
  }

  protected confirmDelete(): void {
    const child = this.deleteTarget();
    if (!child) {
      return;
    }

    if (!child.id) {
      return;
    }

    const childId = child.id;
    this.deleting.set(true);
    this.api
      .deleteChild(childId)
      .pipe(
        finalize(() => this.deleting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.showDeleteDialog.set(false);
        this.deleteTarget.set(null);

        if (result.succeeded) {
          this.children.update((items) => items.filter((item) => item.id !== childId));
          return;
        }

        this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
      });
  }

  private loadChildren(): void {
    this.api
      .listChildren()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.children.set(result.data);
          return;
        }
        this.errorMessage.set(translateParentErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
