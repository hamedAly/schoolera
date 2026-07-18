import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Client,
  CountryTaxonomyItemDtoIReadOnlyListResult,
  TaxonomyItemDtoIReadOnlyListResult,
} from '../../../core/api-client/SwaggerClient.service';

@Injectable({
  providedIn: 'root',
})
export class TaxonomiesApi {
  private readonly client = inject(Client);

  getCountries(): Observable<CountryTaxonomyItemDtoIReadOnlyListResult> {
    return this.client.countriesGET();
  }

  getGovernoratesByCountry(countryId: string): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.governoratesGET(countryId);
  }

  getCitiesByGovernorate(governorateId: string): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.citiesGET(governorateId);
  }

  getCities(): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.citiesGET2();
  }

  getDistrictsByCity(cityId: string): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.districtsGET(cityId);
  }

  getCurricula(): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.curriculaGET();
  }

  getEducationalStages(): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.educationalStagesGET();
  }

  getGradesByStage(stageId: string): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.gradesGET(stageId);
  }

  getFacilities(): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.facilitiesGET2();
  }

  getAcademicYears(): Observable<TaxonomyItemDtoIReadOnlyListResult> {
    return this.client.academicYearsGET();
  }
}
