# API image for Fly.io (fly.toml). Build from the repository root: docker build -t heardback-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Build settings first: they rarely change, so the restore layer below stays cached.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/
# ReadyToRun: compiled ahead of time, so a machine woken from sleep spends less of its first requests on JIT.
RUN dotnet publish src/Heardback.Api/Heardback.Api.csproj --configuration Release --output /app \
    --runtime linux-x64 --self-contained false -p:PublishReadyToRun=true

# Chiseled: no shell or package manager, and it runs as a non-root user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Heardback.Api.dll"]
