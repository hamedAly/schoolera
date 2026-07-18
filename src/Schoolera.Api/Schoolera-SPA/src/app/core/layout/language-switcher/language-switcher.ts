import { Component, computed, inject, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { DocumentLanguageService } from '../../i18n/document-language.service';
import { SCHOOLERA_DEFAULT_LANG, SchooleraLang, resolveSchooleraLang } from '../../i18n/schoolera-lang';

@Component({
  selector: 'se-language-switcher',
  imports: [TranslocoPipe],
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.scss',
})
export class LanguageSwitcher {
  private readonly documentLanguage = inject(DocumentLanguageService);
  private readonly transloco = inject(TranslocoService);

  /** Emitted after a successful language change (used to close the mobile menu). */
  readonly languageChange = output<SchooleraLang>();

  private readonly langChanges = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  protected readonly activeLang = computed<SchooleraLang>(() =>
    resolveSchooleraLang(this.langChanges() ?? SCHOOLERA_DEFAULT_LANG),
  );

  protected readonly nextLang = computed<SchooleraLang>(() =>
    this.activeLang() === 'ar' ? 'en' : 'ar',
  );

  protected readonly nextLabelKey = computed(() =>
    this.nextLang() === 'en' ? 'language.switchToEnglish' : 'language.switchToArabic',
  );

  switchLanguage(): void {
    const next = this.nextLang();
    this.documentLanguage.setLanguage(next);
    this.languageChange.emit(next);
  }
}
