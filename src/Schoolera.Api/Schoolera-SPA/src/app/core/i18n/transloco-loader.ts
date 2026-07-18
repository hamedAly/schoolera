import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

/**
 * Loads root and scoped translation files from the Angular `public/i18n` asset root.
 * Paths: `/i18n/ar.json`, `/i18n/schools/ar.json`, etc.
 */
@Injectable({ providedIn: 'root' })
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(langPath: string) {
    return this.http.get<Translation>(`./i18n/${langPath}.json`);
  }
}
