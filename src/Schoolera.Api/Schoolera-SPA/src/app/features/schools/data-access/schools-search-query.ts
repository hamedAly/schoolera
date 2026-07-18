import { ParamMap, Params } from '@angular/router';

import { GenderType, SchoolType } from '../../../core/api-client/SwaggerClient.service';

export interface SchoolsSearchQuery {
  search?: string;
  countryId?: string;
  governorateId?: string;
  cityId?: string;
  districtId?: string;
  stageId?: string;
  gradeId?: string;
  schoolType?: SchoolType;
  genderType?: GenderType;
  curriculumIds: string[];
  facilityIds: string[];
  minimumTuition?: number;
  maximumTuition?: number;
  academicYearId?: string;
  admissionOpen?: boolean;
  latitude?: number;
  longitude?: number;
  /** list | map — URL-owned view mode */
  view?: 'list' | 'map';
  /** Committed map viewport (Search this area) */
  northLatitude?: number;
  southLatitude?: number;
  eastLongitude?: number;
  westLongitude?: number;
  selectedBranchId?: string;
  sort?: string;
  pageNumber: number;
  pageSize: number;
}

export const SCHOOLS_SEARCH_DEFAULT_PAGE_SIZE = 12;

export const SCHOOLS_SORT_OPTIONS = [
  'relevance',
  'name-asc',
  'name-desc',
  'lowest-fee',
  'highest-fee',
  'newest',
  'nearest',
] as const;

export type SchoolsSortOption = (typeof SCHOOLS_SORT_OPTIONS)[number];

export function createDefaultSchoolsSearchQuery(): SchoolsSearchQuery {
  return {
    curriculumIds: [],
    facilityIds: [],
    pageNumber: 1,
    pageSize: SCHOOLS_SEARCH_DEFAULT_PAGE_SIZE,
  };
}

export function parseSchoolsSearchQuery(paramMap: ParamMap): SchoolsSearchQuery {
  const defaults = createDefaultSchoolsSearchQuery();

  const search = readOptionalString(paramMap.get('search'));
  const countryId = readOptionalString(paramMap.get('countryId'));
  const governorateId = readOptionalString(paramMap.get('governorateId'));
  const cityId = readOptionalString(paramMap.get('cityId'));
  const districtId = readOptionalString(paramMap.get('districtId'));
  const stageId = readOptionalString(paramMap.get('stageId'));
  const gradeId = readOptionalString(paramMap.get('gradeId'));
  const schoolType = readEnum(paramMap.get('schoolType'), SchoolType);
  const genderType = readEnum(paramMap.get('genderType'), GenderType);
  const curriculumIds = readGuidList(paramMap, 'curriculumIds', 'curriculumId');
  const facilityIds = readGuidList(paramMap, 'facilityIds');
  const minimumTuition = readOptionalNumber(paramMap.get('minimumTuition'));
  const maximumTuition = readOptionalNumber(paramMap.get('maximumTuition'));
  const academicYearId = readOptionalString(paramMap.get('academicYearId'));
  const admissionOpen = readOptionalBoolean(paramMap.get('admissionOpen'));
  const latitude = readOptionalNumber(paramMap.get('latitude'));
  const longitude = readOptionalNumber(paramMap.get('longitude'));
  const view = readView(paramMap.get('view'));
  const northLatitude = readOptionalCoordinate(paramMap.get('northLatitude'));
  const southLatitude = readOptionalCoordinate(paramMap.get('southLatitude'));
  const eastLongitude = readOptionalCoordinate(paramMap.get('eastLongitude'));
  const westLongitude = readOptionalCoordinate(paramMap.get('westLongitude'));
  const selectedBranchId = readOptionalString(paramMap.get('selectedBranchId'));
  const sort = readOptionalString(paramMap.get('sort'));
  const pageNumber = readPositiveInt(paramMap.get('pageNumber'), defaults.pageNumber);
  const pageSize = readPositiveInt(paramMap.get('pageSize'), defaults.pageSize);

  return {
    search,
    countryId,
    governorateId,
    cityId,
    districtId,
    stageId,
    gradeId,
    schoolType,
    genderType,
    curriculumIds,
    facilityIds,
    minimumTuition,
    maximumTuition,
    academicYearId,
    admissionOpen,
    latitude,
    longitude,
    view,
    northLatitude,
    southLatitude,
    eastLongitude,
    westLongitude,
    selectedBranchId,
    sort,
    pageNumber,
    pageSize,
  };
}

export function serializeSchoolsSearchQuery(query: SchoolsSearchQuery): Params {
  const params: Params = {};

  setOptional(params, 'search', query.search);
  setOptional(params, 'countryId', query.countryId);
  setOptional(params, 'governorateId', query.governorateId);
  setOptional(params, 'cityId', query.cityId);
  setOptional(params, 'districtId', query.districtId);
  setOptional(params, 'stageId', query.stageId);
  setOptional(params, 'gradeId', query.gradeId);
  setOptional(params, 'schoolType', query.schoolType);
  setOptional(params, 'genderType', query.genderType);
  setGuidList(params, 'curriculumIds', query.curriculumIds);
  setGuidList(params, 'facilityIds', query.facilityIds);
  setOptional(params, 'minimumTuition', query.minimumTuition);
  setOptional(params, 'maximumTuition', query.maximumTuition);
  setOptional(params, 'academicYearId', query.academicYearId);
  setOptionalBoolean(params, 'admissionOpen', query.admissionOpen);
  setOptional(params, 'latitude', query.latitude);
  setOptional(params, 'longitude', query.longitude);
  if (query.view === 'map') {
    params['view'] = 'map';
  }
  setOptionalCoordinate(params, 'northLatitude', query.northLatitude);
  setOptionalCoordinate(params, 'southLatitude', query.southLatitude);
  setOptionalCoordinate(params, 'eastLongitude', query.eastLongitude);
  setOptionalCoordinate(params, 'westLongitude', query.westLongitude);
  setOptional(params, 'selectedBranchId', query.selectedBranchId);
  setOptional(params, 'sort', query.sort);

  if (query.pageNumber !== 1) {
    params['pageNumber'] = query.pageNumber;
  }

  if (query.pageSize !== SCHOOLS_SEARCH_DEFAULT_PAGE_SIZE) {
    params['pageSize'] = query.pageSize;
  }

  return params;
}

export function areSchoolsSearchQueriesEqual(a: SchoolsSearchQuery, b: SchoolsSearchQuery): boolean {
  return (
    a.search === b.search &&
    a.countryId === b.countryId &&
    a.governorateId === b.governorateId &&
    a.cityId === b.cityId &&
    a.districtId === b.districtId &&
    a.stageId === b.stageId &&
    a.gradeId === b.gradeId &&
    a.schoolType === b.schoolType &&
    a.genderType === b.genderType &&
    arraysEqual(a.curriculumIds, b.curriculumIds) &&
    arraysEqual(a.facilityIds, b.facilityIds) &&
    a.minimumTuition === b.minimumTuition &&
    a.maximumTuition === b.maximumTuition &&
    a.academicYearId === b.academicYearId &&
    a.admissionOpen === b.admissionOpen &&
    a.latitude === b.latitude &&
    a.longitude === b.longitude &&
    a.view === b.view &&
    a.northLatitude === b.northLatitude &&
    a.southLatitude === b.southLatitude &&
    a.eastLongitude === b.eastLongitude &&
    a.westLongitude === b.westLongitude &&
    a.selectedBranchId === b.selectedBranchId &&
    a.sort === b.sort &&
    a.pageNumber === b.pageNumber &&
    a.pageSize === b.pageSize
  );
}

export function countActiveSchoolFilters(query: SchoolsSearchQuery): number {
  let count = 0;
  if (query.search?.trim()) count += 1;
  if (query.countryId) count += 1;
  if (query.governorateId) count += 1;
  if (query.cityId) count += 1;
  if (query.districtId) count += 1;
  if (query.stageId) count += 1;
  if (query.gradeId) count += 1;
  if (query.schoolType !== undefined) count += 1;
  if (query.genderType !== undefined) count += 1;
  count += query.curriculumIds.length;
  count += query.facilityIds.length;
  if (query.minimumTuition !== undefined) count += 1;
  if (query.maximumTuition !== undefined) count += 1;
  if (query.academicYearId) count += 1;
  if (query.admissionOpen === true) count += 1;
  if (query.latitude !== undefined && query.longitude !== undefined) count += 1;
  return count;
}

export function clearsPageOnChange(
  previous: SchoolsSearchQuery,
  next: SchoolsSearchQuery,
): boolean {
  return (
    previous.search !== next.search ||
    previous.countryId !== next.countryId ||
    previous.governorateId !== next.governorateId ||
    previous.cityId !== next.cityId ||
    previous.districtId !== next.districtId ||
    previous.stageId !== next.stageId ||
    previous.gradeId !== next.gradeId ||
    previous.schoolType !== next.schoolType ||
    previous.genderType !== next.genderType ||
    !arraysEqual(previous.curriculumIds, next.curriculumIds) ||
    !arraysEqual(previous.facilityIds, next.facilityIds) ||
    previous.minimumTuition !== next.minimumTuition ||
    previous.maximumTuition !== next.maximumTuition ||
    previous.academicYearId !== next.academicYearId ||
    previous.admissionOpen !== next.admissionOpen ||
    previous.latitude !== next.latitude ||
    previous.longitude !== next.longitude ||
    previous.view !== next.view ||
    previous.northLatitude !== next.northLatitude ||
    previous.southLatitude !== next.southLatitude ||
    previous.eastLongitude !== next.eastLongitude ||
    previous.westLongitude !== next.westLongitude ||
    previous.sort !== next.sort ||
    previous.pageSize !== next.pageSize
  );
}

function readView(value: string | null): 'list' | 'map' | undefined {
  const trimmed = value?.trim().toLowerCase();
  if (trimmed === 'map') {
    return 'map';
  }
  if (trimmed === 'list') {
    return 'list';
  }
  return undefined;
}

function readOptionalCoordinate(value: string | null): number | undefined {
  const parsed = readOptionalNumber(value);
  if (parsed === undefined) {
    return undefined;
  }
  return normalizeCoordinate(parsed);
}

function normalizeCoordinate(value: number): number {
  return Math.round(value * 1e5) / 1e5;
}

function setOptionalCoordinate(params: Params, key: string, value: number | undefined): void {
  if (value === undefined) {
    return;
  }
  params[key] = normalizeCoordinate(value);
}

function readOptionalString(value: string | null): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

function readPositiveInt(value: string | null, fallback: number): number {
  if (!value) {
    return fallback;
  }

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function readOptionalNumber(value: string | null): number | undefined {
  if (!value?.trim()) {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function readOptionalBoolean(value: string | null): boolean | undefined {
  if (!value?.trim()) {
    return undefined;
  }

  if (value === 'true' || value === '1') {
    return true;
  }

  if (value === 'false' || value === '0') {
    return false;
  }

  return undefined;
}

function readEnum<T extends Record<string, number | string>>(
  value: string | null,
  enumType: T,
): T[keyof T] | undefined {
  if (!value?.trim()) {
    return undefined;
  }

  const numeric = Number(value);
  if (Number.isFinite(numeric)) {
    const match = Object.values(enumType).find((entry) => entry === numeric);
    if (match !== undefined) {
      return match as T[keyof T];
    }
  }

  return undefined;
}

function readGuidList(paramMap: ParamMap, ...keys: string[]): string[] {
  const values = keys.flatMap((key) => {
    const repeated = paramMap.getAll(key);
    if (repeated.length > 0) {
      return repeated;
    }

    const single = paramMap.get(key);
    return single ? [single] : [];
  });

  const expanded = values.flatMap((value) =>
    value
      .split(',')
      .map((part) => part.trim())
      .filter(Boolean),
  );

  return [...new Set(expanded)];
}

function setOptional(params: Params, key: string, value: string | number | undefined): void {
  if (value === undefined || value === '') {
    return;
  }

  params[key] = value;
}

function setOptionalBoolean(params: Params, key: string, value: boolean | undefined): void {
  if (value === undefined) {
    return;
  }

  params[key] = value ? 'true' : 'false';
}

function setGuidList(params: Params, key: string, values: readonly string[]): void {
  if (values.length === 0) {
    return;
  }

  params[key] = [...values];
}

function arraysEqual(a: readonly string[], b: readonly string[]): boolean {
  if (a.length !== b.length) {
    return false;
  }

  return a.every((value, index) => value === b[index]);
}
