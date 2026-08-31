FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY ["src/Catchlogr.Api/Catchlogr.Api.csproj", "src/Catchlogr.Api/"]
COPY ["src/Catchlogr.Application/Catchlogr.Application.csproj", "src/Catchlogr.Application/"]
COPY ["src/Catchlogr.Contracts/Catchlogr.Contracts.csproj", "src/Catchlogr.Contracts/"]
COPY ["src/Catchlogr.Domain/Catchlogr.Domain.csproj", "src/Catchlogr.Domain/"]
COPY ["src/Catchlogr.Infrastructure/Catchlogr.Infrastructure.csproj", "src/Catchlogr.Infrastructure/"]

RUN dotnet workload restore "src/Catchlogr.Api/Catchlogr.Api.csproj"

RUN dotnet restore "src/Catchlogr.Api/Catchlogr.Api.csproj"

COPY . .

FROM build AS migrations

RUN dotnet tool install --global dotnet-ef --version 10.0.11

ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /src

FROM build AS publish

WORKDIR "/src/src/Catchlogr.Api"

RUN dotnet publish "Catchlogr.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Catchlogr.Api.dll"]