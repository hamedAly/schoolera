import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { Button } from '../../../../shared/ui/button/button';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolGalleryImageDto, SchoolMediaDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';

@Component({
  selector: 'se-portal-gallery-page',
  imports: [
    Button,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    TranslocoPipe,
  ],
  templateUrl: './portal-gallery-page.html',
  styleUrl: './portal-gallery-page.scss',
})
export class PortalGalleryPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly uploading = signal(false);
  protected readonly uploadPercent = signal(0);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly media = signal<SchoolMediaDto | null>(null);

  ngOnInit(): void {
    this.loadMedia();
  }

  protected galleryImages(): readonly SchoolGalleryImageDto[] {
    return this.media()?.galleryImages ?? [];
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    this.uploading.set(true);
    this.uploadPercent.set(0);
    this.errorMessage.set(null);

    this.api
      .uploadGalleryImageWithProgress(schoolId, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (event) => {
          if (event.kind === 'progress') {
            this.uploadPercent.set(event.percent);
            return;
          }
          this.uploading.set(false);
          if (event.result.succeeded) {
            this.loadMedia();
            return;
          }
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, event.result.errorCodes));
        },
        error: () => {
          this.uploading.set(false);
          this.errorMessage.set(this.transloco.translate('portal.errors.generic'));
        },
      });

    input.value = '';
  }

  protected deleteImage(imageId?: string | null): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId || !imageId) {
      return;
    }

    this.api.deleteGalleryImage(schoolId, imageId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadMedia();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private loadMedia(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api
      .getMedia(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.media.set(result.data);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
