import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  CmsPageAdminDto,
  CmsPublicationStatus,
} from '../../../../core/api-client/SwaggerClient.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { AdminCmsApi } from '../../data-access/admin-cms.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';

type ConfirmAction = 'publish' | 'unpublish' | 'archive' | null;

@Component({
  selector: 'se-admin-cms-page-edit-page',
  imports: [
    BilingualFieldGroup,
    ConfirmationDialog,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-cms-page-edit-page.html',
  styleUrl: './admin-cms-page-edit-page.scss',
})
export class AdminCmsPageEditPage implements OnInit {
  private readonly api = inject(AdminCmsApi);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isCreate = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly page = signal<CmsPageAdminDto | null>(null);
  protected readonly confirmAction = signal<ConfirmAction>(null);
  protected readonly confirming = signal(false);
  protected readonly publishedStatus = CmsPublicationStatus._2;
  protected readonly archivedStatus = CmsPublicationStatus._3;

  protected readonly form = this.fb.group({
    slug: ['', Validators.required],
    titleAr: ['', Validators.required],
    titleEn: ['', Validators.required],
    contentAr: ['', Validators.required],
    contentEn: ['', Validators.required],
    metaTitleAr: [''],
    metaTitleEn: [''],
    metaDescriptionAr: [''],
    metaDescriptionEn: [''],
  });

  ngOnInit(): void {
    const pageId = this.route.snapshot.paramMap.get('pageId');
    if (!pageId || pageId === 'new') {
      this.isCreate.set(true);
      this.loading.set(false);
      return;
    }

    this.load(pageId);
  }

  protected load(pageId: string): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getPage(pageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.applyPage(result.data);
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

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);

    if (this.isCreate()) {
      this.api
        .createPage({
          slug: value.slug?.trim(),
          titleAr: value.titleAr?.trim(),
          titleEn: value.titleEn?.trim(),
          contentAr: value.contentAr?.trim(),
          contentEn: value.contentEn?.trim(),
          metaTitleAr: value.metaTitleAr?.trim() || undefined,
          metaTitleEn: value.metaTitleEn?.trim() || undefined,
          metaDescriptionAr: value.metaDescriptionAr?.trim() || undefined,
          metaDescriptionEn: value.metaDescriptionEn?.trim() || undefined,
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (result) => {
            this.saving.set(false);
            if (result.succeeded && result.data?.id) {
              this.toast.success(this.transloco.translate('admin.cms.pageEdit.saved'));
              void this.router.navigate(['/admin/cms/pages', result.data.id]);
              return;
            }
            this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
          },
          error: () => {
            this.saving.set(false);
            this.toast.error(this.transloco.translate('admin.errors.generic'));
          },
        });
      return;
    }

    const current = this.page();
    if (!current?.id) {
      this.saving.set(false);
      return;
    }

    this.api
      .updatePage(current.id, {
        slug: current.isSystemPage ? undefined : value.slug?.trim(),
        titleAr: value.titleAr?.trim(),
        titleEn: value.titleEn?.trim(),
        contentAr: value.contentAr?.trim(),
        contentEn: value.contentEn?.trim(),
        metaTitleAr: value.metaTitleAr?.trim() || undefined,
        metaTitleEn: value.metaTitleEn?.trim() || undefined,
        metaDescriptionAr: value.metaDescriptionAr?.trim() || undefined,
        metaDescriptionEn: value.metaDescriptionEn?.trim() || undefined,
        rowVersion: current.rowVersion,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          if (result.succeeded && result.data) {
            this.applyPage(result.data);
            this.toast.success(this.transloco.translate('admin.cms.pageEdit.saved'));
            return;
          }
          this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
        },
        error: () => {
          this.saving.set(false);
          this.toast.error(this.transloco.translate('admin.errors.generic'));
        },
      });
  }

  protected requestAction(action: ConfirmAction): void {
    this.confirmAction.set(action);
  }

  protected cancelConfirm(): void {
    this.confirmAction.set(null);
  }

  protected confirmPending(): void {
    const action = this.confirmAction();
    const id = this.page()?.id;
    if (!action || !id) {
      return;
    }

    this.confirming.set(true);
    const request =
      action === 'publish'
        ? this.api.publishPage(id)
        : action === 'unpublish'
          ? this.api.unpublishPage(id)
          : this.api.archivePage(id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.confirming.set(false);
        this.confirmAction.set(null);
        if (result.succeeded && result.data) {
          this.applyPage(result.data);
          this.toast.success(this.transloco.translate(`admin.cms.pageEdit.${action}Success`));
          return;
        }
        this.toast.error(translateAdminErrorCodes(this.transloco, result.errorCodes));
      },
      error: () => {
        this.confirming.set(false);
        this.confirmAction.set(null);
        this.toast.error(this.transloco.translate('admin.errors.generic'));
      },
    });
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

  protected confirmTitleKey(): string {
    const action = this.confirmAction();
    return action ? `admin.cms.pageEdit.confirm.${action}Title` : 'admin.cms.pageEdit.confirm.publishTitle';
  }

  protected confirmMessageKey(): string {
    const action = this.confirmAction();
    return action
      ? `admin.cms.pageEdit.confirm.${action}Message`
      : 'admin.cms.pageEdit.confirm.publishMessage';
  }

  private applyPage(page: CmsPageAdminDto): void {
    this.page.set(page);
    this.form.reset({
      slug: page.slug ?? '',
      titleAr: page.titleAr ?? '',
      titleEn: page.titleEn ?? '',
      contentAr: page.contentAr ?? '',
      contentEn: page.contentEn ?? '',
      metaTitleAr: page.metaTitleAr ?? '',
      metaTitleEn: page.metaTitleEn ?? '',
      metaDescriptionAr: page.metaDescriptionAr ?? '',
      metaDescriptionEn: page.metaDescriptionEn ?? '',
    });

    if (page.isSystemPage) {
      this.form.controls.slug.disable({ emitEvent: false });
    } else {
      this.form.controls.slug.enable({ emitEvent: false });
    }
  }
}
