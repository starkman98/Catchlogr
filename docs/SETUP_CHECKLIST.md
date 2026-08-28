# Catchlogr Development Setup Checklist

Use this checklist to verify your development environment is properly configured.

## ✅ Prerequisites

- [ ] **.NET 10 SDK** installed
  ```bash
  dotnet --version
  # Should show: 10.x.x or higher
  ```

- [ ] **Docker Desktop** installed and running
  ```bash
  docker --version
  docker-compose --version
  ```

- [ ] **Visual Studio 2022** (or VS Code) installed
  - [ ] .NET MAUI workload installed
  - [ ] Mobile development with .NET workload (for Android/iOS)

- [ ] **EF Core Tools** installed globally
  ```bash
  dotnet tool install --global dotnet-ef
  # or update: dotnet tool update --global dotnet-ef
  ```

---

## ✅ Repository Setup

- [ ] Repository cloned
  ```bash
  git clone https://github.com/starkman98/Catchlogr.git
  cd Catchlogr
  ```

- [ ] Solution builds successfully
  ```bash
  dotnet build
  ```

- [ ] All projects restore without errors
  ```bash
  dotnet restore
  ```

---

## ✅ Database Setup

- [ ] PostgreSQL container running
  ```bash
  docker-compose up -d
  docker ps  # Should show catchlogr_postgres
  ```

- [ ] Database connection works
  ```bash
  docker exec -it catchlogr_postgres psql -U catchlogr_user -d catchlogr_dev
  # Type \q to exit
  ```

- [ ] Migrations applied (when migrations exist)
  ```bash
  cd src/Catchlogr.Api
  dotnet ef database update
  ```

---

## ✅ API Configuration

- [ ] `appsettings.Development.json` exists in `src/Catchlogr.Api/`
- [ ] Connection string points to local PostgreSQL:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Host=localhost;Port=5432;Database=catchlogr_dev;Username=catchlogr_user;Password=catchlogr_dev_password"
    }
  }
  ```

- [ ] API runs successfully
  ```bash
  cd src/Catchlogr.Api
  dotnet run
  # Should start on https://localhost:5001
  ```

- [ ] Health endpoint accessible (if implemented)
  ```bash
  curl https://localhost:5001/health
  ```

---

## ✅ Mobile Configuration

- [ ] `appsettings.json` exists in `src/Catchlogr.Mobile/`
- [ ] `appsettings.Development.json` exists in `src/Catchlogr.Mobile/`
- [ ] Files marked as `EmbeddedResource` in `.csproj`
- [ ] `Configuration/AppSettings.cs` class exists
- [ ] Settings registered in `MauiProgram.cs`

- [ ] Mobile app builds for target platform
  ```bash
  cd src/Catchlogr.Mobile
  dotnet build -f net10.0-android  # For Android
  ```

- [ ] Can run mobile app from Visual Studio (F5)

---

## ✅ Android-Specific Setup (Optional)

- [ ] Android SDK installed
  - [ ] Android 13.0 (API 33) or higher
  - [ ] Android Emulator configured

- [ ] Emulator can reach API
  - [ ] `appsettings.Development.json` uses `10.0.2.2` instead of `localhost`
  ```json
  {
    "Api": {
      "BaseUrl": "https://10.0.2.2:5001"
    }
  }
  ```

- [ ] SSL certificate trusted on emulator (if needed)

---

## ✅ iOS-Specific Setup (Optional, macOS only)

- [ ] Xcode installed
- [ ] iOS Simulator configured
- [ ] Development certificate configured
- [ ] Mobile app runs on iOS simulator

---

## ✅ Windows-Specific Setup (Optional)

- [ ] Windows App SDK installed
- [ ] Mobile app runs on Windows (net10.0-windows10.0.19041.0)

---

## ✅ Git Configuration

- [ ] `.gitignore` is properly configured
- [ ] No sensitive data (passwords, secrets) committed
- [ ] `bin/`, `obj/`, `.vs/` folders ignored
- [ ] `*.db3` (SQLite files) ignored

---

## ✅ Editor/IDE Setup

### Visual Studio 2022
- [ ] Solution loads without errors
- [ ] IntelliSense working
- [ ] Can set breakpoints and debug
- [ ] NuGet package restore works

### VS Code (Optional)
- [ ] C# extension installed
- [ ] .NET MAUI extension installed
- [ ] Can build and run from terminal

---

## ✅ Documentation Review

- [ ] Read [README.md](../README.md)
- [ ] Read [docs/ROADMAP.md](ROADMAP.md)
- [ ] Read [docs/MOBILE_CONFIGURATION.md](MOBILE_CONFIGURATION.md)
- [ ] Read [.github/copilot-instructions.md](../.github/copilot-instructions.md)

---

## ✅ First Test Run

- [ ] **Terminal 1**: Start PostgreSQL
  ```bash
  docker-compose up
  ```

- [ ] **Terminal 2**: Run API
  ```bash
  cd src/Catchlogr.Api
  dotnet watch run
  ```

- [ ] **Visual Studio**: Run Mobile app (F5)

- [ ] All three components running simultaneously without errors

---

## 🎉 Setup Complete!

If all items are checked, your development environment is ready!

### Next Steps:
1. Check the current phase in [docs/ROADMAP.md](ROADMAP.md)
2. Pick a task from the roadmap
3. Start coding!

### Troubleshooting:
- See [docs/QUICK_START.md](QUICK_START.md) for common issues
- Open an issue on GitHub if you're stuck

---

**Last Updated**: Phase 0 - Foundation & Project Setup
