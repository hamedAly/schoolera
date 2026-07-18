# CMS and contact (Phase 1)

## Scope

Schoolera provides a lightweight bilingual CMS for managed static pages, FAQs, and structured homepage copy, plus a public contact form and Platform Admin monitoring. It is intentionally not a page builder: there are no arbitrary sections, layouts, widgets, or media blocks.

All admin APIs require `SchooleraPolicies.PlatformAdminOnly`. State-changing endpoints require CSRF validation and return the standard `Result<T>` envelope.

## Managed pages and safe HTML

`CmsPage` stores bilingual titles, HTML content, and optional SEO metadata. Its statuses are `Draft`, `Published`, and `Archived`; only published pages are publicly readable.

The seeded system slugs are:

- `about`
- `how-it-works`
- `privacy`
- `terms`
- `sla`
- `contact` (optional intro HTML for the Angular `/contact` form; not a standalone CMS route)

These records are system pages and their slugs are immutable. Their bilingual content and metadata remain editable.

Rich content is sanitized on the server with Ganss.Xss `HtmlSanitizer`. The HTML tag allowlist is:

`p`, `h2`, `h3`, `ul`, `ol`, `li`, `strong`, `em`, `blockquote`, `a`, `br`

Only the link attributes `href`, `title`, `rel`, and `target` are allowed. Allowed URL schemes are `http`, `https`, `mailto`, and `tel`; external HTTP(S) links receive `rel="noopener noreferrer"` and `target="_blank"`. Scripts, styles, iframes, event-handler attributes, `javascript:` URLs, `data:` URLs, and all other non-allowlisted markup are stripped. CMS pages and FAQ answers are sanitized when created or updated and again when published.

## FAQs

`FaqCategory` contains bilingual names, a slug, sort order, publication flag, and ordered `FaqItem` records. Each item contains bilingual question and answer fields, sort order, and its own publication flag.

Admins can create, update, publish/unpublish, and reorder categories and items. An item can be published only when its category is published. Public reads include only published categories with at least one published item, so a published item under an unpublished category is hidden.

## Structured homepage content

`HomepageContent` is a structured singleton-style content record, not a page-builder document. Drafts may coexist, while public reads select published content. Its content fields are:

- `HeroTitleAr`, `HeroTitleEn`
- `HeroSubtitleAr`, `HeroSubtitleEn`
- `PrimaryCtaLabelAr`, `PrimaryCtaLabelEn`, `PrimaryCtaUrl`
- `SecondaryCtaLabelAr`, `SecondaryCtaLabelEn`, `SecondaryCtaUrl`
- `SchoolsSectionTitleAr`, `SchoolsSectionTitleEn`
- `ParentJourneyTitleAr`, `ParentJourneyTitleEn`
- `ParentJourneyTextAr`, `ParentJourneyTextEn`
- `SchoolJourneyTitleAr`, `SchoolJourneyTitleEn`
- `SchoolJourneyTextAr`, `SchoolJourneyTextEn`
- `FaqSectionTitleAr`, `FaqSectionTitleEn`
- `FaqSectionSubtitleAr`, `FaqSectionSubtitleEn`

The entity also tracks `Id`, status, publication/creation/update timestamps, and a row version.

## Contact requests

The public request body contains `Name`, `Phone`, optional `Email`, `Category`, `Subject`, `Message`, `ConsentAccepted`, `Source`, and the honeypot field `Website`. Stored requests also have an internal ID, generated reference, status, timestamps, reviewer ID/time, and optional admin note.

Statuses are `New`, `InReview`, `Resolved`, and `Closed`. Supported categories are:

- `general`
- `parent-support`
- `school-partnership`
- `technical`
- `billing`
- `other`

Privacy requirements:

- The public success response contains only the generated reference.
- Public failure responses do not echo submitted PII.
- Application logs record internal IDs/references and status operations, not names, phone numbers, email addresses, subjects, or message bodies.
- Full request details are available only to Platform Admin users.

`POST /api/contact` uses the `contact-submit` fixed-window policy, partitioned by client IP: 5 requests per minute outside Development (Development uses 1000 for integration work). It requires CSRF validation. `Website` must remain empty; a populated value is rejected as a bot submission. A two-minute duplicate cooldown rejects matching recent email/phone/subject submissions with a generic response.

## Public endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/content/pages/{slug}` | Read a published page localized from `Accept-Language` |
| GET | `/api/content/faqs` | Read ordered, publicly visible FAQ categories and items |
| GET | `/api/content/home` | Read the published structured homepage localized from `Accept-Language` |
| POST | `/api/contact` | Submit a contact request; CSRF, honeypot, rate limit, and duplicate cooldown apply |

## Platform Admin endpoints

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/admin/cms/pages` | Search/filter/paginate pages |
| GET | `/api/admin/cms/pages/{id}` | Get bilingual page details |
| POST | `/api/admin/cms/pages` | Create a page |
| PUT | `/api/admin/cms/pages/{id}` | Update page content, metadata, and allowed slug |
| POST | `/api/admin/cms/pages/{id}/publish` | Publish a page |
| POST | `/api/admin/cms/pages/{id}/unpublish` | Return a page to Draft |
| POST | `/api/admin/cms/pages/{id}/archive` | Archive a page |
| GET | `/api/admin/cms/faq/categories` | List categories with items |
| POST | `/api/admin/cms/faq/categories` | Create a category |
| PUT | `/api/admin/cms/faq/categories/{id}` | Update a category |
| POST | `/api/admin/cms/faq/categories/{id}/publish` | Publish a category |
| POST | `/api/admin/cms/faq/categories/{id}/unpublish` | Unpublish a category |
| POST | `/api/admin/cms/faq/categories/reorder` | Reorder categories |
| POST | `/api/admin/cms/faq/items` | Create an item |
| PUT | `/api/admin/cms/faq/items/{id}` | Update or move an item |
| POST | `/api/admin/cms/faq/items/{id}/publish` | Publish an item |
| POST | `/api/admin/cms/faq/items/{id}/unpublish` | Unpublish an item |
| POST | `/api/admin/cms/faq/items/reorder` | Reorder items within a category |
| GET | `/api/admin/cms/home` | Get bilingual homepage content |
| PUT | `/api/admin/cms/home` | Update homepage content |
| POST | `/api/admin/cms/home/{id}/publish` | Publish homepage content |
| POST | `/api/admin/cms/home/{id}/unpublish` | Return homepage content to Draft |
| GET | `/api/admin/contact-requests` | Search/filter/paginate requests |
| GET | `/api/admin/contact-requests/{id}` | Read request details |
| POST | `/api/admin/contact-requests/{id}/start-review` | Move New to InReview with optional note |
| POST | `/api/admin/contact-requests/{id}/resolve` | Resolve a New or InReview request |
| POST | `/api/admin/contact-requests/{id}/close` | Close a non-Closed request |

All state-changing admin endpoints in this table require CSRF validation.

## Audit

CMS and contact operations append `AdminAuditEvent` entries using these actions:

- Pages: `cms.page_created`, `cms.page_updated`, `cms.page_published`, `cms.page_unpublished`, `cms.page_archived`
- FAQ categories: `cms.faq_category_created`, `cms.faq_category_updated`, `cms.faq_category_published`, `cms.faq_category_unpublished`, `cms.faq_category_reordered`
- FAQ items: `cms.faq_item_created`, `cms.faq_item_updated`, `cms.faq_item_published`, `cms.faq_item_unpublished`, `cms.faq_item_reordered`
- Homepage: `cms.home_updated`, `cms.home_published`, `cms.home_unpublished`
- Contact workflow: `contact.status_changed`

## Localization and SEO

Published CMS content must contain both Arabic and English values. Management DTOs expose both languages; public DTOs expose one localized value selected from `Accept-Language`, with Arabic as the default/fallback.

The Angular pages apply titles, descriptions, canonical/Open Graph metadata, and FAQ JSON-LD at runtime. There is no SSR or SSG, so crawlers that do not execute JavaScript may not see runtime metadata or FAQ structured data in the initial HTML.

## Seed and migration

`CmsContentSeeder` idempotently seeds the six published system pages, published homepage content, published FAQ categories/items, and a demo contact request. Existing rows are not recreated or overwritten.

Migration `20260716202921_AddCmsFaqHomepageAndContact` adds the CMS page, FAQ, homepage, and contact-request persistence model.

## Known limitations

Phase 1 does not include email delivery for contact requests, CRM integration, ticketing, a blog, a page builder, CAPTCHA, a media library, or a version-history UI.
