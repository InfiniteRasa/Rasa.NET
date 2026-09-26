# Docker Setup Guide

This provides an alternative to building and using the project directly on your system. Use Docker with Linux containers and Docker Compose v2.

The Dockerfile builds all .NET 10 projects with SDK **10.0.401**, matching
`global.json` and CI. Each Compose service uses its Release output directory as
its working directory. This matches the runtime loaders, which resolve
`appsettings.json`, `appsettings.env.json`, `databasesettings.json`, and
`databasesettings.env.json` from the process working directory. The three
SQLite files are mounted into the corresponding service output directory.

The Game build copies `kb-articles.json` beside `Rasa.Game.dll`, and the
Dockerfile copies the repository's 77 checked-in `.nav` files into the
`navmesh` folder below that same directory. These locations match the default
relative `GameDataConfig.NavMeshPath` and `KnowledgeBaseFile` values. Rebuild
the image after updating source, dependencies, navmeshes, or knowledge-base
content with `docker compose up --build`.

Mission definitions and scene bindings are compiled C# migration data. Game
reads them from the migrated World database; there is no mission JSON directory
to mount and no separate publisher to run.

To use a different NuGet feed for an image build without changing global configuration, pass `--build-arg NUGET_SOURCE=<feed-url>` to `docker build`.

This guide's Compose example uses SQLite.

The application also supports MySQL 8.0/8.4, but the supplied Compose configuration does not provision it. See [the setup guide](setup.md) for MySQL configuration and migration commands. Do not point smoke tests at an existing developer database.

## Clone the Repo

First, clone the git repository like normal, Then make sure to go into the directory.

## Initialize databases

Compose's individual file mounts require host files to exist. For a **new**
installation, create empty Auth/Char/World files. SQLite startup migrates all
three, including complete Bootcamp mission content.

From PowerShell at the repository root:

```powershell
foreach ($file in 'rasaauth.db', 'rasachar.db', 'rasaworld.db') {
    if (-not (Test-Path -LiteralPath $file)) {
        New-Item -ItemType File -Path $file | Out-Null
    }
}
docker compose build
```

This branch's consolidated migration history requires fresh databases,
including when replacing databases from earlier versions of this branch.
Do not run this as an existing-save conversion procedure or edit migration
history to bypass that boundary.
For later normal upgrades, stop containers and back up the databases before
applying migrations. See [mission authoring and operations](missions.md).
Native-client and live container acceptance remain separate from static layout
checks.

## Create App Settings

Next, create a appsettings.env.json in the root directory with the following contents, replacing the ip address with the one that is running the docker containers. This is useful especially when the system you're running the game on is different from where the containers are running.

```json
{
  "CommunicatorConfig": {
    "Address": "192.168.0.26"
  },
  "GameConfig": {
    "PublicAddress": "192.168.0.26"
  }
}
```

Only include settings you need to override. Compose mounts this file beside
`Rasa.Game.dll`, where the loader reads it. The image's default navigation path
is the `navmesh` folder below the Game output directory through the relative
value `navmesh`. If you set a different `GameDataConfig.NavMeshPath`, add a
matching read-only volume to the `game` service.

`PlatformCompatibilityTests.DockerServicesRunWhereRequiredConfigurationAndAssetsExist`
is the bounded static check for this layout. It parses the Compose service
blocks so each command, working directory, and volume destination is checked
against its owning service. It also models Dockerfile `COPY`, `WORKDIR`, and
Release-build output placement from the project files. This verifies
configuration, SQLite, knowledge-base, and navmesh paths without relying on a
host `bin` directory as proof of image contents.

## Start Server

Next, run `docker compose up --build`.

Confirm the Game startup log reports loaded navmeshes and reaches `Server ready!`.
Missing mission/script diagnostics mean the required database migration or
matching server code is unavailable. Startup readiness does not establish
native-client movement, collision or multi-server transfer acceptance.

## Create a User

Like the setup docs mention, the next step is creating a user. To do so, attach to the auth server and run the command.

## Play the Game

Now, you should be able to run `tabula_rasa.exe /NoPatch /AuthServer=192.168.0.26:2106` and login.
