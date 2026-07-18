import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class FeatureFlagsService {
  readonly favoritesEnabled = environment.features.favoritesEnabled;
  readonly admissionsEnabled = environment.features.admissionsEnabled;
}
