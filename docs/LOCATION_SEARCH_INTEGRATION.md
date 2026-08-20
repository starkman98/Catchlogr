# Location Search Integration

## Purpose

Location search lets a user choose a named lake or other place without granting
the mobile app access to the device's current position. The selected result
provides both a readable location name and the coordinates required for weather
enrichment.

The current provider is [LocationIQ](https://locationiq.com/). Its Search API is
compatible with OpenStreetMap's Nominatim geocoder and exposes OpenStreetMap
feature classes and types that allow water features to be prioritized.

Location search is an optional convenience. A failed search must never prevent
the user from saving a fishing trip.

## Architecture

The LocationIQ access token belongs only to the API:

```text
Add/edit trip page
    -> AddEditFishingTripViewModel
    -> ILocationSearchApiClient
    -> GET /api/locations/search?query=...
    -> ILocationSearchService
    -> LocationIQ Search API
```

The mobile app never calls LocationIQ directly and never contains the LocationIQ
access token. It communicates with `FishingLog.Api` over HTTP or HTTPS, following
the same boundary as the rest of the application.

Location search itself requires connectivity. Selecting a result stores its name,
latitude, and longitude in the normal offline-first trip model, so the trip can
still be saved locally and synchronized later.

## User behavior

The add/edit trip page supports two independent ways to set coordinates:

1. **Use current location** requests device-location permission and uses the
   captured coordinates.
2. **Search by name** sends only the entered text to the FishingLog API. It does
   not request or transmit the device's current position.

For named search:

- the query must contain between 2 and 100 characters;
- the app displays at most five matches;
- selecting a match fills `LocationName`, `Latitude`, and `Longitude`;
- the selected coordinates are later used for server-side weather enrichment;
- clearing the location removes the coordinates, and automatic weather lookup is
  skipped;
- a search error leaves the form usable and the trip can still be saved.

For ambiguous names, include a region or country, for example:

```text
Vänern, Sweden
Siljan, Dalarna
Lake Tahoe, United States
```

A large lake result normally represents the lake's centre point. That is adequate
for general weather enrichment, but a future map pin is better when weather for a
specific shore, bay, or fishing spot is important.

## Provider request

The Infrastructure adapter calls the European LocationIQ endpoint by default:

```http
GET https://eu1.locationiq.com/v1/search
    ?key=LOCATIONIQ_ACCESS_TOKEN
    &q=V%C3%A4nern%2C%20Sweden
    &format=json
    &addressdetails=1
    &normalizeaddress=1
    &accept-language=en
    &limit=10
    &source=nom
```

`source=nom` restricts the search to LocationIQ's OpenStreetMap/Nominatim source.
The provider may return ten candidates; FishingLog ranks them and returns the best
five to the mobile app.

The ranking order is:

1. lakes, reservoirs, ponds, basins, and other natural water features;
2. rivers, streams, canals, and other waterways;
3. ordinary places in their original provider order.

Water ranking improves fishing-related searches without excluding cities or other
locations when no matching water feature exists. Provider-specific JSON models and
ranking logic remain inside `FishingLog.Infrastructure`.

Reference: [LocationIQ Search / Forward Geocoding](https://docs.locationiq.com/docs/search-forward-geocoding).

## API contract

The mobile-facing endpoint is:

```http
GET /api/locations/search?query=V%C3%A4nern%2C%20Sweden
```

Successful response:

```json
[
  {
    "name": "Vänern",
    "displayName": "Vänern, Sweden",
    "latitude": 58.9,
    "longitude": 13.5
  }
]
```

The contract is provider-neutral. `LocationSearchResult` does not expose
LocationIQ-specific feature classes, identifiers, or raw responses. This keeps the
mobile app unchanged if the provider is replaced later.

Invalid queries return `400 Bad Request` with validation details. A provider `404`
is mapped to an empty result list. Other provider or connectivity failures are
reported as unavailable by the mobile ViewModel without blocking trip creation.

## Local development configuration

Create an account and obtain an access token using the
[LocationIQ access-token guide](https://docs.locationiq.com/docs/locationiq-access-token).

The API project already has a `UserSecretsId`. From the repository root, store the
token with:

```powershell
dotnet user-secrets set "LocationSearch:LocationIQ:ApiKey" "YOUR_LOCATIONIQ_KEY" `
  --project .\src\FishingLog.Api\FishingLog.Api.csproj
```

Do not paste the real token into source-controlled configuration, documentation,
logs, tests, or mobile `appsettings` files.

The default regional endpoint is `https://eu1.locationiq.com`. It normally does
not need configuration. To override it locally:

```powershell
dotnet user-secrets set "LocationSearch:LocationIQ:BaseUri" "https://eu1.locationiq.com" `
  --project .\src\FishingLog.Api\FishingLog.Api.csproj
```

Restart the API after changing user-secrets. The API validates configuration at
startup and intentionally refuses to start when the token is missing or the base
URI is not absolute HTTPS.

No EF Core or SQLite migration is required for the provider switch. It reuses the
existing trip name and coordinate fields.

## Production configuration

Use environment variables or the deployment platform's secret store:

```text
LocationSearch__LocationIQ__ApiKey
LocationSearch__LocationIQ__BaseUri=https://eu1.locationiq.com
```

Before commercial release:

- select a LocationIQ plan whose usage and commercial terms match the application;
- monitor request quotas and rate-limit responses;
- add API-side rate limiting and, where allowed by the provider terms, caching;
- preserve visible LocationIQ and OpenStreetMap attribution;
- rotate the token if it is exposed;
- review LocationIQ's current terms rather than relying on values recorded in this
  document.

The `ILocationSearchService` abstraction allows another provider to be introduced
without changing the endpoint or mobile client.

## Security and privacy

- The access token is stored only in API configuration.
- LocationIQ requires the token in the request URL. Built-in HTTP request logging
  is therefore disabled only for the LocationIQ typed client so the complete URL
  and token are not written to application logs.
- Named search sends the entered query to LocationIQ through the API, but it does
  not read the device's position.
- The mobile UI displays `Search powered by LocationIQ · © OpenStreetMap
  contributors` whenever search results are shown.
- Avoid logging search text together with user identity or exact coordinates.

Reference: [LocationIQ security guidance](https://docs.locationiq.com/docs/authentication).

## Important files

| Responsibility | File |
|---|---|
| Shared result contract | `src/FishingLog.Contracts/LocationDTOs/LocationSearchResult.cs` |
| Provider-neutral interface | `src/FishingLog.Application/Interfaces/ILocationSearchService.cs` |
| LocationIQ configuration | `src/FishingLog.Infrastructure/Location/LocationIqOptions.cs` |
| LocationIQ response model | `src/FishingLog.Infrastructure/Location/LocationIqSearchResult.cs` |
| Provider adapter and water ranking | `src/FishingLog.Infrastructure/Location/LocationIqLocationSearchService.cs` |
| Minimal API endpoint | `src/FishingLog.Api/Endpoints/LocationEndpoints.cs` |
| API DI and options validation | `src/FishingLog.Api/Program.cs` |
| Mobile API abstraction | `src/FishingLog.Mobile/Services/ILocationSearchApiClient.cs` |
| Mobile typed API client | `src/FishingLog.Mobile/Services/LocationSearchApiClient.cs` |
| Mobile DI | `src/FishingLog.Mobile/MauiProgram.cs` |
| Search and selection behavior | `src/FishingLog.Mobile/ViewModels/AddEditFishingTripViewModel.cs` |
| Search UI and attribution | `src/FishingLog.Mobile/Pages/AddEditFishingTripPage.xaml` |
| Provider unit tests | `tests/FishingLog.Tests/Location/LocationIqLocationSearchServiceTests.cs` |

## Testing

Unit tests use a stub `HttpMessageHandler` and never call the live provider. They
cover:

- request path, parameters, encoding, and access token;
- European and overridden regional endpoints;
- mapping provider coordinates and display names;
- water-feature prioritization and stable ordering;
- invalid provider rows;
- empty and `404 Not Found` responses;
- missing access-token behavior;
- query validation and cancellation.

Run the focused tests:

```powershell
dotnet test .\tests\FishingLog.Tests\FishingLog.Tests.csproj `
  --filter FullyQualifiedName~LocationIqLocationSearchServiceTests
```

Run the complete regression suite:

```powershell
dotnet test .\tests\FishingLog.Tests\FishingLog.Tests.csproj
```

### Manual verification

1. Configure the LocationIQ token and restart the API.
2. Start the mobile app with the API base URL configured correctly.
3. Enter a known lake plus its region or country.
4. Select a result and confirm the readable name is displayed.
5. Save and synchronize the trip.
6. Confirm weather enrichment uses the selected coordinates.
7. Deny device-location permission and verify named search still works.
8. Disconnect the network and verify search reports that it is unavailable while
   the trip can still be saved.

## Troubleshooting

### API fails during startup

If the error says the LocationIQ API key is required, set the user-secret in the
API project and restart it. Do not put the key in the mobile project.

### Search always reports unavailable

- Confirm the FishingLog API is running and reachable from the device.
- Confirm the mobile API base URL points to the development computer rather than
  `localhost` when testing on a physical device.
- Confirm the LocationIQ token is valid and has remaining quota.
- Inspect API status codes, but never log the full outgoing LocationIQ URL.

### A lake is not found

- Add the region or country to reduce ambiguity.
- Try the lake's official or local-language name.
- Confirm the feature exists and is named in OpenStreetMap.
- Remember that very small or unnamed fishing waters may not be available through
  forward geocoding; a future map-pin picker is the appropriate fallback.

## Future improvements

- Add an interactive map-pin picker for unnamed waters and exact fishing spots.
- Add optional country or map-bounds biasing without hard-coding Sweden.
- Make the result language follow the user's app language.
- Add provider-aware caching and API-side rate limiting.
- Add reverse geocoding for device-captured coordinates if a readable name becomes
  important enough to justify the extra provider request.
