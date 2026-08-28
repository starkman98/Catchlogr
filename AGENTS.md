# Instructions — Catchlogr

## Project Context
This repository contains a production-ready fishing log application with:
- **Catchlogr.Mobile**: .NET MAUI app (offline-first)
- **Catchlogr.Api**: ASP.NET Core Web API (system of record)
- **Catchlogr.Contracts**: Shared DTOs/contracts
- **Catchlogr.Domain**: Domain entities and interfaces
- **Catchlogr.Application**: Business logic and services
- **Catchlogr.Infrastructure**: Data access and external dependencies
- **Local persistence**: sqlite-net-pcl in Mobile
- **Server persistence**: PostgreSQL via EF Core

---

## Core Development Principles

### SOLID & Design Principles
- **KISS (Keep It Simple, Stupid)**: Prefer simple, straightforward solutions
- **SRP (Single Responsibility Principle)**: Each class should have one reason to change
- **OCP (Open/Closed Principle)**: Open for extension, closed for modification

### Code Organization
- **Always use individual files** for classes, interfaces, DTOs, enums, etc.
- **Always add XML summary comments** to public types and members
- **Always show your plan first** before implementing changes

### Dependency Injection
- Always use dependency injection; avoid service locators or singletons except where appropriate
- Register services in proper DI containers (Program.cs for API, MauiProgram.cs for Mobile)

### Repository Pattern
- Use repository pattern for data access
- Repositories should be thin wrappers around data access
- Keep domain logic in services, not repositories

---

## Architecture Rules

### Mobile ↔ API Communication
- **Mobile NEVER talks directly to PostgreSQL**
- Mobile talks to the API over HTTPS only
- Mobile uses local SQLite as offline cache + queue (with dirty flags)
- API enforces authentication, authorization, and business rules

### Project References
- **No circular project references**
- Mobile must NOT reference Infrastructure
- Follow proper layering: UI → Application → Domain ← Infrastructure

### DTOs and Contracts
- **Do not mix Domain entities with API contracts**
- Use DTOs in Catchlogr.Contracts
- Call DTOs meaningful names: `CreateFishingTripRequest`, `FishingTripResponse`, etc.
- Prefer immutable DTOs (use `record` types) where practical
- Use DTOs where it makes sense, not everywhere

---

## API Development Standards

### API Endpoints
- **Always use Minimal APIs** for endpoints
- Keep endpoint handlers thin; call application services for logic
- Use proper HTTP semantics:
  - `201 Created` for successful creation
  - `404 Not Found` for missing resources
  - `409 Conflict` for conflicts
  - `400 Bad Request` for validation errors

### Validation
- Validate inputs using FluentValidation or minimal validation helpers
- Return detailed validation errors to clients

### Data Access
- Use EF Core with migrations for PostgreSQL
- Store all timestamps as UTC
- Do NOT expose internal database IDs; use GUIDs for external identifiers
- Use cancellation tokens for all I/O-bound methods

### Example Minimal API Structure
```csharp
/// <summary>
/// Registers fishing trip endpoints
/// </summary>
public static class FishingTripEndpoints
{
    public static void MapFishingTripEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fishing-trips")
            .WithTags("FishingTrips");

        group.MapGet("/", GetAllTrips);
        group.MapPost("/", CreateTrip);
    }

    private static async Task<IResult> GetAllTrips(
        IFishingTripService service, 
        CancellationToken ct)
    {
        // Implementation
    }
}
```

---

## Mobile Development Standards

### MVVM Pattern
- Use MVVM pattern consistently
- ViewModels should NOT directly call SQLite or HttpClient
- ViewModels call services/repositories through abstractions

### Service Abstractions
- Use an `ApiClient` abstraction (typed client) for API calls
- Use repository interfaces for local data access
- Use a `SyncService` for orchestration

### Sync Service Pattern
The sync service should follow these steps:
1. Upload dirty local rows to server
2. Download remote changes since last sync cursor
3. Apply changes to local DB in a transaction

### Local Database Schema
Local SQLite tables should include:
- `Id` (local primary key)
- `ServerId` (GUID from server)
- `LastModifiedUtc` (sync timestamp)
- `IsDirty` (needs upload)
- `IsDeleted` (soft delete flag)

### UI Thread Management
- **Avoid blocking the UI thread**
- Long operations must be `async`/`await` and show progress indicators
- Use `MainThread.BeginInvokeOnMainThread()` when updating UI from background threads

---

## Coding Standards

### Async/Await
- Use async/await all the way through the call stack
- **NEVER block with `.Result` or `.Wait()`**
- Always include `CancellationToken` parameters for I/O-bound methods

### Null Safety
- Use nullable reference types
- Validate inputs and guard against nulls where appropriate

### Error Handling
- Use structured exception handling
- Log errors appropriately but never log secrets or tokens
- Return meaningful error messages to users

### Naming Conventions
- Use clear, descriptive names
- Follow C# naming conventions:
  - PascalCase for public members
  - camelCase for private fields (with _ prefix)
  - Interfaces start with `I`

---

## Security Baseline

### Authentication & Authorization
- Prepare for JWT authentication
- Do NOT implement insecure "quick auth" shortcuts
- API must enforce authorization rules

### Secrets Management
- **Never log secrets, tokens, or connection strings**
- Use user secrets for local development
- Use environment variables or Azure Key Vault for production

### CORS
- CORS should be locked down in production
- Only allow specific origins, not wildcards

---

## Code Generation Guidelines

### When Generating Code
1. **Show your plan first** before implementing
2. Prefer small, composable classes
3. Include XML summary comments
4. Include example usage in comments
5. Note where to register services (DI)
6. If uncertain, ask a short clarifying question OR choose the simplest option that matches these rules

### File Organization
- One class/interface/enum per file
- File name matches the type name
- Group related files in appropriate folders

### Testing Considerations
- Write code that is testable (uses DI, small methods)
- Prefer pure functions where possible
- Keep side effects isolated

---

## Common Patterns

### API Service Registration
```csharp
// In Program.cs
builder.Services.AddScoped<IFishingTripRepository, FishingTripRepository>();
builder.Services.AddScoped<IFishingTripService, FishingTripService>();
```

### Mobile Service Registration
```csharp
// In MauiProgram.cs
builder.Services.AddSingleton<IApiClient, ApiClient>();
builder.Services.AddSingleton<ILocalDatabase, LocalDatabase>();
builder.Services.AddTransient<FishingTripViewModel>();
```

### Repository Example
```csharp
/// <summary>
/// Repository for fishing trip data access
/// </summary>
public interface IFishingTripRepository
{
    Task<FishingTrip?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<FishingTrip>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(FishingTrip trip, CancellationToken ct = default);
    Task UpdateAsync(FishingTrip trip, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

### DateTime Handling

- **Always store DateTime as UTC** — SQLite entities, PostgreSQL columns, and API DTOs must use UTC.
- **Convert at the ViewModel boundary only** — convert UTC → local when loading into date/time pickers using `.ToLocalTime()`, and convert local → UTC when saving using `DateTime.SpecifyKind(..., DateTimeKind.Local).ToUniversalTime()`.
- Use this helper pattern in ViewModels:

```csharp
// Loading from entity into pickers
var local = entity.StartTime.ToLocalTime();
StartDate      = local.Date;
StartTimeOfDay = local.TimeOfDay;

// Saving from pickers back to entity
private static DateTime ToUtc(DateTime localDate, TimeSpan localTime)
    => DateTime.SpecifyKind(localDate.Date + localTime, DateTimeKind.Local).ToUniversalTime();
```

---

### ObservableProperty (CommunityToolkit.Mvvm)

- **Always use partial property syntax** — field-based `[ObservableProperty] private T _field;` is deprecated and not AOT-safe
- Requires `<LangVersion>preview</LangVersion>` in `Catchlogr.Mobile.csproj`

```csharp
// Correct — partial property syntax
[ObservableProperty]
public partial string Name { get; set; } = string.Empty;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsEndDateVisible))]
public partial bool HasEndDate { get; set; }

// Wrong — do not use
[ObservableProperty] private string _name = string.Empty;
```

---

## Questions to Ask

When requirements are unclear, prefer asking:
- "Should this be available offline in the mobile app?"
- "What validation rules apply to this entity?"
- "Should this trigger a sync, or wait for the next scheduled sync?"
- "Do we need pagination for this endpoint?"

Or choose the simplest option that follows the above patterns.
