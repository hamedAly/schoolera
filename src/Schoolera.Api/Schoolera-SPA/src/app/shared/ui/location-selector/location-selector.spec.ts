import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { TaxonomiesApi } from '../../../features/taxonomies/data-access/taxonomies.api';
import { LocationSelector, LocationSelection } from './location-selector';

describe('LocationSelector', () => {
  let fixture: ComponentFixture<LocationSelector>;
  const api = {
    getCountries: vi.fn(() =>
      of({
        succeeded: true,
        data: [
          { id: 'us', code: 'US', name: 'United States' },
          { id: 'eg', code: 'EG', name: 'Egypt' },
        ],
      }),
    ),
    getGovernoratesByCountry: vi.fn(() =>
      of({ succeeded: true, data: [{ id: 'gov-1', name: 'Cairo' }] }),
    ),
    getCitiesByGovernorate: vi.fn(() =>
      of({ succeeded: true, data: [{ id: 'city-1', name: 'Cairo' }] }),
    ),
    getDistrictsByCity: vi.fn(() =>
      of({ succeeded: true, data: [{ id: 'district-1', name: 'Nasr City' }] }),
    ),
  };

  async function create(inputs: Record<string, unknown> = {}): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [
        LocationSelector,
        TranslocoTestingModule.forRoot({
          langs: {
            en: { schools: { location: {} } },
            ar: { schools: { location: {} } },
          },
          translocoConfig: { availableLangs: ['ar', 'en'], defaultLang: 'en' },
          preloadLangs: true,
        }),
      ],
      providers: [{ provide: TaxonomiesApi, useValue: api }],
    }).compileComponents();
    fixture = TestBed.createComponent(LocationSelector);
    for (const [key, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('defaults to Egypt by code when no country is supplied', async () => {
    await create();

    expect((fixture.nativeElement.querySelector('select') as HTMLSelectElement).value).toBe('eg');
    expect(api.getGovernoratesByCountry).toHaveBeenCalledWith('eg');
  });

  it('clears governorate, city, and district when country changes', async () => {
    await create({
      countryId: 'eg',
      governorateId: 'gov-1',
      cityId: 'city-1',
      districtId: 'district-1',
    });
    const emitted: LocationSelection[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => emitted.push(value));
    const country = fixture.nativeElement.querySelector('select') as HTMLSelectElement;
    country.value = 'us';
    country.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(emitted.at(-1)).toEqual({
      countryId: 'us',
      governorateId: undefined,
      cityId: undefined,
      districtId: undefined,
    });
  });

  it('never requests geolocation on initialization', async () => {
    const getCurrentPosition = vi.fn();
    Object.defineProperty(navigator, 'geolocation', {
      configurable: true,
      value: { getCurrentPosition },
    });

    await create();

    expect(getCurrentPosition).not.toHaveBeenCalled();
  });
});
