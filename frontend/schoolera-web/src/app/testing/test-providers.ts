import { EnvironmentProviders, Provider } from '@angular/core';
import { provideRouter } from '@angular/router';

import { API_BASE_URL } from '../core/config/environment.token';

export function provideSchooleraTesting(): Array<EnvironmentProviders | Provider> {
  return [
    provideRouter([]),
    { provide: API_BASE_URL, useValue: '/api' },
  ];
}
