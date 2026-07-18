# Map-based School Search (Phase 1)

Audit date: 2026-07-17  
Feature: Advanced map view for public School Search

## Provider decision (Product status: **blocked for Production**)

| Layer | Choice | Notes |
|-------|--------|--------|
| Client rendering library | **Leaflet** (BSD-2-Clause) | Single client library; no multi-provider framework |
| Tile / style source | **Database-managed** via `IntegrationType.Map` `SettingsJson` | Never in Angular `environment`, appsettings, User Secrets, or env vars |
| Development seed | Leaflet + OpenStreetMap raster tile template + OSM attribution | **Development / non-production foundation only** |

### Licensing and usage (not a Product approval)

- **Leaflet**: open-source BSD-2-Clause; attribution in library docs.
- **OpenStreetMap tiles** (`tile.openstreetmap.org`): subject to [OSMF Tile Usage Policy](https://operations.osmfoundation.org/policies/tiles/). Heavy or Production usage of the public OSM tile servers is **not** approved here.
- **Arabic map labels**: OSM includes Arabic where contributors added them; no guaranteed bilingual basemap.
- **Pricing**: Leaflet is free; public OSM tiles are not a commercial SLA. Production must select an approved commercial tile provider (e.g. MapTiler / Mapbox / self-hosted) and store its public token/style URL in DB `SettingsJson`.

**Product blocker:** Final Production map tile provider, commercial licensing, rate limits, and attribution text must be approved by Product. This prompt does **not** claim Production map readiness.

## Authoritative public Branch coordinates

Public map pins use **`SchoolBranch.Latitude` / `SchoolBranch.Longitude`** when:

- Parent `School.Status == Published`
- Branch `IsActive == true`
- Both coordinates are non-null

There is no separate private-coordinate column in Phase 1. Portal/admin may edit the same fields; unpublished schools and inactive branches are excluded from public discovery. Do not infer coordinates from addresses.

## List vs Map consistency

| Rule | List (`GET /api/schools`) | Map (`GET /api/schools/map-pins`) |
|------|---------------------------|-----------------------------------|
| Publication / filters | Same | Same business filters |
| Fee visibility | Prompt 9 | Same fee preview gating |
| Missing coordinates | School still listed | Branch omitted from pins |
| Cardinality | One card per School | One pin per matching public Branch |

Map pin counts must not be shown as total List result counts.

## SQL strategy

Decimal lat/lng with SQL-translatable bounding-box / coarse radius filters. **No** NetTopologySuite / SQL Server `geography` in Phase 1 (catalog size + existing equirectangular nearest semantics are sufficient). Additive index: `(IsActive, Latitude, Longitude)` on `SchoolBranches`.

## Public-safe Map configuration

`GET /api/schools/map-configuration` returns only provider code, safe tile URL, optional public browser token, center/zoom bounds, attribution, and availability. Never returns `SettingsJson`, server API keys, or Admin metadata.
