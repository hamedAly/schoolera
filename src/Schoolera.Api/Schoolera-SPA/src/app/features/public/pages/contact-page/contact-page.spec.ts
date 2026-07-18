import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SeoService } from '../../../../core/seo/seo.service';
import { PublicContentApi } from '../../data-access/public-content.api';
import { ContactPage } from './contact-page';

describe('ContactPage', () => {
  it('requires consent before submit', async () => {
    const api = {
      getPage: vi.fn().mockReturnValue(of({ succeeded: false, errorCodes: ['cms.page.notFound'] })),
      submitContact: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [
        ContactPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              contact: {
                title: 'Contact',
                intro: 'Intro',
                metaDescription: 'Meta',
                fields: {
                  name: 'Name',
                  phone: 'Phone',
                  email: 'Email',
                  category: 'Category',
                  subject: 'Subject',
                  message: 'Message',
                  consent: 'Consent',
                },
                categories: {
                  general: 'General',
                  'parent-support': 'Parent',
                  'school-partnership': 'School',
                  technical: 'Tech',
                  billing: 'Billing',
                  other: 'Other',
                },
                submit: 'Send',
                submitting: 'Sending',
                successTitle: 'OK',
                successBody: 'Body',
                referenceLabel: 'Ref',
                errors: {
                  generic: 'Generic',
                  consentRequired: 'Consent required',
                  emailInvalid: 'Bad email',
                  fieldRequired: '{{field}} required',
                },
              },
              titles: { contact: 'Contact | Schoolera' },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        { provide: PublicContentApi, useValue: api },
        { provide: SeoService, useValue: { apply: vi.fn(), clear: vi.fn() } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ContactPage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.patchValue({
      name: 'Ada',
      phone: '0100',
      email: 'ada@example.com',
      category: 'general',
      subject: 'Hello',
      message: 'This is long enough',
      consentAccepted: false,
    });

    component.submit();
    fixture.detectChanges();

    expect(api.submitContact).not.toHaveBeenCalled();
    expect(component.summaryErrors().some((e) => e.message.includes('Consent'))).toBe(true);
  });
});
