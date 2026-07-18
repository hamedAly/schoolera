import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import {
  CmsPublicationStatus,
  HomepageAdminDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { ConfirmationDialog } from '../../../school-portal/components/confirmation-dialog/confirmation-dialog';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { ToastService } from '../../../../shared/ui/toast/toast.service';
import { AdminCmsApi } from '../../data-access/admin-cms.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';

type ConfirmAction = 'publish' | 'unpublish' | null;

@Component({
  selector: 'se-admin-cms-home-page',
  imports: [
    BilingualFieldGroup,
    ConfirmationDialog,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './admin-cms-home-page.html',
  styleUrl: './admin-cms-home-page.scss',
})
export class AdminCmsHomePage implements OnInit {
  private readonly api = inject(AdminCmsApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly content = signal<HomepageAdminDto | null>(null);
  protected readonly confirmAction = signal<ConfirmAction>(null);
  protected readonly confirming = signal(false);
  protected readonly publishedStatus = CmsPublicationStatus._2;

  protected readonly form = this.fb.group({
    heroTitleAr: ['', Validators.required],
    heroTitleEn: ['', Validators.required],
    heroSubtitleAr: [''],
    heroSubtitleEn: [''],
    primaryCtaLabelAr: [''],
    primaryCtaLabelEn: [''],
    primaryCtaUrl: [''],
    secondaryCtaLabelAr: [''],
    secondaryCtaLabelEn: [''],
    secondaryCtaUrl: [''],
    schoolsSectionTitleAr: [''],
    schoolsSectionTitleEn: [''],
    parentJourneyTitleAr: [''],
    parentJourneyTitleEn: [''],
    parentJourneyTextAr: [''],
    parentJourneyTextEn: [''],
    schoolJourneyTitleAr: [''],
    schoolJourneyTitleEn: [''],
    schoolJourneyTextAr: [''],
    schoolJourneyTextEn: [''],
    faqSectionTitleAr: [''],
    faqSectionTitleEn: [''],
    faqSectionSubtitleAr: [''],
    faqSectionSubtitleEn: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getHomepage()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          if (result.succeeded && result.data) {
            this.apply(result.data);
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
    const current = this.content();
    this.saving.set(true);

    this.api
      .updateHomepage({
        ...Object.fromEntries(
          Object.entries(value).map(([k, v]) => [k, typeof v === 'string' ? v.trim() || undefined : v]),
        ),
        rowVersion: current?.rowVersion,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          if (result.succeeded && result.data) {
            this.apply(result.data);
            this.toast.success(this.transloco.translate('admin.cms.home.saved'));
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
    const id = this.content()?.id;
    if (!action || !id) {
      return;
    }

    this.confirming.set(true);
    const request =
      action === 'publish' ? this.api.publishHomepage(id) : this.api.unpublishHomepage(id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.confirming.set(false);
        this.confirmAction.set(null);
        if (result.succeeded && result.data) {
          this.apply(result.data);
          this.toast.success(this.transloco.translate(`admin.cms.home.${action}Success`));
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

  private apply(data: HomepageAdminDto): void {
    this.content.set(data);
    this.form.reset({
      heroTitleAr: data.heroTitleAr ?? '',
      heroTitleEn: data.heroTitleEn ?? '',
      heroSubtitleAr: data.heroSubtitleAr ?? '',
      heroSubtitleEn: data.heroSubtitleEn ?? '',
      primaryCtaLabelAr: data.primaryCtaLabelAr ?? '',
      primaryCtaLabelEn: data.primaryCtaLabelEn ?? '',
      primaryCtaUrl: data.primaryCtaUrl ?? '',
      secondaryCtaLabelAr: data.secondaryCtaLabelAr ?? '',
      secondaryCtaLabelEn: data.secondaryCtaLabelEn ?? '',
      secondaryCtaUrl: data.secondaryCtaUrl ?? '',
      schoolsSectionTitleAr: data.schoolsSectionTitleAr ?? '',
      schoolsSectionTitleEn: data.schoolsSectionTitleEn ?? '',
      parentJourneyTitleAr: data.parentJourneyTitleAr ?? '',
      parentJourneyTitleEn: data.parentJourneyTitleEn ?? '',
      parentJourneyTextAr: data.parentJourneyTextAr ?? '',
      parentJourneyTextEn: data.parentJourneyTextEn ?? '',
      schoolJourneyTitleAr: data.schoolJourneyTitleAr ?? '',
      schoolJourneyTitleEn: data.schoolJourneyTitleEn ?? '',
      schoolJourneyTextAr: data.schoolJourneyTextAr ?? '',
      schoolJourneyTextEn: data.schoolJourneyTextEn ?? '',
      faqSectionTitleAr: data.faqSectionTitleAr ?? '',
      faqSectionTitleEn: data.faqSectionTitleEn ?? '',
      faqSectionSubtitleAr: data.faqSectionSubtitleAr ?? '',
      faqSectionSubtitleEn: data.faqSectionSubtitleEn ?? '',
    });
  }
}
