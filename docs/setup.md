# Setup Guide
This guide will help you install and setup the required tools to run Tabula Rasa using Rasa.NET. There are a few steps:

1. Download, install, and setup the required tooling
2. Download and install Tabula Rasa
3. Patch Tabula Rasa using a custom patcher
4. Download or clone the code
5. Build and run the code from Visual Studio
6. Launch Tabula Rasa

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
Rasa.NET is set to build and run with .NET Core 3.0.0-preview, which is currently in Preview. 

- [Download the .NET Core SDK and Runtime Preview](https://dotnet.microsoft.com/download/dotnet-core/3.0) and install it

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

## Download and install Tabula Rasa
You'll need the Tabula Rasa game client. Below is the demo version which was made freely available. 

- [Download and install the demo game client](http://infiniterasa.org/tools/tabularasa_demo.exe).
- Create a shortcut to the game client on the desktop. You'll need to find the executable in the path where you installed the game. i.e. `C:\ProgramFiles (x86)\TabulaRasa\NCSoft\TabulaRasa\tabula_rasa.exe`
  - Right-click on `tabular_rasa.exe` and choose `Send to > Desktop (create shortcut)`
  - Go to the desktop and right-click on the newly created shortcut and choose `Properties`
  - Select the `Shortcut` tab
  - Edit the `Target` box to include `/NoPatch /AuthServer=localhost:2106` to the end of the file path. So the entire textbox should have `C:\ProgramFiles (x86)\TabulaRasa\NCSoft\TabulaRasa\tabula_rasa.exe /NoPatch /AuthServer=localhost:2106`

## Patch Tabula Rasa using the custom patcher
The game client needs to be patched to a specific version that is compatible with Rasa.NET. 

- [Download and run the custom patcher](http://infiniterasa.org/tools/tabularasa_patcher.exe)

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
- Select the `rasaauth` schema and doubl-click it to make it active
- Select `File > New Query Tab` and execte this statement to create a test account:

```
INSERT INTO `account` (`email`, `username`, `password`, `salt`, `level`, `last_ip`, `last_server_id`, `last_login`, `join_date`, `locked`, `validated`, `validation_token`) VALUES ('test@test.com', 'test', 'cb82594cf66f17acc074c9fd5e6d602b62faadf7b9364f0c95996de45f3eeaec', '0bae44ea0d4d284f9cd451f97100475b0603a8be', 1, '127.0.0.1', 0, '2017-01-21 12:14:11', '2017-01-19 23:00:15', b'0', b'1', NULL);
```

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
- Build the solution
- Run the `Rasa.Auth` project via `Debug > Start without Debugging`
- Run the `Rasa.Game` project via `Debug > Start Debugging`

## Launch Tabula Rasa
If the server consoles launched correctly, you should be ready to start the game client. 

- Start the game client using the shortcut you created earlier
- Login with user `test` and password `test`

> Note* The game server will crash the first time you try to login due to a bug that is not fixed at the time of writing. Go back to Visual Studio and run the `Rasa.Game` project again. Once it's running, switch back to the game client and log back in.

