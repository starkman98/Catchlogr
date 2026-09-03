# Catchlogr 🎣

A cross-platform fishing log application built with .NET MAUI (mobile) and ASP.NET Core (API). Track your fishing trips, catches, and sync data across devices with offline-first architecture.

## 🚧 Status

> ⚠️ **Active development (early-stage / MVP in progress)**  
> This project is currently under active development.  
> Features, structure, and APIs may change frequently.

---

## 🧠 Tech Stack

### Backend

- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL (Docker)

### Frontend (Mobile)

- .NET MAUI

### Testing

- xUnit

---

## 🏗️ Architecture

- **Offline-first mobile app**: Local SQLite database with explicit sync
- **Server system of record**: PostgreSQL backend via ASP.NET Core Web API
- **Clean architecture**: Domain, Application, Infrastructure, API, and Mobile layers
- **Sync strategy**: Dirty flags + last sync cursor pattern

## 📁 Project Structure

```
Catchlogr/
├── src/
│   ├── Catchlogr.Api/              # ASP.NET Core Web API (Minimal APIs)
│   ├── Catchlogr.Application/      # Business logic and services
│   ├── Catchlogr.Contracts/        # Shared DTOs and contracts
│   ├── Catchlogr.Domain/           # Domain entities and interfaces
│   ├── Catchlogr.Infrastructure/   # Data access (EF Core, repositories)
│   └── Catchlogr.Mobile/           # .NET MAUI mobile app
├── tests/
│   └── Catchlogr.Tests/            # xUnit unit tests (services, validators)
├── docs/
│   ├── ROADMAP.md                   # Development roadmap
│   ├── MOBILE_ARCHITECTURE.md       # Mobile architecture overview
│   ├── MOBILE_CONFIGURATION.md      # Mobile app configuration guide
│   ├── SYNC_STRATEGY.md             # Offline sync algorithm details
│   ├── QUICK_START.md               # Quick start guide
│   ├── SETUP_CHECKLIST.md           # Setup checklist
│   └── SETUP_SUMMARY.md             # Setup summary
├── docker-compose.yml               # PostgreSQL local development
└── Catchlogr.sln
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- For mobile development:
  - **Android**: Android SDK (automatically installed with Visual Studio)
  - **iOS**: macOS with Xcode (for iOS development)

### 1. Clone the Repository

```bash
git clone https://github.com/starkman98/Catchlogr.git
cd Catchlogr
```

### 2. Start PostgreSQL (Docker)

```bash
docker-compose up -d
```

This starts a local PostgreSQL instance:

- **Host**: `localhost`
- **Port**: `5432`
- **Database**: `catchlogr_dev`
- **Username**: `catchlogr_user`
- **Password**: `catchlogr_dev_password`

### 3. Apply Database Migrations

```bash
cd src/Catchlogr.Api
dotnet ef database update
```

> **Note**: If you don't have EF Core tools installed:
>
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### 4. Run the API

```bash
cd src/Catchlogr.Api
dotnet run
```

The API will be available at:

- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

### 5. Run the Mobile App

**Option A: Visual Studio**

1. Set `Catchlogr.Mobile` as the startup project
2. Select your target platform (Android/iOS/Windows)
3. Press F5 to run

**Option B: Command Line**

```bash
cd src/Catchlogr.Mobile

# For Android
dotnet build -t:Run -f net10.0-android

# For iOS (macOS only)
dotnet build -t:Run -f net10.0-ios

# For Windows
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

## ⚙️ Configuration

### API Configuration

The API uses standard ASP.NET Core configuration files:

`src/Catchlogr.Api/appsettings.Development.json` (local dev):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=catchlogr_dev;Username=catchlogr_user;Password=catchlogr_dev_password"
  }
}
```

### Mobile Configuration

The mobile app uses embedded JSON configuration files:

`src/Catchlogr.Mobile/appsettings.Local.json` (Local builds):

```json
{
  "BackendEnvironment": "Local",
  "Api": {
    "BaseUrl": "https://localhost:7160"
  }
}
```

`src/Catchlogr.Mobile/appsettings.Development.json` (Debug builds):

```json
{
  "BackendEnvironment": "Development",
  "Api": {
    "BaseUrl": "https://dev-api.catchlogr.com"
  }
}
```

`src/Catchlogr.Mobile/appsettings.json` is used by Release builds and targets
`https://api.catchlogr.com`.

See [docs/MOBILE_CONFIGURATION.md](docs/MOBILE_CONFIGURATION.md) for details.

## 🔧 Development Workflow

### Running Both API and Mobile Together

1. **Terminal 1** - Start PostgreSQL:

   ```bash
   docker-compose up
   ```
2. **Terminal 2** - Run API:

   ```bash
   cd src/Catchlogr.Api
   dotnet watch run
   ```
3. **Visual Studio** - Select the Local configuration and run Mobile (F5)

### Testing API Endpoints

Use your favorite HTTP client (Postman, curl, or REST Client extension):

```bash
# Health check
curl https://localhost:7160/health

# Get all fishing trips (once implemented)
curl https://localhost:7160/api/fishing-trips
```

### Connecting Mobile to API

**Android Emulator**: Local builds automatically use the host alias:

```json
{
  "Api": {
    "BaseUrl": "http://10.0.2.2:5001"
  }
}
```

**Physical Device**: Use your computer's local IP address:

```json
{
  "Api": {
    "BaseUrl": "https://192.168.1.100:5001"
  }
}
```

> **Note**: You may need to trust the development certificate on your device.

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📦 Database Management

### Create a New Migration

```bash
cd src/Catchlogr.Api
dotnet ef migrations add MigrationName
```

### Apply Migrations

```bash
dotnet ef database update
```

### Rollback Migration

```bash
dotnet ef database update PreviousMigrationName
```

### Drop Database (Start Fresh)

```bash
dotnet ef database drop
dotnet ef database update
```

## 🏗️ Project Status

See [docs/ROADMAP.md](docs/ROADMAP.md) for the detailed development roadmap.

### Current Phase: Phase 1 MVP — Fishing Trips ✅

- [x] Solution structure with layered architecture
- [x] Docker Compose for PostgreSQL
- [x] Domain entities (`FishingTrip`)
- [x] EF Core DbContext and migrations
- [x] Repository pattern (Infrastructure)
- [x] API endpoints (Minimal APIs) with FluentValidation
- [x] Swagger/OpenAPI documentation
- [x] Mobile local database (SQLite)
- [x] Offline-first sync service (dirty flags + cursor)
- [x] MVVM UI (trip list, add/edit form)
- [x] Unit tests (FishingTripService, validators)

### Upcoming Features

- 🎣 **Phase 1**: Fishing Trip CRUD + Offline Sync
- 🐟 **Phase 2**: Individual Catch Logging
- ☁️ **Phase 3**: Weather & Moon Phase Integration
- 📸 **Phase 4**: Photo Capture & Upload
- 🔐 **Phase 5**: Authentication & Authorization
- 👥 **Phase 6**: Teams & Sharing

## 🛠️ Tech Stack

### Backend

- **.NET 10** - Runtime
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **PostgreSQL** - Primary database
- **Minimal APIs** - Endpoint definition

### Mobile

- **.NET MAUI** - Cross-platform framework
- **sqlite-net-pcl** - Local SQLite database
- **CommunityToolkit.Mvvm** - MVVM helpers

### DevOps

- **Docker** - Containerization
- **Docker Compose** - Local development orchestration

## 📚 Documentation

- [ROADMAP.md](docs/ROADMAP.md) - Development roadmap and phase breakdown
- [SYNC_STRATEGY.md](docs/SYNC_STRATEGY.md) - Offline sync algorithm and conflict resolution
- [MOBILE_ARCHITECTURE.md](docs/MOBILE_ARCHITECTURE.md) - Mobile architecture overview
- [MOBILE_CONFIGURATION.md](docs/MOBILE_CONFIGURATION.md) - Mobile app configuration guide
- [WEATHER_INTEGRATION.md](docs/WEATHER_INTEGRATION.md) - Weather enrichment architecture and implementation
- [LOCATION_SEARCH_INTEGRATION.md](docs/LOCATION_SEARCH_INTEGRATION.md) - LocationIQ lake and place search integration
- [IDENTITY_EMAIL.md](docs/IDENTITY_EMAIL.md) - Resend configuration, email confirmation, and password recovery
- [QUICK_START.md](docs/QUICK_START.md) - Quick start guide
- [SETUP_CHECKLIST.md](docs/SETUP_CHECKLIST.md) - Setup checklist
- [SETUP_SUMMARY.md](docs/SETUP_SUMMARY.md) - Setup summary
- [Copilot Instructions](.github/copilot-instructions.md) - Development principles and coding standards

## 🤝 Contributing

This is currently a personal project, but contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards

- Follow the rules in `.github/copilot-instructions.md`
- Use async/await throughout (never block with `.Result` or `.Wait()`)
- Add XML summary comments to all public members
- One class/interface per file
- Use dependency injection (no service locators)

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🎯 Success Metrics

- **MVP Success**: User can log a fishing trip offline, sync when online, and view on another device
- **Production Success**: 100+ active users, <1% crash rate, 99.9% API uptime

## 📧 Contact

- **GitHub**: [@starkman98](https://github.com/starkman98)
- **Project**: [Catchlogr](https://github.com/starkman98/Catchlogr)

---

**Happy Fishing!** 🎣 Tight lines and full strings!
