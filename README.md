# Rasa.NET
A C# implementation of a game and authentication server for the game running on .NET 10.

## Before you start
This project is in development and not complete. You may not be able to play the game in any capacity. For the latest information, we recommend [joining our Discord](https://discord.gg/Ph68FmA) chat. 

## How-to use this code
There are a few required tools and steps to get everything setup before you can run the game. Follow the steps in the [setup guide](docs/setup.md). Creature pathfinding uses the checked-in per-map navmeshes; see [navigation and navmesh assets](docs/setup.md#navigation-and-navmesh-assets).

## Game tools
The [game tools](gametools/README.md) are two web pages for a running server: an
editor for creature class flags and one for loot pools. Each opens from disk and,
with a settings file beside it, reads from and writes to the server through its
REST API.

## Contributing
If you are interested in helping in the development of Rasa.NET, please [join the Discord](https://discord.gg/Ph68FmA) and chat!

Mission contributors should read [mission authoring and operations](docs/missions.md)
for C# data migrations, typed scripts and public encounters.
The [mission data and script reference](docs/mission-reference.md) describes
trigger/action bindings, requirements and shared migration helpers. SQLite
startup creates and migrates mission content automatically; MySQL uses the
normal manual migration process. There is no separate mission publish step.
This branch's migration-owned mission design requires fresh databases.

The [Wilderness content reference](docs/wilderness-missions.md) records the
native mission inventory, reconstruction decisions and deferred instance
dependencies for the outdoor rollout.

## Feedback
- Ask questions and discuss development on [Discord](https://discord.gg/Ph68FmA)
- Submit bugs to GitHub Issues
