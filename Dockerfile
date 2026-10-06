FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build

WORKDIR /app

COPY src /app/src
COPY Rasa.NET.sln /app
COPY Rasa.NET.sln.DotSettings /app
COPY global.json /app
COPY .config /app/.config

ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json
RUN dotnet restore --source "$NUGET_SOURCE"
RUN dotnet build --no-restore --configuration Release

COPY navmesh /app/src/Rasa.Game/bin/Release/net10.0/navmesh

RUN mkdir -p /out/auth /out/game
RUN cp -a /app/src/Rasa.Auth/bin/Release/net10.0/. /out/auth/
RUN cp -a /app/src/Rasa.Game/bin/Release/net10.0/. /out/game/


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /out/auth /app/auth
COPY --from=build /out/game /app/game

COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
