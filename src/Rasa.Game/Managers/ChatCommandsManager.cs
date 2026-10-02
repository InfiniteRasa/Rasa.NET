using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Navigation;
    using Data;
    using Game;
    using Models;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Repositories.UnitOfWork;
    using Structures;

    public class ChatCommandsManager
    {
        private static ChatCommandsManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly Dictionary<string, ChatCommand> _commands = new();
        private readonly NpcManager _npcManager;

        /// <summary>A registered dot command and the account level it takes to run it.</summary>
        private class ChatCommand
        {
            public ChatCommand(GmLevel level, Action<string[]> handler, params string[] arguments)
            {
                Level = level;
                Handler = handler;
                Arguments = arguments ?? Array.Empty<string>();
            }

            public GmLevel Level { get; }
            public Action<string[]> Handler { get; }
            public string[] Arguments { get; }
        }
        private Client _client { get; set; }
        public static ChatCommandsManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ChatCommandsManager();
                    }
                }

                return _instance;
            }
        }

        private ChatCommandsManager()
            : this(null)
        {
        }

        internal ChatCommandsManager(NpcManager npcManager)
        {
            _npcManager = npcManager;
        }

        /// <summary>
        /// Every dot command comes through here, and this is the only place access is decided.
        /// RadialChat used to check for GM before it would even call this, which meant one level
        /// for all 33 commands; now it hands over anything starting with a dot and the level is
        /// per command.
        /// </summary>
        public void ProcessCommand(Client client, string command)
        {
            _client = client;

            if (string.IsNullOrWhiteSpace(command))
                return;

            var parts = command.Split(' ');

            if (!_commands.TryGetValue(parts[0], out var registered))
            {
                Logger.WriteLog(LogType.Command, $"Invalid command: {command}");
                CommunicatorManager.Instance.SystemMessage(client, $"Unknown command: {parts[0]}");
                return;
            }

            if (!HasLevel(client, registered.Level))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} (level {client.AccountEntry.Level}) tried to use "
                    + $"{parts[0]}, which needs {(byte)registered.Level}");

                // A player is told the same thing they would hear for a command that does not
                // exist: the answer should not be a way to find out what a server can do. Someone
                // who is already a GM gets the real reason, because they are meant to know.
                CommunicatorManager.Instance.SystemMessage(client,
                    client.AccountEntry.Level > 0
                        ? $"{parts[0]} needs account level {(byte)registered.Level}; yours is {client.AccountEntry.Level}."
                        : $"Unknown command: {parts[0]}");
                return;
            }

            registered.Handler(parts);
        }

        internal static bool HasLevel(Client client, GmLevel required)
        {
            return client?.AccountEntry != null && client.AccountEntry.Level >= (byte)required;
        }

        public void RegisterCommand(string name, GmLevel level, Action<string[]> handler, params string[] arguments)
        {
            _commands.Add(name, new ChatCommand(level, handler, arguments));
        }

        private string BuildCommandUsage(string name)
        {
            if (!_commands.TryGetValue(name, out var registered) || registered.Arguments.Length == 0)
                return $"usage: {name}";

            return $"usage: {name} {string.Join(" ", registered.Arguments)}";
        }

        private void SendCommandUsage(string name)
        {
            CommunicatorManager.Instance.SystemMessage(_client, BuildCommandUsage(name));
        }

        public void RemoveCommand(string name)
        {
            if (_commands.ContainsKey(name))
                _commands.Remove(name);
        }

        public void RegisterChatCommands()
        {
            // Observer: reads the world, changes nothing in it.
            RegisterCommand(".getdistance", GmLevel.Observer, GetDistanceCommand);
            RegisterCommand(".maperrors", GmLevel.Observer, MapErrorsCommand);
            RegisterCommand(".missions", GmLevel.Observer, MissionInspectionCommand, "characterId");
            RegisterCommand(".gm", GmLevel.Observer, EnterGmModCommand);
            RegisterCommand(".help", GmLevel.Observer, HelpGmCommand, "command");
            RegisterCommand(".links", GmLevel.Observer, LinksCommand);
            RegisterCommand(".regions", GmLevel.Observer, RegionsCommand);
            RegisterCommand(".emitters", GmLevel.Observer, EmittersCommand);
            RegisterCommand(".fxpackages", GmLevel.Observer, FxPackagesCommand);
            RegisterCommand(".navmesh", GmLevel.Observer, NavMeshCommand, "action", "x", "y", "z");
            RegisterCommand(".near", GmLevel.Observer, NearCommand);
            RegisterCommand(".npcinfo", GmLevel.Observer, NpcInfoCommand);
            RegisterCommand(".rqs", GmLevel.Observer, RqsWindowCommand);
            RegisterCommand(".where", GmLevel.Observer, WhereCommand);
            RegisterCommand(".motd", GmLevel.Observer, MotdCommand);
            RegisterCommand(".cover", GmLevel.Observer, CoverCommand);
            RegisterCommand(".los", GmLevel.Observer, LosCommand, "entityId");
            RegisterCommand(".camerascript", GmLevel.Observer, CameraScriptCommand, "id");
            RegisterCommand(".clientevent", GmLevel.Observer, ClientEventCommand, "action", "event");

            // GameMaster: moves you, spawns and drives scenery and creatures, drives
            // your own client. A restart undoes all of it.
            RegisterCommand(".actorstate", GmLevel.GameMaster, ActorStateCommand, "state", "entityId");
            RegisterCommand(".usermissions", GmMissionCommands.Level,
                parts => GmMissionCommands.ShowUserMissions(_client, string.Join(" ", parts.Skip(1))), "query");
            RegisterCommand(".completeobjective", GmMissionCommands.Level,
                parts => GmMissionCommands.CompleteObjective(_client, parts), "missionId", "objectiveId");
            RegisterCommand(".givemission", GmMissionCommands.Level,
                parts => GmMissionCommands.GiveMission(_client, parts), "missionId");
            RegisterCommand(".track", GmLevel.GameMaster, TrackCommand, "targetId", "entityId");
            RegisterCommand(".vamp", GmLevel.GameMaster, VampCommand, "attribute", "amount", "entityId");
            RegisterCommand(".effect", GmLevel.GameMaster, EffectCommand, "action", "effectId", "entityId");
            RegisterCommand(".moveflags", GmLevel.GameMaster, MoveFlagsCommand);
            RegisterCommand(".falldamage", GmLevel.GameMaster, FallDamageCommand, "metres");
            RegisterCommand(".immune", GmLevel.GameMaster, ImmuneCommand, "damageType");
            RegisterCommand(".allowdeath", GmLevel.GameMaster, AllowDeathCommand, "on|off");
            RegisterCommand(".feud", GmLevel.GameMaster, FeudCommand, "action", "arg1", "arg2");
            RegisterCommand(".bark", GmLevel.GameMaster, BarkCommand, "creatureEntityId", "barkId");
            RegisterCommand(".comehere", GmLevel.GameMaster, ComeHereCommand, "creatureEntityId");
            RegisterCommand(".createobj", GmLevel.GameMaster, CreateObjectCommand, "entityClassId");
            RegisterCommand(".createobjonloc", GmLevel.GameMaster, CreateObjectOnLocationCommand, "entityClassId", "posX", "posY", "posZ", "orientation");
            RegisterCommand(".creature", GmLevel.GameMaster, CreateCreatureCommand, "dbId");
            RegisterCommand(".creatureappearance", GmLevel.GameMaster, SetCreatureAppearanceCommand, "creatureEntityId", "slotId", "classId", "color");
            RegisterCommand(".creatureloc", GmLevel.GameMaster, SetCreatureLocation, "entityClassId", "posX", "posY", "posZ");
            RegisterCommand(".deleteobj", GmLevel.GameMaster, DeleteObjectCommand, "entityId");
            RegisterCommand(".error", GmLevel.GameMaster, ErrorCommand, "kind", "playerMessageId", "keyValuePairs");
            RegisterCommand(".forcestate", GmLevel.GameMaster, ForceStateCommand, "entityId", "stateId");
            RegisterCommand(".heal", GmLevel.GameMaster, HealCommand, "amount", "familyName");
            RegisterCommand(".link", GmLevel.GameMaster, LinkCommand, "id", "action", "value");
            RegisterCommand(".minion", GmLevel.GameMaster, MinionCommand, "creatureDbIdOrAction");
            RegisterCommand(".linkhere", GmLevel.GameMaster, LinkHereCommand, "destMapId", "destX", "destY", "destZ", "radius", "kind");
            RegisterCommand(".kraftwerks", GmLevel.GameMaster, KraftwerksCommand, "stationIdOrHere", "action", "value");
            RegisterCommand(".cp", GmLevel.GameMaster, ControlPointCommand, "id", "action");
            RegisterCommand(".instance", GmLevel.GameMaster, InstanceCommand, "action", "number");
            RegisterCommand(".bg", GmLevel.GameMaster, BattlegroundCommand, "action", "arg1", "arg2");
            RegisterCommand(".region", GmLevel.GameMaster, RegionCommand, "modeOrId", "regionOrAction", "arg1", "arg2", "comment");
            RegisterCommand(".emitter", GmLevel.GameMaster, EmitterCommand, "emitterId", "action", "value");
            RegisterCommand(".notify", GmLevel.GameMaster, NotifyCommand, "action", "arg1", "arg2", "extra");
            RegisterCommand(".msg", GmLevel.GameMaster, MessageCommand, "type", "value", "extra");
            RegisterCommand(".destination", GmLevel.GameMaster, DestinationCommand, "contextIdOrMapName");
            RegisterCommand(".placefield", GmLevel.GameMaster, PlaceFieldCommand, "action", "arg1", "arg2", "arg3");
            RegisterCommand(".removeobj", GmLevel.GameMaster, RemoveObjectCommand, "entityId");
            RegisterCommand(".moveobj", GmLevel.GameMaster, MoveObjectCommand, "entityId", "x", "y", "z", "rotation");
            RegisterCommand(".rename", GmLevel.GameMaster, RenameCommand, "part", "newName", "familyName");
            RegisterCommand(".setkillstreak", GmLevel.GameMaster, SetKillStreakCommand, "streakCount");
            RegisterCommand(".setregion", GmLevel.GameMaster, SetRegionCommand, "regionIdsOrOff");
            RegisterCommand(".speed", GmLevel.GameMaster, SpeedCommand, "value");
            RegisterCommand(".tele", GmLevel.GameMaster, TeleCommand, "posX", "posY", "posZ");
            RegisterCommand(".teleport", GmLevel.GameMaster, TeleportCommand, "posX", "posY", "posZ", "mapId");
            RegisterCommand(".teleup", GmLevel.GameMaster, TeleUpCommand, "posY");
            RegisterCommand(".targetcategory", GmLevel.GameMaster, TargetCategoryCommand, "category");
            RegisterCommand(".blockaction", GmLevel.GameMaster, BlockActionCommand, "actionId", "off");
            RegisterCommand(".usable", GmLevel.GameMaster, UsableCommand, "state", "entityId");
            RegisterCommand(".announce", GmLevel.GameMaster, AnnounceCommand, "message");
            RegisterCommand(".kick", GmLevel.GameMaster, KickCommand, "familyName", "reason");
            RegisterCommand(".mute", GmLevel.GameMaster, MuteCommand, "familyName", "minutes", "reason");
            RegisterCommand(".unmute", GmLevel.GameMaster, UnmuteCommand, "familyName");

            // Admin: hands out progression, changes who a player is, reloads server data.
            // A restart does not undo these.
            RegisterCommand(".addtitle", GmLevel.Admin, AddTitleCommand, "titleId");
            RegisterCommand(".chg_class", GmLevel.Admin, ChangeClassCommand, "className");
            RegisterCommand(".flag", GmLevel.Admin, FlagCommand, "action", "nameOrId");
            RegisterCommand(".givecredits", GmLevel.Admin, GiveCreditsCommand, "amount", "familyName");
            RegisterCommand(".giveitem", GmLevel.Admin, GiveItemCommand, "itemTemplateId", "quantity");
            RegisterCommand(".givelogos", GmLevel.Admin, GiveLogosCommand, "logosId");
            RegisterCommand(".removelogos", GmLevel.Admin, RemoveLogosCommand, "logosIdOrAll");
            RegisterCommand(".givepads", GmLevel.Admin, GivePadsCommand);
            RegisterCommand(".givexp", GmLevel.Admin, GiveXpCommand, "ammount");
            RegisterCommand(".setlevel", GmLevel.Admin, SetLevelCommand, "level");
            RegisterCommand(".failmission", GmLevel.Admin, FailMissionCommand, "missionId");
            RegisterCommand(".failobjective", GmLevel.Admin, FailObjectiveCommand, "missionId", "objectiveId");
            RegisterCommand(".reloadcreatures", GmLevel.Admin, ReloadCreaturesCommand);
        }

        #region RegularUser

        #endregion

        #region GM

        private NpcManager Npcs => _npcManager ?? NpcManager.Instance;

        private void FailMissionCommand(string[] parts)
        {
            if (parts.Length != 2 || !uint.TryParse(parts[1], out var missionId))
            {
                SendCommandUsage(".failmission");
                return;
            }

            if (!Npcs.MissionFailed(_client, missionId))
                CommunicatorManager.Instance.SystemMessage(
                    _client, $"Mission {missionId} is not active.");
        }

        private void FailObjectiveCommand(string[] parts)
        {
            if (parts.Length != 3 ||
                !uint.TryParse(parts[1], out var missionId) ||
                !uint.TryParse(parts[2], out var objectiveId))
            {
                SendCommandUsage(".failobjective");
                return;
            }

            if (!Npcs.ObjectiveFailed(_client, missionId, objectiveId))
                CommunicatorManager.Instance.SystemMessage(
                    _client,
                    $"Mission {missionId} objective {objectiveId} is not active.");
        }

        /// <summary>
        /// .addtitle titleId: gives you the title (titledata id), saved with the character, as a
        /// title item does. The client lists it in the Titles window and prints "you have gained
        /// the title".
        /// </summary>
        private void AddTitleCommand(string[] parts)
        {
            if (parts.Length != 2 || !uint.TryParse(parts[1], out var titleId) || titleId == 0)
            {
                SendCommandUsage(".addtitle");
                return;
            }

            if (!ManifestationManager.Instance.GrantTitle(_client, titleId))
                CommunicatorManager.Instance.SystemMessage(_client, $"You already have title {titleId}, or it could not be saved.");
        }

        /// <summary>
        /// .actorstate &lt;state&gt; [state ...] [#entityId]: a StateCorrection on yourself, or on the
        /// creature or player the #entityId names, to everyone who can see it
        /// (ActorManager.CorrectState). States by the client's name (standing, sitting,
        /// lying_down, flailing, crouched, dead, stunned, combat_engaged ...) or id. What it looks
        /// like, not what it is: see CorrectState. .speed sets the movement speed this used to.
        /// </summary>
        private void ActorStateCommand(string[] parts)
        {
            var idPart = parts.Skip(1).FirstOrDefault(p => p.StartsWith("#"));
            var stateParts = parts.Skip(1).Where(p => p != idPart).ToList();

            if (stateParts.Count == 0)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .actorstate <state> [state ...] [#entityId] - states by name or id: "
                    + string.Join(", ", Enum.GetNames(typeof(CharacterState)).Select(n => n.ToLowerInvariant())));
                return;
            }

            var states = new List<CharacterState>();

            foreach (var part in stateParts)
            {
                if (!ActorManager.TryParseState(part, out var state))
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"No character state '{part}'.");
                    return;
                }

                states.Add(state);
            }

            Actor actor = _client.Player;

            if (idPart != null)
            {
                actor = ulong.TryParse(idPart.Substring(1), out var entityId)
                    ? EntityManager.Instance.GetEntityType(entityId) switch
                    {
                        EntityType.Creature => EntityManager.Instance.GetCreature(entityId),
                        EntityType.Character => EntityManager.Instance.GetPlayer(entityId),
                        _ => null
                    }
                    : null;

                if (actor == null || actor.MapContextId != _client.Player.MapContextId)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"No creature or player {idPart} on this map.");
                    return;
                }
            }

            ActorManager.CorrectState(_client.Player.MapChannel, actor, states);
            CommunicatorManager.Instance.SystemMessage(_client, $"{actor.EntityId}: {string.Join(", ", states)}.");
        }

        /// <summary>
        /// .track &lt;targetId|me|target|0&gt; [#entityId|#target]: sets the tracking target
        /// (Recv_SetTrackingTarget) of yourself, or of the creature or player named, for everyone
        /// who can see it - what a player's client does on its own body while following or
        /// walking up to something. The client never shows what the engine does with a tracked
        /// body; this is for watching. "me" is you, "target" what you have selected, 0 clears.
        /// With nothing after it, says what you and your target are tracking.
        /// </summary>
        private void TrackCommand(string[] parts)
        {
            var player = _client.Player;
            var mapChannel = player.MapChannel;

            Actor ActorById(ulong entityId) => EntityManager.Instance.GetEntityType(entityId) switch
            {
                EntityType.Creature => EntityManager.Instance.GetCreature(entityId),
                EntityType.Character => EntityManager.Instance.GetPlayer(entityId),
                _ => null
            };

            string Describe(Actor a) => a == null ? "-" : $"{a.EntityId} ({(string.IsNullOrEmpty(a.Name) ? a.EntityClass.ToString() : a.Name)})";

            if (parts.Length < 2)
            {
                var selected = player.Target != 0 ? ActorById(player.Target) : null;
                CommunicatorManager.Instance.SystemMessage(_client,
                    "usage: .track <targetId|me|target|0> [#entityId|#target] - "
                    + $"you track {TrackingTargets.Current(player)}"
                    + (selected != null ? $", your target {Describe(selected)} tracks {TrackingTargets.Current(selected)}" : ""));
                return;
            }

            var idPart = parts.Skip(1).FirstOrDefault(p => p.StartsWith("#"));
            var targetPart = parts.Skip(1).FirstOrDefault(p => !p.StartsWith("#"));

            Actor actor = player;

            if (idPart != null)
            {
                actor = idPart.Equals("#target", StringComparison.OrdinalIgnoreCase)
                    ? (player.Target != 0 ? ActorById(player.Target) : null)
                    : ulong.TryParse(idPart.Substring(1), out var entityId) ? ActorById(entityId) : null;

                if (actor == null || actor.MapContextId != player.MapContextId)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"No creature or player {idPart} on this map.");
                    return;
                }
            }

            ulong targetId;

            if (targetPart == null)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "Track what? A target id, me, target, or 0 to clear.");
                return;
            }
            else if (targetPart.Equals("me", StringComparison.OrdinalIgnoreCase))
                targetId = player.EntityId;
            else if (targetPart.Equals("target", StringComparison.OrdinalIgnoreCase))
                targetId = player.Target;
            else if (!ulong.TryParse(targetPart, out targetId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"'{targetPart}' is not an entity id, me, target or 0.");
                return;
            }

            if (!TrackingTargets.IsValid(actor, targetId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, targetId == actor.EntityId
                    ? "An actor cannot track itself."
                    : $"No entity {targetId} here.");
                return;
            }

            TrackingTargets.Set(mapChannel, actor, targetId);
            CommunicatorManager.Instance.SystemMessage(_client, targetId == 0
                ? $"{Describe(actor)} tracks nothing."
                : $"{Describe(actor)} tracks {targetId}.");
        }

        /// <summary>
        /// .vamp &lt;health|power|armor|adrenaline&gt; &lt;amount&gt; [#entityId]: you steal up to the
        /// amount from your target, or the creature or player named (VampiricDamage.Steal) - what
        /// the Vamp item modules will do on a hit, with the client's AnnounceVamp floats.
        /// </summary>
        private void VampCommand(string[] parts)
        {
            var player = _client.Player;
            var idPart = parts.Skip(1).FirstOrDefault(p => p.StartsWith("#"));
            var args = parts.Skip(1).Where(p => p != idPart).ToList();

            Attributes? attribute = args.Count > 0 ? args[0].ToLowerInvariant() switch
            {
                "health" => Attributes.Health,
                "power" => Attributes.Power,
                "armor" or "armour" => Attributes.Armor,
                "adrenaline" or "chi" => Attributes.Chi,
                _ => null
            } : null;

            if (attribute == null || args.Count < 2 || !int.TryParse(args[1], out var amount) || amount <= 0)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .vamp <health|power|armor|adrenaline> <amount> [#entityId] - from your target, or the one named");
                return;
            }

            var victimId = idPart != null && ulong.TryParse(idPart.Substring(1), out var named) ? named : player.Target;
            var victim = EntityManager.Instance.GetEntityType(victimId) switch
            {
                EntityType.Creature => (Actor)EntityManager.Instance.GetCreature(victimId),
                EntityType.Character => EntityManager.Instance.GetPlayer(victimId),
                _ => null
            };

            if (victim == null || victim == player || victim.MapContextId != player.MapContextId)
            {
                CommunicatorManager.Instance.SystemMessage(_client, idPart != null ? $"No creature or player {idPart} on this map." : "Target a creature or another player first.");
                return;
            }

            var lost = VampiricDamage.Steal(player.MapChannel, player, victim, attribute.Value, amount);

            CommunicatorManager.Instance.SystemMessage(_client, lost > 0
                ? $"Stole {lost} {args[0].ToLowerInvariant()} from {victim.EntityId}."
                : $"Nothing to steal from {victim.EntityId}: it has none, is down, or turned the effect away.");
        }

        /// <summary>
        /// .effect [list] [#entityId|#target]: the effects on you, or on the creature or player
        /// named - id, type, level, buff or debuff, who put it there, time left, paused or not.
        /// .effect pause|restart &lt;effectId|all&gt; [#entityId|#target]: stops or starts an
        /// effect's clock (GameEffectManager.Pause / Restart), with the client's "Paused" tooltip.
        /// </summary>
        private void EffectCommand(string[] parts)
        {
            var player = _client.Player;
            var mapChannel = player.MapChannel;
            var idPart = parts.Skip(1).FirstOrDefault(p => p.StartsWith("#"));
            var args = parts.Skip(1).Where(p => p != idPart).ToList();
            const string usage = "usage: .effect [list] [#entityId|#target] | .effect pause|restart <effectId|all> [#entityId|#target]";

            Actor actor = player;

            if (idPart != null)
            {
                var entityId = idPart.Equals("#target", StringComparison.OrdinalIgnoreCase)
                    ? player.Target
                    : ulong.TryParse(idPart.Substring(1), out var named) ? named : 0;

                actor = EntityManager.Instance.GetEntityType(entityId) switch
                {
                    EntityType.Creature => EntityManager.Instance.GetCreature(entityId),
                    EntityType.Character => EntityManager.Instance.GetPlayer(entityId),
                    _ => null
                };

                if (actor == null || actor.MapContextId != player.MapContextId)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"No creature or player {idPart} on this map.");
                    return;
                }
            }

            var verb = args.Count > 0 ? args[0].ToLowerInvariant() : "list";

            if (verb == "list")
            {
                var effects = actor.ActiveEffects.Values.OrderBy(e => e.EffectId).ToList();

                CommunicatorManager.Instance.SystemMessage(_client, $"{actor.EntityId}: {effects.Count} effect(s)");

                foreach (var e in effects)
                    CommunicatorManager.Instance.SystemMessage(_client,
                        $"  #{e.EffectId} type {e.TypeId} L{e.EffectLevel} {(e.IsBuff ? "buff" : "debuff")} from {e.SourceId}"
                        + (e.HasDuration ? $", {e.RemainingMs / 1000.0:0.0} s left" : ", no end")
                        + (e.IsPaused ? ", PAUSED" : "")
                        + (e.ServerOnly ? ", server only" : e.IsSkillPassive ? ", skill passive" : "")
                        + (actor is Manifestation holder && EffectCarry.Carries(holder, e) ? ", carried across maps" : ""));

                return;
            }

            if ((verb != "pause" && verb != "restart") || args.Count < 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, usage);
                return;
            }

            var all = args[1].Equals("all", StringComparison.OrdinalIgnoreCase);
            var chosen = all
                ? actor.ActiveEffects.Values.Where(e => e.Parent?.Holder != actor).OrderBy(e => e.EffectId).ToList()   // an aura of its own takes its copies with it
                : int.TryParse(args[1], out var effectId) && actor.ActiveEffects.TryGetValue(effectId, out var one)
                    ? new List<GameEffect> { one }
                    : null;

            if (chosen == null)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"No effect {args[1]} on {actor.EntityId}; .effect list{(idPart != null ? " " + idPart : "")} shows them.");
                return;
            }

            var changed = chosen.Count(e => verb == "pause"
                ? GameEffectManager.Instance.Pause(mapChannel, actor, e)
                : GameEffectManager.Instance.Restart(mapChannel, actor, e));

            CommunicatorManager.Instance.SystemMessage(_client, verb == "pause"
                ? $"Paused {changed} effect(s) on {actor.EntityId}{(changed < chosen.Count ? $"; {chosen.Count - changed} already paused" : "")}."
                : $"Restarted {changed} effect(s) on {actor.EntityId}{(changed < chosen.Count ? $"; {chosen.Count - changed} not paused" : "")}.");
        }

        /// <summary>
        /// .moveflags: shows your Move packets' flags byte and leading bytes whenever they change,
        /// with your height, the step, and whether you are in one of the map's water planes. Run
        /// it again to stop. For finding out whether the client marks being in the air or swimming.
        /// </summary>
        private void MoveFlagsCommand(string[] parts)
        {
            var tracker = _client.Player.Fall;

            tracker.WatchFlags = !tracker.WatchFlags;
            tracker.LastFlags = -1;

            CommunicatorManager.Instance.SystemMessage(_client, tracker.WatchFlags
                ? "Watching your move flags: jump, fall and swim, and each change is shown. .moveflags again to stop."
                : "Stopped watching your move flags.");
        }

        /// <summary>
        /// .feud: clan feuds (ClanFeuds).
        ///  - .feud - the feuds running, with time left and score, and the challenges waiting;
        ///  - .feud start &lt;clan&gt; &lt;clan&gt; - starts one between two clans, by id or by a name with no
        ///    spaces, skipping every rule (PvP, leaders online) - for testing with few players;
        ///  - .feud end &lt;id&gt; [tie|cancel|&lt;winning clan&gt;] - ends one, as its clock would (most kills
        ///    wins) unless told otherwise;
        ///  - .feud length [minutes] - how long a feud started from now lasts.
        /// </summary>
        private void FeudCommand(string[] parts)
        {
            var feuds = ClanFeuds.Instance;
            var sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : "list";

            Structures.Char.ClanEntry Clan(string text) =>
                uint.TryParse(text, out var id) ? ClanManager.Instance.Clans.GetValueOrDefault(id)?.Value
                    : ClanManager.Instance.Clans.Values.Select(c => c.Value).FirstOrDefault(c => c != null && string.Equals(c.Name, text, StringComparison.OrdinalIgnoreCase));

            string Name(uint clanId) => $"{ClanManager.Instance.Clans.GetValueOrDefault(clanId)?.Value?.Name ?? "?"} ({clanId})";

            switch (sub)
            {
                case "list":
                {
                    var running = feuds.Feuds;
                    var waiting = feuds.Challenges;

                    if (running.Count == 0 && waiting.Count == 0)
                    {
                        CommunicatorManager.Instance.SystemMessage(_client, $"No clan feuds or challenges. New feuds last {feuds.Duration.TotalMinutes:0} minutes.");
                        return;
                    }

                    foreach (var feud in running.OrderBy(f => f.Id))
                        CommunicatorManager.Instance.SystemMessage(_client,
                            $"Feud {feud.Id}: {Name(feud.ChallengerClanId)} {feud.ChallengerKills} : {feud.TargetKills} {Name(feud.TargetClanId)}, {feuds.SecondsLeft(feud) / 60}m {feuds.SecondsLeft(feud) % 60}s left");

                    foreach (var challenge in waiting.OrderBy(c => c.WargameId))
                        CommunicatorManager.Instance.SystemMessage(_client,
                            $"Challenge {challenge.WargameId}: {Name(challenge.ChallengerClanId)} challenged {Name(challenge.TargetClanId)}, unanswered");

                    return;
                }

                case "start" when parts.Length >= 4:
                {
                    var first = Clan(parts[2]);
                    var second = Clan(parts[3]);

                    if (first == null || second == null || first.Id == second.Id)
                    {
                        CommunicatorManager.Instance.SystemMessage(_client, "usage: .feud start <clan> <clan> - two different clans, by id or by a name with no spaces");
                        return;
                    }

                    var feud = feuds.Start(first, second);

                    CommunicatorManager.Instance.SystemMessage(_client, feud == null
                        ? $"{first.Name} and {second.Name} are already at feud."
                        : $"Feud {feud.Id} started: {first.Name} against {second.Name}, {feuds.Duration.TotalMinutes:0} minutes.");
                    return;
                }

                case "end" when parts.Length >= 3 && uint.TryParse(parts[2], out var feudId):
                {
                    var feud = feuds.Feuds.Find(f => f.Id == feudId);

                    if (feud == null)
                    {
                        CommunicatorManager.Instance.SystemMessage(_client, $"No feud {feudId}.");
                        return;
                    }

                    var how = parts.Length >= 4 ? parts[3] : null;

                    if (how == null)
                        feuds.Expire(feud);
                    else if (how.Equals("tie", StringComparison.OrdinalIgnoreCase))
                        feuds.End(feud, ClanFeuds.Outcome.Tied);
                    else if (how.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                        feuds.End(feud, ClanFeuds.Outcome.Cancelled);
                    else if (Clan(how) is Structures.Char.ClanEntry winner && feud.Involves(winner.Id))
                        feuds.End(feud, ClanFeuds.Outcome.Won, winner.Id);
                    else
                    {
                        CommunicatorManager.Instance.SystemMessage(_client, "usage: .feud end <id> [tie|cancel|<winning clan>]");
                        return;
                    }

                    CommunicatorManager.Instance.SystemMessage(_client, $"Feud {feudId} ended.");
                    return;
                }

                case "length":
                {
                    if (parts.Length >= 3)
                    {
                        if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var minutes) || minutes <= 0 || minutes > 7 * 24 * 60)
                        {
                            CommunicatorManager.Instance.SystemMessage(_client, "usage: .feud length <minutes> - more than 0, at most a week");
                            return;
                        }

                        feuds.Duration = TimeSpan.FromMinutes(minutes);
                    }

                    CommunicatorManager.Instance.SystemMessage(_client, $"Clan feuds started from now last {feuds.Duration.TotalMinutes:0.#} minutes (default {ClanFeuds.DefaultDuration.TotalMinutes:0}).");
                    return;
                }

                default:
                    CommunicatorManager.Instance.SystemMessage(_client, "usage: .feud [list] | start <clan> <clan> | end <id> [tie|cancel|<winning clan>] | length [minutes]");
                    return;
            }
        }

        /// <summary>
        /// .falldamage &lt;metres&gt;: takes what a fall of that height would (FallDamage), straight off
        /// your health, announced as map damage - to see what the client shows. Says whether you
        /// are standing in water, where a real fall would have done nothing.
        /// </summary>
        private void FallDamageCommand(string[] parts)
        {
            var player = _client.Player;

            if (parts.Length < 2 || !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var metres) || metres <= 0)
            {
                CommunicatorManager.Instance.SystemMessage(_client,
                    $"usage: .falldamage <metres> - a fall does {FallDamage.PercentPerMetre}% of maximum health a metre past {FallDamage.SafeDrop:0} m");
                return;
            }

            var water = FallDamage.InWater(player.MapChannel?.MapInfo?.MapName, player.Position);
            var taken = FallDamage.Apply(player.MapChannel, player, metres);

            CommunicatorManager.Instance.SystemMessage(_client,
                $"A {metres:0.#} m fall: {taken} health taken{(water ? "; you are in water, where a real fall would have taken nothing" : "")}.");
        }

        private void BarkCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".bark");
                return;
            }

            if (parts.Length == 3)
                if (ulong.TryParse(parts[1], out var creatureEntityId))
                    if (uint.TryParse(parts[2], out var barkId))
                        _client.CallMethod(creatureEntityId, new BarkPackage(barkId));
        }

        /// <summary>
        /// Fires a Notification at yourself, or at everyone who can see an entity for the ones
        /// that are about a place. The ids come from the client's own tables: timer types from
        /// generated/client/timertype.py, animation ids from animationdata.py
        /// objectAnimationSpecification, audio ids from audiodata.py audioSpecification.
        /// </summary>
        /// <summary>
        /// .error fatal|nonfatal &lt;playerMessageId&gt; [key value ...]
        ///
        /// Both put a modal dialog on screen built from that player message id. fatal is the one
        /// whose OK button quits the client, so it disconnects whoever it is aimed at - it is sent
        /// to the caller only, deliberately: there is no form of this command that can boot another
        /// player, because the id is unvalidated and a typo should not cost someone their session.
        /// </summary>
        private void ErrorCommand(string[] parts)
        {
            var kind = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

            if (parts.Length < 3 || kind != "fatal" && kind != "nonfatal" || !uint.TryParse(parts[2], out var msgId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .error fatal|nonfatal <playerMessageId> [key value ...]");
                CommunicatorManager.Instance.SystemMessage(_client, "fatal closes your own client when you press OK. 15 is PM_TECHNICAL_DIFFICULTY.");
                return;
            }

            var args = new Dictionary<string, string>();

            for (var i = 3; i + 1 < parts.Length; i += 2)
                args[parts[i]] = parts[i + 1];

            if (kind == "fatal")
                CommunicatorManager.Instance.FatalError(_client, (PlayerMessage)msgId, args);
            else
                CommunicatorManager.Instance.NonFatalError(_client, (PlayerMessage)msgId, args);
        }

        /// <summary>
        /// .msg system|big|info|alert|destination|location &lt;playerMessageId&gt; [key value ...]
        /// .msg tutorial &lt;tutorialId|name&gt;
        /// .msg audio &lt;audioSetId&gt; | .msg audio stop
        /// .msg cells &lt;type&gt; &lt;playerMessageId&gt; [key value ...]
        ///
        /// Drives the three player-message methods so the plumbing can be seen working; nothing
        /// in the game sends them yet. Targets the caller, except `cells`, which is how a region
        /// announcement would reach everyone nearby.
        /// </summary>
        /// <summary>
        /// .flag list | .flag set &lt;name|id&gt; | .flag clear &lt;name|id&gt;
        ///
        /// Admin rather than GameMaster: this is the whole server, not one player. It lasts until
        /// the server restarts, when GameDataConfig.ServerFlags takes over again.
        /// </summary>
        /// <summary>
        /// .maperrors - what is wrong with the data for the map the caller is standing in.
        ///
        /// The same dialog a GM gets on entering a broken map, on demand. Observer level: it
        /// reads the world and changes nothing in it.
        /// </summary>
        /// <summary>
        /// .heal [full|&lt;amount&gt;] [familyName] - put health back, on yourself or on someone else.
        ///
        /// Exercises ActorManager.Heal, which is the one path health goes up by. No source
        /// entity is passed, so the client announces the change itself rather than waiting for
        /// an ability that is never coming.
        /// </summary>
        /// <summary>
        /// .givecredits &lt;amount&gt; [familyName] - credits on or off a character.
        ///
        /// Admin, alongside .giveitem and .givexp: this makes money out of nothing, which is the
        /// one thing the rest of the economy work has been about stopping. A negative amount
        /// takes credits away, clamped at zero rather than allowed to run a character negative -
        /// nothing in the game reads a balance as signed.
        /// </summary>
        private void GiveCreditsCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;
            var target = _client;

            if (parts.Length > 2)
            {
                target = Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null
                                                  && string.Equals(c.Player.FamilyName, parts[2], StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    communicator.SystemMessage(_client, $"{parts[2]} is not in the world.");
                    return;
                }
            }

            if (parts.Length < 2 || !int.TryParse(parts[1], out var amount) || amount == 0)
            {
                communicator.SystemMessage(_client, "usage: .givecredits <amount> [familyName]");
                communicator.SystemMessage(_client, "A negative amount takes credits away.");
                return;
            }

            var before = target.Player.Credits[CurencyType.Credits];

            // Clamped, so taking more than they have empties the purse rather than owing.
            if (amount < 0)
                amount = -Math.Min(before, Math.Abs(amount));

            if (amount == 0)
            {
                communicator.SystemMessage(_client, $"{target.Player.FamilyName} has no credits to take.");
                return;
            }

            // The command takes a signed amount; the two primitives do not. Positive is a gain,
            // negative is a charge of that size, already clamped to what they have above.
            var changed = amount > 0
                ? ManifestationManager.Instance.GainCredits(target, amount)
                : ManifestationManager.Instance.LossCredits(target, -amount);

            if (!changed)
            {
                communicator.SystemMessage(_client,
                    $"Could not persist the credit change for {target.Player.FamilyName}.");
                return;
            }

            var after = target.Player.Credits[CurencyType.Credits];
            var who = target == _client ? "You" : target.Player.FamilyName;

            communicator.SystemMessage(_client,
                $"{who}: {before} -> {after} credits ({(amount > 0 ? "+" : "")}{amount}).");

            if (target != _client)
                communicator.SystemMessage(target,
                    $"A GM has {(amount > 0 ? "given you" : "taken")} {Math.Abs(amount)} credits. You now have {after}.");
        }

        private void HealCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;
            var target = _client;

            if (parts.Length > 2)
            {
                target = Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null
                                                  && string.Equals(c.Player.FamilyName, parts[2], StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    communicator.SystemMessage(_client, $"{parts[2]} is not in the world.");
                    return;
                }
            }

            var toFull = parts.Length < 2 || parts[1].ToLowerInvariant() == "full";
            var requested = 0;

            if (!toFull && (!int.TryParse(parts[1], out requested) || requested <= 0))
            {
                communicator.SystemMessage(_client, "usage: .heal [full|<amount>] [familyName]");
                return;
            }

            var applied = toFull
                ? ActorManager.Instance.HealToFull(target.Player)
                : ActorManager.Instance.Heal(target.Player, requested);

            var health = target.Player.Attributes.TryGetValue(Attributes.Health, out var h) ? h : null;
            var who = target == _client ? "You are" : $"{target.Player.FamilyName} is";

            if (applied == 0)
            {
                communicator.SystemMessage(_client,
                    health == null ? "That actor has no health to put back."
                    : health.Current <= 0 || target.Player.State == CharacterState.Dead
                        ? $"{who} dead - healing will not bring them back."
                        : $"{who} already at full health.");
                return;
            }

            communicator.SystemMessage(_client,
                $"{who} healed for {applied} ({health?.Current} of {health?.CurrentMax}).");
        }

        private void MissionInspectionCommand(string[] parts)
        {
            var characterId = _client.Player.Id;
            if (parts.Length > 1 && !uint.TryParse(parts[1], out characterId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "Usage: .missions [character-id]");
                return;
            }
            foreach (var line in MissionApplication.Instance.Inspect(characterId))
                CommunicatorManager.Instance.SystemMessage(_client, line);
        }

        private void MapErrorsCommand(string[] parts)
        {
            if (MapErrorManager.Instance.SendTo(_client))
                return;

            CommunicatorManager.Instance.SystemMessage(_client,
                $"Nothing recorded against map {_client.Player?.MapContextId.ToString() ?? "?"}.");
        }

        private void FlagCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;
            var flags = ServerFlagManager.Instance;
            var action = parts.Length > 1 ? parts[1].ToLowerInvariant() : "list";

            if (action == "list")
            {
                var set = flags.Flags;

                communicator.SystemMessage(_client,
                    set.Count == 0
                        ? "No server flags are set."
                        : "Set: " + string.Join(", ", set.Select(f => $"{f} ({(uint)f})")));

                communicator.SystemMessage(_client, "Known: " + ServerFlagManager.KnownFlags());
                return;
            }

            if (action != "set" && action != "clear")
            {
                communicator.SystemMessage(_client, "usage: .flag list | .flag set <name|id> | .flag clear <name|id>");
                return;
            }

            if (parts.Length < 3 || !ServerFlagManager.TryParse(parts[2], out var flag))
            {
                communicator.SystemMessage(_client, "usage: .flag list | .flag set <name|id> | .flag clear <name|id>");
                communicator.SystemMessage(_client, "known flags: " + ServerFlagManager.KnownFlags());
                return;
            }

            if (action == "set")
            {
                communicator.SystemMessage(_client,
                    flags.Set(flag) ? $"{flag} is now set for everyone." : $"{flag} was already set.");
                return;
            }

            communicator.SystemMessage(_client,
                flags.Clear(flag) ? $"{flag} is now clear for everyone." : $"{flag} was already clear.");
        }

        /// <summary>.motd: the message of the day as players get it, shown to you whatever your client has seen.</summary>
        private void MotdCommand(string[] parts)
        {
            if (!MessageOfTheDay.Preview(_client))
                CommunicatorManager.Instance.SystemMessage(_client, "There is no message of the day (MessageOfTheDay.Text in appsettings.json is empty).");
        }

        /// <summary>.announce message: a line in chat for everyone in the world (Moderation).</summary>
        private void AnnounceCommand(string[] parts)
        {
            var text = string.Join(" ", parts.Skip(1)).Trim();

            if (text.Length == 0)
            {
                SendCommandUsage(".announce");
                return;
            }

            Logger.WriteLog(LogType.Command, $"{_client.AccountEntry?.FamilyName} announced: {text}");
            Moderation.Announce(text);
        }

        /// <summary>.kick familyName [reason]: disconnects that player after telling them why.</summary>
        private void KickCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                SendCommandUsage(".kick");
                return;
            }

            var result = Moderation.Kick(parts[1], string.Join(" ", parts.Skip(2)), _client);
            CommunicatorManager.Instance.SystemMessage(_client, result.Text);
        }

        /// <summary>.mute familyName minutes [reason]: silences that account's chat, online or not.</summary>
        private void MuteCommand(string[] parts)
        {
            if (parts.Length < 3 || !int.TryParse(parts[2], out var minutes))
            {
                SendCommandUsage(".mute");
                return;
            }

            var result = Moderation.Mute(parts[1], minutes, string.Join(" ", parts.Skip(3)), _client);

            // A silence that went on is confirmed in the client's own words (PM_GM_USER_SILENCED).
            if (!result.Told)
                CommunicatorManager.Instance.SystemMessage(_client, result.Text);
        }

        /// <summary>.unmute familyName: lifts a silence.</summary>
        private void UnmuteCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                SendCommandUsage(".unmute");
                return;
            }

            var result = Moderation.Unmute(parts[1], _client);

            // "Lifted" and "was not silenced" are told in the client's own words.
            if (!result.Told)
                CommunicatorManager.Instance.SystemMessage(_client, result.Text);
        }

        private void MessageCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;
            var kind = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

            if (kind == "tutorial")
            {
                if (parts.Length < 3 || !TryParseTutorial(parts[2], out var tutorial))
                {
                    communicator.SystemMessage(_client, "usage: .msg tutorial <tutorialId|name>");
                    communicator.SystemMessage(_client, "e.g. .msg tutorial Levelup, or .msg tutorial 10000002");
                    return;
                }

                communicator.DisplayPlayerTutorial(_client, tutorial);
                return;
            }

            if (kind == "audio")
            {
                if (parts.Length > 2 && parts[2].ToLowerInvariant() == "stop")
                {
                    communicator.StopTutorialAudio(_client);
                    communicator.SystemMessage(_client, "Stopped the tutorial voice-over.");
                    return;
                }

                if (parts.Length < 3 || !uint.TryParse(parts[2], out var audioSetId))
                {
                    communicator.SystemMessage(_client, "usage: .msg audio <audioSetId> | .msg audio stop");
                    communicator.SystemMessage(_client, "Audio set ids are the client's own, from generated.client.audiosetdata.");
                    return;
                }

                communicator.PlayTutorialAudio(_client, audioSetId);

                // No answer comes back and an id the client does not know is simply silence, so
                // say what was sent rather than leaving a silent result looking like a failure.
                communicator.SystemMessage(_client, $"Sent audio set {audioSetId}. Silence means the client has no such set.");
                return;
            }

            var toCells = kind == "cells";
            var typeFrom = toCells ? 2 : 1;
            var idFrom = toCells ? 3 : 2;

            if (parts.Length <= idFrom || !uint.TryParse(parts[idFrom], out var msgId))
            {
                communicator.SystemMessage(_client, "usage: .msg system|big|info|alert|destination|location <playerMessageId> [key value ...]");
                communicator.SystemMessage(_client, "       .msg tutorial <tutorialId|name>");
                communicator.SystemMessage(_client, "       .msg audio <audioSetId> | .msg audio stop");
                communicator.SystemMessage(_client, "       .msg cells <type> <playerMessageId> [key value ...]");
                return;
            }

            var args = new Dictionary<string, string>();

            for (var i = idFrom + 1; i + 1 < parts.Length; i += 2)
                args[parts[i]] = parts[i + 1];

            var typeName = parts[typeFrom].ToLowerInvariant();

            // "system" is the one that is not a notification type: it goes through
            // DisplaySystemMessage, where the client decides for itself what the message is for.
            if (!toCells && typeName == "system")
            {
                communicator.DisplaySystemMessage(_client, (PlayerMessage)msgId, args);
                return;
            }

            if (!TryParseNotificationType(typeName, out var type))
            {
                communicator.SystemMessage(_client,
                    "type must be system, or one of: " + string.Join(", ", Enum.GetNames(typeof(PlayerNotificationType))).ToLowerInvariant());
                return;
            }

            if (toCells)
                communicator.NotifyCells(_client, type, (PlayerMessage)msgId, args);
            else
                communicator.DisplayPlayerNotification(_client, type, (PlayerMessage)msgId, args);
        }

        /// <summary>
        /// .destination &lt;contextId|map name&gt;: DisplayDestinationContextNotification to every
        /// player in the world - the map's name on the sub-region strip of their screens. A number
        /// is sent as it is, any of the client's game contexts; a name is looked up among the
        /// loaded maps as /gotomap looks it up.
        /// </summary>
        private void DestinationCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;

            if (parts.Length < 2)
            {
                communicator.SystemMessage(_client, "usage: .destination <contextId|map name> - shows that map's name to every player in the world");
                return;
            }

            uint contextId;
            string label;

            if (uint.TryParse(parts[1], out contextId) && contextId != 0)
                label = MapChannelManager.Instance.MapChannelArray.TryGetValue(contextId, out var known) ? known.MapInfo.MapName : $"context {contextId}";
            else
            {
                var token = string.Join(" ", parts.Skip(1));
                var matching = GmMapCommands.MapsMatching(GmMapCommands.Maps(MapChannelManager.Instance.MapChannelArray.Values), token);

                if (matching.Count != 1)
                {
                    communicator.SystemMessage(_client, matching.Count == 0
                        ? $"No loaded map '{token}'; a context id sends any map's name."
                        : $"'{token}' is in more than one map's name: {string.Join(", ", matching.Take(8).Select(m => $"{m.MapInfo.MapName} ({m.MapInfo.MapContextId})"))}.");
                    return;
                }

                contextId = matching[0].MapInfo.MapContextId;
                label = matching[0].MapInfo.MapName;
            }

            List<Client> recipients;

            lock (Server.Clients)
                recipients = Server.Clients.Where(c => c.State == ClientState.Ingame && c.Player != null).ToList();

            foreach (var recipient in recipients)
                recipient.CallMethod(SysEntity.ClientMethodId, new Packets.ClientMethod.Server.DisplayDestinationContextNotificationPacket(contextId));

            Logger.WriteLog(LogType.Command, $"AccountId = {_client.AccountEntry?.Id}: .destination {contextId} to {recipients.Count} player(s)");
            communicator.SystemMessage(_client, $"Showed {label} ({contextId}) to {recipients.Count} player(s) in the world.");
        }

        /// <summary>How far .placefield looks for the nearest field when no id is given.</summary>
        private const float NearestFieldRange = 50f;

        /// <summary>
        /// .placefield: force fields (ForceFields), placed by hand to see what they did.
        ///   .placefield &lt;kind|classId&gt; [a|b] [hp]   - at your feet, facing the way you face
        ///   .placefield kinds | list
        ///   .placefield remove|repair [#id]
        ///   .placefield side &lt;a|b&gt; [#id]
        ///   .placefield turn &lt;degrees&gt; [#id]
        ///   .placefield nudge &lt;along&gt; &lt;through&gt; [up] [#id]   - metres, in the field's own frame
        ///   .placefield damage &lt;amount&gt; [#id]
        /// Without a #id, the nearest field within NearestFieldRange.
        /// </summary>
        private void PlaceFieldCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;
            var player = _client.Player;
            var mapChannel = player.MapChannel;
            var sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

            void Say(string text) => communicator.SystemMessage(_client, text);

            string Describe(ForceFields.Field f) =>
                $"#{f.Id} {f.Class.Key} ({f.Class.ClassId}) side {f.Side}, {f.Health}/{f.MaxHealth} hp, {ForceFields.StateOf(f)}, "
                + $"{Vector3.Distance(f.Position, player.Position):0.0} m away, yaw {f.Yaw * 180 / Math.PI:0}"
                + $", {(ForceFields.StopsPlayer(f) ? "stops players" : "lets players through")}";

            // The field a "#id" names, or the nearest one when there is no #id.
            var idPart = parts.Skip(2).FirstOrDefault(p => p.StartsWith("#"));

            parts = parts.Where(p => p != idPart).ToArray();

            ForceFields.Field Target()
            {
                if (idPart != null)
                {
                    if (!int.TryParse(idPart.Substring(1), out var id))
                    {
                        Say($"{idPart} is not a field id; .placefield list shows them.");
                        return null;
                    }

                    var byId = ForceFields.FindById(id);

                    if (byId == null || byId.MapChannel != mapChannel)
                        Say($"No force field #{id} on this map.");

                    return byId?.MapChannel == mapChannel ? byId : null;
                }

                var nearest = ForceFields.OnMap(mapChannel)
                    .OrderBy(f => Vector3.DistanceSquared(f.Position, player.Position))
                    .FirstOrDefault(f => Vector3.Distance(f.Position, player.Position) <= NearestFieldRange);

                if (nearest == null)
                    Say($"No force field within {NearestFieldRange:0} m. .placefield list shows this map's.");

                return nearest;
            }

            bool TryParseSide(string value, out ForceFields.Side side)
            {
                side = ForceFields.Side.A;

                switch (value?.ToLowerInvariant())
                {
                    case "a": case "afs": side = ForceFields.Side.A; return true;
                    case "b": case "bane": side = ForceFields.Side.B; return true;
                    default: return false;
                }
            }

            switch (sub)
            {
                case "":
                    Say("usage: .placefield <kind|classId> [a|b] [hp] - at your feet, facing your way; a = AFS, b = Bane");
                    Say("       .placefield kinds | list | remove [#id] | repair [#id] | side <a|b> [#id]");
                    Say("       .placefield turn <degrees> [#id] | nudge <along> <through> [up] [#id] | damage <amount> [#id]");
                    return;

                case "kinds":
                    foreach (var c in ForceFields.Classes)
                        Say($"{c.Key} ({c.ClassId}, {c.Kind}): {c.Gate}, {c.Max.X - c.Min.X:0.#} x {c.Max.Y - c.Min.Y:0.#} m");
                    return;

                case "list":
                    {
                        var onMap = ForceFields.OnMap(mapChannel);

                        if (onMap.Count == 0)
                            Say("No force fields on this map.");

                        foreach (var f in onMap)
                            Say(Describe(f));

                        return;
                    }

                case "remove":
                    {
                        var f = Target();

                        if (f == null)
                            return;

                        ForceFields.Remove(f);
                        Say($"Removed force field #{f.Id}.");
                        return;
                    }

                case "repair":
                    {
                        var f = Target();

                        if (f == null)
                            return;

                        ForceFields.Repair(f, player.EntityId);
                        Say(Describe(f));
                        return;
                    }

                case "side":
                    {
                        if (parts.Length < 3 || !TryParseSide(parts[2], out var side))
                        {
                            Say("usage: .placefield side <a|b> [#id]");
                            return;
                        }

                        var f = Target();

                        if (f == null)
                            return;

                        ForceFields.SetSide(f, side);
                        Say(Describe(f));
                        return;
                    }

                case "turn":
                    {
                        if (parts.Length < 3 || !float.TryParse(parts[2], out var degrees))
                        {
                            Say("usage: .placefield turn <degrees> [#id]");
                            return;
                        }

                        var f = Target();

                        if (f == null)
                            return;

                        ForceFields.Move(f, f.Position, f.Yaw + degrees * (float)Math.PI / 180f);
                        Say(Describe(f));
                        return;
                    }

                case "nudge":
                    {
                        if (parts.Length < 4 || !float.TryParse(parts[2], out var along) || !float.TryParse(parts[3], out var through))
                        {
                            Say("usage: .placefield nudge <along> <through> [up] [#id] - metres along the field's width, through it, and up");
                            return;
                        }

                        var up = 0f;

                        if (parts.Length > 4 && !float.TryParse(parts[4], out up))
                        {
                            Say("usage: .placefield nudge <along> <through> [up] [#id]");
                            return;
                        }

                        var f = Target();

                        if (f == null)
                            return;

                        var offset = Vector3.Transform(new Vector3(along, up, through), Quaternion.CreateFromYawPitchRoll(f.Yaw, 0f, 0f));

                        ForceFields.Move(f, f.Position + offset, f.Yaw);
                        Say(Describe(f));
                        return;
                    }

                case "damage":
                    {
                        if (parts.Length < 3 || !int.TryParse(parts[2], out var amount) || amount <= 0)
                        {
                            Say("usage: .placefield damage <amount> [#id]");
                            return;
                        }

                        var f = Target();

                        if (f == null)
                            return;

                        var taken = ForceFields.Damage(f, amount, player.EntityId);
                        Say($"#{f.Id} took {taken}. " + Describe(f));
                        return;
                    }
            }

            var fieldClass = ForceFields.ClassOf(parts[1]);

            if (fieldClass == null)
            {
                Say($"No force field kind '{parts[1]}'. .placefield kinds lists them.");
                return;
            }

            var placeSide = ForceFields.Side.A;

            if (parts.Length > 2 && !TryParseSide(parts[2], out placeSide))
            {
                Say("The side is a (AFS) or b (Bane).");
                return;
            }

            var health = ForceFields.DefaultHealth;

            if (parts.Length > 3 && (!int.TryParse(parts[3], out health) || health <= 0))
            {
                Say("The hit points are a whole number above 0.");
                return;
            }

            var placed = ForceFields.Place(mapChannel, fieldClass, placeSide, player.Position, _client.Movement.ViewDirection.X, health);

            Logger.WriteLog(LogType.Command, $"AccountId = {_client.AccountEntry?.Id}: .placefield {fieldClass.Key} {placeSide} at {player.Position} on {mapChannel.MapInfo.MapContextId}");
            Say("Placed " + Describe(placed));
        }

        /// <summary>Accepts the client's own tutorial name or its raw id.</summary>
        private static bool TryParseTutorial(string value, out TutorialId tutorial)
        {
            if (Enum.TryParse(value, true, out tutorial) && Enum.IsDefined(typeof(TutorialId), tutorial))
                return true;

            if (uint.TryParse(value, out var raw))
            {
                tutorial = (TutorialId)raw;
                return Enum.IsDefined(typeof(TutorialId), tutorial);
            }

            return false;
        }

        private static bool TryParseNotificationType(string value, out PlayerNotificationType type)
        {
            // "location" is shorter to type than CurrentLocation and means the same thing.
            if (value == "location")
            {
                type = PlayerNotificationType.CurrentLocation;
                return true;
            }

            return Enum.TryParse(value, true, out type) && Enum.IsDefined(typeof(PlayerNotificationType), type);
        }

        private void NotifyCommand(string[] parts)
        {
            var manager = NotificationManager.Instance;
            var mapChannel = _client.Player.MapChannel;

            switch (parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty)
            {
                case "timer" when parts.Length >= 4 && uint.TryParse(parts[2], out var timerType) && int.TryParse(parts[3], out var seconds):
                    manager.DisplayTimer(_client, (TimerType)timerType, seconds, parts.Length <= 4 || parts[4] != "0");
                    return;

                case "stoptimer":
                    manager.StopTimer(_client);
                    return;

                case "anim" when parts.Length >= 4 && uint.TryParse(parts[3], out var animationSpecId):
                    {
                        var target = NotifyTarget(parts[2]);

                        if (target != 0)
                            manager.PlayObjectAnimation(mapChannel, NotifyPosition(target), target, animationSpecId);

                        return;
                    }

                case "stopanim" when parts.Length >= 3:
                    {
                        var target = NotifyTarget(parts[2]);

                        if (target != 0)
                            manager.StopObjectAnimation(mapChannel, NotifyPosition(target), target);

                        return;
                    }

                case "bgaudio" when parts.Length >= 3 && uint.TryParse(parts[2], out var audioSpecId):
                    manager.PlayBackgroundAudio(_client, audioSpecId);
                    return;

                case "locaudio" when parts.Length >= 4 && uint.TryParse(parts[3], out var locationAudioSpecId):
                    {
                        var target = NotifyTarget(parts[2]);

                        if (target != 0)
                            manager.PlayLocationAudio(mapChannel, NotifyPosition(target), target, locationAudioSpecId);

                        return;
                    }

                case "stoplocaudio" when parts.Length >= 3:
                    {
                        var target = NotifyTarget(parts[2]);

                        if (target != 0)
                            manager.StopLocationAudio(mapChannel, NotifyPosition(target), target);

                        return;
                    }

                case "raw" when parts.Length >= 3 && uint.TryParse(parts[2], out var notificationId):
                    {
                        var args = new List<long>();

                        for (var i = 3; i < parts.Length; i++)
                            if (long.TryParse(parts[i], out var arg))
                                args.Add(arg);

                        manager.Send(_client, NotificationPacket.Raw((NotificationId)notificationId, args.ToArray()));
                        return;
                    }
            }

            CommunicatorManager.Instance.SystemMessage(_client, "usage: .notify timer <type 1-7> <seconds> [countdown 0|1]");
            CommunicatorManager.Instance.SystemMessage(_client, "       .notify stoptimer");
            CommunicatorManager.Instance.SystemMessage(_client, "       .notify anim|stopanim <me|target|entityId> [animationSpecId]");
            CommunicatorManager.Instance.SystemMessage(_client, "       .notify bgaudio <audioSpecId>");
            CommunicatorManager.Instance.SystemMessage(_client, "       .notify locaudio|stoplocaudio <me|target|entityId> [audioSpecId]");
            CommunicatorManager.Instance.SystemMessage(_client, "       .notify raw <notificationId> [int args...]");
        }

        /// <summary>me, target, or an entity id.</summary>
        private ulong NotifyTarget(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "me":
                    return _client.Player.EntityId;

                case "target":
                    if (_client.Player.Target == 0)
                        CommunicatorManager.Instance.SystemMessage(_client, "no target selected");

                    return _client.Player.Target;

                default:
                    if (ulong.TryParse(value, out var entityId))
                        return entityId;

                    CommunicatorManager.Instance.SystemMessage(_client, $"not an entity id: {value}");
                    return 0;
            }
        }

        /// <summary>Where to broadcast from: the entity's own position when the server knows it.</summary>
        private Vector3 NotifyPosition(ulong entityId)
        {
            var entityType = EntityManager.Instance.GetEntityType(entityId);

            switch (entityType)
            {
                case EntityType.Creature:
                    return EntityManager.Instance.GetCreature(entityId)?.Position ?? _client.Player.Position;

                case EntityType.Character:
                    return EntityManager.Instance.GetPlayer(entityId)?.Position ?? _client.Player.Position;

                case EntityType.Object:
                    return EntityManager.Instance.GetObject(entityId)?.Position ?? _client.Player.Position;

                default:
                    return _client.Player.Position;
            }
        }

        private void ComeHereCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".comehere");
                return;
            }

            if (parts.Length == 2)
                if (ulong.TryParse(parts[1], out var entityId))
                {
                    var test = new Movement(_client.Movement.Position, 6.5f, 0, _client.Movement.ViewDirection);

                    _client.MoveObject(entityId, test);
                }
        }

        private void CreateCreatureCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".creature");
                return;
            }

            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint dbId))
                {
                    var creature = CreatureManager.Instance.CreateCreature(dbId, null);

                    if (creature != null)
                    {
                        CreatureManager.Instance.SetLocation(creature, _client.Movement.Position, _client.Movement.ViewDirection.X, _client.Player.MapContextId);
                        CellManager.Instance.AddToWorld(_client.Player.MapChannel, creature);
                        CommunicatorManager.Instance.SystemMessage(_client, $"Created new creature with EntityId {creature.EntityId}");
                    }
                    else
                        CommunicatorManager.Instance.SystemMessage(_client, $"Creature with dbId={dbId} isn't in database");
                }
            }

            return;
        }

        /// <summary>
        /// Spawns a creature and adopts it as the caller's minion, so the minion command system
        /// can be exercised before there is an ability framework to summon one properly.
        ///
        /// The four bots the Engineer's Bot Construction ability builds are seeded at
        /// 600001..600004 - Flame, Rocket, Shield, Repair - and any other creature dbId works
        /// just as well; nothing here is specific to bots.
        ///
        /// This is a GM tool standing in for a game system, not the game system. It applies none
        /// of the ability's rules: no pump level, no duration, no level scaling, and no
        /// one-at-a-time limit, since that limit belongs to the ability rather than to the
        /// command layer. <c>.minion</c> twice gives you two, and commands go to the newer.
        /// </summary>
        private void MinionCommand(string[] parts)
        {
            if (_client?.Player?.MapChannel == null)
                return;

            if (parts.Length < 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .minion <creatureDbId> | .minion list | .minion clear");
                CommunicatorManager.Instance.SystemMessage(_client, "seeded bots: 600001 Flame, 600002 Rocket, 600003 Shield, 600004 Repair");
                return;
            }

            switch (parts[1])
            {
                case "list":
                {
                    var minions = MinionManager.Instance.MinionsOf(_client);

                    if (minions.Count == 0)
                    {
                        CommunicatorManager.Instance.SystemMessage(_client, "No minions.");
                        return;
                    }

                    foreach (var minion in minions)
                        CommunicatorManager.Instance.SystemMessage(_client,
                            $"{minion.EntityId} {minion.Name} stance={minion.Stance} action={minion.Controller.CurrentAction} hp={minion.Attributes[Attributes.Health].Current}");

                    return;
                }

                case "clear":
                    MinionManager.Instance.DismissAll(_client);
                    CommunicatorManager.Instance.SystemMessage(_client, "Minions dismissed.");
                    return;
            }

            if (!uint.TryParse(parts[1], out var dbId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .minion <creatureDbId> | .minion list | .minion clear");
                return;
            }

            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"Creature with dbId={dbId} isn't in database");
                return;
            }

            // On the player's side whatever the row says, or the thing they just summoned shoots
            // them. The seeded bots are already FRIENDLY; this covers spawning anything else.
            creature.TargetCategory = TargetCategory.Friendly;

            CreatureManager.Instance.SetLocation(creature, _client.Movement.Position, _client.Movement.ViewDirection.X, _client.Player.MapContextId);
            CellManager.Instance.AddToWorld(_client.Player.MapChannel, creature);

            MinionManager.Instance.Adopt(_client, creature);

            CommunicatorManager.Instance.SystemMessage(_client,
                $"Minion {creature.Name} spawned as EntityId {creature.EntityId}. Commands need the MinionCommands server flag.");
        }

        private void CreateObjectCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".createobj");
                return;
            }
            if (parts.Length == 2)
            {
                if (Enum.TryParse(parts[1], out EntityClasses entityClassId))
                {
                    var newObject = new DynamicObject
                    {
                        Position = _client.Movement.Position,
                        Rotation = _client.Movement.ViewDirection.X,
                        MapContextId = _client.Player.MapContextId,
                        EntityClassId = entityClassId
                    };

                    CellManager.Instance.AddToWorld(_client.Player.MapChannel, newObject);
                    CommunicatorManager.Instance.SystemMessage(_client, $"Created object EntityId = {newObject.EntityId}");
                }
            }
            return;
        }

        private void CreateObjectOnLocationCommand(string[] parts)
        {
            if (parts.Length != 6)
            {
                SendCommandUsage(".createobjonloc");
                return;
            }

            if (Enum.TryParse(parts[1], out EntityClasses entityClassId))
                if (float.TryParse(parts[2], out var posX))
                    if (float.TryParse(parts[3], out var posY))
                        if (float.TryParse(parts[4], out var posZ))
                            if (float.TryParse(parts[5], out var orientation))
                            {
                                var newObject = new DynamicObject
                                {
                                    Position = new Vector3(posX, posY, posZ),
                                    Rotation = orientation,
                                    MapContextId = _client.Player.MapContextId,
                                    EntityClassId = entityClassId
                                };

                                CellManager.Instance.AddToWorld(_client.Player.MapChannel, newObject);
                                CommunicatorManager.Instance.SystemMessage(_client, $"Created object EntityId = {newObject.EntityId}");
                            }
            return;
        }

        private void DeleteObjectCommand(string[] parts)
        {
            if (parts.Length != 2)
            {
                SendCommandUsage(".deleteobj");
                return;
            }

            if (ulong.TryParse(parts[1], out ulong entityId))
            {
                _client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
            }

            return;
        }
        private void EnterGmModCommand(string[] parts)
        {
            _client.CallMethod(SysEntity.ClientMethodId, new SetIsGMPacket(true));
            CommunicatorManager.Instance.SystemMessage(_client, "GM Mode enabled!");
            return;
        }

        private void ForceStateCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".forcestate");
                return;
            }
            // if only item template, give max stack size
            if (parts.Length == 3)
                if (ulong.TryParse(parts[1], out var entityId))
                    if (Enum.TryParse(parts[2], out UseObjectState state))
                        _client.CallMethod(entityId, new ForceStatePacket(state, 100));

            return;
        }

        private void GetDistanceCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                var msg = "Distance between you and target ";

                var entityId = _client.Player.Target;

                if (entityId == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, "Please select target to use .getdistance command");
                    return;
                }

                var entityType = EntityManager.Instance.GetEntityType(entityId);

                if (entityType == EntityType.Creature)
                    msg += $"\nEntityId = {entityId} is {Vector3.Distance(_client.Movement.Position, EntityManager.Instance.GetCreature(entityId).SpawnPool.Position)}\n";

                if (entityType == EntityType.Character)
                    msg += $"\nEntityId = {entityId} is {Vector3.Distance(_client.Movement.Position, EntityManager.Instance.GetActor(entityId).Position)}\n";

                if (entityType == EntityType.Object)
                    msg = $"EntityId = {entityId} is object, ToDo\n";

                CommunicatorManager.Instance.SystemMessage(_client, msg);

                return;
            }
        }

        private void GiveItemCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".giveitem");
                return;
            }
            // if only item template, give max stack size
            if (parts.Length == 2)
                if (uint.TryParse(parts[1], out uint itemTemplateId))
                {
                    var classInfo = EntityClassManager.Instance.GetClassInfo(ItemManager.Instance.ItemTemplateItemClass[itemTemplateId]);
                    var item = ItemManager.Instance.CreateFromTemplateId(itemTemplateId, classInfo.ItemClassInfo.StackSize, _client.Player.FamilyName);
                    item.Crafter = _client.Player.FamilyName;
                    InventoryManager.Instance.GrantItemToInventory(_client, item);
                }
            if (parts.Length == 3)
                if (uint.TryParse(parts[1], out uint itemTemplateId))
                    if (uint.TryParse(parts[2], out uint quantity))
                    {
                        var item = ItemManager.Instance.CreateFromTemplateId(itemTemplateId, quantity, _client.Player.FamilyName);
                        item.Crafter = _client.Player.FamilyName;
                        InventoryManager.Instance.GrantItemToInventory(_client, item);
                    }

            return;
        }

        /// <summary>Gains every dropship pad in the world, as walking into each beam would.</summary>
        private void GivePadsCommand(string[] parts)
        {
            var given = DynamicObjectManager.Instance.GainAllDropshipPads(_client);

            CommunicatorManager.Instance.SystemMessage(_client, $"{given} dropship pad{(given == 1 ? "" : "s")} gained; step onto a pad to see them.");
        }

        /// <summary>
        /// .blockaction [actionId [off]]: blocks an action for yourself, or unblocks it, the way
        /// the server blocks an unimplemented ability or a fourth crab mine (ActionBlocks) - the
        /// client greys it out in the ability drawer and refuses it itself. Alone, lists what is
        /// blocked for you and why. Only the GM's own block is taken away by off; the others stay
        /// as long as their reasons do.
        /// </summary>
        private void BlockActionCommand(string[] parts)
        {
            var player = _client.Player;

            if (parts.Length == 1)
            {
                if (player.ActionBlocks.Count == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, "No actions are blocked for you.");
                    return;
                }

                foreach (var entry in player.ActionBlocks.OrderBy(e => (uint)e.Key))
                    CommunicatorManager.Instance.SystemMessage(_client,
                        $"{(uint)entry.Key} {AbilityManager.Instance.ActionName(entry.Key) ?? entry.Key.ToString()}: {string.Join(", ", entry.Value)}");

                return;
            }

            var off = parts.Length == 3 && string.Equals(parts[2], "off", StringComparison.OrdinalIgnoreCase);

            if ((parts.Length != 2 && !off) || !uint.TryParse(parts[1], out var id))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .blockaction [actionId [off]]");
                return;
            }

            var actionId = (ActionId)id;
            var name = AbilityManager.Instance.ActionName(actionId) ?? $"action {id}";

            ActionBlocks.Set(_client, actionId, ActionBlocks.Gm, !off);

            var reasons = ActionBlocks.ReasonsFor(player, actionId);

            CommunicatorManager.Instance.SystemMessage(_client, reasons.Count == 0
                ? $"{name} is not blocked."
                : $"{name} is blocked: {string.Join(", ", reasons)}.");
        }

        /// <summary>Ours: how far .usable looks for an object when none is named or targeted.</summary>
        private const float UsableCommandReach = 10f;

        /// <summary>
        /// .usable [on|off] [#entityId|#target]: puts an object in or out of service
        /// (DynamicObjectManager.SetEnabled, SetUsable) or, with neither, says which it is in. The
        /// object is the one named, else your target if that is an object, else the nearest object
        /// within UsableCommandReach. An object out of service cannot be moused over, so bringing it
        /// back takes its id - which this prints - or standing next to it.
        /// </summary>
        private void UsableCommand(string[] parts)
        {
            var player = _client.Player;
            var idPart = parts.Skip(1).FirstOrDefault(p => p.StartsWith("#"));
            var args = parts.Skip(1).Where(p => p != idPart).Select(p => p.ToLowerInvariant()).ToList();
            const string usage = "usage: .usable [on|off] [#entityId|#target]";

            if (args.Count > 1 || (args.Count == 1 && args[0] != "on" && args[0] != "off"))
            {
                CommunicatorManager.Instance.SystemMessage(_client, usage);
                return;
            }

            DynamicObject obj = null;

            if (idPart != null)
            {
                var entityId = idPart.Equals("#target", StringComparison.OrdinalIgnoreCase)
                    ? player.Target
                    : ulong.TryParse(idPart.Substring(1), out var named) ? named : 0;

                if (!EntityManager.Instance.TryGetObject(entityId, out obj) || obj.MapContextId != player.MapContextId)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"No object {idPart} on this map.");
                    return;
                }
            }
            else if (player.Target != 0 && EntityManager.Instance.TryGetObject(player.Target, out var targeted) && targeted.MapContextId == player.MapContextId)
                obj = targeted;
            else
            {
                // The map loop adds and removes objects as this runs; a look that loses the race
                // finds nothing, and saying so beats taking the command handler down.
                try
                {
                    obj = EntityManager.Instance.DynamicObjects.Values
                        .Where(o => o.MapContextId == player.MapContextId && Vector3.Distance(o.Position, player.Position) <= UsableCommandReach)
                        .OrderBy(o => Vector3.Distance(o.Position, player.Position))
                        .FirstOrDefault();
                }
                catch (InvalidOperationException)
                {
                    obj = null;
                }
            }

            if (obj == null)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"No object named, targeted or within {UsableCommandReach:F0} m. {usage}");
                return;
            }

            var label = $"Object {obj.EntityId} ({obj.EntityClassId}, {obj.DynamicObjectType})";

            if (args.Count == 0)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"{label} is {(obj.IsEnabled ? "in" : "out of")} service.");
                return;
            }

            var enabled = args[0] == "on";
            var changed = DynamicObjectManager.Instance.SetEnabled(obj, enabled);

            CommunicatorManager.Instance.SystemMessage(_client, changed
                ? $"{label} is now {(enabled ? "in" : "out of")} service."
                : $"{label} was already {(enabled ? "in" : "out of")} service.");
        }

        /// <summary>
        /// .givelogos logosId: puts a Logos in your own Tabula. Only a Logos the client knows
        /// (LogosStones.KnownLogosIds) - any other id was taken, saved and announced as a missing
        /// translation - and only one you do not already have.
        /// </summary>
        private void GiveLogosCommand(string[] parts)
        {
            if (parts.Length != 2 || !uint.TryParse(parts[1], out var logosId))
            {
                SendCommandUsage(".givelogos");
                return;
            }

            if (!LogosStones.IsKnownLogos(logosId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"There is no Logos {logosId}: the client's ids run from 1 to 408, with gaps.");
                return;
            }

            if (_client.Player.Logos.Contains(logosId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"Logos {logosId} is already in your Tabula.");
                return;
            }

            CharacterManager.Instance.UpdateCharacter(_client, CharacterUpdate.Logos, logosId);
        }

        /// <summary>
        /// .removelogos logosId|all: takes a Logos, or every Logos, out of your own Tabula - the
        /// character's list, its saved rows and the client's Tabula (LogosStoneRemoved). Players
        /// never lose a Logos in play; this is for testing shrines and stones from scratch, and for
        /// putting right a Tabula a GM got wrong.
        /// </summary>
        private void RemoveLogosCommand(string[] parts)
        {
            if (parts.Length != 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .removelogos logosId|all");
                return;
            }

            if (string.Equals(parts[1], "all", StringComparison.OrdinalIgnoreCase))
            {
                var removed = CharacterManager.Instance.RemoveAllLogos(_client);

                CommunicatorManager.Instance.SystemMessage(_client,
                    removed == 0 ? "Your Tabula is already empty." : $"Removed all {removed} Logos from your Tabula.");
                return;
            }

            if (!uint.TryParse(parts[1], out var logosId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .removelogos logosId|all");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(_client,
                CharacterManager.Instance.RemoveLogos(_client, logosId) == 0
                    ? $"Logos {logosId} is not in your Tabula."
                    : $"Removed Logos {logosId} from your Tabula.");
        }

        private void GiveXpCommand(string[] parts)
        {
            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint xp))
                    ManifestationManager.Instance.GainExperience(_client, xp);
            }
            else
                SendCommandUsage(".givexp");

            return;
        }

        /// <summary>
        /// .setlevel level: puts your own character at that level, 1 to 50. Up levels you through
        /// every level between, as experience would; down resets what the new level no longer
        /// allows. See ManifestationManager.SetLevel.
        /// </summary>
        private void SetLevelCommand(string[] parts)
        {
            if (parts.Length != 2 || !int.TryParse(parts[1], out var level))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"usage: .setlevel level (1 to {ManifestationManager.MaxPlayerLevel})");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(_client, ManifestationManager.Instance.SetLevel(_client, level));
        }

        private void ChangeClassCommand(string[] parts)
        {
            Boolean validInput = false;
            if (parts.Length == 2)
            {
                uint newClassId = 1;
                switch(parts[1].ToUpper())
                {
                    case "RECRUIT": 
                        validInput = true; 
                        newClassId = 1; 
                        break;
                    case "SOLDIER": validInput = true; 
                        newClassId = 2; 
                        break;
                    case "SPECIALIST": validInput = true; 
                        newClassId = 3; 
                        break;
                    case "COMMANDO": validInput = true; 
                        newClassId = 4; 
                        break;
                    case "RANGER": validInput = true; 
                        newClassId = 5; 
                        break;
                    case "SAPPER": 
                        validInput = true; 
                        newClassId = 6; break;
                    case "BIOTECHNICIAN": 
                        validInput = true; 
                        newClassId = 7; break;
                    case "GRENADIER": 
                        validInput = true; 
                        newClassId = 8; break;
                    case "GUARDIAN": 
                        validInput = true; 
                        newClassId = 9; break;
                    case "SNIPER": 
                        validInput = true; 
                        newClassId = 10; break;
                    case "SPY": 
                        validInput = true; 
                        newClassId = 11; break;
                    case "DEMOLITIONIST": 
                        validInput = true; 
                        newClassId = 12; break;
                    case "ENGINEER": 
                        validInput = true; 
                        newClassId = 13; break;
                    case "MEDIC": 
                        validInput = true; 
                        newClassId = 14; break;
                    case "EXOBIOLOGIST": 
                        validInput = true; 
                        newClassId = 15; break;
                    default: 
                        validInput = false;
                        break;
                }
                if (validInput)
                {
                    ManifestationManager.Instance.DebugChgPlayerClass(_client, newClassId);
                }
            }

            if (!validInput) {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .chg_class <className>, availableClasses: \n  RECRUIT\n  SOLDIER\n  SPECIALIST\n  COMMANDO\n  RANGER\n  SAPPER\n  BIOTECHNICIAN\n  GRENADIER\n  GUARDIAN\n  SNIPER\n  SPY\n  DEMOLITIONIST\n  ENGINEER\n  MEDIC\n  EXOBIOLOGIST\n");
            }

            return;
        }

        /// <summary>
        /// Lists what this account can actually run. Printing the whole table to an Observer
        /// would just be a list of things that answer "you do not have access to that".
        /// </summary>
        private void HelpGmCommand(string[] parts)
        {
            var client = _client;

            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(client,
                    $"Commands available at account level {client.AccountEntry.Level}:");

                foreach (var command in _commands.Where(c => HasLevel(client, c.Value.Level))
                                                .OrderBy(c => c.Value.Level)
                                                .ThenBy(c => c.Key))
                    CommunicatorManager.Instance.SystemMessage(client, $"{command.Key} ({command.Value.Level})");

                return;
            }

            var requested = parts[1];

            if (!requested.StartsWith("."))
                requested = "." + requested;

            if (!_commands.TryGetValue(requested, out var requestedCommand) ||
                !HasLevel(client, requestedCommand.Level))
            {
                CommunicatorManager.Instance.SystemMessage(client, $"Unknown command: {requested}");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(client,
                requestedCommand.Arguments.Length == 0 ? "Usage: No arguments" : BuildCommandUsage(requested));
        }

        private void NearCommand(string[] parts)
        {
            var listObj = new List<DynamicObject>();
            var listCreatures = new List<Creature>();

            foreach (var cellSeed in _client.Player.Cells)
            {
                var objects = _client.Player.MapChannel.MapCellInfo.Cells[cellSeed].DynamicObjectList;

                if (objects.Count > 0)
                {
                    foreach (var obj in objects)
                        Console.WriteLine($"object: entityId=> {obj.EntityId}, entityClass=> {obj.EntityClassId}, type=> {obj.DynamicObjectType}, position => {obj.Position}.");
                }
            }
            foreach (var cellSeed in _client.Player.Cells)
            {
                var creatures = _client.Player.MapChannel.MapCellInfo.Cells[cellSeed].CreatureList;
                if (creatures.Count > 0)
                    foreach (var creature in creatures)
                        Console.WriteLine($"creature: entityId=> {creature.EntityId}, entityClass=> {creature.EntityClass}, dbId=> {creature.DbId}, position => {creature.HomePos.Position}.");

            }

            Console.WriteLine();
            return;
        }

        /// <summary>
        /// .immune [all | off | &lt;damage type&gt;... | -&lt;damage type&gt;...]: makes your target
        /// (yourself with none) immune to every hit, or to hits of the named types, so "Immune"
        /// shows as the hit lands (DamageImmunity) - a type's name or number, a leading - takes it
        /// off again. With nothing after it, says what the target is immune to. Held in memory
        /// only: a restart, or the creature respawning, ends it.
        /// </summary>
        /// <summary>
        /// .allowdeath [on|off]: whether this GM dies at zero health like anyone else, or stands
        /// back up (PlayerDeath.IsDeathless). With no argument it toggles. For this session only.
        /// </summary>
        private void AllowDeathCommand(string[] parts)
        {
            var player = _client.Player;
            var word = parts.Length > 1 ? parts[1].ToLowerInvariant() : null;

            switch (word)
            {
                case null:
                    player.AllowDeath = !player.AllowDeath;
                    break;
                case "on":
                case "1":
                case "true":
                    player.AllowDeath = true;
                    break;
                case "off":
                case "0":
                case "false":
                    player.AllowDeath = false;
                    break;
                default:
                    SendCommandUsage(".allowdeath");
                    return;
            }

            CommunicatorManager.Instance.SystemMessage(_client, player.AllowDeath
                ? "Death is on: you die at zero health like anyone else. .allowdeath off to stop."
                : "Death is off: at zero health you stand back up. .allowdeath on to die.");
        }

        private void ImmuneCommand(string[] parts)
        {
            var communicator = CommunicatorManager.Instance;
            var targetId = _client.Player.Target;
            var target = targetId != 0 ? EntityManager.Instance.GetActor(targetId) : null;

            target ??= _client.Player;

            string Describe() =>
                target.ImmuneToAllDamage ? "all damage"
                : target.DamageImmunities.Count == 0 ? "nothing"
                : string.Join(", ", target.DamageImmunities.OrderBy(type => type).Select(type => $"{type} ({(int)type})"));

            var name = target == _client.Player ? "You are" : $"{target.Name ?? target.EntityId.ToString()} is";

            if (parts.Length < 2)
            {
                communicator.SystemMessage(_client, $"{name} immune to {Describe()}. usage: .immune [all|off|<damage type>...|-<damage type>...]");
                return;
            }

            foreach (var word in parts.Skip(1))
            {
                var text = word.ToLowerInvariant();

                if (text == "all")
                {
                    target.ImmuneToAllDamage = true;
                    continue;
                }

                if (text == "off" || text == "none")
                {
                    target.ImmuneToAllDamage = false;
                    target.DamageImmunities.Clear();
                    continue;
                }

                var remove = text.StartsWith("-");
                var typeText = remove ? text.Substring(1) : text;

                if (!Enum.TryParse<DamageType>(typeText, true, out var type) || !Enum.IsDefined(typeof(DamageType), type) || type == 0)
                {
                    communicator.SystemMessage(_client, $"Unknown damage type {word}; use one of {string.Join(", ", Enum.GetNames(typeof(DamageType)))}, or all, or off.");
                    return;
                }

                if (remove)
                    target.DamageImmunities.Remove(type);
                else
                    target.DamageImmunities.Add(type);
            }

            communicator.SystemMessage(_client, $"{name} now immune to {Describe()}.");
        }

        /// <summary>
        /// .targetcategory [hostile|friendly|object|neutral|decoration|decorationproxy|ignore|0-6]:
        /// shows the targeted creature's target category, or sets it and tells the clients, so a
        /// NEUTRAL or inert creature can be tried out without touching the creature table.
        /// </summary>
        private void TargetCategoryCommand(string[] parts)
        {
            var entityId = _client.Player.Target;

            if (entityId == 0 || EntityManager.Instance.GetEntityType(entityId) != EntityType.Creature)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "Target a creature to use .targetcategory [hostile|friendly|object|neutral|decoration|decorationproxy|ignore]");
                return;
            }

            var creature = EntityManager.Instance.GetCreature(entityId);

            if (parts.Length < 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"Target category: {creature.TargetCategory} ({(int)creature.TargetCategory})");
                return;
            }

            if (!Enum.TryParse<TargetCategory>(parts[1], true, out var category) || !Enum.IsDefined(typeof(TargetCategory), category))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"Unknown target category {parts[1]}; use hostile, friendly, object, neutral, decoration, decorationproxy, ignore or 0-6");
                return;
            }

            creature.TargetCategory = category;
            creature.Hate.Clear();
            BehaviorManager.Instance.StopFighting(creature);
            CellManager.Instance.CellCallMethod(creature, new TargetCategoryPacket(category));

            CommunicatorManager.Instance.SystemMessage(_client, $"Target category set to {category} ({(int)category})");
        }

        /// <summary>
        /// .cover: the cover between you and your target, both ways - how many of the body points
        /// each can see of the other, and the damage share a ranged hit would do (Managers.Cover).
        /// Crouch and move about to see what a sandbag or a wall is worth.
        /// </summary>
        private void CoverCommand(string[] parts)
        {
            var player = _client.Player;
            var mapChannel = player.MapChannel;

            if (mapChannel?.Cover == null)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "No cover file for this map (navmesh/<map>.cover, Rasa.NavMesh --cover-only)");
                return;
            }

            var targetId = player.Target;
            Actor target = EntityManager.Instance.GetEntityType(targetId) switch
            {
                EntityType.Creature => EntityManager.Instance.GetCreature(targetId),
                EntityType.Character => EntityManager.Instance.GetPlayer(targetId),
                _ => null
            };

            if (target == null || target.MapContextId != player.MapContextId)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "Target a creature or player to use .cover");
                return;
            }

            string Describe(Actor from, Actor to)
            {
                var eye = Cover.EyeOf(from);
                var points = Cover.SamplePoints(to, eye);
                var clear = points.Count(p => !mapChannel.Cover.Blocked(eye, p));
                var modifier = Cover.Modifier(mapChannel, from, to);

                return $"{clear}/{points.Length} points clear{(to.IsCrouching ? " (crouched)" : "")}, damage x{modifier:0.00}";
            }

            CommunicatorManager.Instance.SystemMessage(_client, $"Its shots at you: {Describe(target, player)}");
            CommunicatorManager.Instance.SystemMessage(_client, $"Your shots at it: {Describe(player, target)}");
        }

        /// <summary>
        /// .los [entityId]: the server's line of sight report (LosReport) from you to your target,
        /// or to the entity given - what the client's unshipped LOS slash commands asked for with
        /// RequestLOSReport.
        /// </summary>
        private void LosCommand(string[] parts)
        {
            var targetId = _client.Player.Target;

            if (parts.Length > 1 && !ulong.TryParse(parts[1], out targetId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "Usage: .los [entityId] - your target when no id is given");
                return;
            }

            LosReport.Send(_client, targetId);
        }

        /// <summary>
        /// .camerascript list | &lt;id&gt;: the camera scripts of the map you are on (CameraScriptTable),
        /// or one of them played on your own client (CameraScripts). Space or escape cuts it short.
        /// </summary>
        private void CameraScriptCommand(string[] parts)
        {
            var mapInfo = _client.Player.MapChannel?.MapInfo;

            if (mapInfo == null)
                return;

            var onMap = CameraScriptTable.OnMap(mapInfo.MapName).ToList();

            if (parts.Length < 2 || parts[1].ToLowerInvariant() == "list")
            {
                if (onMap.Count == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"{mapInfo.MapName} has no camera scripts.");
                    return;
                }

                CommunicatorManager.Instance.SystemMessage(_client, $"{mapInfo.MapName}: {onMap.Count} camera script(s). .camerascript <id> plays one; space or escape ends it.");

                foreach (var s in onMap)
                    CommunicatorManager.Instance.SystemMessage(_client, $"  {s.ScriptId}: {s.Keyframes} keyframe(s), {s.LengthMs / 1000.0:0.#} s");

                return;
            }

            if (!uint.TryParse(parts[1], out var scriptId) || !CameraScripts.Run(_client, scriptId))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"{mapInfo.MapName} has no camera script {parts[1]}. .camerascript list shows its scripts.");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(_client, $"Playing camera script {scriptId}.");
        }

        /// <summary>
        /// .clientevent [list] | track &lt;event&gt;... | stop &lt;event&gt;... | stop all: the scriptable client
        /// events on your own client (ScriptableClientEvents). An event is its id or its name (8,
        /// crouched, MOVE_FORWARD). Events tracked here are echoed to you as they come back; "all" on
        /// track tracks every one.
        /// </summary>
        private void ClientEventCommand(string[] parts)
        {
            var player = _client.Player;
            var verb = parts.Length > 1 ? parts[1].ToLowerInvariant() : "list";

            if (verb == "list")
            {
                var tracked = ScriptableClientEvents.Tracked(player);

                CommunicatorManager.Instance.SystemMessage(_client,
                    $"Scriptable client events ({tracked.Count} tracked). .clientevent track|stop <id|name>... or all.");

                foreach (ScriptableClientEvent value in Enum.GetValues(typeof(ScriptableClientEvent)))
                    CommunicatorManager.Instance.SystemMessage(_client,
                        $"  {(uint)value}: {value}{(tracked.Contains(value) ? " (tracked)" : "")}");

                return;
            }

            if ((verb != "track" && verb != "stop") || parts.Length < 3)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .clientevent [list] | track <id|name>... | stop <id|name>... | stop all");
                return;
            }

            if (parts[2].ToLowerInvariant() == "all")
            {
                if (verb == "stop")
                {
                    var stopped = ScriptableClientEvents.StopAll(_client);
                    player.EchoClientEvents = false;
                    CommunicatorManager.Instance.SystemMessage(_client, $"Stopped {stopped} client event(s).");
                    return;
                }

                parts = new[] { parts[0], parts[1] }
                    .Concat(Enum.GetValues(typeof(ScriptableClientEvent)).Cast<ScriptableClientEvent>().Select(value => ((uint)value).ToString()))
                    .ToArray();
            }

            var done = new List<ScriptableClientEvent>();

            foreach (var text in parts.Skip(2))
            {
                if (!ScriptableClientEvents.TryParse(text, out var eventId))
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"{text} is not a client event. .clientevent list shows them.");
                    continue;
                }

                if (verb == "track" ? ScriptableClientEvents.Start(_client, eventId) : ScriptableClientEvents.Stop(_client, eventId))
                    done.Add(eventId);
            }

            if (verb == "track" && done.Count > 0)
                player.EchoClientEvents = true;
            else if (verb == "stop" && ScriptableClientEvents.Tracked(player).Count == 0)
                player.EchoClientEvents = false;

            if (done.Count > 0)
                CommunicatorManager.Instance.SystemMessage(_client,
                    $"{(verb == "track" ? "Tracking" : "Stopped")}: {string.Join(", ", done)}.");
        }

        private void NpcInfoCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                var msg = "target = (\n";

                var entityId = _client.Player.Target;

                if (entityId == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, "Please select target to use .npcinfo command");
                    return;
                }

                var entityType = EntityManager.Instance.GetEntityType(entityId);

                if (entityId != 0)
                    msg += $"EntityId = {entityId}\nEntityType = {entityType}\n";

                if (entityType == EntityType.Creature)
                {
                    var creature = EntityManager.Instance.GetCreature(entityId);

                    msg += $"CreatureDbId = {creature.DbId}\n";
                    msg += $"TargetCategory = {creature.TargetCategory}\n";

                    if (creature.Attributes.TryGetValue(Attributes.Health, out var npcHealth))
                        msg += $"Health = {npcHealth.Current} / {npcHealth.CurrentMax}\n";

                    // Armour, and what it regenerates with the effects on it (CreatureArmor).
                    if (creature.Attributes.TryGetValue(Attributes.Armor, out var npcArmor))
                        msg += $"Armor = {npcArmor.Current} / {npcArmor.CurrentMax}, +{GameEffectManager.RegenAmount(creature, npcArmor)} every {npcArmor.RefreshPeriod} s\n";

                    if (creature.SpawnPool != null)
                        msg += $"SpawnPoolDbId = {creature.SpawnPool.DbId}\n";

                    msg += $"PosX = {creature.Position.X}\n";
                    msg += $"PosY = {creature.Position.Y}\n";
                    msg += $"PosZ = {creature.Position.Z}\n";
                }

                msg += ")\n";

                Logger.WriteLog(LogType.Debug, msg);
                CommunicatorManager.Instance.SystemMessage(_client, msg);

                return;
            }
        }

        private void ReloadCreaturesCommand(string[] obj)
        {
            CreatureManager.Instance.LoadedCreatures.Clear();
            CreatureManager.Instance.CreatureInit();
            CommunicatorManager.Instance.SystemMessage(_client, "Creatures Reloaded.");
        }

        private void RemoveObjectCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".removeobj");
                return;
            }
            if (parts.Length == 2)
            {
                if (ulong.TryParse(parts[1], out var entityId))
                {
                    CellManager.Instance.RemoveFromWorld(_client.Player.MapChannel, entityId);
                    CommunicatorManager.Instance.SystemMessage(_client, $"Removed object EntityId = {entityId}");
                }
            }
            return;
        }

        /// <summary>
        /// .moveobj &lt;entityId&gt; &lt;x&gt; &lt;y&gt; &lt;z&gt; [yawDegrees | qx qy qz qw] - moves an object that is not
        /// an actor, for every client on your map: an experiment in moving what MoveObject cannot.
        ///
        /// The client's MoveObject only reaches entities registered as moving (Python's
        /// RegisterAsMovingEntity, which only actors call), and an in-world body refuses
        /// SetPosition. UpdatePhysicalEntity takes the entity out of the world first, then runs
        /// WorldLocationDescriptor, which sets the position and orientation and adds it back.
        ///
        /// Works on objects the server made (ids below 2^32) and, as far as the client's code goes,
        /// on the static objects each client builds from the map file (ids above it) - those are in
        /// its entity list too, but were made on a separate static path whose culling bounds are
        /// set once, so what they do when moved is what this is for finding out. A static object is
        /// only moved for the clients on the map now: a client that loads the map later builds it
        /// where the map file says. Orientation: none given is yaw 0; one number is a yaw in
        /// degrees; four are a quaternion (x y z w), which keeps a prop's tilt.
        /// </summary>
        private void MoveObjectCommand(string[] parts)
        {
            if ((parts.Length != 5 && parts.Length != 6 && parts.Length != 9)
                || !ulong.TryParse(parts[1], out var entityId)
                || !TryFloat(parts[2], out var x) || !TryFloat(parts[3], out var y) || !TryFloat(parts[4], out var z))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .moveobj entityId x y z [yawDegrees | qx qy qz qw]");
                return;
            }

            var mapChannel = _client.Player?.MapChannel;

            if (mapChannel == null)
                return;

            var position = new Vector3(x, y, z);
            var rotation = Quaternion.Identity;

            if (parts.Length == 6)
            {
                if (!TryFloat(parts[5], out var yawDegrees))
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"{parts[5]} is not a number");
                    return;
                }

                rotation = Quaternion.CreateFromYawPitchRoll(yawDegrees * MathF.PI / 180f, 0f, 0f);
            }
            else if (parts.Length == 9)
            {
                if (!TryFloat(parts[5], out var qx) || !TryFloat(parts[6], out var qy) || !TryFloat(parts[7], out var qz) || !TryFloat(parts[8], out var qw))
                {
                    CommunicatorManager.Instance.SystemMessage(_client, "the quaternion is four numbers: qx qy qz qw");
                    return;
                }

                rotation = Quaternion.Normalize(new Quaternion(qx, qy, qz, qw));
            }

            switch (EntityManager.Instance.GetEntityType(entityId))
            {
                case EntityType.Character:
                case EntityType.Creature:
                case EntityType.Npc:
                    CommunicatorManager.Instance.SystemMessage(_client, $"{entityId} is an actor, and actors move with MoveObject; use .tele or .teleport for yourself.");
                    return;
            }

            // An object the server made: its own position too, so anyone who comes into range later
            // is told the new one. Its cell is not changed; a move within the cell is what that suits.
            var dynamicObject = mapChannel.DynamicObjects.Find(o => o.EntityId == entityId);

            if (dynamicObject != null)
            {
                dynamicObject.Position = position;
                dynamicObject.Rotation = Math.Atan2(2.0 * (rotation.W * rotation.Y + rotation.X * rotation.Z), 1.0 - 2.0 * (rotation.Y * rotation.Y + rotation.X * rotation.X));
            }

            var update = new UpdatePhysicalEntityPacket(entityId, new List<Packets.PythonPacket> { new WorldLocationDescriptorPacket(position, rotation) });
            var sent = 0;

            foreach (var client in mapChannel.ClientList)
            {
                if (client?.Player == null || client.State != ClientState.Ingame)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, update);
                sent++;
            }

            var kind = entityId > uint.MaxValue ? "a static object from the map file" : dynamicObject != null ? "a server object" : "an id the server does not know";

            CommunicatorManager.Instance.SystemMessage(_client, $"Moved {entityId} ({kind}) to {x} {y} {z} for {sent} client(s) on this map.");
            Logger.WriteLog(LogType.Command, $"{_client.Player.FamilyName} moved entity {entityId} ({kind}) on map {mapChannel.MapInfo.MapContextId} to {position}, rotation {rotation}.");
        }

        private static bool TryFloat(string text, out float value) =>
            float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);

        /// <summary>
        /// .rename first|last &lt;NewName&gt; [familyName] - renames yourself, or the player with
        /// that family name. /changefirstname and /changelastname do the same for yourself.
        /// </summary>
        private void RenameCommand(string[] parts)
        {
            var familyName = parts.Length > 1 && parts[1].ToLowerInvariant() == "last";

            if (parts.Length < 3 || (parts[1].ToLowerInvariant() != "first" && !familyName))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .rename first|last <NewName> [familyName of the player]");
                return;
            }

            var target = _client;

            if (parts.Length > 3)
            {
                target = Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null
                                                  && string.Equals(c.Player.FamilyName, parts[3], StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    CommunicatorManager.Instance.SystemMessage(_client, $"{parts[3]} is not in the world");
                    return;
                }
            }

            CharacterManager.Instance.Rename(_client, target, parts[2], familyName);
        }

        private void RqsWindowCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                _client.CallMethod(SysEntity.ClientMethodId, new DevRQSWindowPacket());
                return;
            }
        }

        private void SetKillStreakCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".setkillstreak");
                return;
            }
            if (parts.Length == 2)
                if (int.TryParse(parts[1], out int count))
                    _client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(count));

            return;
        }

        private void TeleCommand(string[] parts)
        {
            if (parts.Length != 4)
            {
                SendCommandUsage(".tele");
                return;
            }

            if (float.TryParse(parts[1], out float posX))
                if (float.TryParse(parts[2], out float posY))
                    if (float.TryParse(parts[3], out float posZ))
                    {
                        // PlaceAt as well as MoveObject: this only ever told the client to move,
                        // so the server went on holding the position the GM had left and every
                        // range check on them was measured from it.
                        var destination = new Vector3(posX, posY, posZ);

                        _client.Player.PlaceAt(destination);
                        _client.MoveObject(_client.Player.EntityId, new Movement(destination, new Vector2(0f, 0f)));
                    }
        }

        private void TeleportCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".teleport");
                return;
            }
            if (parts.Length == 5)
            {
                if (float.TryParse(parts[1], out float posX))
                    if (float.TryParse(parts[2], out float posY))
                        if (float.TryParse(parts[3], out float posZ))
                            if (uint.TryParse(parts[4], out uint mapId))
                            {
                                if (!MapChannelManager.Instance.ChangeMap(_client, mapId, new Vector3(posX, posY, posZ), _client.Movement.ViewDirection.X))
                                    CommunicatorManager.Instance.SystemMessage(_client, $"Map {mapId} is not loaded, or you cannot teleport right now.");
                            }

            }

            return;
        }

        private void TeleUpCommand(string[] parts)
        {
            if (parts.Length != 2)
            {
                SendCommandUsage(".teleup");
                return;
            }

            if (float.TryParse(parts[1], out float posY))
            {
                var destination = new Vector3(_client.Movement.Position.X, posY, _client.Movement.Position.Z);

                _client.Player.PlaceAt(destination);
                _client.MoveObject(_client.Player.EntityId, new Movement(destination, _client.Movement.ViewDirection));
            }
        }

        private void SetCreatureAppearanceCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".creatureappearance");
                return;
            }
            if (parts.Length == 5)
            {
                if (ulong.TryParse(parts[1], out var entityId))
                    if (uint.TryParse(parts[2], out uint slotId))
                        if (uint.TryParse(parts[3], out uint classId))
                            if (uint.TryParse(parts[4], out uint color))
                            {
                                // get creature from entityId
                                var creature = EntityManager.Instance.GetCreature(entityId);
                                var appearanceData = new AppearanceData(new Structures.Char.CharacterAppearanceEntry(slotId, classId,color));
                                CreatureManager.Instance.CreateOrUpdateAppearance(creature, appearanceData);

                                Logger.WriteLog(LogType.Debug, "Creature Look updated");
                            }
            }

            return;
        }

        private void SetCreatureLocation(string[] parts)
        {
            /*if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client.MapClient, "usage: .creatureloc entityClassId, posX, posY, posZ");
                return;
            }
            if (parts.Length == 5)
            {
                double posX, posY, posZ;
                int entityClassId;
                if (int.TryParse(parts[1], out entityClassId))
                    if (double.TryParse(parts[2], out posX))
                        if (double.TryParse(parts[3], out posY))
                            if (double.TryParse(parts[4], out posZ))
                            {
                                var creatureType = new CreatureType();
                                var position = new Position { PosX = posX, PosY = posY, PosZ = posZ };
                                
                                creatureType.NameId = 0;
                                creatureType.Name = "test Npc";

                                var creature = CreatureManager.Instance.CreateCreature(creatureType, entityClassId, _client.MapClient.Player.AppearanceData, null);
                                CreatureManager.Instance.SetLocation(creature, position, _client.MapClient.Player.Actor.Rotation);
                                CellManager.Instance.AddToWorld(_client.MapClient.MapChannel, creature);
                                CommunicatorManager.Instance.SystemMessage(_client.MapClient, $"Created new creature with EntityId {creature.Actor.EntityId}");
                            }
            }
            return;*/
        }

        #region Map links

        /// <summary>
        /// The links on this map, nearest first: what would fire where you stand, and how far
        /// the next pass is. Distances are on the ground, the way the trigger measures them.
        /// </summary>
        /// <summary>
        /// .navmesh              - is there a navmesh here, and where is its ground under you
        /// .navmesh path x y z   - the route the AI would take from you to (x, y, z)
        /// </summary>
        private void NavMeshCommand(string[] parts)
        {
            var client = _client;
            var mapChannel = client.Player.MapChannel;
            var position = client.Player.Position;

            if (mapChannel?.NavMesh == null)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"Map {client.Player.MapContextId} has no navmesh loaded (folder {NavMeshManager.Instance.Directory}, {NavMeshManager.Instance.LoadedMaps} maps loaded).");
                return;
            }

            if (parts.Length == 5 && parts[1] == "path"
                && float.TryParse(parts[2], out var x) && float.TryParse(parts[3], out var y) && float.TryParse(parts[4], out var z))
            {
                var path = mapChannel.NavMesh.FindPath(position, new Vector3(x, y, z), out var complete);

                if (path == null)
                {
                    CommunicatorManager.Instance.SystemMessage(client, "No path: you or the target are off the navmesh.");
                    return;
                }

                var length = 0f;
                var previous = position;

                foreach (var corner in path)
                {
                    length += Vector3.Distance(previous, corner);
                    previous = corner;
                }

                CommunicatorManager.Instance.SystemMessage(client, $"{(complete ? "Complete" : "Partial")} path, {path.Count} corners, {length:0.#} m; ends at ({previous.X:0.#}, {previous.Y:0.#}, {previous.Z:0.#}).");

                foreach (var corner in path.Take(8))
                    CommunicatorManager.Instance.SystemMessage(client, $"  ({corner.X:0.#}, {corner.Y:0.#}, {corner.Z:0.#})");

                return;
            }

            var ground = mapChannel.NavMesh.GroundHeight(position);
            var nearest = mapChannel.NavMesh.Nearest(position);

            CommunicatorManager.Instance.SystemMessage(client, ground == null
                ? $"No walkable surface within {NavMeshQuery.SearchExtents.X:0.#} m of you."
                : $"Navmesh ground at y = {ground.Value:0.##}, you are at {position.Y:0.##} ({position.Y - ground.Value:+0.##;-0.##} m); nearest walkable point ({nearest.Value.X:0.#}, {nearest.Value.Y:0.#}, {nearest.Value.Z:0.#}).");
        }

        /// <summary>
        /// .kraftwerks                       - the crafting stations on this map, nearest first
        /// .kraftwerks here [comment]        - a new station where you stand, facing as you face
        /// .kraftwerks id here               - move station id to where you stand, facing as you face
        /// .kraftwerks id rotate yaw         - turn station id (radians, the client's ViewDirection.X)
        /// .kraftwerks id comment text       - relabel it
        /// .kraftwerks id delete
        /// The stations were seeded from the client's map markers, which have no facing; this is
        /// how they get one.
        /// </summary>
        /// <summary>
        /// The control points (ControlPoints): those of this map, who holds each and how its
        /// garrison stands; a point given to a side, gone to, or stood where the game master is.
        /// </summary>
        /// <summary>
        /// .instance: the shared copies of the map the game master stands on (MapChannelManager's
        /// instances) - listing them, opening and closing one, going to one, and showing the
        /// client's instance picker, which otherwise takes a full copy to see.
        /// </summary>
        private void InstanceCommand(string[] parts)
        {
            const string usage = "usage: .instance | .instance open | .instance pick | .instance go <number> | .instance close <number>";
            var client = _client;
            var player = client.Player;
            var maps = MapChannelManager.Instance;
            var mapContextId = player.MapContextId;
            var copies = maps.CopiesOf(mapContextId);
            var policy = maps.SharedPolicyOf(mapContextId);

            void Say(string text) => CommunicatorManager.Instance.SystemMessage(client, text);

            if (parts.Length == 1)
            {
                Say(policy == null
                    ? $"Map {mapContextId} runs in one copy (no MapInstances entry with MaxCopies above 1)."
                    : $"Map {mapContextId}: {copies.Count} of {policy.MaxCopies} copies, {policy.Capacity} players each, closed after {policy.IdleCloseSeconds} s empty.");

                for (var i = 0; i < copies.Count; i++)
                {
                    var copy = copies[i];
                    var population = maps.PopulationOf(copy);
                    var status = policy == null ? string.Empty : $", {MapChannelManager.StatusOf(population, policy.Capacity)}";
                    var here = ReferenceEquals(copy, player.MapChannel) ? " (you are here)" : string.Empty;

                    Say($"#{i + 1} instance {copy.InstanceId}: {population} player(s){status}{here}");
                }

                if (player.MapChannel != null && !copies.Contains(player.MapChannel))
                    Say($"You are in instance {player.MapChannel.InstanceId}, which is a private one.");

                return;
            }

            switch (parts[1])
            {
                case "open" when parts.Length == 2:
                {
                    if (policy == null)
                    {
                        Say($"Map {mapContextId} runs in one copy: give it a MapInstances entry in appsettings.json first.");
                        return;
                    }

                    var opened = maps.OpenSharedCopy(mapContextId);

                    Say(opened == null
                        ? $"Map {mapContextId} already has its {policy.MaxCopies} copies."
                        : $"Opened #{maps.CopiesOf(mapContextId).IndexOf(opened) + 1}, instance {opened.InstanceId}. It closes after {policy.IdleCloseSeconds} s empty.");
                    return;
                }

                case "pick" when parts.Length == 2:
                {
                    if (policy == null || copies.Count < 2)
                    {
                        Say("The picker is shown for a map with more than one copy: .instance open first.");
                        return;
                    }

                    if (!maps.EnterMap(client, mapContextId, player.Position, (float)player.Rotation))
                        Say("The picker could not be shown.");

                    return;
                }

                case "go" when parts.Length == 3:
                case "close" when parts.Length == 3:
                {
                    if (!int.TryParse(parts[2], out var number) || number < 1 || number > copies.Count)
                    {
                        Say($"There is no copy #{parts[2]} of map {mapContextId}: .instance lists them.");
                        return;
                    }

                    var copy = copies[number - 1];

                    if (parts[1] == "go")
                    {
                        if (ReferenceEquals(copy, player.MapChannel))
                            Say("You are in that copy.");
                        else if (!maps.Send(client, copy, player.Position, (float)player.Rotation))
                            Say("You could not be moved there.");

                        return;
                    }

                    if (!copy.IsSharedInstance)
                        Say("#1 is the map's own channel and is never closed.");
                    else if (!maps.CloseSharedCopy(copy))
                        Say($"#{number} has {maps.PopulationOf(copy)} player(s) in it or on the way: it is closed empty.");
                    else
                        Say($"Closed #{number}, instance {copy.InstanceId}.");

                    return;
                }

                default:
                    Say(usage);
                    return;
            }
        }

        /// <summary>
        /// .bg: the match of the battleground channel the game master is on (Battlegrounds) - how
        /// it stands, starting and ending it, a team for themselves whatever the rules say, and
        /// a control point for a team.
        /// </summary>
        private void BattlegroundCommand(string[] parts)
        {
            const string usage = "usage: .bg | .bg start | .bg end [red|blue|none] | .bg team red|blue|none | .bg capture <point> red|blue|none | .bg forgive [name]";
            var client = _client;
            var player = client.Player;
            var grounds = Battlegrounds.Instance;
            var match = grounds.MatchOf(player.MapChannel);

            void Say(string text) => CommunicatorManager.Instance.SystemMessage(client, text);

            static uint? Team(string word) => word switch
            {
                "red" => Battlegrounds.Red,
                "blue" => Battlegrounds.Blue,
                "none" => 0u,
                _ => null
            };

            // Wherever the game master stands: a desertion and its lockout forgotten, their own or
            // a player's in the world.
            if (parts.Length >= 2 && parts[1] == "forgive" && parts.Length <= 3)
            {
                var whose = player;

                if (parts.Length == 3)
                {
                    lock (Server.Clients)
                        whose = Server.Clients.Find(c => c?.Player != null && c.State == ClientState.Ingame
                                                         && string.Equals(c.Player.FamilyName, parts[2], StringComparison.OrdinalIgnoreCase))?.Player;

                    if (whose == null)
                    {
                        Say($"{parts[2]} is not in the world.");
                        return;
                    }
                }

                if (!grounds.Forgive(whose.Id))
                {
                    Say($"{whose.FamilyName} has deserted nothing.");
                    return;
                }

                Say($"{whose.FamilyName}'s desertion is forgotten: every team and every instance is open to them.");
                Logger.WriteLog(LogType.Command, $"{player.FamilyName} forgave {whose.FamilyName}'s battleground desertion.");
                return;
            }

            if (match == null)
            {
                Say($"Map {player.MapContextId} has no battleground.");
                return;
            }

            if (parts.Length == 1)
            {
                var clock = match.Phase == Battlegrounds.Phase.Waiting ? "" : $", {grounds.SecondsLeft(match)} s left";

                Say($"Instance {match.Map.InstanceId}: {match.Phase}{clock}{(match.Forced ? ", started by a game master" : "")}. "
                    + $"Red {match.Count(Battlegrounds.Red)}, Blue {match.Count(Battlegrounds.Blue)}; each needs {Math.Max(1, grounds.Config.MinPlayersPerTeam)}.");

                foreach (var team in new[] { Battlegrounds.Red, Battlegrounds.Blue })
                    Say($"{Battlegrounds.TeamName(team)}: {match.Held(team)} point(s), {match.Kills(team)} kill(s) - "
                        + (match.Count(team) == 0 ? "nobody" : string.Join(", ", match.Team(team).Select(m => m.Client.Player.FamilyName))));

                foreach (var point in match.Points)
                {
                    var garrison = grounds.GarrisonOf(match.Map, point.Source) switch
                    {
                        ControlPoints.Garrison.None => "no Bane",
                        ControlPoints.Garrison.Down => "Bane down",
                        _ => "Bane standing"
                    };

                    Say($"#{point.Id} {point.Name}: {Battlegrounds.TeamName(point.Owner)}, {garrison}, {(point.Object.IsEnabled ? "open" : "shut")}");
                }

                var deserted = grounds.DesertedTeamOf(player.Id);

                if (deserted != 0)
                    Say($"You are a deserter of {Battlegrounds.TeamName(deserted)}.");

                var lockout = grounds.LockoutOf(player.Id);

                if (lockout != null)
                    Say($"You left a match in progress on instance {lockout.InstanceId} of map {lockout.MapContextId}: every other is shut to you for {grounds.TimeLeftOf(lockout)}"
                        + (grounds.IsExempt(client) ? " (a game master is not held to it)." : "."));

                return;
            }

            switch (parts[1])
            {
                case "start" when parts.Length == 2:
                    if (match.Phase == Battlegrounds.Phase.Running)
                    {
                        Say("The match is running.");
                        return;
                    }

                    grounds.Start(match, forced: true);
                    Logger.WriteLog(LogType.Command, $"{player.FamilyName} started the battleground match on map {player.MapContextId}, instance {match.Map.InstanceId}.");
                    Say("The match has begun. It runs until its clock or .bg end, whatever the teams have in them.");
                    return;

                case "end" when parts.Length == 2 || parts.Length == 3 && Team(parts[2]).HasValue:
                    if (match.Phase != Battlegrounds.Phase.Running)
                    {
                        Say("No match is running.");
                        return;
                    }

                    var winner = parts.Length == 3 ? Team(parts[2]).Value : Battlegrounds.Leader(match);

                    grounds.End(match, winner);
                    Logger.WriteLog(LogType.Command, $"{player.FamilyName} ended the battleground match on map {player.MapContextId}, instance {match.Map.InstanceId}: {Battlegrounds.TeamName(winner)}.");
                    Say($"The match is over: {Battlegrounds.TeamName(winner)} won.");
                    return;

                case "team" when parts.Length == 3 && Team(parts[2]).HasValue:
                {
                    var team = Team(parts[2]).Value;

                    if (team == 0)
                        Say(grounds.LeaveTeam(client) ? "You are on no team." : "You were on no team.");
                    else
                        Say(grounds.Join(client, team, force: true) ? $"You are on {Battlegrounds.TeamName(team)}." : $"You could not be put on {Battlegrounds.TeamName(team)}.");

                    return;
                }

                case "capture" when parts.Length == 4 && Team(parts[3]).HasValue:
                {
                    var point = match.Points.Find(p => p.Id.ToString() == parts[2] || string.Equals(p.Name, parts[2], StringComparison.OrdinalIgnoreCase));

                    if (point == null)
                    {
                        Say($"There is no control point {parts[2]} here: .bg lists them.");
                        return;
                    }

                    var team = Team(parts[3]).Value;

                    Say(grounds.SetOwner(match, point, team)
                        ? $"{point.Name} is {Battlegrounds.TeamName(team)}'s."
                        : $"{point.Name} is {Battlegrounds.TeamName(team)}'s already.");
                    return;
                }

                default:
                    Say(usage);
                    return;
            }
        }

        private void ControlPointCommand(string[] parts)
        {
            const string usage = "usage: .cp | .cp all | .cp <id> afs | bane | goto | here";
            var client = _client;
            var player = client.Player;
            var points = ControlPoints.Instance;

            if (parts.Length == 1 || parts.Length == 2 && parts[1] == "all")
            {
                var all = parts.Length == 2;
                var list = (all ? points.Points : points.OnMap(player.MapContextId))
                    .OrderBy(p => p.MapContextId).ThenBy(p => p.Id).ToList();

                if (list.Count == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(client, all ? "There are no control points." : $"No control points on map {player.MapContextId}.");
                    return;
                }

                CommunicatorManager.Instance.SystemMessage(client, all
                    ? $"{list.Count} control point(s), {list.Count(p => p.HeldByAfs)} held by the AFS:"
                    : $"{list.Count} control point(s) on map {player.MapContextId}:");

                foreach (var point in list)
                {
                    var mapChannel = point.Object?.RuntimeMapChannel;
                    var garrison = mapChannel == null ? "not placed" : points.GarrisonOf(mapChannel, point, point.Owner) switch
                    {
                        ControlPoints.Garrison.None => "no garrison",
                        ControlPoints.Garrison.Down => "garrison down",
                        _ => "garrison standing"
                    };
                    var where = all
                        ? $"map {point.MapContextId}"
                        : $"{Vector3.Distance(point.Object?.Position ?? point.Position, player.Position):0.#} m";

                    CommunicatorManager.Instance.SystemMessage(client, point.IsBattleground
                        ? $"#{point.Id} {point.Name}: a battleground's (.bg), {where}"
                        : $"#{point.Id} {point.Name}: {ControlPoints.FactionName(point.Owner)}, {garrison}, {where}");
                }

                return;
            }

            if (parts.Length != 3 || !uint.TryParse(parts[1], out var id) || points.ById(id) is not { } target)
            {
                CommunicatorManager.Instance.SystemMessage(client, usage);
                return;
            }

            switch (parts[2])
            {
                case "afs" when target.IsBattleground:
                case "bane" when target.IsBattleground:
                    CommunicatorManager.Instance.SystemMessage(client, $"Control point #{id} {target.Name} is a battleground's: .bg capture gives it to a team.");
                    return;

                case "afs":
                case "bane":
                    var owner = parts[2] == "afs" ? ControlPoints.Afs : ControlPoints.Bane;

                    CommunicatorManager.Instance.SystemMessage(client, points.SetOwner(target, owner, null)
                        ? $"Control point #{id} {target.Name} is the {ControlPoints.FactionName(owner)}'s."
                        : $"Control point #{id} {target.Name} is the {ControlPoints.FactionName(owner)}'s already.");
                    Logger.WriteLog(LogType.Command, $"{player.FamilyName} gave control point {id} ({target.Name}) to the {ControlPoints.FactionName(owner)}.");
                    return;

                case "goto":
                    if (!MapChannelManager.Instance.ChangeMap(client, target.MapContextId, target.Object?.Position ?? target.Position, (float)player.Rotation))
                        CommunicatorManager.Instance.SystemMessage(client, $"Map {target.MapContextId} is not loaded, or you cannot teleport right now.");
                    return;

                case "here":
                    if (target.MapContextId != player.MapContextId || player.MapChannel == null || player.MapChannel.IsCopy)
                    {
                        CommunicatorManager.Instance.SystemMessage(client, $"Control point #{id} {target.Name} is on map {target.MapContextId}; stand where it should be, on that map's own channel.");
                        return;
                    }

                    CommunicatorManager.Instance.SystemMessage(client, points.Move(target, player.Position, player.Rotation, Server.GameUnitOfWorkFactory)
                        ? $"Control point #{id} {target.Name} now stands at ({player.Position.X:0.#}, {player.Position.Y:0.#}, {player.Position.Z:0.#})."
                        : $"Control point #{id} {target.Name} could not be moved; see the server log.");
                    return;

                default:
                    CommunicatorManager.Instance.SystemMessage(client, usage);
                    return;
            }
        }

        private void KraftwerksCommand(string[] parts)
        {
            var client = _client;
            var player = client.Player;

            if (parts.Length == 1)
            {
                var stations = KraftwerksManager.Instance.OnMap(player.MapContextId, player.Position);

                if (stations.Count == 0)
                {
                    CommunicatorManager.Instance.SystemMessage(client, $"No crafting stations on map {player.MapContextId}.");
                    return;
                }

                CommunicatorManager.Instance.SystemMessage(client, $"{stations.Count} crafting station(s) on map {player.MapContextId}, nearest first:");

                foreach (var station in stations.Take(10))
                {
                    var e = station.Entry;
                    CommunicatorManager.Instance.SystemMessage(client, $"{Vector3.Distance(e.Position, player.Position),6:0.#} m  #{e.Id} ({e.PosX:0.#}, {e.PosY:0.#}, {e.PosZ:0.#}) yaw {e.Rotation:0.##}  {e.Comment}");
                }

                return;
            }

            if (parts[1] == "here")
            {
                var comment = string.Join(' ', parts.Skip(2));
                var station = KraftwerksManager.Instance.Add(player.MapContextId, player.Position, player.Rotation, comment.Length > 64 ? comment.Substring(0, 64) : comment);

                CommunicatorManager.Instance.SystemMessage(client, station == null
                    ? "The station could not be created; see the server log."
                    : $"Created crafting station #{station.Entry.Id} at ({player.Position.X:0.#}, {player.Position.Y:0.#}, {player.Position.Z:0.#}).");
                return;
            }

            if (!uint.TryParse(parts[1], out var id) || !KraftwerksManager.Instance.TryGet(id, out var target))
            {
                CommunicatorManager.Instance.SystemMessage(client, "usage: .kraftwerks [here [comment] | id here | id rotate yaw | id comment text | id delete]");
                return;
            }

            var ok = false;
            var what = parts.Length > 2 ? parts[2] : "";

            switch (what)
            {
                case "here":
                    ok = KraftwerksManager.Instance.Move(target, player.Position, player.Rotation);
                    break;

                case "rotate" when parts.Length > 3 && double.TryParse(parts[3], out var yaw):
                    ok = KraftwerksManager.Instance.Move(target, target.Entry.Position, yaw);
                    break;

                case "comment":
                    var comment = string.Join(' ', parts.Skip(3));
                    ok = KraftwerksManager.Instance.SetComment(target, comment.Length > 64 ? comment.Substring(0, 64) : comment);
                    break;

                case "delete":
                    ok = KraftwerksManager.Instance.Delete(target);
                    break;

                default:
                    CommunicatorManager.Instance.SystemMessage(client, "usage: .kraftwerks [here [comment] | id here | id rotate yaw | id comment text | id delete]");
                    return;
            }

            CommunicatorManager.Instance.SystemMessage(client, ok
                ? $"Crafting station #{id}: {what} done."
                : $"Crafting station #{id}: {what} failed; see the server log.");
        }

        private void LinksCommand(string[] parts)
        {
            var client = _client;
            var links = MapLinkManager.Instance.OnMap(client.Player.MapContextId, client.Player.Position);

            if (links.Count == 0)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"No map links on map {client.Player.MapContextId}.");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(client, $"{links.Count} map link(s) on map {client.Player.MapContextId}, nearest first:");

            foreach (var link in links.Take(10))
            {
                var dx = link.Position.X - client.Player.Position.X;
                var dz = link.Position.Z - client.Player.Position.Z;
                var distance = Math.Sqrt(dx * dx + dz * dz);
                var standing = MapLinkManager.Contains(link, client.Player.Position) ? " <- you are in it" : "";

                CommunicatorManager.Instance.SystemMessage(client, $"{distance,6:0.#} m  {link}{standing}");
            }

            if (links.Count > 10)
                CommunicatorManager.Instance.SystemMessage(client, $"... and {links.Count - 10} more.");
        }

        /// <summary>
        /// Drops a new link at your feet: the trigger is where you stand, on this map; the
        /// arrival is the position given, on the destination map. Fine-tune it with .link.
        /// </summary>
        private void LinkHereCommand(string[] parts)
        {
            if (parts.Length < 5 || parts.Length > 7)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .linkhere destMapId destX destY destZ [radius] [border|instance]");
                CommunicatorManager.Instance.SystemMessage(_client, "Creates a link at your position. Stand at the arrival on the other map and use .link <id> arrival to set where it lands.");
                return;
            }

            if (!uint.TryParse(parts[1], out var destMap) || !float.TryParse(parts[2], out var destX)
                || !float.TryParse(parts[3], out var destY) || !float.TryParse(parts[4], out var destZ))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "destMapId must be a map context id and destX destY destZ numbers.");
                return;
            }

            var radius = 8.0f;

            if (parts.Length >= 6 && !float.TryParse(parts[5], out radius))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "radius must be a number of metres.");
                return;
            }

            var kind = MapLinkKind.Border;

            if (parts.Length == 7 && !Enum.TryParse(parts[6], true, out kind))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "kind must be border or instance.");
                return;
            }

            if (!MapChannelManager.Instance.MapChannelArray.ContainsKey(destMap))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"Map {destMap} is not loaded.");
                return;
            }

            var link = new MapLink
            {
                MapContextId = _client.Player.MapContextId,
                Position = _client.Player.Position,
                Radius = radius,
                DestMapContextId = destMap,
                DestPosition = new Vector3(destX, destY, destZ),
                DestRotation = 0,
                Kind = kind,
                Enabled = true,
                Comment = $"{_client.Player.MapContextId} -> {destMap} (.linkhere by {_client.Player.FamilyName})"
            };

            var created = MapLinkManager.Instance.Add(link);

            if (created == null)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "The link could not be saved; see the server log.");
                return;
            }

            // The GM is standing in the new gate. Treat it like an arrival so it does not fire
            // on them until they step out of it.
            MapLinkManager.Instance.PlayerEnteredMap(_client);
            CommunicatorManager.Instance.SystemMessage(_client, $"Created map link {created}");
        }

        /// <summary>
        /// Adjusts one link in place and in the database. 'trigger' and 'arrival' take your
        /// current map, position and facing, so a pass is tuned by walking to where it should
        /// fire, then to where it should land, and running the two subcommands.
        /// </summary>
        private void LinkCommand(string[] parts)
        {
            if (parts.Length < 2 || !uint.TryParse(parts[1], out var id))
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .link id [trigger | arrival | radius r | enable | disable | delete | goto | gotoarrival | comment text]");
                return;
            }

            if (!MapLinkManager.Instance.TryGet(id, out var link))
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"No map link with id {id}. .links lists the ones on this map.");
                return;
            }

            if (parts.Length == 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, link.ToString());
                CommunicatorManager.Instance.SystemMessage(_client, $"arrival yaw {link.DestRotation:0.###}, {(link.Enabled ? "enabled" : "disabled")}");
                return;
            }

            var previousPosition = link.Position;
            var previousMap = link.MapContextId;
            var player = _client.Player;

            switch (parts[2].ToLowerInvariant())
            {
                case "trigger":
                    link.MapContextId = player.MapContextId;
                    link.Position = player.Position;
                    break;

                case "arrival":
                    link.DestMapContextId = player.MapContextId;
                    link.DestPosition = player.Position;
                    link.DestRotation = (float)player.Rotation;
                    break;

                case "radius":
                    if (parts.Length < 4 || !float.TryParse(parts[3], out var radius) || radius <= 0 || radius > 200)
                    {
                        CommunicatorManager.Instance.SystemMessage(_client, "usage: .link id radius metres  (0 < metres <= 200)");
                        return;
                    }

                    link.Radius = radius;
                    break;

                case "enable":
                    link.Enabled = true;
                    break;

                case "disable":
                    link.Enabled = false;
                    break;

                case "comment":
                    link.Comment = string.Join(' ', parts.Skip(3));

                    if (link.Comment.Length > 64)
                        link.Comment = link.Comment.Substring(0, 64);
                    break;

                case "delete":
                    if (MapLinkManager.Instance.Delete(link))
                        CommunicatorManager.Instance.SystemMessage(_client, $"Deleted map link {id}.");
                    else
                        CommunicatorManager.Instance.SystemMessage(_client, $"Map link {id} could not be deleted; see the server log.");
                    return;

                case "goto":
                    // Land on the trigger itself. PlayerEnteredMap seeds the link into InsideMapLinks, so
                    // it does not fire until the player steps out and back in. Landing beside it is not
                    // safe: most passes are tunnel meshes bored under the heightmap, and a point a few
                    // metres off the marker can be inside the rock, with nothing to stand on.
                    if (!MapChannelManager.Instance.ChangeMap(_client, link.MapContextId, link.Position, (float)player.Rotation))
                        CommunicatorManager.Instance.SystemMessage(_client, $"Map {link.MapContextId} is not loaded, or you cannot teleport right now.");
                    return;

                case "gotoarrival":
                    if (!MapChannelManager.Instance.ChangeMap(_client, link.DestMapContextId, link.DestPosition, link.DestRotation))
                        CommunicatorManager.Instance.SystemMessage(_client, $"Map {link.DestMapContextId} is not loaded, or you cannot teleport right now.");
                    return;

                default:
                    CommunicatorManager.Instance.SystemMessage(_client, "usage: .link id [trigger | arrival | radius r | enable | disable | delete | goto | gotoarrival | comment text]");
                    return;
            }

            if (MapLinkManager.Instance.Update(link, previousPosition, previousMap))
            {
                // If the GM moved the trigger onto themselves, do not fire it on them.
                MapLinkManager.Instance.PlayerEnteredMap(_client);
                CommunicatorManager.Instance.SystemMessage(_client, $"Updated map link {link}");
            }
            else
                CommunicatorManager.Instance.SystemMessage(_client, $"Map link {id} could not be saved; see the server log.");
        }

        #endregion

        /// <summary>
        /// Forces a region list on your own client and holds it there, so a region's ambience,
        /// sky and minimap can be seen without standing in a volume for it. .setregion off hands
        /// control back to the volumes; leaving the map does too.
        /// </summary>
        private void SetRegionCommand(string[] parts)
        {
            var client = _client;

            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(client, "usage: .setregion regionId [regionId ...] | off");
                return;
            }

            if (parts[1] == "off")
            {
                RegionManager.Instance.Release(client);
                CommunicatorManager.Instance.SystemMessage(client, "Regions follow the volumes again.");
                return;
            }

            var regionIds = new List<uint>();

            foreach (var part in parts.Skip(1))
            {
                if (!uint.TryParse(part, out var regionId))
                {
                    CommunicatorManager.Instance.SystemMessage(client, $"{part} is not a region id.");
                    return;
                }

                regionIds.Add(regionId);
            }

            RegionManager.Instance.Hold(client, regionIds);
            CommunicatorManager.Instance.SystemMessage(client, $"Holding regions [{string.Join(", ", regionIds)}] until .setregion off or a map change.");
        }

        /// <summary>The region volumes on this map, nearest first, and what you are currently sent.</summary>
        private void RegionsCommand(string[] parts)
        {
            var client = _client;
            var player = client.Player;
            var underground = NavMeshManager.IsUnderground(player.MapChannel, player.Position);
            var current = player.RegionIds == null ? "nothing yet" : $"[{string.Join(", ", player.RegionIds)}]";

            CommunicatorManager.Instance.SystemMessage(client, $"You are {(underground ? "underground" : "on the surface")} at ({player.Position.X:0.#}, {player.Position.Y:0.#}, {player.Position.Z:0.#}); regions sent: {current}{(player.RegionsHeld ? " (held by .setregion)" : "")}.");

            var volumes = RegionManager.Instance.OnMap(player.MapContextId, player.Position);

            if (volumes.Count == 0)
            {
                CommunicatorManager.Instance.SystemMessage(client, $"No region volumes on map {player.MapContextId}.");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(client, $"{volumes.Count} region volume(s) on map {player.MapContextId}, nearest first:");

            foreach (var volume in volumes.Take(12))
            {
                var inside = volume.Enabled && volume.Contains(player.Position, underground) ? " <- you are in it" : "";
                CommunicatorManager.Instance.SystemMessage(client, $"{volume.Distance(player.Position),6:0.#} m  {volume.Describe()}{inside}");
            }

            if (volumes.Count > 12)
                CommunicatorManager.Instance.SystemMessage(client, $"... and {volumes.Count - 12} more.");
        }

        private const string RegionUsage = "usage: .region here regionId [radius] [comment] | box regionId halfX halfZ [comment] | id here | id radius r | id size halfX halfZ | id y min max | id underground 0|1|2 | id region regionId | id enable | id disable | id comment text | id delete";

        /// <summary>Creates and edits region volumes; see RegionManager and docs/regions.md.</summary>
        private void RegionCommand(string[] parts)
        {
            var client = _client;
            var player = client.Player;

            if (parts.Length < 3)
            {
                CommunicatorManager.Instance.SystemMessage(client, RegionUsage);
                return;
            }

            if (parts[1] == "here" || parts[1] == "box")
            {
                if (!uint.TryParse(parts[2], out var newRegionId))
                {
                    CommunicatorManager.Instance.SystemMessage(client, RegionUsage);
                    return;
                }

                var volume = new MapRegion
                {
                    MapContextId = player.MapContextId,
                    RegionId = newRegionId,
                    Position = player.Position,
                    MinY = -2000,
                    MaxY = 2000,
                    Underground = MapRegionUnderground.Any,
                    Enabled = true
                };

                int commentFrom;

                if (parts[1] == "here")
                {
                    volume.Shape = MapRegionShape.Circle;
                    volume.Radius = 50;
                    commentFrom = 3;

                    if (parts.Length > 3 && float.TryParse(parts[3], out var radius) && radius > 0)
                    {
                        volume.Radius = radius;
                        commentFrom = 4;
                    }
                }
                else
                {
                    if (parts.Length < 5 || !float.TryParse(parts[3], out var halfX) || !float.TryParse(parts[4], out var halfZ) || halfX <= 0 || halfZ <= 0)
                    {
                        CommunicatorManager.Instance.SystemMessage(client, RegionUsage);
                        return;
                    }

                    volume.Shape = MapRegionShape.Box;
                    volume.HalfX = halfX;
                    volume.HalfZ = halfZ;
                    commentFrom = 5;
                }

                volume.Comment = ClampComment(string.Join(' ', parts.Skip(commentFrom)));

                var created = RegionManager.Instance.Add(volume);

                CommunicatorManager.Instance.SystemMessage(client, created == null
                    ? "The region volume could not be created; see the server log."
                    : $"Created {created.Describe()}");
                return;
            }

            if (!uint.TryParse(parts[1], out var id) || !RegionManager.Instance.TryGet(id, out var target))
            {
                CommunicatorManager.Instance.SystemMessage(client, RegionUsage);
                return;
            }

            var what = parts[2];

            switch (what)
            {
                case "here":
                    target.Position = player.Position;
                    break;

                case "radius" when parts.Length > 3 && float.TryParse(parts[3], out var radius) && radius > 0:
                    target.Shape = MapRegionShape.Circle;
                    target.Radius = radius;
                    break;

                case "size" when parts.Length > 4 && float.TryParse(parts[3], out var halfX) && float.TryParse(parts[4], out var halfZ) && halfX > 0 && halfZ > 0:
                    target.Shape = MapRegionShape.Box;
                    target.HalfX = halfX;
                    target.HalfZ = halfZ;
                    break;

                case "y" when parts.Length > 4 && float.TryParse(parts[3], out var minY) && float.TryParse(parts[4], out var maxY) && minY < maxY:
                    target.MinY = minY;
                    target.MaxY = maxY;
                    break;

                case "underground" when parts.Length > 3 && byte.TryParse(parts[3], out var mode) && mode <= 2:
                    target.Underground = (MapRegionUnderground)mode;
                    break;

                case "region" when parts.Length > 3 && uint.TryParse(parts[3], out var regionId):
                    target.RegionId = regionId;
                    break;

                case "enable":
                    target.Enabled = true;
                    break;

                case "disable":
                    target.Enabled = false;
                    break;

                case "comment":
                    target.Comment = ClampComment(string.Join(' ', parts.Skip(3)));
                    break;

                case "delete":
                    CommunicatorManager.Instance.SystemMessage(client, RegionManager.Instance.Delete(target)
                        ? $"Deleted region volume #{id}."
                        : $"Region volume #{id} could not be deleted; see the server log.");
                    return;

                default:
                    CommunicatorManager.Instance.SystemMessage(client, RegionUsage);
                    return;
            }

            CommunicatorManager.Instance.SystemMessage(client, RegionManager.Instance.Update(target)
                ? $"Updated {target.Describe()}"
                : $"Region volume #{id} could not be saved; see the server log.");
        }

        #region FX emitters

        /// <summary>The FX emitters on this map, nearest first.</summary>
        private void EmittersCommand(string[] parts)
        {
            var player = _client.Player;
            var emitters = EmitterManager.Instance.OnMap(player.MapContextId, player.Position);

            if (emitters.Count == 0)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"No FX emitters on map {player.MapContextId}.");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(_client, $"{emitters.Count} FX emitter(s) on map {player.MapContextId}, nearest first:");

            foreach (var emitter in emitters.Take(15))
                CommunicatorManager.Instance.SystemMessage(_client, $"{Vector3.Distance(emitter.Position, player.Position):0} m: {emitter.Describe()}");

            if (emitters.Count > 15)
                CommunicatorManager.Instance.SystemMessage(_client, $"... and {emitters.Count - 15} more.");
        }

        /// <summary>The client's FX packages whose names contain every word given.</summary>
        private void FxPackagesCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                CommunicatorManager.Instance.SystemMessage(_client, $"usage: .fxpackages word [word ...] - searches the client's {FxPackages.Names.Count} FX packages by name");
                return;
            }

            var found = FxPackages.Search(parts.Skip(1));

            if (found.Count == 0)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "No FX package matches.");
                return;
            }

            CommunicatorManager.Instance.SystemMessage(_client, $"{found.Count} FX package(s):");

            foreach (var (id, name) in found.Take(25))
                CommunicatorManager.Instance.SystemMessage(_client, $"{id} {name}");

            if (found.Count > 25)
                CommunicatorManager.Instance.SystemMessage(_client, $"... and {found.Count - 25} more; add a word to narrow it.");
        }

        private const string EmitterUsage = "usage: .emitter here package [off] [comment] | id on | id off | id package package | id here | id goto | id comment text | id delete - a package is a name from .fxpackages or its id";

        /// <summary>Places and edits FX emitters; see EmitterManager. Everything but goto is saved to map_emitter.</summary>
        private void EmitterCommand(string[] parts)
        {
            var client = _client;
            var player = client.Player;

            if (parts.Length < 3)
            {
                CommunicatorManager.Instance.SystemMessage(client, EmitterUsage);
                return;
            }

            if (parts[1] == "here")
            {
                if (!FxPackages.TryResolve(parts[2], out var packageId))
                {
                    CommunicatorManager.Instance.SystemMessage(client, $"{parts[2]} is not one of the client's FX packages; .fxpackages finds them.");
                    return;
                }

                var on = !(parts.Length > 3 && parts[3] == "off");
                var created = EmitterManager.Instance.Add(new MapEmitter
                {
                    MapContextId = player.MapContextId,
                    Position = player.Position,
                    Rotation = player.Rotation,
                    PackageId = packageId,
                    OnByDefault = on,
                    IsOn = on,
                    Comment = ClampComment(string.Join(' ', parts.Skip(on ? 3 : 4)))
                });

                CommunicatorManager.Instance.SystemMessage(client, created == null
                    ? "The emitter could not be created; see the server log."
                    : $"Created {created.Describe()}");
                return;
            }

            if (!uint.TryParse(parts[1], out var id) || !EmitterManager.Instance.TryGet(id, out var emitter))
            {
                CommunicatorManager.Instance.SystemMessage(client, EmitterUsage);
                return;
            }

            switch (parts[2])
            {
                case "on":
                    EmitterManager.Instance.TurnOn(emitter);
                    emitter.OnByDefault = true;
                    break;

                case "off":
                    EmitterManager.Instance.TurnOff(emitter);
                    emitter.OnByDefault = false;
                    break;

                case "package" when parts.Length > 3:
                    if (!FxPackages.TryResolve(parts[3], out var packageId))
                    {
                        CommunicatorManager.Instance.SystemMessage(client, $"{parts[3]} is not one of the client's FX packages; .fxpackages finds them.");
                        return;
                    }

                    EmitterManager.Instance.SetPackage(emitter, packageId);
                    break;

                case "here":
                    EmitterManager.Instance.MoveTo(emitter, player.MapContextId, player.Position, player.Rotation);
                    break;

                case "goto":
                    if (!MapChannelManager.Instance.ChangeMap(client, emitter.MapContextId, emitter.Position, (float)emitter.Rotation))
                        CommunicatorManager.Instance.SystemMessage(client, $"Map {emitter.MapContextId} is not loaded, or you cannot teleport right now.");
                    return;

                case "comment":
                    emitter.Comment = ClampComment(string.Join(' ', parts.Skip(3)));
                    break;

                case "delete":
                    CommunicatorManager.Instance.SystemMessage(client, EmitterManager.Instance.Delete(emitter)
                        ? $"Deleted FX emitter #{id}."
                        : $"FX emitter #{id} could not be deleted; see the server log.");
                    return;

                default:
                    CommunicatorManager.Instance.SystemMessage(client, EmitterUsage);
                    return;
            }

            CommunicatorManager.Instance.SystemMessage(client, EmitterManager.Instance.Save(emitter)
                ? $"Updated {emitter.Describe()}"
                : $"FX emitter #{id} could not be saved; see the server log.");
        }

        #endregion

        private static string ClampComment(string comment)
        {
            return comment.Length > 96 ? comment.Substring(0, 96) : comment;
        }

        private void SpeedCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                SendCommandUsage(".speed");
                return;
            }
            if (parts.Length == 2)
            {
                if (double.TryParse(parts[1], out double speed))
                {
                    // The server's own figure as well as the client's. It only ever told the
                    // client, so the two disagreed about how fast the GM was: the movement check
                    // pays a Move out of a budget that refills at MovementSpeed, and a speed the
                    // server had never heard of would have been refused as fast as it was used.
                    _client.Player.MovementSpeed = speed;

                    // ToDO send on cell domain
                    _client.CallMethod(_client.Player.EntityId, new MovementModChangePacket(speed));
                }
            }
            return;
        }

        private void WhereCommand(string[] parts)
        {
            var position = _client.Movement.Position;
            var rotation = _client.Movement.ViewDirection.X;
            var mapId = _client.Player.MapContextId;

            CommunicatorManager.Instance.SystemMessage(_client, $"PosX = {position.X}\nPosY = "
                + $"{position.Y}\nPosZ = {position.Z}\nOrientation = {rotation}"
                + $"\nMapId = {mapId}");

            // Logged too, so a live position can be read off the server console/log without the
            // player needing to relay it or log out (position otherwise only persists to the
            // character row on logout/map-exit).
            Logger.WriteLog(LogType.Command,
                $"[.where] {_client.Player.FamilyName}: map={mapId} pos=({position.X:0.####}, {position.Y:0.####}, {position.Z:0.####}) rot={rotation:0.####}");

            return;
        }

        #endregion

        /// <summary>A slash command the client sends as PrivilegedCommand, and the account level it takes.</summary>
        private sealed class PrivilegedChatCommand
        {
            public PrivilegedChatCommand(GmLevel level, Action<Client, string> handler)
            {
                Level = level;
                Handler = handler;
            }

            public GmLevel Level { get; }
            public Action<Client, string> Handler { get; }
        }

        private static readonly Dictionary<string, PrivilegedChatCommand> PrivilegedCommands = new Dictionary<string, PrivilegedChatCommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["gotomap"] = new PrivilegedChatCommand(GmLevel.GameMaster, GmMapCommands.GotoMap),
            ["gotostartgroup"] = new PrivilegedChatCommand(GmLevel.GameMaster, GmMapCommands.GotoStartGroup),
            ["killmap"] = new PrivilegedChatCommand(GmLevel.Admin, GmMapCommands.KillMap),
            ["usermissions"] = new PrivilegedChatCommand(GmMissionCommands.Level, GmMissionCommands.ShowUserMissions),
            ["givemission"] = new PrivilegedChatCommand(GmMissionCommands.Level, GmMissionCommands.GiveMission)

            // "getservercollisiondata" is intentionally not registered: it asks for
            // ServerCollisionData, which the retail client cannot load and this server has no
            // shapes for. See ServerCollisionDataPacket.
        };

        /// <summary>
        /// A slash command the client has no handler of its own for: client/communicator.py's
        /// ProcessSlashCommand sends whatever is not a local command as (command, arg), and the
        /// client's GM pickers send their picks the same way. The ones the server knows are in
        /// <see cref="PrivilegedCommands"/>, each with the level it takes; anything else, or one
        /// above the account's level, is answered as the dot commands answer it.
        /// </summary>
        internal void PrivilegedCommand(Client client, PrivilegedCommandPacket packet)
        {
            if (client?.Player == null || string.IsNullOrWhiteSpace(packet.Command))
                return;

            var command = packet.Command.Trim();

            if (!PrivilegedCommands.TryGetValue(command, out var registered))
            {
                Logger.WriteLog(LogType.Command, $"Invalid slash command: /{command} {packet.Args}");
                CommunicatorManager.Instance.SystemMessage(client, $"Unknown command: /{command}");
                return;
            }

            if (!HasLevel(client, registered.Level))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry?.Id} (level {client.AccountEntry?.Level}) tried to use /{command}, which needs {(byte)registered.Level}");

                CommunicatorManager.Instance.SystemMessage(client,
                    client.AccountEntry?.Level > 0
                        ? $"/{command} needs account level {(byte)registered.Level}; yours is {client.AccountEntry.Level}."
                        : $"Unknown command: /{command}");
                return;
            }

            Logger.WriteLog(LogType.Command, $"AccountId = {client.AccountEntry.Id}: /{command} {packet.Args}");
            registered.Handler(client, packet.Args ?? "");
        }
    }
}
