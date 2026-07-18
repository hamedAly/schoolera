import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable, switchMap } from 'rxjs';

import {
  CountryTaxonomyItemDto,
  TaxonomyItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { BilingualFieldGroup } from '../../../../shared/ui/bilingual-field-group/bilingual-field-group';
import { PortalErrorState } from '../../../school-portal/components/portal-error-state/portal-error-state';
import { PortalLoadingSkeleton } from '../../../school-portal/components/portal-loading-skeleton/portal-loading-skeleton';
import { PortalPageHeader } from '../../../school-portal/components/portal-page-header/portal-page-header';
import { TaxonomiesApi } from '../../../taxonomies/data-access/taxonomies.api';
import { translateAdminErrorCodes } from '../../data-access/admin-errors';
import { AdminPlatformApi } from '../../data-access/admin-platform.api';

type TaxonomyTab = 'countries' | 'governorates' | 'cities' | 'curricula';
type PublicTaxonomyResult = {
  succeeded?: boolean;
  data?: Array<CountryTaxonomyItemDto | TaxonomyItemDto>;
  errorCodes?: string[];
};

@Component({
  selector: 'se-admin-taxonomies-page',
  imports: [
    BilingualFieldGroup,
    PortalPageHeader,
    PortalErrorState,
    PortalLoadingSkeleton,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './admin-taxonomies-page.html',
  styleUrl: './admin-taxonomies-page.scss',
})
export class AdminTaxonomiesPage implements OnInit {
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly adminApi = inject(AdminPlatformApi);
  private readonly fb = inject(FormBuilder);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly actionIsError = signal(false);
  protected readonly activeTab = signal<TaxonomyTab>('cities');
  protected readonly countries = signal<CountryTaxonomyItemDto[]>([]);
  protected readonly governorates = signal<TaxonomyItemDto[]>([]);
  protected readonly cities = signal<TaxonomyItemDto[]>([]);
  protected readonly curricula = signal<TaxonomyItemDto[]>([]);

  protected readonly createForm = this.fb.group({
    nameAr: ['', Validators.required],
    nameEn: ['', Validators.required],
    slug: ['', Validators.required],
    sortOrder: [0, Validators.required],
    code: [''],
    countryId: [''],
  });

  ngOnInit(): void {
    this.configureContextValidators();
    this.loadActiveTab();
  }

  protected setTab(tab: TaxonomyTab): void {
    this.activeTab.set(tab);
    this.actionMessage.set(null);
    this.createForm.reset({ nameAr: '', nameEn: '', slug: '', sortOrder: 0, code: '', countryId: '' });
    this.configureContextValidators();
    this.loadActiveTab();
  }

  protected loadActiveTab(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    let request: Observable<PublicTaxonomyResult>;
    if (this.activeTab() === 'countries') {
      request = this.taxonomiesApi.getCountries();
    } else if (this.activeTab() === 'governorates') {
      request = this.taxonomiesApi.getCountries().pipe(
        switchMap((countriesResult) => {
          const countries = countriesResult.data ?? [];
          this.countries.set(countries);
          const countryId =
            this.createForm.controls.countryId.value ||
            countries.find((country) => country.code?.toUpperCase() === 'EG')?.id ||
            countries[0]?.id;
          this.createForm.controls.countryId.setValue(countryId ?? '');
          return countryId
            ? this.taxonomiesApi.getGovernoratesByCountry(countryId)
            : new Observable<PublicTaxonomyResult>((subscriber) => {
                subscriber.next({ succeeded: true, data: [] });
                subscriber.complete();
              });
        }),
      );
    } else {
      request =
        this.activeTab() === 'cities'
          ? this.taxonomiesApi.getCities()
          : this.taxonomiesApi.getCurricula();
    }

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.loading.set(false);
      if (result.succeeded) {
        const items = result.data ?? [];
        if (this.activeTab() === 'countries') {
          this.countries.set(items as CountryTaxonomyItemDto[]);
        } else if (this.activeTab() === 'governorates') {
          this.governorates.set(items as TaxonomyItemDto[]);
        } else if (this.activeTab() === 'cities') {
          this.cities.set(items);
        } else {
          this.curricula.set(items);
        }
        return;
      }
      this.errorMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
    });
  }

  protected createItem(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.saving.set(true);
    this.actionMessage.set(null);
    this.actionIsError.set(false);

    const body = {
      nameAr: value.nameAr ?? '',
      nameEn: value.nameEn ?? '',
      slug: value.slug ?? '',
      sortOrder: Number(value.sortOrder ?? 0),
    };

    const request =
      this.activeTab() === 'countries'
        ? this.adminApi.createCountry({ ...body, code: value.code ?? '' })
        : this.activeTab() === 'governorates'
          ? this.adminApi.createGovernorate({ ...body, countryId: value.countryId ?? '' })
          : this.activeTab() === 'cities'
            ? this.adminApi.createCity(body)
            : this.adminApi.createCurriculum(body);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.saving.set(false);
      if (result.succeeded) {
        this.actionMessage.set(this.transloco.translate('admin.taxonomies.created'));
        this.createForm.reset({
          nameAr: '',
          nameEn: '',
          slug: '',
          sortOrder: 0,
          code: '',
          countryId: value.countryId ?? '',
        });
        this.loadActiveTab();
        return;
      }
      this.actionMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      this.actionIsError.set(true);
    });
  }

  protected deactivateItem(item: TaxonomyItemDto): void {
    if (!item.id) {
      return;
    }

    this.saving.set(true);
    this.actionMessage.set(null);
    this.actionIsError.set(false);

    const request =
      this.activeTab() === 'cities'
        ? this.adminApi.deactivateCity(item.id)
        : this.adminApi.deactivateCurriculum(item.id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result) => {
      this.saving.set(false);
      if (result.succeeded) {
        this.actionMessage.set(this.transloco.translate('admin.taxonomies.deactivated'));
        this.loadActiveTab();
        return;
      }
      this.actionMessage.set(translateAdminErrorCodes(this.transloco, result.errorCodes));
      this.actionIsError.set(true);
    });
  }

  private configureContextValidators(): void {
    const code = this.createForm.controls.code;
    const countryId = this.createForm.controls.countryId;
    code.setValidators(this.activeTab() === 'countries' ? [Validators.required] : []);
    countryId.setValidators(this.activeTab() === 'governorates' ? [Validators.required] : []);
    code.updateValueAndValidity();
    countryId.updateValueAndValidity();
  }
}
