import { Injectable, inject } from '@angular/core';

import { DocumentLanguageService } from './document-language.service';

/**
 * Central locale-aware formatting. Prefer this over concatenating localized sentence fragments.
 */
@Injectable({ providedIn: 'root' })
export class LocaleFormatService {
  private readonly language = inject(DocumentLanguageService);

  formatDate(value: Date | string | number, options?: Intl.DateTimeFormatOptions): string {
    const date = value instanceof Date ? value : new Date(value);
    return new Intl.DateTimeFormat(this.language.locale(), options ?? { dateStyle: 'medium' }).format(date);
  }

  formatDateTime(value: Date | string | number, options?: Intl.DateTimeFormatOptions): string {
    const date = value instanceof Date ? value : new Date(value);
    return new Intl.DateTimeFormat(
      this.language.locale(),
      options ?? { dateStyle: 'medium', timeStyle: 'short' },
    ).format(date);
  }

  formatNumber(value: number, options?: Intl.NumberFormatOptions): string {
    return new Intl.NumberFormat(this.language.locale(), options).format(value);
  }

  formatCurrency(value: number, currencyCode: string, options?: Intl.NumberFormatOptions): string {
    return new Intl.NumberFormat(this.language.locale(), {
      style: 'currency',
      currency: currencyCode,
      ...options,
    }).format(value);
  }

  formatPercent(value: number, options?: Intl.NumberFormatOptions): string {
    return new Intl.NumberFormat(this.language.locale(), {
      style: 'percent',
      ...options,
    }).format(value);
  }
}
