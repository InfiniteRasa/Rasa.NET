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
- Build the solution
- Run the `Rasa.Auth` project via `Debug > Start without Debugging`
- Run the `Rasa.Game` project via `Debug > Start Debugging`

### Create a game user
The authentication server can be used to create a user by running a command in the terminal. The usage is: `create <email> <username> <password>`. Running this command will create a new user in the database that you can use to login with the game client.

- Run the command in the authentication termain. i.e. `create test@test.com test test`
  - You can use any username / password that you want to create an account.

## Launch the game
If the server consoles launched correctly, you should be ready to start the game client. 

- Start the game client using the shortcut you created earlier
- Login with the user you created for the game above

> Note* The game server will crash the first time you try to login due to a bug that is not fixed at the time of writing. Go back to Visual Studio and run the `Rasa.Game` project again. Once it's running, switch back to the game client and log back in.


