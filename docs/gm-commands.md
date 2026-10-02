# GM commands

Every GM command the game and auth servers understand: the level each one needs, what it takes and what it does.
Checked against `development` at `f4439234`. The sources are:

- `Rasa.Game/Managers/ChatCommandsManager.cs`: dot commands and the privileged slash-command table.
- `GmMapCommands.cs` and `GmMissionCommands.cs`: map and mission commands.
- `Moderation.cs` and `MessageOfTheDay.cs`: announcements, kicks, silences, and the message of the day.
- `Game/Server.cs` and `Auth/Server.cs`: console commands.

When a command is added or its level changes, update this page with it.

## How commands are entered

There are three ways in:

| Kind | Where you type it | How it reaches the server |
|---|---|---|
| **Dot commands** (`.tele`, `.giveitem` ...) | In-game chat | `RadialChat` hands anything starting with `.` to `ChatCommandsManager.ProcessCommand`. |
| **Privileged slash commands** (`/gotomap`, `/killmap` ...) | In-game chat, or the client's GM pickers | The client has no local handler for them, so it sends `PrivilegedCommand(command, args)`. |
| **Console commands** (`gm`, `ban` ...) | The game or auth server's console window | `CommandProcessor`. They need no account, and granting GM goes through this route. |

A few GM actions are client packets with their own handlers instead of commands. These are `/gotomob`, `/changefirstname`, `/changelastname`, the LOS slash commands and the mission window's Complete button. They are listed in the sections they belong to.

Conventions used below:

- `<x>` is required and `[x]` is optional.
- `#entityId` means a literal `#` followed by an entity id, for example `#1234`. `#target` means whatever you have selected.
- A dot command's arguments are split on single spaces, so a family name or map name cannot contain spaces unless the command says otherwise.
- Unless a command names another player, it acts on you.

## Access levels

The account's level is `game_account.level`, stored as a byte. Levels are cumulative, so each rank can use everything below it. The gaps between the numbers are deliberate, so a rank can be added later without renumbering.

| Level | Rank | What it allows |
|---|---|---|
| 0 | Player | No GM commands at all. |
| 1 | **Observer** | Look, don't touch: positions, distances, what is nearby, map diagnostics, the GM UI flag. |
| 5 | **GameMaster** | Move yourself, spawn and drive scenery and creatures, drive your own client, edit map data, rename characters. A restart undoes most of it; map-data edits are saved. |
| 10 | **Admin** | Hand out progression, items, credits, Logos and titles; set server-wide flags; reset maps; reload data. A restart does not undo these. |

**Granting a level (game server console):** `gm <familyName> [level]`.

- With no level, it prints the account's current level.
- The level can be a number from 0 to 255 or a rank name, for example `gm Ellimist admin`.
- A player who is logged in gets the new level immediately.
- This is a console command so that granting GM never depends on already having it.

**When the level is too low:** the server answers an account at level 0 with "Unknown command", so the reply doesn't reveal what the server can do. A GM whose level is too low is told which level the command needs. Both cases are logged as `Security`.

**`.help`** (Observer) lists only the commands your level can run, ordered by level.

## Information and diagnostics

These change nothing in the world, except where noted.

| Command | Level | What it does |
|---|---|---|
| `.gm` | Observer | Sends `SetIsGM(true)` so your client shows its GM UI. |
| `.help` | Observer | The commands available at your level. |
| `.where` | Observer | Your position, facing and map id. Also written to the server log with a `[.where]` tag. |
| `.getdistance` | Observer | Distance to your selected target. For a creature it measures to the creature's **spawn pool position**, not to the creature. Objects print "ToDo". |
| `.near` | Observer | Lists the objects and creatures in your cells. The output goes to the **server console**, not to chat. |
| `.npcinfo` | Observer | For your target: entity id and type. For a creature it adds DB id, target category, health, armour and its regeneration, spawn pool id and position. |
| `.maperrors` | Observer | The map-errors dialog for the map you are on (the same one an Observer or above gets on entering a broken map). |
| `.missions [characterId]` | Observer | Mission state for a character: you by default, or the character id given. |
| `.links` | Observer | Map links on this map, nearest first (up to 10), marking the one you are standing in. |
| `.regions` | Observer | Whether you are underground, the region ids you are being sent, and the region volumes on this map, nearest first (up to 12). |
| `.emitters` | Observer | FX emitters on this map, nearest first (up to 15). |
| `.fxpackages <word> [word ...]` | Observer | Searches the client's FX package names; every word must match (up to 25 results). |
| `.navmesh` | Observer | Whether this map has a navmesh, and the navmesh ground height under you. |
| `.navmesh path <x> <y> <z>` | Observer | The route the AI would take from you to that point: complete or partial, number of corners, length. |
| `.cover` | Observer | Cover between you and your target in both directions: body points visible and the ranged damage multiplier. Needs `navmesh/<map>.cover`. |
| `.los [entityId]` | Observer | The server's line-of-sight report to your target or to the id given. The LOS slash commands send the same `RequestLOSReport`, but they are in the unshipped developer module, so a retail client never sends them. |
| `.camerascript list` / `.camerascript <id>` | Observer | Lists this map's camera scripts, or plays one on your own client. Space or Escape ends it. |
| `.clientevent [list]` | Observer | The client's scriptable events, marking which ones you are tracking. |
| `.clientevent track <id\|name\|all> ...` / `stop <id\|name> ...` / `stop all` | Observer | Starts or stops tracking scriptable client events on your own client; tracked events are echoed to you. |
| `.rqs` | Observer | Opens the client's developer RQS window (`DevRQSWindow`). |
| `.motd` | Observer | Shows you the message of the day as players get it, even if your client has seen it before. Says so if there is none. |
| `.moveflags` | GameMaster | Toggle. Shows the flags byte of your `Move` packets each time it changes, with your height and whether you are in water. |

These also happen automatically, without a command:

- **Observer and above:** you receive `ServerPerformanceMetrics` every `PerformanceMetricsInterval`, and you get the map-errors dialog when you enter a map that has data errors.
- **GameMaster and above:** your client is sent `EnableDevCommands` once per connection. A retail client lacks the `developerkeys` module and only logs an ImportError, so in practice nothing happens.

## Movement and travel

| Command | Level | What it does |
|---|---|---|
| `.tele <x> <y> <z>` | GameMaster | Moves you to that point on the current map. The server's copy of your position moves too. |
| `.teleup <y>` | GameMaster | Changes only your height. |
| `.teleport <x> <y> <z> <mapId>` | GameMaster | Changes map to that point. Fails if the map is not loaded. |
| `.speed <value>` | GameMaster | Sets your movement speed on the server and on your client (`MovementModChange`). Only your own client is told. |
| `/gotomap` | GameMaster | With no argument, opens the client's map picker. |
| `/gotomap <map> [startGroup]` | GameMaster | Goes to a map's start group; the default is the first one. The map can be a context id, a whole name, or a unique part of a name. The start group can contain spaces. |
| `/gotostartgroup [name\|id]` | GameMaster | With no argument, lists this map's start groups in the waypoint window; with one, moves you to it. |
| `/gotomob <name>` | GameMaster | Puts you 2 m from the nearest creature of that name on this map. An exact name match wins over partial ones. |
| `.link <id> goto` / `.link <id> gotoarrival` | GameMaster | Goes to a map link's trigger point or its arrival point. |
| `.emitter <id> goto` | GameMaster | Goes to an FX emitter. |

A map's start groups are generated in this order:

1. Its waypoints (`Waypoint <id>`).
2. Its hospitals (`Hospital <id>`), each 1 m above the pad.
3. The arrival point of every enabled map link into the map (`Entrance <linkId>`).

`characterselection` is never listed.

## Characters and progression

| Command | Level | What it does |
|---|---|---|
| `.rename first\|last <NewName> [familyName]` | GameMaster | Renames yourself or the player with that family name (the name must be in the world). `last` renames the family, so every character on the account. |
| `/changefirstname`, `/changelastname` | GameMaster | The client's own rename slash commands, for yourself. |
| `.heal [full\|<amount>] [familyName]` | GameMaster | Heals you or a player in the world. Does not revive the dead. |
| `.setkillstreak <count>` | GameMaster | Sends `SetKillStreak` to your own client (display only). |
| `.setlevel <level>` | Admin | Puts your character at level 1–50. Going up passes through every level in between, as experience would; going down resets what the new level no longer allows. Equipment worn above the new level stays equipped. |
| `.givexp <amount>` | Admin | Gives you experience. |
| `.chg_class <class>` | Admin | Changes your class. Class names: RECRUIT, SOLDIER, SPECIALIST, COMMANDO, RANGER, SAPPER, BIOTECHNICIAN, GRENADIER, GUARDIAN, SNIPER, SPY, DEMOLITIONIST, ENGINEER, MEDIC, EXOBIOLOGIST. |
| `.givecredits <amount> [familyName]` | Admin | Credits for you or a player in the world. A negative amount takes credits away; the balance stops at zero and never goes negative. The player is told. |
| `.giveitem <itemTemplateId> [quantity]` | Admin | Puts an item in your own inventory. With no quantity you get a full stack. You are recorded as the crafter. |
| `.givelogos <logosId>` | Admin | Adds a Logos (1–408, with gaps) to your Tabula. Ids the client doesn't know and Logos you already have are refused. |
| `.removelogos <logosId\|all>` | Admin | Removes one Logos, or all of them, from your Tabula, including the saved rows. |
| `.givepads` | Admin | Unlocks every dropship pad in the world for you. |
| `.addtitle <titleId>` | Admin | Grants you a title (`titledata` id): saved with the character, and announced by the client with "you have gained the title". |

## Missions

The player being inspected or changed has to be in the world. A target is given as a family name, or as `#characterId`; if omitted, it is you.

| Command | Level | What it does |
|---|---|---|
| `.usermissions [familyName\|#characterId]` or `/usermissions ...` | GameMaster | Opens that player's mission log in your intel window (`GmShowUserMissionsAck`). |
| GM **Complete** button in that window | GameMaster | Completes the objective (`ForceCompleteObjective`) and re-sends you the log. |
| `.completeobjective <missionId> <objectiveId> [familyName\|#characterId]` | GameMaster | The same, without the window. |
| `/givemission` or `.givemission` | GameMaster | With no argument, opens the client's Give Mission picker. |
| `/givemission <missionId>` | GameMaster | Gives the mission to you. |
| `.givemission <missionId> [familyName\|#characterId]` | GameMaster | Gives the mission to you or to another player. |
| `.failmission <missionId>` | Admin | Fails one of your active missions. |
| `.failobjective <missionId> <objectiveId>` | Admin | Fails one of your active objectives. |

`.missions [characterId]` (Observer, under Information and diagnostics) is the read-only view.

## Combat, effects and actor state

These are testing tools. Their changes are held in memory only.

| Command | Level | What it does |
|---|---|---|
| `.effect [list] [#entityId\|#target]` | GameMaster | The effects on you or on the actor named: id, type, level, buff or debuff, source, time left, paused. |
| `.effect pause\|restart <effectId\|all> [#entityId\|#target]` | GameMaster | Stops or restarts an effect's clock; the client shows its "Paused" tooltip. |
| `.vamp <health\|power\|armor\|adrenaline> <amount> [#entityId]` | GameMaster | Steals up to that amount from your target or the actor named, as the Vamp item modules will. |
| `.immune [all \| off \| <type> ... \| -<type> ...]` | GameMaster | Makes your target (or you, with no target) immune to all damage or to the damage types named. With no argument, shows the current immunities. |
| `.falldamage <metres>` | GameMaster | Deals you the damage a fall of that height would, and says whether you are standing in water. |
| `.blockaction [actionId [off]]` | GameMaster | With no argument, lists the actions blocked for you and why. With an id, blocks that action for you; `off` removes only the GM block. |
| `.actorstate <state> [state ...] [#entityId]` | GameMaster | Sends a `StateCorrection` to everyone who can see the actor, by state name or id (`standing`, `sitting`, `crouched`, `dead`, `stunned` ...). It changes appearance only. |
| `.track [<targetId\|me\|target\|0> [#entityId\|#target]]` | GameMaster | Sets an actor's tracking target for everyone who can see it; `0` clears it. With no argument, shows what you and your target are tracking. |
| `.targetcategory [category]` | GameMaster | With no argument, shows the targeted creature's category. With one, sets it and tells nearby clients; the creature's hate list is cleared. |
| `.feud [list]` | GameMaster | Clan feuds running (with score and time left) and challenges waiting. |
| `.feud start <clan> <clan>` | GameMaster | Starts a feud and skips every rule (PvP, leaders online). A clan is an id or a name with no spaces. |
| `.feud end <id> [tie\|cancel\|<winning clan>]` | GameMaster | Ends a feud. With no outcome, it is decided as its clock would decide it (most kills wins). A feud that ends with a winner, here or on its own, takes the wagered items of the losing clan's members, and of anyone who left or was kicked from it while the feud ran: they go to the winning clan's lockbox, or by mail to the winning clan's leader of the challenge when the lockbox is full. `tie` and `cancel` take nothing. |
| `.feud length [minutes]` | GameMaster | Shows or sets how long new feuds last. Maximum one week. |

Values these commands accept:

- **Damage types:** Physical 1, Fire 2, Ice 3, Virulent 4, EMP 5, Laser 6, Sonic 7, KnockBack 8, Stun 9, Sleep 10, System 11, Environmental 12, Electrical 13, Snare 14, Root 15, Confusion 16, Jam 17, Fear 18, Logos 19, Blind 20.
- **Target categories:** hostile 0, friendly 1, object 2, neutral 3, decoration 4, decorationproxy 5, ignore 6.

## Creatures and minions

| Command | Level | What it does |
|---|---|---|
| `.creature <creatureDbId>` | GameMaster | Spawns a creature at your feet, facing your way. |
| `.minion <creatureDbId>` | GameMaster | Spawns a creature and makes it your minion, forced friendly. The seeded bots are 600001 Flame, 600002 Rocket, 600003 Shield and 600004 Repair. Giving it orders needs the `MinionCommands` server flag. |
| `.minion list` / `.minion clear` | GameMaster | Lists your minions, or dismisses them all. |
| `.comehere <entityId>` | GameMaster | Sends a `MoveObject` to your own client that brings the entity to you. Only your client sees it move; the server's copy stays put. |
| `.bark <entityId> <barkId>` | GameMaster | Plays a bark on that entity, on your client only. |
| `.creatureappearance <entityId> <slotId> <classId> <color>` | GameMaster | Sets one appearance slot on a creature. |
| `.creatureloc` | GameMaster | Registered but does nothing: the body is commented out. |
| `.reloadcreatures` | Admin | Clears and re-reads the creature table. Creatures already spawned are unaffected. |

## Objects and map data

Map links, region volumes, FX emitters and crafting stations are **saved to the database**. Everything else in this section is in memory only, or only visible to your own client.

| Command | Level | What it does |
|---|---|---|
| `.createobj <entityClassId>` | GameMaster | A dynamic object at your feet, facing your way. |
| `.createobjonloc <entityClassId> <x> <y> <z> <orientation>` | GameMaster | A dynamic object at the given point. |
| `.removeobj <entityId>` | GameMaster | Removes an entity from the world for everyone. |
| `.deleteobj <entityId>` | GameMaster | Sends `DestroyPhysicalEntity` to **your own client only**; the object still exists on the server. |
| `.moveobj <entityId> <x> <y> <z> [yawDegrees \| qx qy qz qw]` | GameMaster | Moves a non-actor object for every client on the map. Also works on static map objects, but only for clients on the map now. Actors are refused. |
| `.forcestate <entityId> <stateId\|name>` | GameMaster | Sends `ForceState` for a `UseObjectState`, for example `DoorStateOpen` or `91`, to your own client. |
| `.usable [on\|off] [#entityId\|#target]` | GameMaster | Puts an object in or out of service. The object is the one named, else your target, else the nearest within 10 m. With no argument, shows the object's state. |
| `.placefield <kind\|classId> [a\|b] [hp]` | GameMaster | Places a force field at your feet. `a` is the AFS side and `b` the Bane side. |
| `.placefield kinds \| list` | GameMaster | The kinds of force field, or the fields on this map. |
| `.placefield remove\|repair [#id]`, `side <a\|b> [#id]`, `turn <degrees> [#id]`, `nudge <along> <through> [up] [#id]`, `damage <amount> [#id]` | GameMaster | Edits a field. Without `#id`, it acts on the nearest field within 50 m. |
| `.linkhere <destMapId> <x> <y> <z> [radius] [border\|instance]` | GameMaster | Creates a map link whose trigger is where you stand. The radius defaults to 8 m. |
| `.link <id>` | GameMaster | Shows a link. |
| `.link <id> trigger \| arrival \| radius <r> \| enable \| disable \| comment <text> \| delete` | GameMaster | Edits a link. `trigger` and `arrival` take your current map, position and facing. |
| `.region here <regionId> [radius] [comment]` / `.region box <regionId> <halfX> <halfZ> [comment]` | GameMaster | Creates a region volume at your position: a circle (radius defaults to 50) or a box. |
| `.region <id> here \| radius <r> \| size <hx> <hz> \| y <min> <max> \| underground 0\|1\|2 \| region <regionId> \| enable \| disable \| comment <text> \| delete` | GameMaster | Edits a volume. For `underground`: 0 = any, 1 = underground only, 2 = surface only. |
| `.setregion <regionId> [regionId ...]` / `.setregion off` | GameMaster | Forces the listed region ids on your own client until `off` or a map change. |
| `.emitter here <package> [off] [comment]` | GameMaster | Places an FX emitter. The package is a name or id from `.fxpackages`. |
| `.emitter <id> on \| off \| package <package> \| here \| comment <text> \| delete` | GameMaster | Edits an emitter; `on` and `off` also set its default state. |
| `.kraftwerks` | GameMaster | Crafting stations on this map, nearest first. |
| `.kraftwerks here [comment]` / `.kraftwerks <id> here \| rotate <yaw> \| comment <text> \| delete` | GameMaster | Creates or edits a station. `rotate` takes the yaw in radians. |
| `.cp` / `.cp all` | GameMaster | Control points on this map, or everywhere: who holds each and whether its garrison stands. |
| `.cp <id> afs \| bane` | GameMaster | Gives a control point to a side: the garrisons change over, and its hospital and waypoint open or shut. Kept through a restart. |
| `.cp <id> goto \| here` | GameMaster | Goes to a control point, or stands its object where you are (kept in the world database). |

## Moderation

Commands that act on other players. A GM can only use them on accounts **below their own level**; the console can use them on anyone. Each one is also on the game server console without the dot (see Console: game server). Kicks, silences and lifted silences are logged as `Security`, with who did it and the reason.

| Command | Level | What it does |
|---|---|---|
| `.announce <message>` | GameMaster | Sends `[Announcement] <message>` as a system message to everyone in the world or loading into it. The message may contain spaces. |
| `.kick <familyName> [reason]` | GameMaster | Tells the player "You have been disconnected by a game master" (with the reason, if given), then closes every connection of that account a second later. The character leaves as if the connection had dropped, but never stays behind in a fight. Works at character selection too. |
| `.mute <familyName> <minutes> [reason]` | GameMaster | Silences the account's chat for 1 to 525,600 minutes (one year). Works on accounts that are not online. The player gets the client's own "You are now blocked from sending chat messages for the duration of N minutes", plus the reason if given; you get "User ... is silenced". |
| `.unmute <familyName>` | GameMaster | Lifts a silence. Answers "User ... is not currently silenced" if there is none. |

How a silence works:

- It is saved on the account (`account.muted_until`), so logging out, switching character or restarting the server does not lift it.
- A silenced player cannot say, shout, emote, or use squad, clan, clan leader, channel or whisper/reply chat. Each attempt is answered with "You have been silenced by a game master."
- They can still **whisper a GM** (GameMaster level and above), send petitions, and use dot commands.
- They are reminded when they enter the world, and told "You are no longer silenced" when the time runs out.

## Client messages and presentation

| Command | Level | What it does |
|---|---|---|
| `.msg system\|big\|info\|alert\|destination\|location <playerMessageId> [key value ...]` | GameMaster | Shows a player message or notification on your own screen. |
| `.msg cells <type> <playerMessageId> [key value ...]` | GameMaster | The same notification, sent to everyone near you. |
| `.msg tutorial <tutorialId\|name>` | GameMaster | Shows a tutorial, for example `.msg tutorial Levelup`. |
| `.msg audio <audioSetId>` / `.msg audio stop` | GameMaster | Plays or stops a tutorial voice-over. An unknown id is silent. |
| `.notify timer <type 1-7> <seconds> [countdown 0\|1]` / `.notify stoptimer` | GameMaster | The notification timer on your own screen. |
| `.notify anim\|stopanim <me\|target\|entityId> [animationSpecId]` | GameMaster | Plays or stops an object animation, broadcast from that entity's position. |
| `.notify bgaudio <audioSpecId>` | GameMaster | Background audio on your own client. |
| `.notify locaudio\|stoplocaudio <me\|target\|entityId> [audioSpecId]` | GameMaster | Positional audio, broadcast from the entity's position. |
| `.notify raw <notificationId> [int args ...]` | GameMaster | Any notification, sent to you as-is. |
| `.error fatal\|nonfatal <playerMessageId> [key value ...]` | GameMaster | A modal error dialog on your own client. With `fatal`, pressing OK **closes your client**; it is never sent to anyone else. 15 is `PM_TECHNICAL_DIFFICULTY`. |
| `.destination <contextId\|map name>` | GameMaster | Shows a map's name in the location banner on **every player's** screen. The map name may contain spaces. |

Timer types: 1 TestTimer, 2 TimeTillReinforcements, 3 Countdown, 4 TimeTillAdventureStart, 5 HoldTime, 6 BombCountdownTimer, 7 BossEvacuationTimer.

## Server-wide

| Command | Level | What it does |
|---|---|---|
| `.flag list` | Admin | Server flags currently set, and every known flag. |
| `.flag set <name\|id>` / `.flag clear <name\|id>` | Admin | Changes a flag for everyone now in the world and everyone who logs in later, **until restart**. After a restart, `GameDataConfig.ServerFlags` applies again. |
| `/killmap` | Admin | With no argument, opens the map picker. |
| `/killmap <map>` | Admin | Resets the map on its next tick: its spawn pools' creatures are removed and the pools start over. |

Known server flags: PtsTestGateNpc 1, PtsPvpMap 4, PtsNewCrafting 7, MapEpicGauntlet 8, PtsDisablePalisades 9, MinionCommands 10, TestFlag1 10000001, TestFlag2 10000002.

`/getservercollisiondata` is deliberately **not** registered. The retail client can't load `ServerCollisionData`, and the server has no shapes to send.

## Console: game server

Typed in the game server's window. There is no account check; the console is the authority.

| Command | What it does |
|---|---|
| `gm <familyName> [level]` | Shows or sets an account's GM level; see Access levels. |
| `petition list [open\|resolved\|cancelled\|all] [count]` | Petitions, newest first. The default is open ones. |
| `petition show <id>` | One petition, including its body. |
| `petition resolve <id> [what you did]` | Closes a petition and tells the player if they are online. |
| `flag list \| set <name\|id> \| clear <name\|id>` | The same as `.flag`. |
| `perf` | Main-loop rate, slowest pass, players and connections since the last metrics window. |
| `maperrors [clear]` | Everything the world load and spawn path found wrong with the data. `clear` resets the list; errors come back as the data is used again. |
| `kb` / `kb show <id>` / `kb reload` | The knowledge base: list, one article, or re-read the file. |
| `voice` | Whether voice chat is on, and who is in each squad's group. |
| `reload config` | Re-reads the configuration. |
| `motd` | The message of the day in force, how it is shown (once per change or every login), and its translations. |
| `announce <message>` | The same as `.announce`. |
| `kick <familyName> [reason]` | The same as `.kick`, for any account. |
| `mute <familyName> <minutes> [reason]` / `unmute <familyName>` | The same as `.mute` / `.unmute`, for any account. |
| `exit` | Saves every player the way a logout does, then shuts the server down. |
| `exit <minutes> [reason]` | The same, after a countdown. Minutes may be fractional (`exit 0.5`). Players are warned in chat at the start, at 60, 30, 15 and 10 minutes, each minute from 5, and at 30 and 10 seconds. New connections to the world are refused in the last minute. |
| `exit cancel` | Calls off a countdown and tells the players. |

Stopping the game server any other way (Ctrl+C, stopping the service, `docker stop`) also saves every player first, waiting up to 8 seconds for it.

The message of the day itself is set in `appsettings.json` under `MessageOfTheDay` (`Text`, `ShowEveryLogin`, `Translations`), not by a command. Editing the file sends a changed message to everyone online.

## Console: auth server

| Command | What it does |
|---|---|
| `create <email> <username> <password>` | Creates an account. |
| `ban <username>` / `unban <username>` | Sets or clears `account.locked`. A ban also closes the player's auth connection and tells every connected game server, which kicks the player from the queue or world and refuses a handoff in progress. |
| `reload config` | Re-reads the configuration. |
| `exit [minutes]` | Shuts the auth server down now, or after that many minutes. |
