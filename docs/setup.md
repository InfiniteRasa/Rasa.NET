# Setup Guide
This guide will help you install and setup the required tools to run Rasa.NET. There are a few steps:

1. Download, install, and setup the required tooling
2. Download and install the game
3. Download or clone the code
4. Build and run the code from Visual Studio
5. Launch the game

## Download, install, and setup the required tooling
The following tools are required to setup, build, and run Rasa.NET:

- The .NET 10 SDK version pinned in the repository's `global.json`
- Optional: a Visual Studio release that supports this .NET 10 SDK
- Database System, either:
  - MySQL Server and Workbench or
  - Sqlite (no tools required, but a Sqlite DB browser might help)
- Git

### Install Visual Studio
There is a single Visual Studio solution that contains the projects needed for Rasa.NET. Use a Visual Studio release that supports .NET 10, or build with the .NET CLI below. Visual Studio 2017 cannot build this solution.

- Download [Visual Studio](https://visualstudio.microsoft.com/). Choose the license that works for you
- Run the installer after it's downloaded
- In the Workloads selection, select `.NET desktop development`
- Continue the installation and let it finish

### Install .NET 10
All solution projects target .NET 10, including `Rasa.Missions`.
Install the exact SDK selected by `global.json`; SDK
roll-forward is disabled so local builds, CI and Docker use the same version.

- [Download the .NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and install the exact version in `global.json`'s `sdk.version`. The SDK includes the runtime.
- From the repository root, run `dotnet --version` and verify it prints that version.

#### Updating the SDK or packages

The SDK version lives in two places: `global.json` and the `sdk` build stage in
the `Dockerfile`. NuGet versions all live in `Directory.Packages.props`, and the
`dotnet-ef` tool in `.config/dotnet-tools.json` follows the EF Core packages.
Change every copy in the same commit; `PlatformCompatibilityTests` fails when
they disagree. Dependabot opens grouped update PRs (`.github/dependabot.yml`),
and moves `global.json` and the `Dockerfile` together in one SDK PR. If an SDK
PR changes only one of them (for example because the image isn't published
yet), push the other change to that PR's branch before merging it. EF Core 10 and Pomelo
updates are ignored until the project chooses a MySQL provider for EF Core 10.

The supported portable deployment identifiers are `win-x64`, `osx-x64` and `linux-x64`. These preserve the Windows, macOS and Linux x64 deployment families, not support for the obsolete operating-system versions named by the old .NET 5 identifiers. Use an operating system supported by .NET 10.

### Optional: Install MySQL Server and MySQL Workbench
Rasa.NET uses MySQL or Sqlite to store game data. Sqlite requires no separate
database server; startup applies both schema and mission-data migrations.
If you want to use MySql, download and install MySQL Community Server following
these steps:

- Download the [MySQL Installer for Windows](https://dev.mysql.com/downloads/windows/installer/8.0.html)
- Run the installer after it's downloaded
- The default options are fine, but you can customize the install and choose only MySQL Sever and MySQL Workbench.
- For help, follow [the documentation](https://dev.mysql.com/doc/refman/5.7/en/mysql-installer-setup.html)
- Set the `root` user password to something unique when prompted after installation

### Install Git
Git is a version control system and will allow you to download the code. Alternatively, if you're not planning on developing for Rasa.NET you can download a .zip of the code directly from GitHub.

- Download [Git](https://git-scm.com/downloads)
- Run the installer. If you're not sure of what options to select, choose the defaults

## Download and install the game
You'll need the game client. Below is the demo version which was made freely available.

- Refer to the forums to [download and install the game client](https://infiniterasa.org/viewtopic.php?f=15&t=8).
- Create a shortcut to the game client on the desktop. You'll need to find the executable in the path where you installed the game
  - Right-click on the `.exe` and choose `Send to > Desktop (create shortcut)`
  - Go to the desktop and right-click on the newly created shortcut and choose `Properties`
  - Select the `Shortcut` tab
  - Edit the `Target` box to append `/NoPatch /AuthServer=localhost:2106` to the end of the file path.

### Version 1.16.5.0 Required
The game client needs to be version 1.16.5.0. If you have questions, [join the Discord](https://discord.gg/Ph68FmA).

## Download or clone the code
Download Rasa.NET from GitHub or use Git to clone the project (recommended).

- Launch a command prompt or Git bash terminal
- Change directories to where you want the source to download to, i.e. `cd C:\Projects`
- Run the command `git clone https://github.com/InfiniteRasa/Rasa.NET.git`

## Optional: Setup MySql
If you want to use a MySql database, here are the steps required to get things to work.

### Setup the database
If you're using MySql, before you can build and run the code, there are some configuration steps required for the database.

- Launch MySQL Workbench and select your `Local instance MySQL80` under `MySQL Connections` that was created during install
- Enter your root password that you created to connect to it
- In the Navigator panel on the left, right-click in the blank space and choose `Create Schema`
  - In the new tab, name the first schema `rasaauth` and click `Apply`
  - Repeat this two more times for `rasachar` and `rasaworld`

### Add a database user for the servers
The auth and game servers are pre-configured to use the user `rasa` to connect. You'll need to add this user to your database instance.

- In MySQL Workbench, go to `Server > Users and Privileges`
- Click `Add Account`
- Set the `Login Name` to `rasa`
- Set the `Limit Hosts to Matching` to `localhost`
- Set the password to `rasa`
- Select the `Administrative Roles` tab and check `DBA`
- Click `Apply`


## Custom configuration
RASA.NET uses configuration files for important settings the servers need to work. These files are named
- appsettings.json for application specific settings
- databasesettings.json for connection to the database

Changing the configuration is at least required for the database settings.

### How to overwrite configuration for your enviroment
If you want to overwrite one or multiple settings from the appsettings.json of `Rasa.Auth` or `Rasa.Game` or the databasesettings.json of Rasa.DBL, do the following:

- Launch Visual Studio and open the `Rasa.NET.sln` file in the code repository
- Create a file named `appsettings.env.json`/`databasesettings.env.json` in the root of the respective project or copy and rename the existing file
- The new file will be shown as subelement of the original file
- Use the new file to overwrite settings from appsettings.json/databasesettings.json. Keep property naming and json structure. You don't need to add settings that you don't want to change. For example, to overwrite GameConfig.PublicAddress in the Rasa.Game settings, use

```json
{
  "GameConfig":{
    "PublicAddress": "<new value>"
  }
}
```

- The env.json files is ignored in git. Keep it that way, this configuration applies only for your development enviroment.

### Squad voice chat
`Rasa.Game` runs the voice server the game client's built-in squad voice chat connects to. It is configured in the `VoiceConfig` section of its appsettings.json:

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | `false` turns squad voice chat off; clients are told it is unavailable and never try to connect. A missing section also means off. |
| `PublicAddress` | `""` | Host or IP clients connect to for voice. Empty uses `GameConfig.PublicAddress`. |
| `BindAddress` | `0.0.0.0` | Local address the UDP socket binds to. |
| `Port` | `8103` | UDP port. Forward it as **UDP** on your router or firewall (docker-compose maps `8103/udp`). |
| `MaxTalkTime` | `60` | Seconds of talk time shown on the player's talk-time bar. Display only. |
| `TalkTimeRegen` | `1` | Seconds of talk time regained per second of silence. |
| `TimeoutSeconds` | `30` | A voice connection silent this long is dropped. |
| `TokenLifetimeSeconds` | `60` | How long a voice login token stays valid. |
| `LogSessions` | `true` | Log voice logins, logouts and refusals. |

Changes are picked up when the file is reloaded; a new `BindAddress` or `Port` restarts the voice listener. Type `voice` on the game server console to see who is connected. Players talk with the game's push-to-talk key while in a squad, with voice enabled in their options.
### Game configuration ownership

`Rasa.Game\Config` owns the server settings loaded from `src\Rasa.Game\appsettings.json`:

- `GameConfig` owns the public game endpoint, listener backlog, the 60-second transfer acknowledgement timeout, the 6-metre corpse-looting distance, and the optional GM performance-metrics interval. The metrics interval is `0` by default, which disables those packets.
- `QueueConfig`, `CommunicatorConfig`, `ServerInfoConfig`, and `SocketAsyncConfig` own their existing queue, auth-communicator, server-list, and socket settings.
- `GameDataConfig` owns enabled races, startup server flags, the knowledge-base JSON path, and `NavMeshPath`. The default navigation directory is `navmesh`.

Action and action-level behavior is loaded from the world data tables. Missions
load enabled definitions and typed scene bindings installed in the World
database by migrations. They are not activated by adding an environment setting
or copying JSON into the server directory. See [mission data migrations](#mission-data-migrations).
Do not add environment keys for auction, crafting or missions unless the owning
code first adds a supported configuration property.

For the exact transfer and loot validation rules and focused checks, see the [world regression guide](world-testing.md).

### Database configuration
There are three databases: Auth (accounts), Char (character and scene state),
and World (shared definitions and migrated mission content). EF Core supports
MySql and Sqlite providers. Connection settings are defined in
`src\Rasa.DBL\databasesettings.json` and its environment-specific override.
Sqlite is suitable for development and small servers; configure its file paths
and start the servers normally.

To setup your database settings, have a look in "Custom configuration" to learn how to create an enviroment specific settings file.

To use Sqlite with EF Core, set `Provider` to `Sqlite`. Sqlite uses only the value
_database_ of the configuration to create a file named `<database>.db`; supply a
base path without the extension. Pending migrations run automatically at the
corresponding server's startup, including the complete Bootcamp content. An
absolute database base path avoids choosing different files when launching from
different working directories. No preexisting World file or publication script
is required.

To access a MySql server with EF Core, set `Provider` to `MySql`. You need to provide host, port, database, user, password and timeout in the config file. Keep in mind that we fall back to the values in databasesettings.json, if your enviroment file does not overwrite a value. For MySql, you have to apply any pending migrations yourself. See "Applying migrations" for a quick start.

If you want to add additional migrations as part of a feature, see "Creating migrations".

## Working with the databases and EF Core
The databases are kept up to date with EF Core. The compatible package set is EF Core/SQLite/Design 9 with Pomelo MySQL 9, running on .NET 10; the exact versions are in `Directory.Packages.props`. Pomelo 9 supports EF Core 9, not EF Core 10; upgrade these providers together. EF Core 9 support ends November 10, 2026, so this dependency choice needs review before that date. MySQL 8.0 and 8.4 are supported by the provider.

MySQL schema names up to the server's 64-character limit are supported. Rasa preserves Pomelo's migration-lock names for schemas up to 45 characters and uses a deterministic, case-normalized SHA256 lock name for longer schemas. This keeps migration synchronization and history intact without renaming databases. The naming override uses Pomelo 9's protected lock-name hook; revalidate it when upgrading the provider.

### Applying migrations
This section describes how to apply migrations to your MySql database as well as how to add additional migrations if you changed the data model in a way that requires an update to the database.

First restore the solution and the repository-local EF tool from the repository root. The manifest pins `dotnet-ef` to the same version as EF Core; its runtime roll-forward allows the tool to run with only .NET 10 installed. Do not install an unpinned global tool.

- Open powershell
- `dotnet restore`
- `dotnet tool restore`
- `dotnet ef --version` (expected: the `dotnet-ef` version in `.config/dotnet-tools.json`)

Before upgrading an existing database, back it up and test these commands on a disposable copy. Keep `__EFMigrationsHistory`; do not use `EnsureCreated`, delete the database, or suppress pending-model errors to bypass an upgrade failure. SQLite applies migrations automatically on server startup; MySQL requires the commands below before starting the servers.

Before declaring content or gameplay work ready, also check all six contexts for
provider/model drift from the repository root. CI's `migration drift` job runs the
same commands and fails on any pending change. None of them needs a database server.

- `dotnet ef migrations has-pending-model-changes --project src/Rasa.DBL --startup-project src/Rasa.Game --context SqliteAuthContext`
- `dotnet ef migrations has-pending-model-changes --project src/Rasa.DBL --startup-project src/Rasa.Game --context MySqlAuthContext`
- `dotnet ef migrations has-pending-model-changes --project src/Rasa.DBL --startup-project src/Rasa.Game --context SqliteCharContext`
- `dotnet ef migrations has-pending-model-changes --project src/Rasa.DBL --startup-project src/Rasa.Game --context MySqlCharContext`
- `dotnet ef migrations has-pending-model-changes --project src/Rasa.DBL --startup-project src/Rasa.Game --context SqliteWorldContext`
- `dotnet ef migrations has-pending-model-changes --project src/Rasa.DBL --startup-project src/Rasa.Game --context MySqlWorldContext`

If required mission content is broken, `Rasa.Game` now logs each actionable
mission diagnostic and refuses to print `Server ready!` until the content is
fixed. Apply the matching World migrations and deploy the required C# mission
scripts; there is no separate active-release requirement.

The .NET 10 platform update itself requires no schema change. Apply the
repository's migrations in their recorded order, subject to the fresh-database
boundary below.

Now navigate to the folder of the Rasa.DBL project:

- `cd Path\to\Rasa.Net\src\Rasa.DBL`

To apply any pending migrations, execute the following commands:

- `dotnet ef database update --context=MySqlAuthContext`
- `dotnet ef database update --context=MySqlCharContext`
- `dotnet ef database update --context=MySqlWorldContext`

Explicitly providing the context is required as we have to work with different contexts according to database and provider.

If a MySQL World database was first built before the fix for `reconstruction_note`
(October 2026), its build may have stopped at `SeedWorldContent` with "Data too long
for column 'reconstruction_note'". `ConsolidatedWorldSchema` is then already recorded
as applied with the column too narrow, so re-running the update fails the same way.
Widen the column once, then run the World update again:

```sql
ALTER TABLE mission_evidence MODIFY reconstruction_note text CHARACTER SET utf8mb4 NOT NULL;
```

You can also migrate to any specific migration (forward or backward) by passing the migration name or the index/number of the migration as an argument. Obviously, this works with other DbContexts, too:

- `dotnet ef database update "MigrationName" --context=MySqlAuthContext` migrates MySqlAuthContext to "MigrationName".
- `dotnet ef database update 0 --context=MySqlAuthContext` rollback any migration on MySqlAuthContext, essentially resetting the database to an empty state.

To see existing migrations and if they are applied to your database, use one of the following:

- `dotnet ef migrations list --context=MySqlAuthContext`
- `dotnet ef migrations list --context=MySqlCharContext`
- `dotnet ef migrations list --context=MySqlWorldContext`


### Creating migrations
Basically, we differentiate between two types of migrations:
- Migrations that change the model / schema of the database
- Migrations that add or remove data to or from the database

Mission definitions, rewards, routes and scene bindings belong in C# **data**
migrations, using shared helpers where appropriate. See [mission authoring](missions.md).
Keep schema changes separate, and add a new migration instead of modifying
previously applied migration data.

Always ensure, that a migration only does one or the other. This is very easy, as we use code first approach. If you're developing a feature that requires an update to the schema of one or more of the databases, change the entry classes in RASA.DBL/Structures or add new entries by creating the class and adding a DbSet<EntryClass> to the respective DbContext. Then, create a migration applying those changes by executing the following commands:

- `dotnet ef migrations add <Name_of_the_Migration> --context=MySqlAuthContext`
- `dotnet ef migrations add <Name_of_the_Migration> --context=SqliteAuthContext`

If you realize something is wrong with the created migrations and you want to remove and recreate the last migration, execute the following commands:
- `dotnet ef migrations remove --context=MySqlAuthContext`
- `dotnet ef migrations remove --context=SqliteAuthContext`

Always add migrations for MySql *and* Sqlite for your changes.

As the code generated by migrations is database provider specific, seperate migrations for MySql and Sqlite are required. Examples for such differences are:
- Autoincrementing a primary key in Sqlite only works with the type `integer`
- Sqlite does not know unsigned numbers
- Sqlite does not know an explicit datetime type but instead uses `TEXT`

As already said, we use code first approach, so **do not** change the generated migration or model snapshot files that apply changes to the database model. With code first, you have the following methods to manipulate the output of the migration generator to your disposal (in order of priority):

- Try to work with Annotations in the Entry classes as much as possible. Examples:
-- `[Column("column_name", TypeName = "varchar(40)")]` sets a columns name and data type
-- `[Required]` makes a column "not null"
- If no annotation exists for your use case but the changes work for MySql and Sqlite, use the OnModelCreating method in the corresponding abstract base DbContext (AuthContext). Examples:
-- `.HasDefaultValue(<some value>)` sets a default value for a column
- If the change of the model needs to distinguish between MySql and Sqlite, try to move them to the database provider specific implementations of **IDbContextPropertyModifier**. For examples, see the implementations of this interface and how it is used.
- If that does not work, put them in the OnModelCreating methods of the corresponding derived DbContexts (MySqlAuthContext, SqliteAuthContext, MySqlCharContext, SqliteCharContext, MySqlWorldContext, SqliteWorldContext).

If, on the other hand, you use a migration to provide default data that needs to be imported into the database, you just create an empty migration. Ensure you didn't change any entry classes and add the migrations as descibed above. The created migration will be empty and you can use them do add data. As an example how this can be implemented for MySql and Sqlite can be seen in `20201218081744_Preload_ItemTemplate_PlayerExp_RandomName`.

## Mission data migrations

Mission content follows the same deployment flow as other database content.
SQLite startup creates missing files and applies pending schema/data migrations.
MySQL requires the normal explicit `dotnet ef database update` commands before
starting Game. Both providers use shared C# mission-data helpers.

The mission baseline consolidates its 162 development-time migration steps
into **six**, counting SQLite and MySQL separately. Migrations already on
`development` remain unchanged. The Wilderness rollout appends paired World
migrations after the complete PR105 history.

| Database | New migrations for each provider |
| --- | --- |
| Auth | None; the MySQL identity correction is snapshot metadata only |
| Char | `ConsolidatedCharacterSchema` |
| World | `ConsolidatedWorldSchema`, then `SeedWorldContent` |

The consolidated baseline targets **fresh databases**. It does not upgrade
databases that recorded the removed branch migration IDs, convert experimental
mission releases, or backfill intermediate character saves. Use fresh database
paths, or remove your own disposable files when you intend to start over.
Do not rewrite `__EFMigrationsHistory` to make an old branch database appear
compatible. The server does not delete databases or reset characters.

The supported rollout targets are a fresh merged database and an existing
PR105 database. PR105's final World migration is
`20261103000000_Snowball_stacks_not_unique`. All 17 Wilderness migration pairs
follow it, in the order below; each timestamp is identical for SQLite and MySQL.
The Wilderness-only World updates do not reset existing PR105 Char assignments,
inventory, flags or history.

| Timestamp | Wilderness migration |
| --- | --- |
| `20261104000000` | `WildernessOpeningWorld` |
| `20261104000100` | `NativeMissionCategory` |
| `20261104000200` | `WildernessAliaOpening` |
| `20261104000300` | `WildernessHubWorld` |
| `20261104000400` | `RelatedMissionFailureAction` |
| `20261104000500` | `WildernessRewardEquipment` |
| `20261104000600` | `WildernessAdditionalWorld` |
| `20261104000700` | `WildernessSpawnStatistics` |
| `20261104000800` | `WildernessAliaBranches` |
| `20261104000900` | `WildernessSniperPlacement` |
| `20261104001000` | `WildernessElohPinhole` |
| `20261104001100` | `WildernessLandingZone` |
| `20261104001200` | `WildernessSupportedRewards` |
| `20261104001300` | `WildernessTwinPillars` |
| `20261104001400` | `WildernessRanjaGorge` |
| `20261104001500` | `WildernessDaghdasUrn` |
| `20261104001600` | `WildernessEvidenceCapacity` |

The earlier September Wilderness migration IDs were unshipped and are not an
upgrade source for this integration. Their development databases are disposable;
use fresh configured paths rather than rewriting `__EFMigrationsHistory`.
Intermediate-upgrade fixtures use the November lineage and retain their own
active mission progress. That regression coverage does not convert old
experimental or September Wilderness saves.

Mission-authored World creatures, pools and attack rows now use the allocated
`630001..630199` namespace, leaving PR105's Divide `530xxx` rows untouched.
Char outcome flags `530002` and `530003` are separate identities and do not move.
PR105's `Add_armor_values` supplies native `itemclass.max_hp` armor values before
Wilderness starts. `WildernessRewardEquipment` is a no-op compatibility marker
and cannot insert duplicates or delete those PR105-owned rows on rollback.

`SeedWorldContent` installs shared World content, including the five enabled
Bootcamp missions and their private experience bindings. Both providers call
`WorldContentDataV1`; schema and data remain separate. Subsequent content changes
should add new migrations rather than edit this seed. Required script and content
validation remains part of startup. Read the [authoring guide](missions.md) and
[data/script reference](mission-reference.md) when adding or changing missions.

## Build and run the code from Visual Studio
You should be ready to compile Rasa.NET and run the servers.

- Launch Visual Studio and open the `Rasa.NET.sln` file in the code repository
- If you have to overwrite the default database connection parameters, see "Custom configuration" and "Database configuration"
- Build the solution
- For MySQL, apply pending migrations; SQLite runs them automatically on startup
- Run the `Rasa.Auth` project via `Debug > Start without Debugging`
- Run the `Rasa.Game` project via `Debug > Start Debugging`
- Alternatively, define multiple start projects as follows:
  - Right click on the solution
  - Click "Set StartUp Projects"
  - Choose "Multiple startup projects"
  - Set "Action" for both projects wether you want to start with or without debugging
- Of course, you can also build the project and start the Auth server from the bin directory.

### Build and test from the command line

From the repository root:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src\Rasa.Auth\Rasa.Auth.csproj --no-build
```

Start Game in a second terminal with
`dotnet run --project src\Rasa.Game\Rasa.Game.csproj --no-build`.
For a self-contained deployment, publish each server with a portable identifier, for example:

```powershell
dotnet publish src\Rasa.Auth\Rasa.Auth.csproj -c Release -r win-x64 --self-contained true
dotnet publish src\Rasa.Game\Rasa.Game.csproj -c Release -r win-x64 --self-contained true
```

Use `osx-x64` or `linux-x64` for the other deployment targets.

Game publishes include compiled migration data, mission scripts and navigation
assets. Use the published executable's
`--check-mission-assets` diagnostic from an unrelated working directory to check
asset discovery without starting listeners or opening databases.

### Navigation and navmesh assets

`Rasa.Navigation` is the runtime query library, `Rasa.NavMesh` is the offline builder, and `Rasa.ClientData` reads the installed client's map and mesh data. The repository contains 77 generated `.nav` files under `navmesh`. `Rasa.Game` loads matching files at startup from `GameDataConfig.NavMeshPath`. For the default `navmesh` setting, it checks the working directory, the application's directory, then the repository root when running from a source checkout. Explicit custom paths remain relative to the working directory and are not replaced by this discovery.

Game publishes include the navigation assets. The startup log reports the resolved folder and loaded-map count. Restart the server after updating assets so new private map instances inherit the loaded mesh. Scripted routes such as Alister's move require navigation and refuse to start without it. A failed query on a loaded mesh never becomes a straight line through geometry; ordinary creatures on maps that intentionally have no mesh retain their legacy movement.

To rebuild all navmeshes from a local 1.16.5.0 client installation:

```powershell
dotnet run --project src\Rasa.NavMesh\Rasa.NavMesh.csproj --no-build -- --client "C:\Games\Tabula Rasa" --out navmesh
```

Use `--map adv_foreas_concordia_wilderness` to rebuild one map. Generated files are inputs to `Rasa.Game`; no game client or live database is required to compile the navigation projects.

### Database compatibility tests

The compatibility tests verify that the SDK, Docker images and package versions agree across the files that pin them, all solution project targets, the retained navigation project/package references, the Docker image layout, cryptographic fixtures, connection-string-specific MySQL server-version caching, and deterministic migration-lock names for schemas through MySQL's 64-character limit. The MySQL configuration tests use a fixed server version and do not connect to a database.

```powershell
dotnet test src\Rasa.Test\Rasa.Test.csproj --filter "FullyQualifiedName~Compatibility"
```

### Live MySQL tests

The tests in the `MySql` category run against a real MySQL server: every MySQL migration applied from empty, the same row counts as Sqlite in every table afterwards, and accounts, characters and missions written and read back through the repositories. CI runs them against MySQL 8.0 and 8.4 in the `mysql` job and leaves them out of the other test lanes. Without a server, `dotnet test` reports them as skipped.

To run them locally, start a throwaway server (`--tmpfs` keeps its data in memory, which makes the seed migrations much faster):

```powershell
docker run -d --name rasa-mysql -p 3306:3306 --tmpfs /var/lib/mysql -e MYSQL_ROOT_PASSWORD=rasa-ci mysql:8.4
```

Point the design-time factories at it with a `src\Rasa.DBL\databasesettings.env.json` (see [Database configuration](#database-configuration)) that sets `Host` `127.0.0.1`, `User` `root`, `Password` `rasa-ci` and a `TimeoutInMilliseconds` of a few minutes for each of `Auth`, `Char` and `World`; the timeout is also the command timeout, and the World seed is large. Then build, apply the migrations, and run the category with the server in `RASA_TEST_MYSQL`:

```powershell
dotnet build src\Rasa.Test
dotnet ef database update --no-build --project src\Rasa.DBL --startup-project src\Rasa.Game --context MySqlAuthContext
dotnet ef database update --no-build --project src\Rasa.DBL --startup-project src\Rasa.Game --context MySqlCharContext
dotnet ef database update --no-build --project src\Rasa.DBL --startup-project src\Rasa.Game --context MySqlWorldContext
$env:RASA_TEST_MYSQL = "Server=127.0.0.1;Port=3306;User ID=root;Password=rasa-ci"
dotnet test src\Rasa.Test --no-build --filter TestCategory=MySql
```

The tests use the database names from `databasesettings.json` (and `databasesettings.env.json`, if it sets them), check row counts against an empty build, and remove the rows they add, so point them only at a throwaway server, never at one with real data. Delete `databasesettings.env.json` afterwards, or the servers you run from that build will read it too.

### Create a game user
The authentication server can be used to create a user by running a command in the terminal. The usage is: `create <email> <username> <password>`. Running this command will create a new user in the database that you can use to login with the game client.

- Run the command in the authentication termain. i.e. `create test@test.com test test`
  - You can use any username / password that you want to create an account.
  
  
## Launch the game
If the server consoles launched correctly, you should be ready to start the game client.

- Start the game client using the shortcut you created earlier
- Login with the user you created for the game above

> A first-login server crash is reported in [InfiniteRasa/Rasa.NET#45](https://github.com/InfiniteRasa/Rasa.NET/issues/45). The automated protocol checks do not reproduce the complete native-client first-load sequence. If you encounter it, capture the server error and frame boundaries, restart `Rasa.Game`, and retry without treating the workaround as acceptance. See the [protocol regression guide](protocol-testing.md).
