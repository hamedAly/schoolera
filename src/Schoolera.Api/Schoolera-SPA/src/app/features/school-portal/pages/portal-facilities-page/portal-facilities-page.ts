import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { Button } from '../../../../shared/ui/button/button';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolFacilityListItemDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { HasUnsavedPortalChanges } from '../../guards/unsaved-changes.models';
import { localizedBilingualName } from '../../utils/localized-name';

@Component({
  selector: 'se-portal-facilities-page',
  imports: [
    Button,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    TranslocoPipe,
  ],
  templateUrl: './portal-facilities-page.html',
  styleUrl: './portal-facilities-page.scss',
})
export class PortalFacilitiesPage implements OnInit, HasUnsavedPortalChanges {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly facilities = signal<readonly SchoolFacilityListItemDto[]>([]);
  protected readonly selectedIds = signal<readonly string[]>([]);
  private readonly baselineIds = signal<readonly string[]>([]);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  ngOnInit(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api
      .getFacilities(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.facilities.set(result.data);
          const selected = result.data
            .filter((f) => f.isSelected && f.facilityId)
            .map((f) => f.facilityId as string);
          this.selectedIds.set(selected);
          this.baselineIds.set([...selected]);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  hasUnsavedChanges(): boolean {
    const current = [...this.selectedIds()].sort().join(',');
    const baseline = [...this.baselineIds()].sort().join(',');
    return current !== baseline;
  }

  protected facilityName(facility: SchoolFacilityListItemDto): string {
    return localizedBilingualName(facility.nameAr ?? '', facility.nameEn, this.activeLang());
  }

  protected isSelected(facilityId?: string | null): boolean {
    return !!facilityId && this.selectedIds().includes(facilityId);
  }

  protected toggle(facilityId?: string | null): void {
    if (!facilityId) {
      return;
    }
    const current = new Set(this.selectedIds());
    if (current.has(facilityId)) {
      current.delete(facilityId);
    } else {
      current.add(facilityId);
    }
    this.selectedIds.set([...current]);
  }

  protected save(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    this.saving.set(true);
    this.api
      .replaceFacilities(schoolId, { facilityIds: [...this.selectedIds()] })
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.baselineIds.set([...this.selectedIds()]);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
