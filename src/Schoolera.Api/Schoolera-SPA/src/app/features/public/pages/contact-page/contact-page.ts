import { Component, DestroyRef, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { SeoService } from '../../../../core/seo/seo.service';
import {
  FormErrorSummary,
  FormErrorSummaryItem,
} from '../../../../shared/ui/form-error-summary/form-error-summary';
import { FormField } from '../../../../shared/ui/form-field/form-field';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { requiredTextValidator } from '../../../../shared/validators/required-text.validator';
import { sanitizeHtml } from '../../../../shared/utils/sanitize-html';
import { PublicContentApi } from '../../data-access/public-content.api';

/** Backend ContactCategories allowlist. */
const CONTACT_CATEGORIES = [
  'general',
  'parent-support',
  'school-partnership',
  'technical',
  'billing',
  'other',
] as const;

const CONTACT_SOURCE = 'contact-page';

@Component({
  selector: 'se-contact-page',
  imports: [
    FormErrorSummary,
    FormField,
    PublicPageContainer,
    ReactiveFormsModule,
    TranslocoPipe,
  ],
  templateUrl: './contact-page.html',
  styleUrl: './contact-page.scss',
})
export class ContactPage implements OnInit, OnDestroy {
  private readonly api = inject(PublicContentApi);
  private readonly fb = inject(FormBuilder);
  private readonly seo = inject(SeoService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly categories = CONTACT_CATEGORIES;
  protected readonly submitting = signal(false);
  protected readonly successReference = signal<string | null>(null);
  readonly summaryErrors = signal<FormErrorSummaryItem[]>([]);
  protected readonly introHtml = signal<string | null>(null);
  protected readonly introTitle = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [requiredTextValidator()]],
    phone: ['', [requiredTextValidator()]],
    email: ['', [Validators.required, Validators.email]],
    category: ['general' as (typeof CONTACT_CATEGORIES)[number], [Validators.required]],
    subject: ['', [requiredTextValidator()]],
    message: ['', [requiredTextValidator(), Validators.minLength(10)]],
    consentAccepted: [false, [Validators.requiredTrue]],
    website: [''],
  });

  ngOnInit(): void {
    this.applySeo();
    this.loadOptionalIntro();
    this.transloco.langChanges$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.applySeo();
      this.loadOptionalIntro();
    });
  }

  ngOnDestroy(): void {
    this.seo.clear();
  }

  submit(): void {
    if (this.submitting()) {
      return;
    }

    this.successReference.set(null);
    this.summaryErrors.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.summaryErrors.set(this.collectClientErrors());
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);

    this.api
      .submitContact({
        name: value.name.trim(),
        phone: value.phone.trim(),
        email: value.email.trim(),
        category: value.category,
        subject: value.subject.trim(),
        message: value.message.trim(),
        consentAccepted: value.consentAccepted,
        source: CONTACT_SOURCE,
        website: value.website?.trim() || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.submitting.set(false);
          if (result.succeeded && result.data?.reference) {
            this.successReference.set(result.data.reference);
            this.form.reset({
              name: '',
              phone: '',
              email: '',
              category: 'general',
              subject: '',
              message: '',
              consentAccepted: false,
              website: '',
            });
            this.summaryErrors.set([]);
            return;
          }

          this.summaryErrors.set(this.mapApiErrors(result.errorCodes));
        },
        error: () => {
          this.submitting.set(false);
          this.summaryErrors.set([
            {
              message: this.transloco.translate('contact.errors.generic'),
            },
          ]);
        },
      });
  }

  protected categoryLabelKey(category: string): string {
    return `contact.categories.${category}`;
  }

  private loadOptionalIntro(): void {
    this.api
      .getPage('contact')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded && result.data) {
            this.introTitle.set(result.data.title?.trim() || null);
            const html = sanitizeHtml(this.sanitizer, result.data.content);
            this.introHtml.set(html.trim() ? html : null);
            return;
          }
          this.introTitle.set(null);
          this.introHtml.set(null);
        },
        error: () => {
          this.introTitle.set(null);
          this.introHtml.set(null);
        },
      });
  }

  private applySeo(): void {
    this.seo.apply({
      title: this.transloco.translate('titles.contact'),
      description: this.transloco.translate('contact.metaDescription'),
      canonicalPath: '/contact',
    });
  }

  private collectClientErrors(): FormErrorSummaryItem[] {
    const items: FormErrorSummaryItem[] = [];
    const controls: Array<{ name: string; key: string }> = [
      { name: 'name', key: 'contact.fields.name' },
      { name: 'phone', key: 'contact.fields.phone' },
      { name: 'email', key: 'contact.fields.email' },
      { name: 'category', key: 'contact.fields.category' },
      { name: 'subject', key: 'contact.fields.subject' },
      { name: 'message', key: 'contact.fields.message' },
      { name: 'consentAccepted', key: 'contact.fields.consent' },
    ];

    for (const { name, key } of controls) {
      const control = this.form.get(name);
      if (control?.invalid) {
        if (name === 'consentAccepted') {
          items.push({ message: this.transloco.translate('contact.errors.consentRequired') });
        } else if (control.hasError('email')) {
          items.push({ message: this.transloco.translate('contact.errors.emailInvalid') });
        } else {
          items.push({
            message: this.transloco.translate('contact.errors.fieldRequired', {
              field: this.transloco.translate(key),
            }),
          });
        }
      }
    }

    return items;
  }

  private mapApiErrors(errorCodes: string[] | null | undefined): FormErrorSummaryItem[] {
    if (!errorCodes?.length) {
      return [{ message: this.transloco.translate('contact.errors.generic') }];
    }

    const messages = errorCodes.map((code) => {
      const key = `contact.errors.${code.replace(/^contact\./, '')}`;
      const translated = this.transloco.translate(key);
      return translated === key
        ? this.transloco.translate('contact.errors.generic')
        : translated;
    });

    return [...new Set(messages)].map((message) => ({ message }));
  }
}
