import { EnvironmentProviders, Provider } from '@angular/core';
import { provideRouter } from '@angular/router';

import { API_BASE_URL } from '../core/api-client/SwaggerClient.service';

export function provideSchooleraTesting(): Array<EnvironmentProviders | Provider> {
  return [
    provideRouter([]),
    { provide: API_BASE_URL, useValue: '' },
  ];
}
