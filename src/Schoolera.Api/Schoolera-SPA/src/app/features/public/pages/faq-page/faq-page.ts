import { DOCUMENT } from '@angular/common';
import {
  Component,
  computed,
  DestroyRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

import { PublicFaqCategoryDto } from '../../../../core/api-client/SwaggerClient.service';
import { SeoService } from '../../../../core/seo/seo.service';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { sanitizeHtml, stripHtml } from '../../../../shared/utils/sanitize-html';
import { PublicContentApi } from '../../data-access/public-content.api';

type PageState = 'loading' | 'ready' | 'error';

interface FlatFaqItem {
  readonly id: string;
  readonly categorySlug: string;
  readonly categoryName: string;
  readonly question: string;
  readonly answerHtml: string;
  readonly answerText: string;
}

/**
 * FAQ accordion behavior: multiple items may be open at once.
 * Deep-link `#faq-{id}` opens and focuses that item without closing others.
 */
@Component({
  selector: 'se-faq-page',
  imports: [PublicPageContainer, RouterLink, TranslocoPipe],
  templateUrl: './faq-page.html',
  styleUrl: './faq-page.scss',
})
export class FaqPage implements OnInit, OnDestroy {
  private readonly api = inject(PublicContentApi);
  private readonly seo = inject(SeoService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly transloco = inject(TranslocoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly doc = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();

  protected readonly state = signal<PageState>('loading');
  protected readonly categories = signal<ReadonlyArray<PublicFaqCategoryDto>>([]);
  protected readonly openIds = signal<ReadonlySet<string>>(new Set());
  protected readonly searchQuery = signal('');
  protected readonly activeCategory = signal('');

  protected readonly flatItems = computed(() => {
    const items: FlatFaqItem[] = [];
    for (const cat of this.categories()) {
      const slug = cat.slug ?? '';
      const name = cat.name ?? '';
      for (const item of cat.items ?? []) {
        if (!item.id) {
          continue;
        }
        const answer = item.answer ?? '';
        items.push({
          id: item.id,
          categorySlug: slug,
          categoryName: name,
          question: item.question ?? '',
          answerHtml: sanitizeHtml(this.sanitizer, answer),
          answerText: stripHtml(answer),
        });
      }
    }
    return items;
  });

  protected readonly filteredItems = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    const cat = this.activeCategory();
    return this.flatItems().filter((item) => {
      if (cat && item.categorySlug !== cat) {
        return false;
      }
      if (!q) {
        return true;
      }
      return (
        item.question.toLowerCase().includes(q) ||
        item.answerText.toLowerCase().includes(q) ||
        item.categoryName.toLowerCase().includes(q)
      );
    });
  });

  protected readonly resultCount = computed(() => this.filteredItems().length);

  ngOnInit(): void {
    this.searchInput$
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.searchQuery.set(value);
        this.syncQueryParams();
      });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const q = params.get('q') ?? '';
      const category = params.get('category') ?? '';
      this.searchQuery.set(q);
      this.activeCategory.set(category);
    });

    this.load();

    this.transloco.langChanges$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.state() === 'ready' || this.state() === 'error') {
        this.load();
      }
    });
  }

  ngOnDestroy(): void {
    this.seo.clear();
  }

  protected load(): void {
    this.state.set('loading');
    this.api
      .getFaqs()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (result.succeeded) {
            this.categories.set(result.data ?? []);
            this.state.set('ready');
            this.applySeo();
            this.openHashTarget();
            return;
          }
          this.state.set('error');
        },
        error: () => this.state.set('error'),
      });
  }

  protected onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput$.next(value);
  }

  protected selectCategory(slug: string): void {
    this.activeCategory.set(slug);
    this.syncQueryParams();
  }

  protected clearSearch(): void {
    this.searchQuery.set('');
    this.activeCategory.set('');
    this.syncQueryParams();
  }

  protected toggleItem(id: string): void {
    this.openIds.update((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  protected isOpen(id: string): boolean {
    return this.openIds().has(id);
  }

  protected panelId(id: string): string {
    return `faq-panel-${id}`;
  }

  protected buttonId(id: string): string {
    return `faq-button-${id}`;
  }

  protected anchorId(id: string): string {
    return `faq-${id}`;
  }

  private syncQueryParams(): void {
    const q = this.searchQuery().trim();
    const category = this.activeCategory();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: q || null,
        category: category || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private openHashTarget(): void {
    const hash = this.doc.defaultView?.location.hash ?? '';
    const match = /^#faq-(.+)$/.exec(hash);
    if (!match) {
      return;
    }

    const id = match[1];
    this.openIds.update((current) => new Set(current).add(id));

    queueMicrotask(() => {
      const el = this.doc.getElementById(this.anchorId(id));
      el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      const btn = this.doc.getElementById(this.buttonId(id)) as HTMLButtonElement | null;
      btn?.focus();
    });
  }

  private applySeo(): void {
    const title = this.transloco.translate('titles.faq');
    const description = this.transloco.translate('faq.metaDescription');
    const visible = this.filteredItems().slice(0, 20);
    const jsonLd =
      visible.length > 0
        ? {
            '@context': 'https://schema.org',
            '@type': 'FAQPage',
            mainEntity: visible.map((item) => ({
              '@type': 'Question',
              name: item.question,
              acceptedAnswer: {
                '@type': 'Answer',
                text: item.answerText,
              },
            })),
          }
        : undefined;

    this.seo.apply({
      title,
      description,
      canonicalPath: '/faq',
      jsonLd,
    });
  }
}
