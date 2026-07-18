import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import {
  provideHttpClient,
  withInterceptors,
  withXsrfConfiguration,
} from '@angular/common/http';
import { provideRouter, TitleStrategy } from '@angular/router';

import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { API_BASE_URL } from './core/api-client/SwaggerClient.service';
import { acceptLanguageInterceptor } from './core/http/accept-language.interceptor';
import { apiCredentialsInterceptor } from './core/http/api-credentials.interceptor';
import { httpErrorInterceptor } from './core/http/http-error.interceptor';
import { provideSchooleraI18n } from './core/i18n/provide-schoolera-i18n';
import { TranslocoTitleStrategy } from './core/i18n/transloco-title.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN',
      }),
      withInterceptors([
        apiCredentialsInterceptor,
        acceptLanguageInterceptor,
        httpErrorInterceptor,
      ]),
    ),
    // Provide the NSwag-generated token (do not create a second API_BASE_URL token).
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
    ...provideSchooleraI18n(),
    { provide: TitleStrategy, useClass: TranslocoTitleStrategy },
  ],
};
