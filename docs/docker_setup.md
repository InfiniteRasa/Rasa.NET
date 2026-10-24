# Docker Setup Guide

This provides an alternative to building and using the project directly on your system. Use Docker with Linux containers and Docker Compose v2.

The published Docker image contains both the Auth and Game servers. An entrypoint selects which server to start for each Compose service. The applications are installed at stable `/app/auth` and `/app/game` paths so the Compose configuration does not depend on a specific .NET output directory such as `net10.0`.

The Dockerfile builds all .NET 10 projects with SDK **10.0.401**, matching `global.json` and CI.

The Game build copies `kb-articles.json` alongside `Rasa.Game.dll`, and the Dockerfile includes the repository's 77 checked-in `.nav` files in the Game `navmesh` directory. These locations match the default relative `GameDataConfig.NavMeshPath` and `KnowledgeBaseFile` values.

Mission definitions and scene bindings are compiled C# migration data. Game reads them from the migrated World database; there is no mission JSON directory to mount and no separate publisher to run.

The default `compose.yml` and `compose.dev.yml` use SQLite. For a new, isolated MySQL 8.4 deployment, use `compose.mysql.yml` and optionally `compose.mysql.dev.yml` as described below. The database providers do **not** share saved data; changing Compose files does not convert an existing SQLite save. Do not point smoke tests at an existing developer or production database.

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

## MySQL 8.4 (published or development images)

The MySQL stack uses **four application containers** (Auth, Game, and two one-shot migration jobs) and one MySQL 8.4 database container. Three independent MySQL schemas are created: `rasaauth`, `rasachar`, and `rasaworld`. All three schemas live in the persistent Docker named volume `mysql_data`. No SQLite `.db` files or host-side MySQL installation are needed. The MySQL port is *not* published to the host: only the Rasa containers can reach the `mysql` service on the Compose network.

The current application deliberately **does not automatically migrate MySQL on normal server startup**. The MySQL Compose file runs `auth --migrate` and `game --migrate` as explicit one-shot jobs instead, then starts Auth and Game **only if both migration jobs succeed**. A migration failure blocks that server; inspect the migration container's logs and fix the failure before retrying. Each migration is logged with its name. The jobs use the same application image as the servers, so published users do **not** need a local SDK or `dotnet-ef`. These `--migrate` commands require an image built with this change: publish a new `unstable` or release tag before deploying the new Compose file with published images.

### 1. Prepare credentials and game settings

From the repository root, copy the example to a local file:

```bash
cp mysql.env.example mysql.env
```

Set **different, strong** values for `MYSQL_ROOT_PASSWORD` and `MYSQL_PASSWORD` in `mysql.env` (`openssl rand -hex 32` can generate each value). `mysql.env` is ignored by Git; never commit it. Compose will refuse to start if either variable is unset. `RASA_IMAGE_TAG` is optional and defaults to `unstable`; you can set `latest` or a published release tag such as `v1.2.3`. Do not use placeholder passwords outside a disposable local setup.

Create `appsettings.env.json` exactly as in [Create App Settings](#create-app-settings) above. The **same game JSON file** is used for SQLite and MySQL (public IP, navmesh settings, etc.). MySQL connections do not go in this file.

For clarity, the corresponding PowerShell command to create the credentials file is:

```powershell
Copy-Item mysql.env.example mysql.env
```

### 2. Start with published images

```bash
docker compose --env-file mysql.env -f compose.mysql.yml up -d
```

To pull an updated `unstable`/release image first:

```bash
docker compose --env-file mysql.env -f compose.mysql.yml pull
docker compose --env-file mysql.env -f compose.mysql.yml up -d
```

Compose starts MySQL, waits for its health check, runs the Auth/Char/World migrations, and only then starts the servers. The initial World migration can take longer than a subsequent startup.

### 3. Or use your local development build

Use the **same base MySQL Compose file** plus the small development override; it replaces the published Rasa image on Auth, Game, and both migration jobs with an image built from the local Dockerfile. MySQL itself is still the official `mysql:8.4` image.

```bash
docker compose --env-file mysql.env -f compose.mysql.yml -f compose.mysql.dev.yml up --build -d
```

Repeat with `--build` after source changes. Do not also include `compose.yml` or `compose.dev.yml` in either MySQL command: those are complete SQLite deployments with SQLite bind mounts.

### How database configuration works

`src/Rasa.DBL/databasesettings.json` defaults to SQLite and is copied into both application output directories. The servers load `databasesettings.json`, then optional `databasesettings.env.json`, **then environment variables**. The MySQL Compose files use .NET's `__` notation for nested configuration keys, which takes precedence over the JSON files:

| App setting | Value supplied by Compose |
|---|---|
| `Databases:Provider` | `MySql` |
| `Databases:Auth:Host`, `:Port`, `:Database` | `mysql`, `3306`, `rasaauth` |
| `Databases:Char:Host`, `:Port`, `:Database` | `mysql`, `3306`, `rasachar` |
| `Databases:World:Host`, `:Port`, `:Database` | `mysql`, `3306`, `rasaworld` |
| `Databases:<each>:User` | `rasa` |
| `Databases:<each>:Password` | `MYSQL_PASSWORD` from `mysql.env` |
| `Databases:<each>:TimeoutInMilliseconds` | Default `5000` from `databasesettings.json` |

`mysql` is the **Compose service hostname**, not `localhost` inside the Auth/Game containers. MySQL's image initializes the `rasaauth` database and creates the `rasa` user from `MYSQL_DATABASE`, `MYSQL_USER`, and `MYSQL_PASSWORD`; `docker/mysql/init.sql` additionally creates `rasachar` and `rasaworld` and grants the same user privileges. `MYSQL_ROOT_PASSWORD` belongs only to the MySQL service. All three application database contexts have credentials available; Auth uses the Auth database, while Game uses Char and World.

The SQL init file and MySQL image credentials are evaluated **only when the data volume is empty**. Changing names or passwords later in `mysql.env` alone does not update an existing MySQL user/database. Make changes inside MySQL with the appropriate SQL statements, or perform an explicit backup/migration. **Never delete the named volume as an upgrade or troubleshooting shortcut; doing so destroys saved data.**

### Check status, migrate on upgrades, and troubleshoot

To see the job and server logs:

```bash
docker compose --env-file mysql.env -f compose.mysql.yml ps -a
docker compose --env-file mysql.env -f compose.mysql.yml logs mysql mysql-auth-migrate mysql-game-migrate
docker compose --env-file mysql.env -f compose.mysql.yml logs auth game
```

To confirm the initialized schemas from inside the container:

```bash
docker compose --env-file mysql.env -f compose.mysql.yml exec mysql sh -c 'mysql -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" -e "SHOW DATABASES;"'
```

For an existing installation, **back up MySQL and stop the game/auth servers before applying updated migrations**. The one-shot migration jobs are also available explicitly and apply only pending migrations:

```bash
docker compose --env-file mysql.env -f compose.mysql.yml stop auth game
docker compose --env-file mysql.env -f compose.mysql.yml run --rm mysql-auth-migrate
docker compose --env-file mysql.env -f compose.mysql.yml run --rm mysql-game-migrate
docker compose --env-file mysql.env -f compose.mysql.yml up -d
```

For a local development build, insert `-f compose.mysql.dev.yml` after `-f compose.mysql.yml` in these commands so the migration job runs against the local image too. Do not run migrations concurrently with live gameplay. See [Applying migrations](setup.md#applying-migrations) for schema history, review/backup requirements, and the ordinary-loot readiness boundary.

MySQL remains private to the Docker network; if you need Workbench/CLI access from the host, explicitly add a loopback-only mapping (`127.0.0.1:3306:3306`) to the `mysql` service, keeping it off the public interface. Never expose MySQL port 3306 directly to the internet.

### Clean shutdown

```bash
docker compose --env-file mysql.env -f compose.mysql.yml down
```

This stops/removes containers **without** deleting `mysql_data`. Do **not** add `-v` unless you intentionally want to permanently delete the MySQL database volume after a verified backup.

## Verify Startup

Confirm the Game startup log reports loaded navmeshes and reaches:

```text
Server ready!
```

Missing mission or script diagnostics mean the required database migration or matching server code is unavailable.

Startup readiness does not establish native-client movement, collision, or multi-server transfer acceptance.

`PlatformCompatibilityTests.DockerServicesRunWhereRequiredConfigurationAndAssetsExist` is the bounded static check for the existing SQLite Docker layout. It verifies configuration, SQLite, knowledge-base, and navmesh paths without relying on a host `bin` directory as proof of image contents. `MySqlDockerComposeTests` checks the MySQL topology and migration command wiring; a real Docker/MySQL startup is still required to validate live migrations.

## Create a User

As described in the setup documentation, the next step is creating a user.

Attach to the running Auth service:

```bash
docker attach $(docker compose ps -q auth)
```

You can then run the appropriate Auth server command to create the user. When using MySQL, replace the attach command with:

```bash
docker attach $(docker compose --env-file mysql.env -f compose.mysql.yml ps -q auth)
```

For a development MySQL stack, add `-f compose.mysql.dev.yml` after `-f compose.mysql.yml` in that command.

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
