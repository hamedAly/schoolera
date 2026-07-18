import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { GenderType, SchoolType } from '../../../core/api-client/SwaggerClient.service';
import {
  areSchoolsSearchQueriesEqual,
  clearsPageOnChange,
  countActiveSchoolFilters,
  createDefaultSchoolsSearchQuery,
  parseSchoolsSearchQuery,
  serializeSchoolsSearchQuery,
} from './schools-search-query';

describe('schools-search-query', () => {
  it('parses repeated and comma-separated curriculumIds', () => {
    const parsed = parseSchoolsSearchQuery(
      convertToParamMap({
        curriculumIds: ['id-1', 'id-2,id-3'],
        search: '  international  ',
        countryId: 'country-1',
        governorateId: 'gov-1',
        pageNumber: '2',
        schoolType: '2',
        admissionOpen: 'true',
      }),
    );

    expect(parsed.search).toBe('international');
    expect(parsed.countryId).toBe('country-1');
    expect(parsed.governorateId).toBe('gov-1');
    expect(parsed.pageNumber).toBe(2);
    expect(parsed.schoolType).toBe(SchoolType._2);
    expect(parsed.admissionOpen).toBe(true);
    expect(parsed.curriculumIds).toEqual(['id-1', 'id-2', 'id-3']);
  });

  it('serializes filters into router query params', () => {
    const params = serializeSchoolsSearchQuery({
      search: 'nile',
      countryId: 'country-1',
      governorateId: 'gov-1',
      cityId: 'city-1',
      genderType: GenderType._3,
      curriculumIds: ['cur-1', 'cur-2'],
      facilityIds: ['fac-1'],
      minimumTuition: 10000,
      sort: 'lowest-fee',
      pageNumber: 3,
      pageSize: 12,
    });

    expect(params).toEqual({
      search: 'nile',
      countryId: 'country-1',
      governorateId: 'gov-1',
      cityId: 'city-1',
      genderType: GenderType._3,
      curriculumIds: ['cur-1', 'cur-2'],
      facilityIds: ['fac-1'],
      minimumTuition: 10000,
      sort: 'lowest-fee',
      pageNumber: 3,
    });
  });

  it('detects equivalent query states', () => {
    const first = parseSchoolsSearchQuery(convertToParamMap({ search: 'test', pageSize: '12' }));
    const second = parseSchoolsSearchQuery(convertToParamMap({ search: 'test' }));

    expect(areSchoolsSearchQueriesEqual(first, second)).toBe(true);
  });

  it('counts hierarchy filters and resets paging when they change', () => {
    const previous = { ...createDefaultSchoolsSearchQuery(), pageNumber: 3 };
    const next = {
      ...previous,
      countryId: 'country-1',
      governorateId: 'gov-1',
    };

    expect(countActiveSchoolFilters(next)).toBe(2);
    expect(clearsPageOnChange(previous, next)).toBe(true);
    expect(areSchoolsSearchQueriesEqual(previous, next)).toBe(false);
  });
});
