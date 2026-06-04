# FishingLog Roadmap

## Guiding Principles
- **Offline-first**: Local SQLite is the source of truth on device until synced
- **Server is system of record**: API + PostgreSQL is the canonical source across devices
- **Explicit sync**: Dirty flags + last sync cursor pattern
- **Responsive UI**: async/await everywhere, no blocking calls
- **Incremental delivery**: Ship working features in small, testable increments

---

## Phase 0: Foundation & Project Setup

### Project Structure ✅
- [x] Solution structure with layered architecture
- [x] FishingLog.Domain project (entities, interfaces)
- [x] FishingLog.Application project (business logic, services)
- [x] FishingLog.Infrastructure project (data access, EF Core)
- [x] FishingLog.Api project (ASP.NET Core Web API)
- [x] FishingLog.Mobile project (.NET MAUI)
- [x] FishingLog.Contracts project (DTOs, shared models)

### Development Environment
- [x] Docker Compose for local PostgreSQL
- [x] API appsettings.Development.json with connection strings
- [x] User secrets configuration for sensitive data
- [x] Mobile appsettings.json with API base URL
- [x] README.md with setup instructions
- [x] .gitignore properly configured

### Core Infrastructure (API)
- [x] **Domain**: `FishingTrip` entity with properties (Id, UserId, StartTime, EndTime, Location, Notes, etc.)
- [x] **Infrastructure**: DbContext setup for PostgreSQL
- [x] **Infrastructure**: Initial EF Core migration
- [x] **API**: Program.cs with DI container configuration
- [x] **API**: CORS configuration (locked down)
- [x] **API**: Health check endpoint (`/health`)
- [x] **API**: Global exception handling middleware

### Core Infrastructure (Mobile)
- [x] **Mobile**: SQLite database initialization
- [x] **Mobile**: Base repository interface
- [x] **Mobile**: Base entity with sync metadata (Id, ServerId, LastModifiedUtc, IsDirty, IsDeleted)
- [x] **Mobile**: MauiProgram.cs with DI container
- [x] **Mobile**: ApiClient service abstraction
- [x] **Mobile**: Base ViewModel with INotifyPropertyChanged

---

## Phase 1: MVP — Fishing Trips (Local + Sync)

### 1.1 Domain Layer: Fishing Trip Entity
- [x] Create `FishingTrip` domain entity
  - Id (Guid), UserId (Guid), StartTime, EndTime, Location, WaterType, Notes, CreatedUtc, LastModifiedUtc
- [x] Create `IFishingTripRepository` interface in Domain
- [x] Create `IFishingTripService` interface in Application
- [x] Add XML summary comments to all public members

### 1.2 API: Database & Migrations
- [x] Create `FishingTripRepository` in Infrastructure
  - Implement CRUD operations using EF Core
  - All methods async with CancellationToken
- [x] Create EF Core migration for FishingTrip table
- [x] Apply migration to local PostgreSQL
- [ ] Seed test data (optional, development only)

### 1.3 API: Business Logic
- [x] Create `FishingTripService` in Application
  - GetAllAsync(userId, ct)
  - GetByIdAsync(id, userId, ct)
  - CreateAsync(trip, ct)
  - UpdateAsync(trip, ct)
  - DeleteAsync(id, userId, ct)
- [x] Register services in Program.cs DI container
- [x] Add basic validation (start time < end time, required fields)

### 1.4 API: Endpoints (Minimal APIs)
- [x] Create `FishingTripEndpoints.cs` class
  - `GET /api/fishing-trips` → Get all trips for user
  - `GET /api/fishing-trips/{id}` → Get single trip
  - `POST /api/fishing-trips` → Create trip (returns 201)
  - `PUT /api/fishing-trips/{id}` → Update trip
  - `DELETE /api/fishing-trips/{id}` → Delete trip (soft delete)
- [x] Map endpoints in Program.cs
- [x] Test with Postman/curl (manual verification)

### 1.5 Contracts: DTOs
- [x] Create `CreateFishingTripRequest` record in Contracts
- [x] Create `UpdateFishingTripRequest` record in Contracts
- [x] Create `FishingTripResponse` record in Contracts
- [x] Add FluentValidation validators (optional but recommended)

### 1.6 Mobile: Local Database Schema
- [x] Create `FishingTripLocalEntity` class
  - Id (int, local PK), ServerId (Guid?), StartTime, EndTime, Location, Notes
  - LastModifiedUtc, IsDirty (bool), IsDeleted (bool)
- [x] Create `SyncMetadata` table
  - Id, EntityType, LastSyncUtc
- [x] Create database initialization code
- [x] Test local insert/update/delete

### 1.7 Mobile: Repository Layer
- [x] Create `IFishingTripLocalRepository` interface
  - GetAllAsync(), GetByIdAsync(id), GetDirtyAsync()
  - AddAsync(trip), UpdateAsync(trip), DeleteAsync(id)
  - MarkAsSyncedAsync(id, serverId, timestamp)
- [x] Create `FishingTripLocalRepository` implementation using sqlite-net-pcl
- [x] Register in MauiProgram.cs

### 1.8 Mobile: API Client
- [x] Create `IFishingTripApiClient` interface
  - GetAllAsync(ct), GetByIdAsync(id, ct)
  - CreateAsync(request, ct), UpdateAsync(id, request, ct), DeleteAsync(id, ct)
- [x] Create `FishingTripApiClient` implementation using HttpClient
- [x] Handle HTTP errors gracefully (404, 409, 500)
- [x] Register as singleton in MauiProgram.cs

### 1.9 Mobile: Sync Service (Core)
- [x] Create `IFishingTripSyncService` interface
  - SyncAsync(ct)
- [x] Create `FishingTripSyncService` implementation
  - **Step 1**: Get all dirty local trips
  - **Step 2**: Upload each dirty trip to API (create or update based on ServerId)
  - **Step 3**: Download trips modified since LastSyncUtc from API
  - **Step 4**: Upsert remote trips into local DB (transaction)
  - **Step 5**: Update LastSyncUtc in SyncMetadata
  - **Step 6**: Mark uploaded trips as clean (IsDirty = false)
- [x] Add conflict resolution (last-write-wins for MVP)
- [x] Add error handling and retry logic
- [x] Register in MauiProgram.cs

### 1.10 Mobile: ViewModel & UI
- [x] Create `FishingTripsViewModel`
  - ObservableCollection<FishingTripLocalEntity> Trips
  - LoadTripsCommand, AddTripCommand, EditTripCommand, DeleteTripCommand, SyncCommand
  - Inject IFishingTripLocalRepository and IFishingTripSyncService
- [x] Create `FishingTripsPage.xaml` with list view
  - Display all trips with start time, location, notes
  - Pull-to-refresh triggers sync
  - Tap to edit
- [x] Create `AddEditFishingTripPage.xaml` with form
  - Date/time pickers, location entry, notes field
  - Save button (saves locally, sets IsDirty = true)
- [x] Register pages and ViewModels in MauiProgram.cs
- [x] Add navigation between pages

### 1.11 Testing & Validation
- [x] Unit tests for FishingTripService (Application layer)
- [x] Unit tests for CreateFishingTripRequestValidator (FluentValidation)
- [x] Unit tests for FishingTripSyncService (blocked — see Phase 4 refactor)
- [ ] Integration test: Create trip on Mobile → Sync → Verify in API
- [ ] Integration test: Create trip on API → Sync → Verify on Mobile
- [ ] Manual test: Offline mode (airplane mode, create trips, go online, sync)
- [ ] Manual test: Conflict resolution (edit same trip on two devices)

### 1.12 Documentation
- [x] Update README with "How to Run" instructions and project status
- [x] Document sync algorithm in docs/SYNC_STRATEGY.md
- [x] Add API endpoint documentation (Swagger/OpenAPI)
- [ ] Add troubleshooting guide

---

## Phase 1.x: Technical Improvements

### 1.x SyncService Testability Refactor
- [x] Extract `FishingTripSyncService` from `FishingLog.Mobile` into a new `FishingLog.Sync` class library
- [x] Reference `FishingLog.Sync` from both `FishingLog.Mobile` and `FishingLog.Tests`
- [x] Add unit tests for `FishingTripSyncService` (mock `IFishingTripApiClient`, mock `IFishingTripLocalRepository`)

---

## Phase 2: Catches (Fish Records per Trip)

### 2.1 Domain & Database
- [x] Create `Catch` domain entity (Id, FishingTripId, Species, Length, Weight, PhotoUrl, Notes, CaughtAt)
- [x] Create `ICatchRepository` and `ICatchService` interfaces
- [x] EF Core migration for Catches table (with FK to FishingTrips)
- [x] Create CatchLocalEntity for Mobile

### 2.2 API Implementation
- [x] Implement CatchRepository and CatchService
- [x] Create Catch DTOs (CreateCatchRequest, CatchResponse)
- [x] Create CatchEndpoints (nested under trips: `/api/fishing-trips/{tripId}/catches`)
- [x] Test CRUD operations

### 2.3 Mobile Implementation
- [x] Create CatchLocalRepository
- [x] Create CatchApiClient
- [x] Extend FishingTripSyncService to sync catches
- [ ] Create CatchesViewModel and UI
- [ ] Add "Add Catch" button on trip details page
- [ ] Test sync with catches

---

## Phase 3: Weather & Moon Phase Enrichment

### 3.1 Weather Integration (Server-Side)
- [ ] Choose weather API (OpenWeatherMap, WeatherAPI.com, etc.)
- [ ] Create `IWeatherService` interface in Application
- [ ] Implement WeatherService in Infrastructure
  - Fetch weather based on location + timestamp
  - Store API key in user secrets / Azure Key Vault
- [ ] Add Weather properties to FishingTrip (Temperature, Conditions, WindSpeed, Pressure)
- [ ] Auto-fetch weather when trip is created/synced
- [ ] Display weather on trip details page (Mobile)

### 3.2 Moon Phase Calculation
- [ ] Create `IMoonPhaseService` interface
- [ ] Implement MoonPhaseService (calculate phase based on date)
- [ ] Add MoonPhase property to FishingTrip
- [ ] Display moon phase icon on trip cards

---

## Phase 4: Photo Capture & Upload

### 4.1 Photo Capture (Mobile)
- [ ] Add camera permission to AndroidManifest.xml and Info.plist
- [ ] Create photo capture service using MediaPicker
- [ ] Save photo locally (app data folder)
- [ ] Reference photo path in CatchLocalEntity

### 4.2 Photo Upload (API)
- [ ] Create blob storage account (Azure Blob or local file storage)
- [ ] Create photo upload endpoint (`POST /api/photos`)
- [ ] Accept multipart/form-data
- [ ] Return photo URL

### 4.3 Photo Sync
- [ ] Extend CatchSyncService to upload photos before syncing catch
- [ ] Store PhotoUrl from server in CatchLocalEntity
- [ ] Display photos in catch list and details
- [ ] Handle photo deletion

---

## Phase 5: Authentication & Authorization

### 5.1 User Registration & Login (API)
- [ ] Create `User` entity (Id, Email, PasswordHash, CreatedUtc)
- [ ] Create authentication endpoints (register, login)
- [ ] Use ASP.NET Core Identity or custom JWT implementation
- [ ] Store password securely (bcrypt/PBKDF2)

### 5.2 JWT Token Handling
- [ ] Issue JWT tokens on successful login
- [ ] Add `[Authorize]` to all endpoints (except auth endpoints)
- [ ] Validate tokens in middleware
- [ ] Extract UserId from token claims

### 5.3 Mobile Authentication
- [ ] Create login page (XAML)
- [ ] Store JWT token in SecureStorage
- [ ] Attach token to all API requests (Authorization header)
- [ ] Handle token expiration and refresh

### 5.4 User Context
- [ ] Filter all trips and catches by authenticated user
- [ ] Add UserId to all entities
- [ ] Update all queries to scope by user

---

## Phase 6: Teams & Sharing (Multi-User Collaboration)

### 6.1 Teams Domain
- [ ] Create `Team` entity (Id, Name, CreatedByUserId)
- [ ] Create `TeamMember` entity (Id, TeamId, UserId, Role)
- [ ] Create `TeamFishingTrip` junction (many-to-many)

### 6.2 Sharing Rules
- [ ] Define roles (Owner, Editor, Viewer)
- [ ] Implement permission checks in services
- [ ] Create team endpoints (CRUD teams, add/remove members)

### 6.3 Mobile Team Features
- [ ] Team selection UI
- [ ] Shared trips view (filter by team)
- [ ] Invite members by email

---

## Phase 7: Analytics & Reporting

### 7.1 Statistics
- [ ] Total trips, total catches
- [ ] Average catches per trip
- [ ] Most common species
- [ ] Best fishing times (time of day, moon phase correlation)

### 7.2 Visualizations
- [ ] Charts for catches over time (Line/Bar chart)
- [ ] Species distribution (Pie chart)
- [ ] Heatmap of best fishing locations

### 7.3 Export
- [ ] Export data as CSV
- [ ] Export trip summary as PDF

---

## Phase 8: Polish & Production Readiness

### 8.1 Error Handling & Logging
- [ ] Structured logging (Serilog)
- [ ] Application Insights / Log aggregation
- [ ] User-friendly error messages
- [ ] Retry policies for transient failures

### 8.2 Performance Optimization
- [ ] API response caching
- [ ] Database indexing
- [ ] Lazy loading for large lists (pagination)
- [ ] Image compression for photos

### 8.3 Testing
- [ ] Comprehensive unit test coverage (>80%)
- [ ] Integration tests for all endpoints
- [ ] UI tests for critical flows (Appium or similar)
- [ ] Load testing for API

### 8.4 Deployment
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] API deployed to Azure App Service / AWS / Docker
- [ ] Database migrations automated
- [ ] App published to Google Play / App Store

### 8.5 Documentation
- [ ] User guide
- [ ] Developer documentation
- [ ] Architecture decision records (ADRs)
- [ ] Contribution guidelines

---

## Future Ideas (Backlog)

- [ ] Social features (share catches, leaderboards)
- [ ] Fishing spot recommendations (ML-based)
- [ ] Integration with fish finder devices
- [ ] Tide predictions
- [ ] Barometric pressure tracking
- [ ] Bait/lure effectiveness tracking
- [ ] Regulatory compliance (catch limits, protected species)
- [ ] Dark mode support
- [ ] Localization (multiple languages)
- [ ] Widget for quick trip logging

---

## Success Metrics

- **MVP Success**: User can log a fishing trip offline, sync when online, and view on another device
- **Phase 2 Success**: User can track individual catches with species, weight, and photos
- **Phase 3 Success**: Trips are automatically enriched with weather and moon phase
- **Phase 5 Success**: Multiple users can securely access their own data
- **Phase 6 Success**: Users can collaborate on shared fishing trips within teams
- **Production Success**: 100+ active users, <1% crash rate, 99.9% API uptime

---

## Notes

- Each phase should be shippable independently
- Prioritize user feedback after MVP
- Keep security top-of-mind from the start
- Document decisions and trade-offs
- Celebrate small wins! 🎣
