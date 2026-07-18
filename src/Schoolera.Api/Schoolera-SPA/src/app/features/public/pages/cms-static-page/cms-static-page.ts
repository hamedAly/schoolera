import { Component, DestroyRef, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Subject, switchMap } from 'rxjs';

import { PublicCmsPageDto } from '../../../../core/api-client/SwaggerClient.service';
import { SeoService } from '../../../../core/seo/seo.service';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { sanitizeHtml } from '../../../../shared/utils/sanitize-html';
import { PublicContentApi } from '../../data-access/public-content.api';

type PageState = 'loading' | 'ready' | 'notFound' | 'error';

@Component({
  selector: 'se-cms-static-page',
  imports: [PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './cms-static-page.html',
  styleUrl: './cms-static-page.scss',
})
export class CmsStaticPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(PublicContentApi);
  private readonly seo = inject(SeoService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly transloco = inject(TranslocoService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<string>();

  protected readonly state = signal<PageState>('loading');
  protected readonly page = signal<PublicCmsPageDto | null>(null);
  protected readonly sanitizedContent = signal('');

  ngOnInit(): void {
    this.reload$
      .pipe(
        switchMap((slug) => {
          this.state.set('loading');
          this.page.set(null);
          this.sanitizedContent.set('');
          return this.api.getPage(slug);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          if (result.succeeded && result.data) {
            this.page.set(result.data);
            this.sanitizedContent.set(sanitizeHtml(this.sanitizer, result.data.content));
            this.state.set('ready');
            this.applySeo(result.data);
            return;
          }

          const codes = result.errorCodes ?? [];
          if (codes.includes('cms.page.notFound')) {
            this.state.set('notFound');
            return;
          }

          this.state.set('error');
        },
        error: () => this.state.set('error'),
      });

    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((data) => {
      const slug = typeof data['slug'] === 'string' ? data['slug'] : '';
      if (slug) {
        this.reload$.next(slug);
      } else {
        this.state.set('notFound');
      }
    });

    this.transloco.langChanges$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const current = this.page();
      if (current && this.state() === 'ready') {
        // Re-fetch so Accept-Language returns localized title/content.
        const slug = current.slug ?? (this.route.snapshot.data['slug'] as string | undefined);
        if (slug) {
          this.reload$.next(slug);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.seo.clear();
  }

  protected retry(): void {
    const slug =
      this.page()?.slug ??
      (typeof this.route.snapshot.data['slug'] === 'string'
        ? (this.route.snapshot.data['slug'] as string)
        : '');
    if (slug) {
      this.reload$.next(slug);
    }
  }

  private applySeo(page: PublicCmsPageDto): void {
    const title =
      page.metaTitle?.trim() ||
      page.title?.trim() ||
      this.transloco.translate('titles.cmsPage') ||
      '';
    const description =
      page.metaDescription?.trim() ||
      this.transloco.translate('cms.metaDescriptionFallback') ||
      '';
    const path = `/${page.slug ?? ''}`;

    this.seo.apply({
      title,
      description,
      canonicalPath: path,
    });
  }
}
