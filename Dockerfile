FROM mcr.microsoft.com/dotnet/sdk:10.0.401

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
