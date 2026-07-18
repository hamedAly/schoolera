import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Client } from '../../../core/api-client/SwaggerClient.service';
import { AdminPlatformApi } from './admin-platform.api';

describe('AdminPlatformApi', () => {
  let api: AdminPlatformApi;
  let client: { dashboard: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    client = {
      dashboard: vi.fn(() =>
        of({
          succeeded: true,
          data: { totalSchools: 5, admissionsAvailable: false },
        }),
      ),
    };

    TestBed.configureTestingModule({
      providers: [AdminPlatformApi, { provide: Client, useValue: client }],
    });

    api = TestBed.inject(AdminPlatformApi);
  });

  it('getDashboard delegates to generated Client.dashboard()', () => {
    api.getDashboard().subscribe((result) => {
      expect(result.succeeded).toBe(true);
      expect(result.data?.totalSchools).toBe(5);
    });

    expect(client.dashboard).toHaveBeenCalledOnce();
  });
});
