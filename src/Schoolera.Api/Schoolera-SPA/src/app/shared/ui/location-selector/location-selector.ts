import { Component, DestroyRef, EventEmitter, inject, Input, OnChanges, OnInit, Output, SimpleChanges, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';

import {
  CountryTaxonomyItemDto,
  TaxonomyItemDto,
} from '../../../core/api-client/SwaggerClient.service';
import { TaxonomiesApi } from '../../../features/taxonomies/data-access/taxonomies.api';

export interface LocationSelection {
  countryId?: string;
  governorateId?: string;
  cityId?: string;
  districtId?: string;
}

type LoadState = 'idle' | 'loading' | 'loaded' | 'error';

@Component({
  selector: 'se-location-selector',
  imports: [FormsModule, TranslocoPipe],
  templateUrl: './location-selector.html',
  styleUrl: './location-selector.scss',
})
export class LocationSelector implements OnInit, OnChanges {
  private readonly taxonomiesApi = inject(TaxonomiesApi);
  private readonly destroyRef = inject(DestroyRef);
  private initialized = false;
  private governoratesRequest = 0;
  private citiesRequest = 0;
  private districtsRequest = 0;

  @Input() countryId?: string;
  @Input() governorateId?: string;
  @Input() cityId?: string;
  @Input() districtId?: string;
  @Input() showDistrict = true;
  @Input() disabled = false;
  @Input() idPrefix = 'location';

  @Output() readonly selectionChange = new EventEmitter<LocationSelection>();

  protected readonly countries = signal<CountryTaxonomyItemDto[]>([]);
  protected readonly governorates = signal<TaxonomyItemDto[]>([]);
  protected readonly cities = signal<TaxonomyItemDto[]>([]);
  protected readonly districts = signal<TaxonomyItemDto[]>([]);
  protected readonly countriesState = signal<LoadState>('idle');
  protected readonly governoratesState = signal<LoadState>('idle');
  protected readonly citiesState = signal<LoadState>('idle');
  protected readonly districtsState = signal<LoadState>('idle');

  protected selectedCountryId = '';
  protected selectedGovernorateId = '';
  protected selectedCityId = '';
  protected selectedDistrictId = '';

  ngOnInit(): void {
    this.initialized = true;
    this.syncSelectionFromInputs();
    this.loadCountries(true);
    if (this.selectedCountryId) {
      this.loadGovernorates(this.selectedCountryId);
    }
    if (this.selectedGovernorateId) {
      this.loadCities(this.selectedGovernorateId);
    }
    if (this.showDistrict && this.selectedCityId) {
      this.loadDistricts(this.selectedCityId);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) {
      return;
    }

    this.syncSelectionFromInputs();
    if (changes['countryId']) {
      this.loadGovernorates(this.selectedCountryId);
    }
    if (changes['governorateId']) {
      this.loadCities(this.selectedGovernorateId);
    }
    if (changes['cityId'] && this.showDistrict) {
      this.loadDistricts(this.selectedCityId);
    }
  }

  protected itemName(item: CountryTaxonomyItemDto | TaxonomyItemDto): string {
    return item.name ?? item.slug ?? '';
  }

  protected onCountryChange(value: string): void {
    this.selectedCountryId = value;
    this.selectedGovernorateId = '';
    this.selectedCityId = '';
    this.selectedDistrictId = '';
    this.governorates.set([]);
    this.cities.set([]);
    this.districts.set([]);
    this.emitSelection();
    this.loadGovernorates(value);
  }

  protected onGovernorateChange(value: string): void {
    this.selectedGovernorateId = value;
    this.selectedCityId = '';
    this.selectedDistrictId = '';
    this.cities.set([]);
    this.districts.set([]);
    this.emitSelection();
    this.loadCities(value);
  }

  protected onCityChange(value: string): void {
    this.selectedCityId = value;
    this.selectedDistrictId = '';
    this.districts.set([]);
    this.emitSelection();
    if (this.showDistrict) {
      this.loadDistricts(value);
    }
  }

  protected onDistrictChange(value: string): void {
    this.selectedDistrictId = value;
    this.emitSelection();
  }

  protected retryCountries(): void {
    this.loadCountries(false);
  }

  protected retryGovernorates(): void {
    this.loadGovernorates(this.selectedCountryId);
  }

  protected retryCities(): void {
    this.loadCities(this.selectedGovernorateId);
  }

  protected retryDistricts(): void {
    this.loadDistricts(this.selectedCityId);
  }

  private syncSelectionFromInputs(): void {
    this.selectedCountryId = this.countryId ?? '';
    this.selectedGovernorateId = this.governorateId ?? '';
    this.selectedCityId = this.cityId ?? '';
    this.selectedDistrictId = this.districtId ?? '';
  }

  private emitSelection(): void {
    this.selectionChange.emit({
      countryId: this.selectedCountryId || undefined,
      governorateId: this.selectedGovernorateId || undefined,
      cityId: this.selectedCityId || undefined,
      districtId: this.showDistrict ? this.selectedDistrictId || undefined : undefined,
    });
  }

  private loadCountries(defaultEgypt: boolean): void {
    this.countriesState.set('loading');
    this.taxonomiesApi
      .getCountries()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (!result.succeeded) {
            this.countriesState.set('error');
            return;
          }
          const items = result.data ?? [];
          this.countries.set(items);
          this.countriesState.set('loaded');
          if (defaultEgypt && !this.countryId && !this.selectedCountryId) {
            const egypt = items.find((item) => item.code?.toUpperCase() === 'EG');
            if (egypt?.id) {
              this.selectedCountryId = egypt.id;
              this.emitSelection();
              this.loadGovernorates(egypt.id);
            }
          }
        },
        error: () => this.countriesState.set('error'),
      });
  }

  private loadGovernorates(countryId: string): void {
    const request = ++this.governoratesRequest;
    this.governorates.set([]);
    if (!countryId) {
      this.governoratesState.set('idle');
      return;
    }
    this.governoratesState.set('loading');
    this.taxonomiesApi
      .getGovernoratesByCountry(countryId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (request !== this.governoratesRequest) return;
          this.governorates.set(result.succeeded ? (result.data ?? []) : []);
          this.governoratesState.set(result.succeeded ? 'loaded' : 'error');
        },
        error: () => request === this.governoratesRequest && this.governoratesState.set('error'),
      });
  }

  private loadCities(governorateId: string): void {
    const request = ++this.citiesRequest;
    this.cities.set([]);
    if (!governorateId) {
      this.citiesState.set('idle');
      return;
    }
    this.citiesState.set('loading');
    this.taxonomiesApi
      .getCitiesByGovernorate(governorateId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (request !== this.citiesRequest) return;
          this.cities.set(result.succeeded ? (result.data ?? []) : []);
          this.citiesState.set(result.succeeded ? 'loaded' : 'error');
        },
        error: () => request === this.citiesRequest && this.citiesState.set('error'),
      });
  }

  private loadDistricts(cityId: string): void {
    const request = ++this.districtsRequest;
    this.districts.set([]);
    if (!cityId) {
      this.districtsState.set('idle');
      return;
    }
    this.districtsState.set('loading');
    this.taxonomiesApi
      .getDistrictsByCity(cityId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (request !== this.districtsRequest) return;
          this.districts.set(result.succeeded ? (result.data ?? []) : []);
          this.districtsState.set(result.succeeded ? 'loaded' : 'error');
        },
        error: () => request === this.districtsRequest && this.districtsState.set('error'),
      });
  }
}
