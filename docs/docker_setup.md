# Docker Setup Guide

This provides an alternative to building and using the project directly on your system. Use Docker with Linux containers and Docker Compose v2.

The published Docker image contains both the Auth and Game servers. An entrypoint selects which server to start for each Compose service. The applications are installed at stable `/app/auth` and `/app/game` paths so the Compose configuration does not depend on a specific .NET output directory such as `net10.0`.

The Dockerfile builds all .NET 10 projects with SDK **10.0.401**, matching `global.json` and CI.

The Game build copies `kb-articles.json` alongside `Rasa.Game.dll`, and the Dockerfile includes the repository's 77 checked-in `.nav` files in the Game `navmesh` directory. These locations match the default relative `GameDataConfig.NavMeshPath` and `KnowledgeBaseFile` values.

Mission definitions and scene bindings are compiled C# migration data. Game reads them from the migrated World database; there is no mission JSON directory to mount and no separate publisher to run.

This guide's Compose example uses SQLite.

The application also supports MySQL 8.0/8.4, but the supplied Compose configuration does not provision it. See [the setup guide](setup.md) for MySQL configuration and migration commands. Do not point smoke tests at an existing developer database.

## Clone the Repo

First, clone the Git repository and enter the repository directory.

## Initialize Databases

Compose's individual SQLite file mounts require the host files to exist. For a **new** installation, create empty Auth, Char, and World database files. SQLite startup migrates all three, including complete Bootcamp mission content.

### Linux / macOS

From a shell at the repository root:

```bash
touch rasaauth.db rasachar.db rasaworld.db
```

### Windows PowerShell

From PowerShell at the repository root:

```powershell
foreach ($file in 'rasaauth.db', 'rasachar.db', 'rasaworld.db') {
    if (-not (Test-Path -LiteralPath $file)) {
        New-Item -ItemType File -Path $file | Out-Null
    }
}
```

The Compose configuration deliberately does not create missing SQLite mount paths. If one of these files is missing, Compose will fail rather than silently creating a directory in its place.

This branch's consolidated migration history requires fresh databases, including when replacing databases from earlier versions of this branch. Do not run this as an existing-save conversion procedure or edit migration history to bypass that boundary.

For later normal upgrades, stop the containers and back up the databases before applying migrations. See [mission authoring and operations](missions.md).

Native-client and live container acceptance remain separate from static layout checks.

## Create App Settings

Create an `appsettings.env.json` file in the repository root with the following contents, replacing the IP address with the address of the system running the Docker containers.

This is especially useful when the system running the game client is different from the system running the server.

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

Only include settings you need to override. Compose mounts this file alongside `Rasa.Game.dll`, where the loader reads it.

The image's default navigation path is the `navmesh` folder below the Game application directory through the relative value `navmesh`. If you set a different `GameDataConfig.NavMeshPath`, add a matching read-only volume to the `game` service.

## Start the Server

The standard `compose.yml` uses the latest **unstable** image published from the `development` branch.

Start the server with:

```bash
docker compose up -d
```

This pulls the published image if necessary and starts both the Auth and Game services.

To explicitly pull newer images before starting:

```bash
docker compose pull
docker compose up -d
```

### Release Versions

Tagged releases are also published as Docker image tags.

For example, a Git release tag such as:

```text
v1.2.3
```

is available as the corresponding Docker image tag:

```text
v1.2.3
```

The most recently tagged release is also published as:

```text
latest
```

To use a specific release rather than `unstable`, change the image tag in `compose.yml` from `unstable` to the desired version, for example:

```yaml
image: ghcr.io/infiniterasa/rasa.net:v1.2.3
```

or use the most recent release:

```yaml
image: ghcr.io/infiniterasa/rasa.net:latest
```

## Development / Building Locally

If you are developing Rasa.NET or want to build the image yourself rather than use a published image, use the development Compose file:

```bash
docker compose -f compose.dev.yml up --build
```

The development Compose configuration builds the image from the repository's `Dockerfile` and then starts Auth and Game from that locally built image.

Re-run with `--build` after updating source code, dependencies, navmeshes, or knowledge-base content:

```bash
docker compose -f compose.dev.yml up --build
```

## Verify Startup

Confirm the Game startup log reports loaded navmeshes and reaches:

```text
Server ready!
```

Missing mission or script diagnostics mean the required database migration or matching server code is unavailable.

Startup readiness does not establish native-client movement, collision, or multi-server transfer acceptance.

`PlatformCompatibilityTests.DockerServicesRunWhereRequiredConfigurationAndAssetsExist` is the bounded static check for the Docker layout. It verifies configuration, SQLite, knowledge-base, and navmesh paths without relying on a host `bin` directory as proof of image contents.

## Create a User

As described in the setup documentation, the next step is creating a user.

Attach to the running Auth service:

```bash
docker attach $(docker compose ps -q auth)
```

You can then run the appropriate Auth server command to create the user.

To detach from the container without stopping it, use Docker's detach sequence:

```text
Ctrl-P, Ctrl-Q
```

## Play the Game

You should now be able to start the client with:

```text
tabula_rasa.exe /NoPatch /AuthServer=192.168.0.26:2106
```

Replace `192.168.0.26` with the address of the system running the Docker containers, then log in using the account you created.
