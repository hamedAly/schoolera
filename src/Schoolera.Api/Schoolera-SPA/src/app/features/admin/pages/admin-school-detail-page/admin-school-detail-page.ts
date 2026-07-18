import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { AdminSchoolDetailDto } from '../../../../core/api-client/SwaggerClient.service';
import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { localizedBilingualName } from '../../../school-portal/utils/localized-name';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

@Component({
  selector: 'se-admin-school-detail-page',
  imports: [
    DatePipe,
    FormsModule,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    RouterLink,
    TranslocoPipe,
  ],
  templateUrl: './admin-school-detail-page.html',
  styleUrl: './admin-school-detail-page.scss',
})
export class AdminSchoolDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AdminPlatformApi);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly statusMessage = signal<string | null>(null);
  protected readonly statusIsError = signal(false);
  protected readonly school = signal<AdminSchoolDetailDto | null>(null);

  protected selectedStatus = '';

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());
  protected readonly statusOptions = ['Draft', 'Published', 'Unpublished', 'Suspended'] as const;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    const schoolId = this.route.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      this.errorMessage.set(this.transloco.translate('admin.errors.generic'));
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.api
      .getSchool(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.school.set(result.data);
          this.selectedStatus = result.data.status ?? 'Draft';
          return;
        }
        this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected localizedName(nameAr?: string | null, nameEn?: string | null): string {
    return localizedBilingualName(nameAr, nameEn, this.activeLang());
  }

  protected statusKey(status: string | undefined): string {
    return status ? `admin.schoolStatus.${status}` : 'admin.common.unknown';
  }

  protected saveStatus(): void {
    const schoolId = this.route.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    this.saving.set(true);
    this.statusMessage.set(null);
    this.statusIsError.set(false);

    this.api
      .updateSchoolStatus(schoolId, { status: this.selectedStatus })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.saving.set(false);
        if (result.succeeded && result.data) {
          this.school.set(result.data);
          this.selectedStatus = result.data.status ?? this.selectedStatus;
          this.statusMessage.set(this.transloco.translate('admin.schoolDetail.statusUpdated'));
          return;
        }
        this.statusMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
        this.statusIsError.set(true);
      });
  }
}
