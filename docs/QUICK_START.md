# Catchlogr Quick Start Guide

## 🚀 First Time Setup (5 minutes)

### 1. Start Database
```bash
docker-compose up -d
```

### 2. Run API
```bash
cd src/Catchlogr.Api
dotnet run
```

### 3. Run Mobile App
- Open `Catchlogr.sln` in Visual Studio
- Set `Catchlogr.Mobile` as startup project
- Press F5

---

## 📱 Mobile App: Connecting to Local API

### Android Emulator
Edit `src/Catchlogr.Mobile/appsettings.Development.json`:
```json
{
  "Api": {
    "BaseUrl": "https://10.0.2.2:5001"
  }
}
```

### Physical Device
Find your computer's IP address:
```bash
# Windows
ipconfig

# Mac/Linux
ifconfig
```

Then use that IP:
```json
{
  "Api": {
    "BaseUrl": "https://192.168.1.100:5001"
  }
}
```

---

## 🗄️ Database Commands

### Create Migration
```bash
cd src/Catchlogr.Api
dotnet ef migrations add MigrationName
```

### Apply Migrations
```bash
dotnet ef database update
```

### Reset Database
```bash
dotnet ef database drop
dotnet ef database update
```

---

## 🐳 Docker Commands

### Start PostgreSQL
```bash
docker-compose up -d
```

### Stop PostgreSQL
```bash
docker-compose down
```

### View Logs
```bash
docker-compose logs -f postgres
```

### Delete Database Volume (Fresh Start)
```bash
docker-compose down -v
docker-compose up -d
```

---

## 🧪 Testing API

### Health Check
```bash
curl https://localhost:5001/health
```

### Get Fishing Trips (once implemented)
```bash
curl https://localhost:5001/api/fishing-trips
```

---

## 🔍 Troubleshooting

### "Can't connect to PostgreSQL"
1. Check Docker is running: `docker ps`
2. Restart container: `docker-compose restart`
3. Check connection string in `appsettings.Development.json`

### "Can't connect to API from Mobile"
1. Verify API is running: `curl https://localhost:5001/health`
2. Check `appsettings.Development.json` in Mobile project
3. Android Emulator: Use `10.0.2.2` instead of `localhost`
4. Physical device: Use computer's local IP address

### "Migration Error"
```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Update EF Core tools
dotnet tool update --global dotnet-ef
```

### "Build Error in Mobile"
1. Clean solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Rebuild: `dotnet build`

---

## 📁 Project Structure Reference

```
Catchlogr/
├── src/
│   ├── Catchlogr.Api/              ← Web API (Minimal APIs)
│   ├── Catchlogr.Application/      ← Business logic
│   ├── Catchlogr.Contracts/        ← DTOs shared between API & Mobile
│   ├── Catchlogr.Domain/           ← Entities & interfaces
│   ├── Catchlogr.Infrastructure/   ← EF Core, repositories
│   └── Catchlogr.Mobile/           ← MAUI app
├── docs/
│   ├── ROADMAP.md                   ← Development roadmap
│   ├── MOBILE_CONFIGURATION.md      ← Mobile config details
│   └── QUICK_START.md               ← This file
├── docker-compose.yml               ← PostgreSQL setup
└── Catchlogr.sln                   ← Solution file
```

---

## 🎯 Next Steps

See [docs/ROADMAP.md](ROADMAP.md) for the full development plan.

**Current Phase**: Foundation & Project Setup ✅
**Next Phase**: Implement FishingTrip entity and CRUD operations

---

## 🆘 Need Help?

1. Check [README.md](../README.md) for detailed setup
2. Check [docs/MOBILE_CONFIGURATION.md](MOBILE_CONFIGURATION.md) for config help
3. Check [.github/copilot-instructions.md](../.github/copilot-instructions.md) for coding standards
4. Open an issue on GitHub

**Happy Fishing!** 🎣
