# ADR-020 — Global Map

## Status

Accepted

## Context

The gateway collects GeoIP metadata and attack data for IP addresses. Administrators need a visual way to understand where threats originate, where attacks are happening, and inspect individual IPs. We needed a lightweight solution that reuses existing GeoIP fields without adding complex infrastructure.

## Decision

We introduced a global map feature with backend aggregation and a Leaflet-based frontend.

Key design points:

- **Backend service**: `IMapService` implemented by `MapService` queries the existing `IpAddresses` table using GeoIP coordinates already stored by the IP intelligence layer.
- **Filterable endpoints**: `MapController` provides endpoints for map points, attack points, IP details, and distinct countries. Filters include date range, country code, minimum threat score, attacks only, and blocked only.
- **Frontend map**: The map page uses `leaflet` directly with OpenStreetMap tiles. Threats are rendered as color-coded circle markers by score, and attacks use distinct markers with popups showing IP, score, request count, and attack count.
- **IP explorer**: A separate page lets administrators search for any tracked IP and view its GeoIP, ISP, ASN, VPN/proxy/Tor/datacenter flags, threat score, and activity counts.
- **Admin-only access**: All map endpoints require the `Administrator` role.

## Consequences

- **Pros**:
  - Reuses existing GeoIP data and IP intelligence infrastructure.
  - No additional server-side map rendering or paid map service required.
  - Decoupled map and IP explorer pages keep the UI focused.

- **Cons**:
  - Map accuracy depends on the configured GeoIP provider; null providers yield no points.
  - Large numbers of points may require client-side clustering in the future.
  - OpenStreetMap tile usage may need attribution compliance and rate consideration at scale.

## Alternatives Considered

- **Mapbox/Google Maps**: Rejected to avoid external API keys and dependencies.
- **Server-side rendered tiles**: Rejected as overkill for this phase; client-side markers are sufficient.
