import { DestroyRef, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Subject, catchError, combineLatest, of, switchMap } from 'rxjs';

import {
  PublicHomepageDto,
  PublicSchoolListItemDto,
} from '../../../../core/api-client/SwaggerClient.service';
import { SeoService } from '../../../../core/seo/seo.service';
import { SchoolsApi } from '../../../schools/data-access/schools.api';
import { PublicPageContainer } from '../../../../shared/ui/public-page-container/public-page-container';
import { sanitizeHtml } from '../../../../shared/utils/sanitize-html';
import { HomeIcon, HomeIconName } from '../../components/home-icon/home-icon';
import { PublicContentApi } from '../../../public/data-access/public-content.api';

type JourneyTab = 'parent' | 'school';

interface IconItem {
  readonly icon: HomeIconName;
  readonly titleKey: string;
  readonly bodyKey: string;
}

interface TrustItem {
  readonly icon: HomeIconName;
  readonly labelKey: string;
}

const FEATURED_LIMIT = 4;
const SCHOOL_CARD_FALLBACK = 'assets/schools/demo-1.svg';

@Component({
  selector: 'se-home-page',
  imports: [PublicPageContainer, RouterLink, TranslocoPipe, HomeIcon],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  private readonly schoolsApi = inject(SchoolsApi);
  private readonly contentApi = inject(PublicContentApi);
  private readonly transloco = inject(TranslocoService);
  private readonly seo = inject(SeoService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);
  private readonly featuredReload$ = new Subject<void>();

  readonly journeyTab = signal<JourneyTab>('parent');
  protected readonly openFaqId = signal<string | null>(null);
  protected readonly cmsHome = signal<PublicHomepageDto | null>(null);

  protected readonly featuredLoading = signal(true);
  protected readonly featuredError = signal(false);
  protected readonly featuredSchools = signal<ReadonlyArray<PublicSchoolListItemDto>>([]);

  protected readonly schoolCardFallback = SCHOOL_CARD_FALLBACK;

  protected readonly heroTrust: readonly TrustItem[] = [
    { icon: 'shield', labelKey: 'home.hero.trust1' },
    { icon: 'chat', labelKey: 'home.hero.trust2' },
    { icon: 'compare', labelKey: 'home.hero.trust3' },
  ];

  protected readonly whyCards: readonly IconItem[] = [
    { icon: 'search', titleKey: 'home.trust.item1Title', bodyKey: 'home.trust.item1Body' },
    { icon: 'compare', titleKey: 'home.trust.item2Title', bodyKey: 'home.trust.item2Body' },
    { icon: 'shield', titleKey: 'home.trust.item3Title', bodyKey: 'home.trust.item3Body' },
    { icon: 'chat', titleKey: 'home.trust.item4Title', bodyKey: 'home.trust.item4Body' },
  ];

  protected readonly parentBenefits: readonly IconItem[] = [
    { icon: 'clock', titleKey: 'home.parents.benefit1Title', bodyKey: 'home.parents.benefit1Body' },
    { icon: 'shield', titleKey: 'home.parents.benefit2Title', bodyKey: 'home.parents.benefit2Body' },
    { icon: 'cap', titleKey: 'home.parents.benefit3Title', bodyKey: 'home.parents.benefit3Body' },
  ];

  protected readonly faqItems = [
    { id: 'faq-1', questionKey: 'home.faq.q1', answerKey: 'home.faq.a1' },
    { id: 'faq-2', questionKey: 'home.faq.q2', answerKey: 'home.faq.a2' },
    { id: 'faq-3', questionKey: 'home.faq.q3', answerKey: 'home.faq.a3' },
    { id: 'faq-4', questionKey: 'home.faq.q4', answerKey: 'home.faq.a4' },
    { id: 'faq-5', questionKey: 'home.faq.q5', answerKey: 'home.faq.a5' },
  ] as const;

  protected readonly parentSteps = [
    { titleKey: 'home.journey.parentStep1Title', bodyKey: 'home.journey.parentStep1Body' },
    { titleKey: 'home.journey.parentStep2Title', bodyKey: 'home.journey.parentStep2Body' },
    { titleKey: 'home.journey.parentStep3Title', bodyKey: 'home.journey.parentStep3Body' },
    { titleKey: 'home.journey.parentStep4Title', bodyKey: 'home.journey.parentStep4Body' },
  ] as const;

  protected readonly schoolSteps = [
    { titleKey: 'home.journey.schoolStep1Title', bodyKey: 'home.journey.schoolStep1Body' },
    { titleKey: 'home.journey.schoolStep2Title', bodyKey: 'home.journey.schoolStep2Body' },
    { titleKey: 'home.journey.schoolStep3Title', bodyKey: 'home.journey.schoolStep3Body' },
    { titleKey: 'home.journey.schoolStep4Title', bodyKey: 'home.journey.schoolStep4Body' },
  ] as const;

  protected readonly activeSteps = computed(() =>
    this.journeyTab() === 'parent' ? this.parentSteps : this.schoolSteps,
  );

  protected readonly heroTitle = computed(() => this.cmsText('heroTitle') ?? null);
  protected readonly heroSubtitle = computed(() => this.cmsText('heroSubtitle') ?? null);
  protected readonly primaryCtaLabel = computed(() => this.cmsText('primaryCtaLabel') ?? null);
  protected readonly secondaryCtaLabel = computed(() => this.cmsText('secondaryCtaLabel') ?? null);
  protected readonly primaryCtaUrl = computed(() => this.cmsUrl('primaryCtaUrl') ?? '/schools');
  protected readonly secondaryCtaUrl = computed(() => this.cmsUrl('secondaryCtaUrl') ?? '/schools');
  protected readonly schoolsSectionTitle = computed(
    () => this.cmsText('schoolsSectionTitle') ?? null,
  );
  protected readonly faqSectionTitle = computed(() => this.cmsText('faqSectionTitle') ?? null);
  protected readonly faqSectionSubtitle = computed(
    () => this.cmsText('faqSectionSubtitle') ?? null,
  );

  protected readonly journeySectionTitle = computed(() => {
    const cms = this.cmsHome();
    if (this.journeyTab() === 'parent') {
      return cms?.parentJourneyTitle?.trim() || null;
    }
    return cms?.schoolJourneyTitle?.trim() || null;
  });

  protected readonly journeySectionSubtitleHtml = computed(() => {
    const cms = this.cmsHome();
    const raw =
      this.journeyTab() === 'parent'
        ? cms?.parentJourneyText?.trim()
        : cms?.schoolJourneyText?.trim();
    return raw ? sanitizeHtml(this.sanitizer, raw) : null;
  });

  constructor() {
    combineLatest([
      this.transloco.selectTranslate('home.meta.title'),
      this.transloco.selectTranslate('home.meta.description'),
    ])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([title, description]) => this.applyHomeMeta(title, description));

    this.transloco.langChanges$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadCmsHome();
    });

    this.loadCmsHome();

    this.featuredReload$
      .pipe(
        switchMap(() => {
          this.featuredLoading.set(true);
          this.featuredError.set(false);
          return this.schoolsApi.getFeaturedSchools(FEATURED_LIMIT).pipe(catchError(() => of(null)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (!result) {
          this.featuredSchools.set([]);
          this.featuredError.set(true);
          this.featuredLoading.set(false);
          return;
        }

        if (!result.succeeded) {
          this.featuredSchools.set([]);
          this.featuredError.set(true);
          this.featuredLoading.set(false);
          return;
        }

        const schools = (result.data ?? []).filter((school) => !!school.id).slice(0, FEATURED_LIMIT);
        this.featuredSchools.set(schools);
        this.featuredError.set(false);
        this.featuredLoading.set(false);
      });

    this.featuredReload$.next();
  }

  protected selectJourney(tab: JourneyTab): void {
    this.journeyTab.set(tab);
  }

  protected onJourneyKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
      return;
    }

    event.preventDefault();
    this.journeyTab.update((current) => (current === 'parent' ? 'school' : 'parent'));
  }

  protected toggleFaq(id: string): void {
    this.openFaqId.update((current) => (current === id ? null : id));
  }

  protected isFaqOpen(id: string): boolean {
    return this.openFaqId() === id;
  }

  protected schoolOpenLabel(name: string | undefined): string {
    return this.transloco.translate('home.featured.openSchoolNamed', { name: name ?? '' });
  }

  protected retryFeatured(): void {
    this.featuredReload$.next();
  }

  protected onSchoolImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img.src.endsWith(SCHOOL_CARD_FALLBACK)) {
      return;
    }
    img.src = SCHOOL_CARD_FALLBACK;
  }

  protected isExternalUrl(url: string): boolean {
    return /^https?:\/\//i.test(url);
  }

  private loadCmsHome(): void {
    this.contentApi
      .getHome()
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        if (result?.succeeded && result.data) {
          this.cmsHome.set(result.data);
          return;
        }
        this.cmsHome.set(null);
      });
  }

  private cmsText(key: keyof PublicHomepageDto): string | null {
    const value = this.cmsHome()?.[key];
    return typeof value === 'string' && value.trim() ? value.trim() : null;
  }

  private cmsUrl(key: 'primaryCtaUrl' | 'secondaryCtaUrl'): string | null {
    const value = this.cmsHome()?.[key];
    return typeof value === 'string' && value.trim() ? value.trim() : null;
  }

  private applyHomeMeta(pageTitle: string, description: string): void {
    this.seo.apply({
      title: pageTitle,
      description,
      canonicalPath: '/',
    });
  }
}
