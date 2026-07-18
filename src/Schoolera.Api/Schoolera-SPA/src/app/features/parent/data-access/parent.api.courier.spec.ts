import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { Client } from '../../../core/api-client/SwaggerClient.service';
import { ParentApi } from './parent.api';

describe('ParentApi Courier wrapper', () => {
  it('delegates owned application availability to the generated method', () => {
    const client = {
      courierAvailability: vi.fn(() => of({ succeeded: true, data: { options: [] } })),
    };
    TestBed.configureTestingModule({
      providers: [
        ParentApi,
        { provide: Client, useValue: client },
        { provide: HttpClient, useValue: {} },
      ],
    });

    TestBed.inject(ParentApi).getOwnedApplicationCourierAvailability('app-1', {
      countryId: 'country-1',
      governorateId: 'governorate-1',
      cityId: 'city-1',
      districtId: 'district-1',
    }).subscribe();

    expect(client.courierAvailability).toHaveBeenCalledWith(
      'app-1',
      'country-1',
      'governorate-1',
      'city-1',
      'district-1',
    );
  });
});
