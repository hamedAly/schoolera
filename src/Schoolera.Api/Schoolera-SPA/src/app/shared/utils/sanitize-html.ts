import { SecurityContext } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

/**
 * Sanitize HTML via Angular DomSanitizer (never bypassSecurityTrustHtml).
 * Content is expected to be server-sanitized; this is a defense-in-depth pass.
 */
export function sanitizeHtml(
  sanitizer: DomSanitizer,
  html: string | null | undefined,
): string {
  return sanitizer.sanitize(SecurityContext.HTML, html ?? '') ?? '';
}

/** Strip tags for plain-text uses (JSON-LD, search). */
export function stripHtml(html: string | null | undefined): string {
  if (!html) {
    return '';
  }
  return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
}
