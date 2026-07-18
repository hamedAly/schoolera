import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { GenderType, SchoolType } from '../../../../core/api-client/SwaggerClient.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { HasUnsavedPortalChanges } from '../../guards/unsaved-changes.models';

@Component({
  selector: 'se-portal-profile-page',
  imports: [
    BilingualFieldGroup,
    Button,
    FormField,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-profile-page.html',
  styleUrl: './portal-profile-page.scss',
})
export class PortalProfilePage implements OnInit, HasUnsavedPortalChanges {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly uploadingMedia = signal<'logo' | 'cover' | null>(null);
  protected readonly uploadPercent = signal(0);
  protected readonly logoUrl = signal<string | null>(null);
  protected readonly coverUrl = signal<string | null>(null);

  protected readonly schoolTypes = [
    { value: SchoolType._1, labelKey: 'portal.enums.schoolType.private' },
    { value: SchoolType._2, labelKey: 'portal.enums.schoolType.international' },
    { value: SchoolType._3, labelKey: 'portal.enums.schoolType.national' },
    { value: SchoolType._4, labelKey: 'portal.enums.schoolType.language' },
  ] as const;

  protected readonly genderTypes = [
    { value: GenderType._1, labelKey: 'portal.enums.genderType.boys' },
    { value: GenderType._2, labelKey: 'portal.enums.genderType.girls' },
    { value: GenderType._3, labelKey: 'portal.enums.genderType.mixed' },
  ] as const;

  protected readonly form = this.formBuilder.nonNullable.group({
    nameAr: ['', Validators.required],
    nameEn: [''],
    shortDescriptionAr: [''],
    shortDescriptionEn: [''],
    fullDescriptionAr: [''],
    fullDescriptionEn: [''],
    schoolType: [SchoolType._1 as SchoolType, Validators.required],
    genderType: [GenderType._3 as GenderType, Validators.required],
    foundedYear: [null as number | null],
    studentCount: [null as number | null],
    publicPhone: [''],
    publicEmail: [''],
    websiteUrl: [''],
    whatsAppNumber: [''],
    seoTitleAr: [''],
    seoTitleEn: [''],
    seoDescriptionAr: [''],
    seoDescriptionEn: [''],
  });

  private readonly baselineJson = signal('');

  ngOnInit(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api
      .getProfile(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (!result.succeeded || !result.data) {
          this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
          return;
        }

        const profile = result.data;
        this.form.patchValue({
          nameAr: profile.nameAr,
          nameEn: profile.nameEn ?? '',
          shortDescriptionAr: profile.shortDescriptionAr ?? '',
          shortDescriptionEn: profile.shortDescriptionEn ?? '',
          fullDescriptionAr: profile.fullDescriptionAr ?? '',
          fullDescriptionEn: profile.fullDescriptionEn ?? '',
          schoolType: profile.schoolType,
          genderType: profile.genderType,
          foundedYear: profile.foundedYear ?? null,
          studentCount: profile.studentCount ?? null,
          publicPhone: profile.publicPhone ?? '',
          publicEmail: profile.publicEmail ?? '',
          websiteUrl: profile.websiteUrl ?? '',
          whatsAppNumber: profile.whatsAppNumber ?? '',
          seoTitleAr: profile.seoTitleAr ?? '',
          seoTitleEn: profile.seoTitleEn ?? '',
          seoDescriptionAr: profile.seoDescriptionAr ?? '',
          seoDescriptionEn: profile.seoDescriptionEn ?? '',
        });
        this.form.markAsPristine();
        this.captureBaseline();
        this.logoUrl.set(profile.logoUrl ?? null);
        this.coverUrl.set(profile.coverUrl ?? null);
      });
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty || JSON.stringify(this.form.getRawValue()) !== this.baselineJson();
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const raw = this.form.getRawValue();
    this.api
      .updateProfile(schoolId, {
        ...raw,
        foundedYear: raw.foundedYear ?? undefined,
        studentCount: raw.studentCount ?? undefined,
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.successMessage.set(this.transloco.translate('portal.profile.saved'));
          this.form.markAsPristine();
          this.captureBaseline();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected onMediaSelected(kind: 'logo' | 'cover', event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    this.uploadingMedia.set(kind);
    this.uploadPercent.set(0);
    const upload$ =
      kind === 'logo'
        ? this.api.uploadLogoWithProgress(schoolId, file)
        : this.api.uploadCoverWithProgress(schoolId, file);

    upload$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (uploadEvent) => {
        if (uploadEvent.kind === 'progress') {
          this.uploadPercent.set(uploadEvent.percent);
          return;
        }
        this.uploadingMedia.set(null);
        if (uploadEvent.result.succeeded && uploadEvent.result.data) {
          this.logoUrl.set(uploadEvent.result.data.logoUrl ?? null);
          this.coverUrl.set(uploadEvent.result.data.coverUrl ?? null);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, uploadEvent.result.errorCodes));
      },
      error: () => {
        this.uploadingMedia.set(null);
        this.errorMessage.set(this.transloco.translate('portal.errors.generic'));
      },
    });

    input.value = '';
  }

  protected deleteMedia(kind: 'logo' | 'cover'): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      return;
    }

    const request$ = kind === 'logo' ? this.api.deleteLogo(schoolId) : this.api.deleteCover(schoolId);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded && result.data) {
        this.logoUrl.set(result.data.logoUrl ?? null);
        this.coverUrl.set(result.data.coverUrl ?? null);
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private captureBaseline(): void {
    this.baselineJson.set(JSON.stringify(this.form.getRawValue()));
  }
}
