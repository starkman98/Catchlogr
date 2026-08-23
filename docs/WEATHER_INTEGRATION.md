# Weather Integration

## Purpose

Enrich a fishing trip with weather for its location and start time while preserving the application's offline-first architecture:

- Mobile records trips locally and syncs them through the API.
- Only the API calls the external weather provider.
- PostgreSQL remains the system of record for enriched weather.
- SQLite stores a downloaded copy for offline display.
- A weather-provider failure must never prevent a trip from being saved or synced.

The initial provider is [Open-Meteo](https://open-meteo.com/en/docs). All provider-specific models and HTTP behavior remain in `FishingLog.Infrastructure`, behind an Application-layer interface, so the provider can be replaced later.

Users can provide weather coordinates through device location or a named lake and
place search. The latter uses LocationIQ through the FishingLog API and is
documented in [Location Search Integration](LOCATION_SEARCH_INTEGRATION.md).

## MVP decision

Store one weather sample per fishing trip: the hourly sample nearest to `FishingTrip.StartTime`.

This is deliberately smaller than storing an hourly series for the entire trip. A separate `WeatherObservation` entity can be introduced later if analytics need changing conditions throughout long trips or weather at each catch time.

### Data ownership

The existing fields have these meanings:

- `WaterTemp`: user-entered or measured water temperature. Do not fill it with atmospheric data.
- `WeatherDescription`: user-entered free text. Do not overwrite it with provider text.
- `Latitude` and `Longitude`: the coordinates used for weather lookup.

The new normalized weather fields are owned by the server:

- `AirTemperatureC`
- `WeatherCode`
- `WindSpeedMps`
- `WindDirectionDegrees`
- `PressureHpa`
- `WeatherSampleTimeUtc`
- `WeatherProvider`

They belong in `FishingTripResponse`, but not in `CreateFishingTripRequest` or `UpdateFishingTripRequest`. This prevents an offline client from overwriting authoritative enrichment with stale values.

## How the weather code works

Open-Meteo returns `weather_code`, a numeric WMO weather interpretation code. It describes the main condition for one sample. It is not a temperature, severity score, or provider database identifier.

Common values are:

| Code | Meaning |
|---:|---|
| 0 | Clear sky |
| 1 | Mainly clear |
| 2 | Partly cloudy |
| 3 | Overcast |
| 45, 48 | Fog or depositing rime fog |
| 51, 53, 55 | Light, moderate, or dense drizzle |
| 56, 57 | Freezing drizzle |
| 61, 63, 65 | Slight, moderate, or heavy rain |
| 66, 67 | Freezing rain |
| 71, 73, 75 | Slight, moderate, or heavy snowfall |
| 77 | Snow grains |
| 80, 81, 82 | Slight, moderate, or violent rain showers |
| 85, 86 | Snow showers |
| 95 | Thunderstorm |
| 96, 99 | Thunderstorm with hail |

Persist the integer code rather than an English description or icon name. The presentation layer can translate the stable code into localized text and an app-specific icon. Unknown future codes must display a neutral fallback such as `Unknown`, not throw an exception.

Reference: [Open-Meteo WMO weather-code table](https://open-meteo.com/en/docs#weathervariables).

## Request behavior

Request these hourly variables:

```text
temperature_2m,
weather_code,
wind_speed_10m,
wind_direction_10m,
pressure_msl
```

Always include:

```text
timezone=UTC
wind_speed_unit=ms
start_date=yyyy-MM-dd
end_date=yyyy-MM-dd
```

Example:

```http
GET https://api.open-meteo.com/v1/forecast
    ?latitude=59.3293
    &longitude=18.0686
    &hourly=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m,pressure_msl
    &wind_speed_unit=ms
    &timezone=UTC
    &start_date=2026-08-19
    &end_date=2026-08-19
```

Choose the response timestamp with the smallest absolute difference from `StartTime`. When two samples are equally close, choose the earlier one for deterministic behavior.

### Endpoint selection

Use the following routing inside the Infrastructure implementation:

| Trip time | Endpoint |
|---|---|
| Today or future within the supported forecast window | `https://api.open-meteo.com/v1/forecast` |
| Past date from 2021 onward | `https://historical-forecast-api.open-meteo.com/v1/forecast` |
| Earlier than 2021 | `https://archive-api.open-meteo.com/v1/archive` |

The historical-forecast API closely matches recent conditions and uses the same response shape as the forecast API. The archive API provides longer historical coverage but represents reanalysis rather than an observation at the exact fishing spot.

## Implementation sequence

Implement and verify one stage at a time. Do not begin with the mobile UI.

### Stage 1: Application abstraction

Create these files:

```text
src/FishingLog.Application/
├── Interfaces/IWeatherService.cs
└── Weather/
    ├── WeatherSnapshot.cs
    └── WmoWeatherCodeDescriptions.cs
```

`IWeatherService` should be provider-neutral:

```csharp
/// <summary>
/// Retrieves normalized weather conditions for fishing-trip locations.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets the weather sample nearest to the supplied UTC timestamp.
    /// </summary>
    Task<WeatherSnapshot?> GetWeatherAsync(
        double latitude,
        double longitude,
        DateTime timestampUtc,
        CancellationToken ct = default);
}
```

`WeatherSnapshot` should be an immutable record containing the seven server-owned values listed above. `WmoWeatherCodeDescriptions` should contain the complete switch expression for the table above and return `Unknown` for unsupported values.

Validation at this boundary:

- latitude must be from `-90` through `90`;
- longitude must be from `-180` through `180`;
- `timestampUtc.Kind` must be `DateTimeKind.Utc`.

### Stage 2: Open-Meteo adapter

Create these files:

```text
src/FishingLog.Infrastructure/Weather/
├── OpenMeteoWeatherService.cs
├── OpenMeteoWeatherResponse.cs
└── OpenMeteoHourlyWeather.cs
```

Responsibilities of `OpenMeteoWeatherService`:

1. Validate arguments.
2. Select the forecast, historical-forecast, or archive endpoint.
3. Build the request using invariant-culture coordinate formatting.
4. Deserialize only the fields the application needs.
5. find the nearest hourly index.
6. Map that index into `WeatherSnapshot`.
7. Return `null` when the response contains no hourly samples.
8. Let cancellation propagate.

Do not return Open-Meteo response classes outside Infrastructure and do not store the raw JSON response.

Register it in `src/FishingLog.Api/Program.cs`:

```csharp
builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
```

The non-commercial development endpoint does not need an API key. If the application becomes commercial, configure the paid customer endpoint and key through options, user secrets during development, and a production secret store. Open-Meteo data requires attribution; add it to the eventual weather UI. See [Open-Meteo pricing and licensing](https://open-meteo.com/en/pricing).

### Stage 3: Server persistence

Edit `src/FishingLog.Domain/Entities/FishingTrip.cs` and add nullable properties for all new weather values. Add XML summaries to every property.

Edit `src/FishingLog.Infrastructure/Persistence/Configurations/FishingTripConfiguration.cs`:

- limit `WeatherProvider` to a reasonable length such as 100;
- configure `WeatherSampleTimeUtc` with the repository's existing UTC conversion pattern;
- keep all weather values nullable so existing trips remain valid.

Create an EF Core migration such as:

```powershell
dotnet ef migrations add AddFishingTripWeather `
  --project src/FishingLog.Infrastructure `
  --startup-project src/FishingLog.Api
```

Review the generated migration before applying it. It should only add nullable columns and update the model snapshot.

### Stage 4: Response contract and mapping

Edit:

```text
src/FishingLog.Contracts/FishingTripDTOs/FishingTripResponse.cs
src/FishingLog.Application/Services/FishingTripService.cs
```

Add the new weather fields to `FishingTripResponse` and to `MapFromTripToResponse`.

Do not add them to:

```text
CreateFishingTripRequest.cs
UpdateFishingTripRequest.cs
```

Those requests describe user-editable data; provider weather is server-owned.

### Stage 5: Enrichment orchestration

Inject `IWeatherService` and `ILogger<FishingTripService>` into `FishingTripService`.

Add one private method with one responsibility:

```csharp
private async Task<bool> TryEnrichWeatherAsync(
    FishingTrip trip,
    CancellationToken ct)
```

Behavior:

1. Return immediately unless both coordinates exist.
2. Call `IWeatherService` with the coordinates and UTC `StartTime`.
3. Copy a returned snapshot to the domain entity.
4. Do not catch `OperationCanceledException` when the caller requested cancellation.
5. Catch provider HTTP, timeout, and malformed-response errors; log a warning without coordinates and leave weather null.

Call the method during creation before `AddAsync`. A weather outage must not prevent the trip from being persisted.

During update, refetch weather only when the location coordinates or start time changed, or when no weather sample exists. This avoids spending one provider call on every ordinary notes edit.

Failed enrichment retries through an explicit server command:

```http
POST /api/fishing-trips/{id}/weather/retry
```

The command returns the current `FishingTripResponse`. It persists and advances
`LastModified` only when weather was added successfully. Trips without coordinates,
trips that already have weather, and temporary provider failures remain unchanged.
A durable background job is a later production improvement, not required for this
vertical slice.

Never log precise coordinates at Information level because they are user location data.

### Stage 6: Local SQLite and synchronization

Add matching nullable properties to:

```text
src/FishingLog.Sync/Entities/FishingTripLocalEntity.cs
```

Then update both response-to-local mapping methods in:

```text
src/FishingLog.Sync/Services/FishingTripSyncService.cs
```

The relevant methods are currently:

- `MapToLocalEntity`
- `ApplyRemoteToLocal`

Do not include server weather in `MapToCreateRequest` or `MapToUpdateRequest`.

There is one important existing update path to adjust. `UpdateTripOnServerAsync` currently calls `MarkAsSyncedAsync` using only the returned timestamp. Change it to apply the complete returned response and save it, matching `UploadNewTripAsync`; otherwise freshly enriched weather returned by an update will not immediately reach SQLite.

`LocalDatabase.InitializeAsync` already calls `CreateTableAsync<FishingTripLocalEntity>()`. Verify both a fresh database and an existing development database after adding the columns; do not assume schema behavior without testing an upgrade.

Synchronization has a third step after upload and download: retry missing weather.
At the start of a sync, capture clean trips that have a server ID, coordinates, and
no `WeatherSampleTimeUtc`. After normal reconciliation, re-read and revalidate each
candidate, call the explicit retry endpoint, and apply its complete response with
`ApplyRemoteToLocal`.

Capturing candidates before upload prevents a newly created trip from calling the
provider twice during the same sync. Network or provider failure during this step
must not fail the rest of synchronization. The trip remains eligible on a later
sync.

### Stage 7: Mobile presentation

Edit:

```text
src/FishingLog.Mobile/ViewModels/FishingTripDetailsViewModel.cs
src/FishingLog.Mobile/Pages/FishingTripDetailsPage.xaml
```

Add read-only observable properties for temperature, condition, wind, direction, pressure, and the sample time. Populate them in `LoadAsync` from `FishingTripLocalEntity`.

Use the WMO mapping to derive text from `WeatherCode`. Initially use text or app-native symbols; do not bind the UI to Open-Meteo icon URLs. Show the weather section only when `WeatherSampleTimeUtc` has a value.

Suggested display:

```text
Partly cloudy
14.2 °C · Wind 3.1 m/s SW · 1012 hPa
Weather near trip start · Open-Meteo
```

Add visible attribution, for example `Weather data by Open-Meteo`, linked or accompanied by an About-page attribution.

## Testing checklist

### Application tests

- Every supported WMO code returns the expected description.
- An unsupported code returns `Unknown`.
- Create without coordinates does not call `IWeatherService`.
- Create with coordinates applies a returned snapshot.
- Provider failure still saves and returns the trip.
- Cancellation is not swallowed.
- Updating notes alone does not refetch weather.
- Changing coordinates or `StartTime` does refetch weather.

### Infrastructure tests

Use a stub `HttpMessageHandler`; do not call the live provider in unit tests.

- Request uses invariant decimal coordinates.
- Request explicitly asks for UTC and metres per second.
- Correct endpoint is selected for current, recent historical, and old historical dates.
- Closest hour is selected.
- An exact timestamp selects the exact sample.
- A tie selects the earlier sample.
- Short or missing value arrays map missing values to null instead of throwing.
- Empty hourly data returns null.
- Invalid coordinates and non-UTC timestamps are rejected.

### Sync tests

- Server weather downloads into a new local trip.
- Server weather updates an existing clean local trip.
- Create and update request DTOs never contain server weather.
- The response from an uploaded update is fully applied locally.
- A clean trip with coordinates and missing weather calls the explicit retry endpoint.
- A successful retry response is fully applied locally.
- Dirty, deleted, coordinate-less, and already enriched trips are skipped.
- A newly uploaded trip is not retried during the same sync.
- Retry network failure does not fail synchronization.

### Manual test

1. Start the API with the forecast endpoint temporarily pointed at an unavailable
   local port.
2. Create and sync a trip with coordinates and a current start time.
3. Confirm the API saves the trip and the mobile app remains usable without weather.
4. Restore the real Open-Meteo endpoint and restart the API.
5. Sync again without editing the trip.
6. Confirm the retry command enriches PostgreSQL and its response updates SQLite.
7. Confirm weather appears on the details page.
8. Confirm `WaterTemp` and the user's `WeatherDescription` remain unchanged.

## Recommended pull-request boundaries

Keep the implementation reviewable:

1. **Weather abstraction and Open-Meteo adapter** — interface, snapshot, WMO mapping, HTTP adapter, DI, and unit tests.
2. **Server enrichment and persistence** — domain fields, EF migration, response DTO, service orchestration, and tests.
3. **Offline sync and mobile display** — local fields, mapping, update-response fix, ViewModel, XAML, and tests.

## Definition of done

- A trip with valid coordinates is enriched for the hour nearest its UTC start time.
- The API remains usable during provider outages.
- No provider types escape Infrastructure.
- Provider weather cannot be overwritten by mobile request DTOs.
- Weather syncs to SQLite and is available offline.
- Missing provider weather retries on a later sync without requiring a user edit.
- UTC and units are explicit and tested.
- WMO code text has an unknown-code fallback.
- Open-Meteo attribution is visible.
- Existing water temperature and free-text weather behavior is preserved.
