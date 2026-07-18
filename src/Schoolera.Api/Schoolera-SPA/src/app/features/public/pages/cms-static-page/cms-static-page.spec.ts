import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SeoService } from '../../../../core/seo/seo.service';
import { PublicContentApi } from '../../data-access/public-content.api';
import { CmsStaticPage } from './cms-static-page';

describe('CmsStaticPage', () => {
  it('loads page by route data slug via PublicContentApi', async () => {
    const api = {
      getPage: vi.fn().mockReturnValue(
        of({
          succeeded: true,
          data: {
            slug: 'about',
            title: 'About',
            content: '<p>Hello</p>',
            metaTitle: 'About meta',
            metaDescription: 'About desc',
          },
        }),
      ),
    };

    const seo = { apply: vi.fn(), clear: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        CmsStaticPage,
        TranslocoTestingModule.forRoot({
          langs: {
            ar: {
              cms: {
                loading: 'loading',
                notFoundTitle: 'nf',
                notFoundBody: 'nfb',
                errorTitle: 'err',
                errorBody: 'errb',
                retry: 'retry',
                metaDescriptionFallback: 'fallback',
              },
              pages: { backHome: 'home' },
              titles: { cmsPage: 'CMS' },
            },
          },
          translocoConfig: { availableLangs: ['ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        { provide: PublicContentApi, useValue: api },
        { provide: SeoService, useValue: seo },
        {
          provide: ActivatedRoute,
          useValue: { data: of({ slug: 'about' }), snapshot: { data: { slug: 'about' } } },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(CmsStaticPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getPage).toHaveBeenCalledWith('about');
    expect(fixture.nativeElement.textContent).toContain('About');
    expect(seo.apply).toHaveBeenCalled();
  });
});
