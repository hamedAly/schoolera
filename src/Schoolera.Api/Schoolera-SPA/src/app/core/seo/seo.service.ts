import { DOCUMENT } from '@angular/common';
import { Injectable, OnDestroy, Renderer2, RendererFactory2, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

import { environment } from '../../../environments/environment';

export interface SeoConfig {
  title: string;
  description: string;
  /** Path only, e.g. `/schools/my-school`. */
  canonicalPath: string;
  image?: string;
  type?: string;
  twitterCard?: 'summary' | 'summary_large_image';
  jsonLd?: Record<string, unknown>;
}

const CANONICAL_ID = 'se-canonical-link';
const JSON_LD_ID = 'se-json-ld';

@Injectable({
  providedIn: 'root',
})
export class SeoService implements OnDestroy {
  private readonly doc = inject(DOCUMENT);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly renderer: Renderer2 = inject(RendererFactory2).createRenderer(null, null);

  private jsonLdScript: HTMLScriptElement | null = null;
  private canonicalLink: HTMLLinkElement | null = null;

  apply(config: SeoConfig): void {
    this.clear();

    this.title.setTitle(config.title);
    this.meta.updateTag({ name: 'description', content: config.description });
    this.meta.updateTag({ property: 'og:title', content: config.title });
    this.meta.updateTag({ property: 'og:description', content: config.description });
    this.meta.updateTag({ property: 'og:type', content: config.type ?? 'website' });

    const canonicalUrl = this.buildAbsoluteUrl(config.canonicalPath);
    this.meta.updateTag({ property: 'og:url', content: canonicalUrl });

    if (config.image) {
      const imageUrl = this.toAbsoluteUrl(config.image);
      this.meta.updateTag({ property: 'og:image', content: imageUrl });
      this.meta.updateTag({ name: 'twitter:image', content: imageUrl });
    }

    this.meta.updateTag({ name: 'twitter:card', content: config.twitterCard ?? 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: config.title });
    this.meta.updateTag({ name: 'twitter:description', content: config.description });

    this.setCanonical(canonicalUrl);

    if (config.jsonLd) {
      this.setJsonLd(config.jsonLd);
    }
  }

  clear(): void {
    this.removeCanonical();
    this.removeJsonLd();
  }

  ngOnDestroy(): void {
    this.clear();
  }

  buildAbsoluteUrl(path: string): string {
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    const base = environment.publicSiteBaseUrl.replace(/\/$/, '');

    if (!base) {
      return normalizedPath;
    }

    return `${base}${normalizedPath}`;
  }

  private toAbsoluteUrl(url: string): string {
    if (/^https?:\/\//i.test(url)) {
      return url;
    }

    const base = environment.publicSiteBaseUrl.replace(/\/$/, '');
    const path = url.startsWith('/') ? url : `/${url}`;
    return base ? `${base}${path}` : path;
  }

  private setCanonical(href: string): void {
    const head = this.doc.head;
    if (!head) {
      return;
    }

    let link = this.doc.getElementById(CANONICAL_ID) as HTMLLinkElement | null;
    if (!link) {
      link = this.renderer.createElement('link') as HTMLLinkElement;
      link.id = CANONICAL_ID;
      link.rel = 'canonical';
      this.renderer.appendChild(head, link);
    }

    link.href = href;
    this.canonicalLink = link;
  }

  private removeCanonical(): void {
    if (this.canonicalLink?.parentNode) {
      this.renderer.removeChild(this.canonicalLink.parentNode, this.canonicalLink);
    }

    this.canonicalLink = null;
  }

  private setJsonLd(payload: Record<string, unknown>): void {
    const head = this.doc.head;
    if (!head) {
      return;
    }

    const script = this.renderer.createElement('script') as HTMLScriptElement;
    script.id = JSON_LD_ID;
    script.type = 'application/ld+json';
    script.textContent = JSON.stringify(payload);
    this.renderer.appendChild(head, script);
    this.jsonLdScript = script;
  }

  private removeJsonLd(): void {
    if (this.jsonLdScript?.parentNode) {
      this.renderer.removeChild(this.jsonLdScript.parentNode, this.jsonLdScript);
    }

    this.jsonLdScript = null;
  }
}
