FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS source
WORKDIR /src
COPY global.json .
COPY src/Catchlogr.Api src/Catchlogr.Api
COPY src/Catchlogr.Application src/Catchlogr.Application
COPY src/Catchlogr.Contracts src/Catchlogr.Contracts
COPY src/Catchlogr.Domain src/Catchlogr.Domain
COPY src/Catchlogr.Infrastructure src/Catchlogr.Infrastructure
COPY src/Catchlogr.Web src/Catchlogr.Web

FROM source AS migrate
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.11
RUN dotnet build src/Catchlogr.Api -c Release
ENTRYPOINT ["/tools/dotnet-ef", "database", "update", "--no-build", "--configuration", "Release", "--project", "src/Catchlogr.Infrastructure", "--startup-project", "src/Catchlogr.Api"]

FROM source AS publish
RUN dotnet publish src/Catchlogr.Api -c Release -o /publish/api /p:UseAppHost=false
RUN dotnet publish src/Catchlogr.Web -c Release -o /publish/web /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=publish /publish/api .
ENTRYPOINT ["dotnet", "Catchlogr.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web
WORKDIR /app
COPY --from=publish /publish/web .
ENTRYPOINT ["dotnet", "Catchlogr.Web.dll"]
