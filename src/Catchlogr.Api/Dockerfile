FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Catchlogr.Api/Catchlogr.Api.csproj", "src/Catchlogr.Api/"]
COPY ["src/Catchlogr.Application/Catchlogr.Application.csproj", "src/Catchlogr.Application/"]
COPY ["src/Catchlogr.Contracts/Catchlogr.Contracts.csproj", "src/Catchlogr.Contracts/"]
COPY ["src/Catchlogr.Domain/Catchlogr.Domain.csproj", "src/Catchlogr.Domain/"]
COPY ["src/Catchlogr.Infrastructure/Catchlogr.Infrastructure.csproj", "src/Catchlogr.Infrastructure/"]

RUN dotnet restore "src/Catchlogr.Api/Catchlogr.Api.csproj"

COPY . .

WORKDIR "/src/src/Catchlogr.Api"

RUN dotnet publish "Catchlogr.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Catchlogr.Api.dll"]