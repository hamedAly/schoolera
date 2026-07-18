import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Meta } from '@angular/platform-browser';
import { TranslocoService } from '@jsverse/transloco';

import {
  SCHOOLERA_DEFAULT_LANG,
  SCHOOLERA_FALLBACK_LANG,
  SCHOOLERA_LANG_STORAGE_KEY,
  SCHOOLERA_SUPPORTED_LANGS,
  SchooleraDirection,
  SchooleraLang,
  SchooleraLocale,
  isSchooleraLang,
  resolveSchooleraLang,
  schooleraAcceptLanguage,
  schooleraDocumentDirection,
  schooleraLocale,
} from './schoolera-lang';

/**
 * Central language owner: supported langs, locale, direction, persistence validation,
 * Transloco switching, and document lang/dir updates.
 */
@Injectable({ providedIn: 'root' })
export class DocumentLanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly meta = inject(Meta);
  private readonly destroyRef = inject(DestroyRef);

  readonly supportedLangs = SCHOOLERA_SUPPORTED_LANGS;
  readonly defaultLang = SCHOOLERA_DEFAULT_LANG;
  readonly fallbackLang = SCHOOLERA_FALLBACK_LANG;
  readonly storageKey = SCHOOLERA_LANG_STORAGE_KEY;

  private readonly langChanges = toSignal(this.transloco.langChanges$, {
    initialValue: resolveSchooleraLang(this.transloco.getActiveLang()),
  });

  readonly activeLang = computed<SchooleraLang>(() => resolveSchooleraLang(this.langChanges()));
  readonly locale = computed<SchooleraLocale>(() => schooleraLocale(this.activeLang()));
  readonly direction = computed<SchooleraDirection>(() => schooleraDocumentDirection(this.activeLang()));
  readonly acceptLanguage = computed(() => schooleraAcceptLanguage(this.activeLang()));

  /** Signal mirror for templates that need a stable lang snapshot. */
  private readonly appliedLang = signal<SchooleraLang>(SCHOOLERA_DEFAULT_LANG);

  constructor() {
    const initial = this.readPersistedOrDefault();
    if (this.transloco.getActiveLang() !== initial) {
      this.transloco.setActiveLang(initial);
    }

    this.applyDocumentLanguage(initial);

    this.transloco.langChanges$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang) => {
      const resolved = resolveSchooleraLang(lang);
      if (!isSchooleraLang(lang)) {
        this.transloco.setActiveLang(resolved);
        return;
      }

      this.applyDocumentLanguage(resolved);
      this.persist(resolved);
    });
  }

  setLanguage(lang: SchooleraLang): void {
    const resolved = resolveSchooleraLang(lang);
    if (resolved === resolveSchooleraLang(this.transloco.getActiveLang())) {
      return;
    }

    // Route is preserved — only the active Transloco language changes (no full reload).
    this.transloco.setActiveLang(resolved);
  }

  toggleLanguage(): SchooleraLang {
    const next: SchooleraLang = this.activeLang() === 'ar' ? 'en' : 'ar';
    this.setLanguage(next);
    return next;
  }

  readPersistedOrDefault(): SchooleraLang {
    try {
      const stored = localStorage.getItem(this.storageKey);
      return resolveSchooleraLang(stored);
    } catch {
      return SCHOOLERA_DEFAULT_LANG;
    }
  }

  private persist(lang: SchooleraLang): void {
    try {
      localStorage.setItem(this.storageKey, lang);
    } catch {
      // Ignore quota / private-mode failures; Transloco persist plugin may also write.
    }
  }

  private applyDocumentLanguage(lang: SchooleraLang): void {
    this.appliedLang.set(lang);
    const html = document.documentElement;
    html.lang = lang;
    html.dir = schooleraDocumentDirection(lang);
    this.meta.updateTag({ name: 'language', content: lang });
  }
}
