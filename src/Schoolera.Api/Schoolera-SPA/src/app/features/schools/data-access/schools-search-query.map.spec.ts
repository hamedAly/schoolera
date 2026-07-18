import { describe, expect, it } from 'vitest';
import { convertToParamMap } from '@angular/router';

import {
  parseSchoolsSearchQuery,
  serializeSchoolsSearchQuery,
} from './schools-search-query';

describe('schools-search-query map state', () => {
  it('round-trips view=map and bounding box without updating on missing view', () => {
    const parsed = parseSchoolsSearchQuery(
      convertToParamMap({
        view: 'map',
        northLatitude: '30.123456',
        southLatitude: '30.000001',
        eastLongitude: '31.400009',
        westLongitude: '31.100001',
      }),
    );

    expect(parsed.view).toBe('map');
    expect(parsed.northLatitude).toBe(30.12346);
    expect(parsed.southLatitude).toBe(30);
    expect(parsed.eastLongitude).toBe(31.40001);
    expect(parsed.westLongitude).toBe(31.1);

    const params = serializeSchoolsSearchQuery(parsed);
    expect(params['view']).toBe('map');
    expect(params['northLatitude']).toBe(30.12346);
  });

  it('omits view=list from URL to preserve bookmarked list URLs', () => {
    const params = serializeSchoolsSearchQuery({
      curriculumIds: [],
      facilityIds: [],
      pageNumber: 1,
      pageSize: 12,
      view: 'list',
    });
    expect(params['view']).toBeUndefined();
  });
});
