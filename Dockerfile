# Build in the SDK image global.json pins, run in the much smaller runtime image as its built-in
# non-root user. The runtime stage keeps the build's output paths, so docker-compose.yml's working
# directories and volume mounts are the same in both.
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build

WORKDIR /app

COPY src /app/src
COPY Rasa.NET.sln /app
COPY Rasa.NET.sln.DotSettings /app
COPY global.json /app
COPY Directory.Packages.props /app
COPY .config /app/.config

ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json
RUN dotnet restore --source "$NUGET_SOURCE"
RUN dotnet build --no-restore --configuration Release

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

# Owned by app (UID 1654) so SQLite can create its journal files beside the mounted databases.
COPY --from=build --chown=app:app /app/src/Rasa.Auth/bin/Release/net10.0 /app/src/Rasa.Auth/bin/Release/net10.0
COPY --from=build --chown=app:app /app/src/Rasa.Game/bin/Release/net10.0 /app/src/Rasa.Game/bin/Release/net10.0
COPY --chown=app:app navmesh /app/src/Rasa.Game/bin/Release/net10.0/navmesh

USER app
