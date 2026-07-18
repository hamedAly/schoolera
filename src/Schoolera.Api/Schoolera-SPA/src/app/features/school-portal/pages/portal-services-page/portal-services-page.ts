import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolAdditionalServiceDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

@Component({
  selector: 'se-portal-services-page',
  imports: [
    BilingualFieldGroup,
    Button,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-services-page.html',
  styleUrl: './portal-services-page.scss',
})
export class PortalServicesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly services = signal<readonly SchoolAdditionalServiceDto[]>([]);
  protected readonly showForm = signal(false);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly form = this.formBuilder.nonNullable.group({
    nameAr: ['', Validators.required],
    nameEn: [''],
    descriptionAr: [''],
    descriptionEn: [''],
    sortOrder: [0, Validators.required],
  });

  ngOnInit(): void {
    this.loadServices();
  }

  protected serviceName(service: SchoolAdditionalServiceDto): string {
    return localizedBilingualName(service.nameAr ?? '', service.nameEn, this.activeLang());
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
    this.api
      .createService(schoolId, this.form.getRawValue())
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded) {
          this.showForm.set(false);
          this.loadServices();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected toggleActive(service: SchoolAdditionalServiceDto): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId || !service.id) {
      return;
    }

    const request$ = service.isActive
      ? this.api.deactivateService(schoolId, service.id)
      : this.api.activateService(schoolId, service.id);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadServices();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private loadServices(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api.listServices(schoolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.loading.set(false);
      if (result.succeeded && result.data) {
        this.services.set(result.data);
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }
}
