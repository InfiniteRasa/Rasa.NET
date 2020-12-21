# Setup Guide
This guide will help you install and setup the required tools to run Rasa.NET. There are a few steps:

1. Download, install, and setup the required tooling
2. Download and install the game
3. Download or clone the code
4. Build and run the code from Visual Studio
5. Launch the game

## Download, install, and setup the required tooling
The following tools are required to setup, build, and run Rasa.NET:

- Microsoft Visual Studio 2017 or higher
- .NET Core SDK and Runtime
- MySQL Server
- MySQL Workbench
- Git

### Install Visual Studio
There is a single Visual Studio solution that contains the projects needed for Rasa.NET. Use Visual Studio 2017 or higher to compile and build these projects.

- Download [Visual Studio](https://visualstudio.microsoft.com/). Choose the license that works for you
- Run the installer after it's downloaded
- In the Workloads selection, make sure that `.NET desktop development` and `.NET Core cross-platform development` boxes are checked
- Continue the installation and let it finish

### Install .NET Core
Rasa.NET is set to build and run with .NET Core.

- [Download the .NET Core 5 SDK and Runtime](https://dotnet.microsoft.com/download/dotnet/5.0) and install it

### Install MySQL Server and MySQL Workbench
Rasa.NET uses MySQL to store game data. Download and install MySQL Community Server following these steps:

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

### Setup the database
Before you can build and run the code, there are some configuration steps for the database.

- Launch MySQL Workbench and select your `Local instance MySQL80` under `MySQL Connections` that was created during install
- Enter your root password that you created to connect to it
- In the Navigator panel on the left, right-click in the blank space and choose `Create Schema`
  - In the new tab, name the first schema `rasaauth` and click `Apply`
  - Repeat this two more times for `rasachar` and `rasaworld`
- Double-click `rasaauth` in the `SCHEMAS` list which makes it active
- Go to `File > Open SQL Script`
- Select `Rasa.NET\database\full\auth_database.sql` file in the code directory that you clone in the above steps
- Execute the script to create the required tables by clicking the icon that looks like a lightning bolt
  - Repeat this process for the `Rasa.NET\database\full\char_database.sql` and `Rasa.NET\database\full\world_database.sql` files - **but first double-click the matching schema on the left to make it the active selection**
- Next, you'll have to execute each of the sql files in the `Rasa.NET\database\patches` folder
  - Follow similar steps as above, making sure to make the correct schema active and execute each patch script against the matching schema in sequential order.

### Add a database user for the servers
The auth and game servers are pre-configured to use the user `rasa` to connect. You'll need to add this user to your database instance.

- In MySQL Workbench, go to `Server > Users and Privileges`
- Click `Add Account`
- Set the `Login Name` to `rasa`
- Set the `Limit Hosts to Matching` to `localhost`
- Set the password to `rasa`
- Select the `Administrative Roles` tab and check `DBA`
- Click `Apply`

## Build and run the code from Visual Studio
You should be ready to compile Rasa.NET and run the servers.

- Launch Visual Studio and open the `Rasa.NET.sln` file in the code repository
- If you have to overwrite the default database connection parameters, see "Custom configuration" and "Database configuration" down below
- Build the solution
- Run the `Rasa.Auth` project via `Debug > Start without Debugging`
- Run the `Rasa.Game` project via `Debug > Start Debugging`
- Alternatively, define multiple start projects as follows:
-- Right click on the solution
-- Click "Set StartUp Projects"
-- Choose "Multiple startup projects"
-- Set "Action" for both projects wether you want to start with or without debugging

### Create a game user
The authentication server can be used to create a user by running a command in the terminal. The usage is: `create <email> <username> <password>`. Running this command will create a new user in the database that you can use to login with the game client.

- Run the command in the authentication termain. i.e. `create test@test.com test test`
  - You can use any username / password that you want to create an account.

## Custom configuration
If you have to overwrite one or multiple settings from the appsettings.json of `Rasa.Auth` or `Rasa.Game` or the databasesettings.json of Rasa.DBL, do the following:

- Create a file named "appsettings.env.json"/"databasesettings.env.json" in the root of the respective project or copy and rename the existing file
- The new file will be shown as subelement of the original file
- Use the new file to overwrite settings from appsettings.json/databasesettings.json. Keep property naming and structure, you don't need to add settings that you don't want to change. For example, to overwrite GameConfig.PublicAddress in the Rasa.Game settings, use
- The env.json files is ignored in git. Keep it that way, this configuration applies only for your development enviroment.

```json
{
  "GameConfig":{
    "PublicAddress": "<new value>"
  }
}
```

### Database configuration
As of now, we use three databases: Auth (Accounting), Char (everything related to characters) and World (mainly common settings regarding the game world). The Auth database is connected via EF Core and supports MySql and Sqlite as database providers. Connection information is provided in the form of a defined data structure in the file `Rasa.DBL\databasesettings.json`. Char and World only support MySql and are connected via connection strings in `Rasa.Game\appsettings.json` as of now. EF Core and Sqlite support are in progress for these databases. For a quick jump into development and small servers, Sqlite works fine and totally out of the box.

To setup your database settings, have a look in "Custom configuration" to learn how to create an enviroment specific settings file.

To access a MySql server with EF Core, set `Provider` to `MySql`. You need to provide host, port, database, user, password and timeout in the config file. Keep in mind that we fall back to the values in databasesettings.json, if  your enviroment file does not overwrite a value. For MySql, you have to apply any pending migrations yourself. See "Working with migrations" for a quick start.

To use Sqlite with EF Core, set `Provider` to `Sqlite`. Sqlite uses only the value _database_ of the configuration to create a file named `<database>.db`. For Sqlite, any pending migrations will be applied __automatically__ at startup of the corresponding project to make usage even easier.

If you want to add additional migrations, see "Working with migrations".

### Working with migrations
This section describes how to apply migrations to your MySql database as well as how to add additional migrations if you changed the data model in a way that requires an update to the database.

First, if not already done, install dotnet-ef (see also https://docs.microsoft.com/en-GB/ef/core/cli/dotnet):

- Open powershell
- `dotnet tool install --global dotnet-ef`

Now navigate to the folder of the Rasa.DBL project:

- `cd Path\to\Rasa.Net\src\Rasa.DBL`

To apply any pending migrations, execute the following commands:

- `dotnet ef database update --context=MySqlAuthContext`
- `dotnet ef database update --context=MySqlCharContext`
- `dotnet ef database update --context=MySqlWorldContext`

Explicitly providing the context is required as we have to work with different contexts according to database and provider.

You can also migrate to any specific migration (forward or backward) by passing the migration name or the index/number of the migration as an argument. Obviously, this works with other DbContexts, too:

- `dotnet ef database update "MigrationName" --context=MySqlAuthContext` migrates MySqlAuthContext to "MigrationName".
- `dotnet ef database update 0 --context=MySqlAuthContext` rollback any migration on MySqlAuthContext, essentially resetting the database to an empty state.

To see existing migrations and if they are applied to your database, use one of the following:

- `dotnet ef migrations list --context=MySqlAuthContext`
- `dotnet ef migrations list --context=MySqlCharContext`
- `dotnet ef migrations list --context=MySqlWorldContext`

If you're developing some functionality that requires an update to one of the databases, execute the following commands:

- `dotnet ef migrations add <Name_of_the_Migration> --context==MySqlAuthContext` 
- `dotnet ef migrations add <Name_of_the_Migration> --context==SqliteAuthContext`

To remove the last migration, execute the following commands:
- `dotnet ef migrations remove --context==MySqlAuthContext` 
- `dotnet ef migrations remove --context==SqliteAuthContext`

Always add migrations for MySql *and* Sqlite for your changes.

As the code generated by migrations is database provider specific, seperate migrations for MySql and Sqlite are required. Examples for such differences are:
- Autoincrementing a primary key in Sqlite only works with the type `integer`
- Sqlite does not know unsigned numbers
- Sqlite does not know an explicit datetime type but instead uses `TEXT`

As we use code first approach, **do not** change the generated migration or model snapshot files. Instead, use one of the following methods to manipulate the output of the migration generator (in order of priority):

- Try to work with Annotations in the Entry classes as much as possible. Examples:
-- `[Column("column_name", TypeName = "varchar(40)")]` sets a columns name and data type
-- `[Required]` makes a column "not null"
- If no annotation exists for your use case but the changes work for MySql and Sqlite, use the OnModelCreating method in the corresponding abstract base DbContext (AuthContext). Examples:
-- `.HasDefaultValue(<some value>)` sets a default value for a column
- If the change of the model needs to distinguish between MySql and Sqlite, try to move them to the database provider specific implementations of **IDbContextPropertyModifier**. For examples, see the implementations of this interface and how it is used.
- If that does not work, put them in the OnModelCreating methods of the corresponding derived DbContexts (MySqlAuthContext, SqliteAuthContext, MySqlCharContext, SqliteCharContext, MySqlWorldContext, SqliteWorldContext).

## Launch the game
If the server consoles launched correctly, you should be ready to start the game client.

- Start the game client using the shortcut you created earlier
- Login with the user you created for the game above

> Note* The game server will crash the first time you try to login due to a bug that is not fixed at the time of writing. Go back to Visual Studio and run the `Rasa.Game` project again. Once it's running, switch back to the game client and log back in.
