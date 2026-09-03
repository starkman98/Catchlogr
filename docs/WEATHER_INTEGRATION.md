# Weather integration

Catchlogr enriches fishing trips with a single server-owned Open-Meteo weather
snapshot near the trip start time. Weather is optional: a provider failure never
prevents a trip from being saved or synchronized.

## Data ownership

Users own `WaterTemp` and the free-text `WeatherDescription`. The API never
replaces those values with provider data.

The server owns these nullable fields:

- `AirTemperatureC`;
- `WeatherCode`;
- `WindSpeedMps`;
- `WindDirectionDegrees`;
- `PressureHpa`;
- `WeatherSampleTimeUtc`; and
- `WeatherProvider`.

They appear in `FishingTripResponse` but not create/update request contracts, so
mobile clients cannot submit provider observations.

## Request selection

`OpenMeteoWeatherService` receives valid latitude/longitude and a UTC trip start.
It explicitly requests UTC hourly data and wind speed in metres per second, then
chooses the sample closest to the trip start. An exact match wins; a tie selects
the earlier sample.

The adapter selects among Open-Meteo forecast, historical-forecast, and archive
hosts according to the requested time. Base URIs are configurable under
`Weather:OpenMeteo`. No key is required for the default public endpoints; an
optional commercial API key is supported by `OpenMeteoOptions`.

Provider JSON models remain in Infrastructure. Application exposes only the
provider-neutral `IWeatherService` and immutable `WeatherSnapshot`.

## Enrichment lifecycle

`FishingTripService` attempts enrichment when a trip has coordinates and needs a
new sample, including relevant start-time or coordinate changes. It stores a
snapshot only when the provider returns usable data.

HTTP failures, timeouts, and malformed provider responses are logged without
precise coordinates and leave weather null. Cancellation requested by the caller
is propagated.

Trips missing weather can be retried explicitly:

```http
POST /api/fishing-trips/{id}/weather/retry
Authorization: Bearer <access-token>
```

During sync, `FishingTripSyncService` also retries clean, non-deleted trips that
have a server ID and coordinates but no weather sample. Candidates are captured
before upload so a new trip does not call the provider twice in one sync.

## Persistence and offline display

Weather fields are stored on `FishingTrip` in PostgreSQL and mirrored on
`FishingTripLocalEntity` in SQLite. Complete create, update, download, and retry
responses are applied locally, making the snapshot available offline.

`FishingTripDetailsViewModel` uses `WeatherPresentationFormatter` and the shared
WMO code descriptions to expose condition, temperature, wind, pressure, sample
time, and provider attribution. The weather card is hidden when no sample exists.

Moon phase is separate from provider weather. `IMoonPhaseService` calculates it
from the trip start time on the server; it is returned and synchronized alongside
the trip and formatted by `MoonPhasePresentationFormatter`.

## Main files

| Responsibility | File |
| --- | --- |
| Provider-neutral interface | `src/Catchlogr.Application/Interfaces/IWeatherService.cs` |
| Snapshot | `src/Catchlogr.Application/Weather/WeatherSnapshot.cs` |
| WMO descriptions | `src/Catchlogr.Application/Weather/WmoWeatherCodeDescriptions.cs` |
| Open-Meteo adapter/options | `src/Catchlogr.Infrastructure/Weather` |
| Enrichment orchestration | `src/Catchlogr.Application/Services/FishingTripService.cs` |
| Retry endpoint | `src/Catchlogr.Api/Endpoints/FishingTripEndpoints.cs` |
| SQLite mapping/retry | `src/Catchlogr.Sync/Services/FishingTripSyncService.cs` |
| Mobile presentation | `src/Catchlogr.Mobile/Presentation` |

## Verification

Focused tests:

```powershell
dotnet test tests/Catchlogr.Tests/Catchlogr.Tests.csproj `
  --filter FullyQualifiedName~Weather|FullyQualifiedName~MoonPhase
```

Manual outage/retry check:

1. Point the forecast base URI at an unavailable local port.
2. Create and sync a trip with coordinates and a current UTC start time.
3. Confirm the trip is saved without provider weather.
4. Restore the default endpoint and restart the API.
5. Sync again and confirm weather reaches PostgreSQL and SQLite.
6. Confirm the user's water temperature and free-text weather are unchanged.

## Future work

- Add provider metrics, caching, and rate-limit handling.
- Test SQLite upgrades from every released schema version.
- Consider a separate observation series only if analytics need changing weather
  throughout a trip or weather at individual catch times.
