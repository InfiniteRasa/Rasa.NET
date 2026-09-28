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
            public ChatCommand(GmLevel level, Action<string[]> handler)
            {
                Level = level;
                Handler = handler;
            }

            public GmLevel Level { get; }
            public Action<string[]> Handler { get; }
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

        private static bool HasLevel(Client client, GmLevel required)
        {
            return client?.AccountEntry != null && client.AccountEntry.Level >= (byte)required;
        }

        public void RegisterCommand(string name, GmLevel level, Action<string[]> handler)
        {
            _commands.Add(name, new ChatCommand(level, handler));
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
            RegisterCommand(".missions", GmLevel.Observer, MissionInspectionCommand);
            RegisterCommand(".gm", GmLevel.Observer, EnterGmModCommand);
            RegisterCommand(".help", GmLevel.Observer, HelpGmCommand);
            RegisterCommand(".links", GmLevel.Observer, LinksCommand);
            RegisterCommand(".regions", GmLevel.Observer, RegionsCommand);
            RegisterCommand(".navmesh", GmLevel.Observer, NavMeshCommand);
            RegisterCommand(".near", GmLevel.Observer, NearCommand);
            RegisterCommand(".npcinfo", GmLevel.Observer, NpcInfoCommand);
            RegisterCommand(".rqs", GmLevel.Observer, RqsWindowCommand);
            RegisterCommand(".where", GmLevel.Observer, WhereCommand);

            // GameMaster: moves you, spawns and drives scenery and creatures, drives
            // your own client. A restart undoes all of it.
            RegisterCommand(".actorstate", GmLevel.GameMaster, ActorStateCommand);
            RegisterCommand(".bark", GmLevel.GameMaster, BarkCommand);
            RegisterCommand(".comehere", GmLevel.GameMaster, ComeHereCommand);
            RegisterCommand(".createobj", GmLevel.GameMaster, CreateObjectCommand);
            RegisterCommand(".createobjonloc", GmLevel.GameMaster, CreateObjectOnLocationCommand);
            RegisterCommand(".creature", GmLevel.GameMaster, CreateCreatureCommand);
            RegisterCommand(".creatureappearance", GmLevel.GameMaster, SetCreatureAppearanceCommand);
            RegisterCommand(".creatureloc", GmLevel.GameMaster, SetCreatureLocation);
            RegisterCommand(".deleteobj", GmLevel.GameMaster, DeleteObjectCommand);
            RegisterCommand(".error", GmLevel.GameMaster, ErrorCommand);
            RegisterCommand(".forcestate", GmLevel.GameMaster, ForceStateCommand);
            RegisterCommand(".heal", GmLevel.GameMaster, HealCommand);
            RegisterCommand(".link", GmLevel.GameMaster, LinkCommand);
            RegisterCommand(".minion", GmLevel.GameMaster, MinionCommand);
            RegisterCommand(".linkhere", GmLevel.GameMaster, LinkHereCommand);
            RegisterCommand(".kraftwerks", GmLevel.GameMaster, KraftwerksCommand);
            RegisterCommand(".region", GmLevel.GameMaster, RegionCommand);
            RegisterCommand(".notify", GmLevel.GameMaster, NotifyCommand);
            RegisterCommand(".msg", GmLevel.GameMaster, MessageCommand);
            RegisterCommand(".removeobj", GmLevel.GameMaster, RemoveObjectCommand);
            RegisterCommand(".rename", GmLevel.GameMaster, RenameCommand);
            RegisterCommand(".setkillstreak", GmLevel.GameMaster, SetKillStreakCommand);
            RegisterCommand(".setregion", GmLevel.GameMaster, SetRegionCommand);
            RegisterCommand(".speed", GmLevel.GameMaster, SpeedCommand);
            RegisterCommand(".tele", GmLevel.GameMaster, TeleCommand);
            RegisterCommand(".teleport", GmLevel.GameMaster, TeleportCommand);
            RegisterCommand(".teleup", GmLevel.GameMaster, TeleUpCommand);

            // Admin: hands out progression, changes who a player is, reloads server data.
            // A restart does not undo these.
            RegisterCommand(".addtitle", GmLevel.Admin, AddTitleCommand);
            RegisterCommand(".chg_class", GmLevel.Admin, ChangeClassCommand);
            RegisterCommand(".flag", GmLevel.Admin, FlagCommand);
            RegisterCommand(".givecredits", GmLevel.Admin, GiveCreditsCommand);
            RegisterCommand(".giveitem", GmLevel.Admin, GiveItemCommand);
            RegisterCommand(".givelogos", GmLevel.Admin, GiveLogosCommand);
            RegisterCommand(".givepads", GmLevel.Admin, GivePadsCommand);
            RegisterCommand(".givexp", GmLevel.Admin, GiveXpCommand);
            RegisterCommand(".failmission", GmLevel.Admin, FailMissionCommand);
            RegisterCommand(".failobjective", GmLevel.Admin, FailObjectiveCommand);
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
                CommunicatorManager.Instance.SystemMessage(
                    _client, "usage: .failmission missionId");
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
                CommunicatorManager.Instance.SystemMessage(
                    _client, "usage: .failobjective missionId objectiveId");
                return;
            }

            if (!Npcs.ObjectiveFailed(_client, missionId, objectiveId))
                CommunicatorManager.Instance.SystemMessage(
                    _client,
                    $"Mission {missionId} objective {objectiveId} is not active.");
        }

        private void AddTitleCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .addtitle titleId");
                return;
            }

            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out var titleId))
                {
                    _client.CallMethod(_client.Player.EntityId, new TitleAddedPacket(titleId));
                }
            }
        }

        private void ActorStateCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .actorstate stateId speed");
                return;
            }

            if (parts.Length == 3)
            {
                if (Enum.TryParse(parts[1], out CharacterState stateId))
                    if (double.TryParse(parts[2], out var speed))
                    {
                        _client.Player.State = stateId;
                        _client.Player.MovementSpeed = speed;
                        _client.CallMethod(_client.Player.EntityId, new ActorInfoPacket(_client.Player));
                    }
            }
        }

        private void BarkCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .bark creatureEntityId barkId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .comehere creatureEntityId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .creature dbId");
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
            // them. The seeded bots are already AFS; this covers spawning anything else.
            creature.Faction = Factions.AFS;

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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .createobj entityClassId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .createobjonloc entityClassId posX posY posZ orientation");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .deleteobj entityId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .forcestate entityId stateId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .giveitem itemTemplateId quantity");
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

        private void GiveLogosCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .givelogos logosId");
                return;
            }
            if (parts.Length == 2)
                if (uint.TryParse(parts[1], out uint logosId))
                    CharacterManager.Instance.UpdateCharacter(_client, CharacterUpdate.Logos, logosId);

            return;
        }

        private void GiveXpCommand(string[] parts)
        {
            if (parts.Length == 2)
            {
                if (uint.TryParse(parts[1], out uint xp))
                    ManifestationManager.Instance.GainExperience(_client, xp);
            }
            else
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .givexp ammount");

            return;
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

            CommunicatorManager.Instance.SystemMessage(client,
                $"Commands available at account level {client.AccountEntry.Level}:");

            foreach (var command in _commands.Where(c => HasLevel(client, c.Value.Level))
                                            .OrderBy(c => c.Value.Level)
                                            .ThenBy(c => c.Key))
                CommunicatorManager.Instance.SystemMessage(client, $"{command.Key} ({command.Value.Level})");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .removeobj entityId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .setkillstreak streakCount");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .tele posX posY posZ");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .teleport posX posY posZ mapId");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .teleup posY");
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
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .creatureappearance creatureEntityId slotId classId color");
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

        private static string ClampComment(string comment)
        {
            return comment.Length > 96 ? comment.Substring(0, 96) : comment;
        }

        private void SpeedCommand(string[] parts)
        {
            if (parts.Length == 1)
            {
                CommunicatorManager.Instance.SystemMessage(_client, "usage: .speed value");
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

        internal void PrivilegedCommand(Client client, PrivilegedCommandPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo: PrivilegedCommand");
        }
    }
}
