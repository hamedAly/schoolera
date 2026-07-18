import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SeoService } from '../../../../core/seo/seo.service';
import { PublicContentApi } from '../../data-access/public-content.api';
import { FaqPage } from './faq-page';

describe('FaqPage', () => {
  it('filters items by search query', async () => {
    const api = {
      getFaqs: vi.fn().mockReturnValue(
        of({
          succeeded: true,
          data: [
            {
              slug: 'general',
              name: 'General',
              items: [
                { id: '1', question: 'How to search?', answer: '<p>Use filters</p>' },
                { id: '2', question: 'How to apply?', answer: '<p>Open admissions</p>' },
              ],
            },
          ],
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [
        FaqPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              faq: {
                title: 'FAQ',
                intro: 'Intro',
                metaDescription: 'Meta',
                loading: 'Loading',
                error: 'Error',
                retry: 'Retry',
                searchLabel: 'Search',
                searchPlaceholder: 'Search…',
                categoriesLabel: 'Categories',
                allCategories: 'All',
                resultCount: '{{count}}',
                noResults: 'None',
                clearSearch: 'Clear',
                contactCta: 'Contact',
              },
              titles: { faq: 'FAQ | Schoolera' },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        { provide: PublicContentApi, useValue: api },
        { provide: SeoService, useValue: { apply: vi.fn(), clear: vi.fn() } },
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: of({ get: () => null }),
          },
        },
        {
          provide: Router,
          useValue: { navigate: vi.fn() },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(FaqPage);
    const component = fixture.componentInstance as unknown as {
      searchQuery: { set: (v: string) => void };
      filteredItems: () => Array<{ question: string }>;
    };
    fixture.detectChanges();
    await fixture.whenStable();

    component.searchQuery.set('apply');
    fixture.detectChanges();

    expect(component.filteredItems().map((i) => i.question)).toEqual(['How to apply?']);
  });
});
