# Location search

Named search is an online convenience; it must not prevent offline trip saving.
Device-location capture is a separate action requiring platform permission.
Named search sends the entered text, not the device's current position.

## Flow and contract

```text
AddEditFishingTripViewModel -> ILocationSearchApiClient
    -> authenticated GET /api/locations/search?query=...
    -> ILocationSearchService -> LocationIQ
```

The endpoint trims queries and accepts 2–100 characters, returning 400 with
validation details otherwise. It returns up to five provider-neutral
LocationSearchResult records: name, displayName, latitude, longitude.

Selecting a result fills LocationName and coordinates in the local trip.
Clearing location removes coordinates; weather enrichment requires coordinates.
Search errors leave the form usable. A named lake may represent its centre
rather than the precise fishing spot.

## Provider behavior

The [Infrastructure adapter](../src/Catchlogr.Infrastructure/Location/LocationIqLocationSearchService.cs)
requests /v1/search from the configured HTTPS base URI (default
https://eu1.locationiq.com). It requests ten candidates from the Nominatim source,
English labels, JSON, and address details.

Invalid rows are removed. Lakes/reservoirs/ponds/basins/water rank first,
waterways second, other results last; provider order is preserved within each
rank. A provider 404 becomes an empty list; other HTTP errors propagate.

## Configuration and privacy

Set LocationSearch:LocationIQ:ApiKey in API user secrets as described in
[local setup](../README.md#2-configure-api-secrets). Optional
LocationSearch:LocationIQ:BaseUri overrides the regional endpoint. Environment
variables use double underscores, for example LocationSearch__LocationIQ__ApiKey.
The API validates a non-empty key and absolute HTTPS base URI at startup.

The key never belongs in Mobile. LocationIQ uses it in the request URL, so the
typed client's built-in HTTP logging is disabled in API Program.cs. Do not log
full provider URLs, search text linked to identity, or exact coordinates.
Search results show LocationIQ/OpenStreetMap attribution.

Before release, verify provider terms, attribution, and quotas against the
provider's current documentation. Planned rate limiting, caching, and map
selection live in [Roadmap](ROADMAP.md).

## Verification

Run LocationIqLocationSearchServiceTests in Catchlogr.Tests using the
[validation guide](VALIDATION.md). Tests cover mapping, ranking, invalid rows,
empty/404 responses, configuration, query validation, and cancellation.

For a UI smoke check, search a known lake plus region, select it, save, and sync.
Repeat with device-location permission denied; named search should still work.
Disconnect the network; search should fail without blocking local trip saving.

If search is unavailable, check API reachability, backend/device URL selection,
and provider credentials/quota. If a lake is missing, add region/country or try
its local name. See [mobile configuration](MOBILE_CONFIGURATION.md) for addresses.
