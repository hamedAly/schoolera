import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { finalize } from 'rxjs/operators';

import { DocumentLanguageService } from '../../../../core/i18n/document-language.service';
import { TaxonomyItemDto } from '../../../../core/api-client/SwaggerClient.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { Button } from '../../../../shared/ui/button/button';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { PortalEmptyState } from '../../components/portal-empty-state/portal-empty-state';
import { PortalErrorState } from '../../components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../components/portal-page-header/portal-page-header';
import { translatePortalErrorCodes } from '../../data-access/portal-errors';
import { SchoolBranchDto } from '../../data-access/school-portal.models';
import { SchoolPortalApi } from '../../data-access/school-portal.api';
import { localizedBilingualName } from '../../utils/localized-name';

@Component({
  selector: 'se-portal-branches-page',
  imports: [
    BilingualFieldGroup,
    Button,
    FormField,
    PortalEmptyState,
    PortalErrorState,
    PortalLoadingSkeleton,
    PortalPageHeader,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './portal-branches-page.html',
  styleUrl: './portal-branches-page.scss',
})
export class PortalBranchesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SchoolPortalApi);
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly branches = signal<readonly SchoolBranchDto[]>([]);
  protected readonly cities = signal<TaxonomyItemDto[]>([]);
  protected readonly districts = signal<TaxonomyItemDto[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingId = signal<string | null>(null);

  protected readonly activeLang = computed(() => this.documentLanguage.activeLang());

  protected readonly form = this.formBuilder.nonNullable.group({
    nameAr: ['', Validators.required],
    nameEn: [''],
    cityId: ['', Validators.required],
    districtId: ['', Validators.required],
    addressLineAr: [''],
    addressLineEn: [''],
    phone: [''],
    email: [''],
    isMainBranch: [false],
  });

  ngOnInit(): void {
    this.loadBranches();
    this.taxonomiesApi
      .getCities()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.succeeded && result.data) {
          this.cities.set(result.data);
        }
      });

    this.form.controls.cityId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((cityId) => {
        this.form.controls.districtId.setValue('');
        this.districts.set([]);
        if (!cityId) {
          return;
        }
        this.taxonomiesApi.getDistrictsByCity(cityId).subscribe((result) => {
          if (result.succeeded && result.data) {
            this.districts.set(result.data);
          }
        });
      });
  }

  protected branchName(branch: SchoolBranchDto): string {
    return localizedBilingualName(branch.nameAr ?? '', branch.nameEn, this.activeLang());
  }

  protected taxonomyName(item: TaxonomyItemDto): string {
    return item.name ?? '';
  }

  protected openCreate(): void {
    this.editingId.set(null);
    this.form.reset({ isMainBranch: false });
    this.showForm.set(true);
  }

  protected openEdit(branch: SchoolBranchDto): void {
    if (!branch.id) {
      return;
    }
    this.editingId.set(branch.id);
    this.form.patchValue({
      nameAr: branch.nameAr,
      nameEn: branch.nameEn ?? '',
      cityId: branch.cityId,
      districtId: branch.districtId,
      addressLineAr: branch.addressLineAr ?? '',
      addressLineEn: branch.addressLineEn ?? '',
      phone: branch.phone ?? '',
      email: branch.email ?? '',
      isMainBranch: branch.isMainBranch,
    });
    this.showForm.set(true);
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
    const body = this.form.getRawValue();
    const editingId = this.editingId();
    const request$ = editingId
      ? this.api.updateBranch(schoolId, editingId, body)
      : this.api.createBranch(schoolId, body);

    request$
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result.succeeded) {
          this.showForm.set(false);
          this.loadBranches();
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }

  protected toggleActive(branch: SchoolBranchDto): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId || !branch.id) {
      return;
    }

    const request$ = branch.isActive
      ? this.api.deactivateBranch(schoolId, branch.id)
      : this.api.activateBranch(schoolId, branch.id);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      if (result.succeeded) {
        this.loadBranches();
        return;
      }
      this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
    });
  }

  private loadBranches(): void {
    const schoolId = this.route.parent?.snapshot.paramMap.get('schoolId');
    if (!schoolId) {
      this.loading.set(false);
      return;
    }

    this.api
      .listBranches(schoolId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading.set(false);
        if (result.succeeded && result.data) {
          this.branches.set(result.data);
          return;
        }
        this.errorMessage.set(translatePortalErrorCodes(this.transloco, result.errorCodes));
      });
  }
}
