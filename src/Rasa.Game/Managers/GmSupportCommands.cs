using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;

    /// <summary>
    /// The commands a GM helping a player reaches for first: find them, go to them or bring them
    /// over, get them out of where they are stuck, stand them back up, look in their bags, and
    /// answer their petitions without the server console.
    ///
    ///  - .who [word]: who is in the world - name, level, class and map. A word narrows the list
    ///    to names and maps that have it.
    ///  - .playerinfo &lt;familyName | #characterId&gt;: one player: account, level, class, map,
    ///    instance, position, whether dead, GM level, and a silence if they have one.
    ///  - .goto / .summon &lt;familyName | #characterId&gt;: to them, or them to you: at once on the
    ///    same copy of a map, otherwise by the route a squad summon takes (SummonManager.MoveTo),
    ///    the loading screen, into their copy of the map.
    ///  - .unstuck &lt;familyName | #characterId&gt;: to the nearest hospital they can use on their map,
    ///    or the entrance of the squad instance they are in. A dead player is revived there, as
    ///    the hospital button does.
    ///  - .revive &lt;familyName | #characterId&gt;: up where they lie, at full health, with no death
    ///    penalties: a GM puts right a death that should not have happened.
    ///  - .inventory &lt;familyName | #characterId&gt; [backpack|equipped|lockbox|inbox]: what they hold.
    ///    Read only. Chat gets the first lines; the server console gets the lot.
    ///  - .petition list [open|resolved|cancelled|all] / show &lt;id&gt; / resolve &lt;id&gt; [what you did]:
    ///    the server console's petition command, in game.
    ///
    /// The player has to be in the world for all but .petition. Every one of these is a dot
    /// command, so GmAudit records it as it is typed.
    /// </summary>
    public static class GmSupportCommands
    {
        /// <summary>How many lines of a long list go to chat; the console gets them all.</summary>
        public const int ChatLines = 15;

        /// <summary>For tests: the clients looked in. Server.Clients when null.</summary>
        internal static Func<IReadOnlyList<Client>> OnlineClients;

        #region Finding players

        public static void Who(Client gm, string[] parts)
        {
            var word = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null;
            var players = Online()
                .Where(client => word == null
                    || DisplayName(client).Contains(word, StringComparison.OrdinalIgnoreCase)
                    || (MapName(client)?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(client => client.Player.FamilyName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (players.Count == 0)
            {
                Say(gm, word == null ? "Nobody is in the world." : $"Nobody in the world matches '{word}'.");
                return;
            }

            var lines = players.Select(client =>
                $"{DisplayName(client)}, {client.Player.Level} {ClassName(client)}, {MapName(client) ?? "?"}"
                + (client.Player.State == CharacterState.Dead ? ", dead" : "")).ToList();

            SayList(gm, $"{players.Count} in the world" + (word == null ? ":" : $" matching '{word}':"), lines, ".who");
        }

        public static void PlayerInfo(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".playerinfo <familyName or #characterId>");
            if (target == null)
                return;

            var player = target.Player;
            var account = target.AccountEntry;
            var map = player.MapChannel;
            var position = player.Position;
            var lines = new List<string>
            {
                $"{DisplayName(target)} (character {player.Id})",
                $"Account {account?.Name ?? "?"} ({account?.Id}), GM level {account?.Level ?? 0}",
                $"Level {player.Level} {ClassName(target)}, {(player.State == CharacterState.Dead ? "dead" : "alive")}",
                $"Map {MapName(target) ?? "?"} ({player.MapContextId})"
                    + (map != null && map.InstanceId > 1 ? $", instance {map.InstanceId}" : ""),
                $"At {position.X:0.#}, {position.Y:0.#}, {position.Z:0.#}",
            };

            if (account != null && account.MutedUntil > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                lines.Add($"Silenced until {DateTimeOffset.FromUnixTimeSeconds(account.MutedUntil):yyyy-MM-dd HH:mm} UTC");

            Say(gm, string.Join("\n", lines));
        }

        #endregion

        #region Moving players

        public static void Goto(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".goto <familyName or #characterId>");
            if (target == null || Refuse(gm, target, "go to yourself"))
                return;

            Move(gm, target);
        }

        public static void Summon(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".summon <familyName or #characterId>");
            if (target == null || Refuse(gm, target, "summon yourself"))
                return;

            Say(target, "A game master is bringing you to them.");
            Move(target, gm);
            Say(gm, $"Bringing {DisplayName(target)} to you.");
        }

        public static void Unstuck(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".unstuck <familyName or #characterId>");
            if (target == null)
                return;

            var player = target.Player;
            var map = player.MapChannel;

            if (map == null)
            {
                Say(gm, $"{DisplayName(target)} is between maps; try again when they arrive.");
                return;
            }

            // Dead: the hospital button does the rest, revive and all.
            if (player.State == CharacterState.Dead)
            {
                PlayerDeath.ReviveMe(target, null);
                Say(gm, $"{DisplayName(target)} was dead, and has been revived at a hospital.");
                return;
            }

            var entrance = InstanceEntrances.Of(map, player);
            var hospital = entrance == null
                ? Hospitals.Nearest(Hospitals.AvailableTo(player, map.MapInfo.MapContextId), player.Position)
                : null;

            if (entrance == null && hospital == null)
            {
                Say(gm, $"{MapName(target)} has no hospital {DisplayName(target)} can use. Try .summon, or /gotomap there yourself.");
                return;
            }

            var at = entrance?.Position ?? hospital.Position + new Vector3(0f, GmMapCommands.PadHeight, 0f);

            // As one who has just arrived through it: the way out of an instance does not fire
            // under them as they land in it.
            if (entrance != null)
                InstanceEntrances.StandIn(map, player, at);

            MoveWithinMap(target, at);

            var where = entrance != null ? "the instance entrance" : hospital.Name ?? $"hospital {hospital.TeleporterId}";
            Say(target, $"A game master has moved you to {where}.");
            Say(gm, $"{DisplayName(target)} moved to {where}.");
        }

        public static void Revive(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".revive <familyName or #characterId>");
            if (target == null)
                return;

            if (target.Player.State != CharacterState.Dead)
            {
                Say(gm, $"{DisplayName(target)} is not dead.");
                return;
            }

            if (!PlayerDeath.ReviveInPlace(target))
            {
                Say(gm, $"{DisplayName(target)} could not be revived.");
                return;
            }

            Say(target, "A game master has revived you.");
            Say(gm, $"{DisplayName(target)} revived where they lay, at full health, without penalties.");
        }

        /// <summary>
        /// <paramref name="traveller"/> to where <paramref name="destination"/> stands: at once when
        /// they are in the same copy of the same map, otherwise by the loading screen, as a squad
        /// summon goes (SummonManager.MoveTo).
        /// </summary>
        private static void Move(Client traveller, Client destination)
        {
            if (traveller.Player.MapChannel != null && traveller.Player.MapChannel == destination.Player.MapChannel)
                MoveWithinMap(traveller, destination.Player.Position);
            else
                SummonManager.MoveTo(traveller, destination);
        }

        /// <summary>
        /// Somewhere else on the map they are on, with no loading screen: the server's position,
        /// everyone's view of them, and the cells and map links around them, as a revive at a
        /// hospital moves a player. Someone frozen is held at the new spot.
        /// </summary>
        private static void MoveWithinMap(Client client, Vector3 at)
        {
            var player = client.Player;

            player.PlaceAt(at);
            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId,
                new Movement(at, client.Movement?.ViewDirection ?? new Vector2(0f, 0f))), false);
            CellManager.Instance.UpdateVisibility(client);
            MapLinkManager.Instance.PlacedOnMap(client);

            if (player.GmFrozenAt != null)
                player.GmFrozenAt = at;
        }

        #endregion

        #region Messages

        /// <summary>
        /// .message &lt;player&gt; [important ["title"]] &lt;text&gt;: a note they can't miss - the yellow
        /// ADMIN MESSAGE line, and the AFS Message notice they open to read it all (GmMessages);
        /// with "important" first, the modal dialog in its place, titled by the quoted words that
        /// follow if there are any.
        /// </summary>
        public static void Message(Client gm, string[] parts)
        {
            const string usage = ".message <familyName or #characterId> [important [\"title\"]] <text>";
            var target = Target(gm, parts, usage);
            if (target == null)
                return;

            var important = parts.Length > 3 && parts[2].Equals("important", StringComparison.OrdinalIgnoreCase);
            var text = string.Join(" ", parts.Skip(important ? 3 : 2)).Trim();
            string title = null;

            // important "Server restart" Please log out: the quoted words are the dialog's title.
            if (important && text.StartsWith("\""))
            {
                var close = text.IndexOf('"', 1);
                if (close < 0)
                {
                    Say(gm, $"The title needs a closing quote. Usage: {usage}");
                    return;
                }

                title = text.Substring(1, close - 1).Trim();
                text = text.Substring(close + 1).Trim();

                if (title.Length > GmMessages.MaxTitleLength)
                {
                    Say(gm, $"Titles can be at most {GmMessages.MaxTitleLength} characters.");
                    return;
                }
            }
            if (text.Length == 0 || text.Length > GmMessages.MaxLength)
            {
                Say(gm, text.Length == 0 ? "Usage: .message <familyName or #characterId> <text>" : $"Messages can be at most {GmMessages.MaxLength} characters.");
                return;
            }

            GmMessages.FromGm(target, text, important, title);
            Say(gm, $"{(important ? "Important message" : "Message")} sent to {DisplayName(target)}.");
        }

        #endregion

        #region Kill and freeze

        /// <summary>The client's net gun immobilize effect (NET_GUN_IMMOBILIZE_EFFECT): its OnAttach blocks movement.</summary>
        public const int FrozenEffectTypeId = CrowdControl.NetGunRootTypeId;

        /// <summary>The freeze marker's effect id: far above any the server hands out.</summary>
        public const int FrozenEffectId = 2_000_000_125;

        /// <summary>
        /// .kill [#entityId]: your target, or the creature named, takes a blow larger than its
        /// health and armour, from you, through ActorManager.Damage - so it dies as any creature
        /// does: loot, credit, its pool's respawn. An immune creature (invulnerable, leashing)
        /// takes nothing, and you are told. Creatures only; never a player.
        /// </summary>
        public static void Kill(Client gm, string[] parts)
        {
            var player = gm.Player;
            ulong entityId = player.Target;

            if (parts.Length > 1 && !ulong.TryParse(parts[1].TrimStart('#'), out entityId))
            {
                Say(gm, "Usage: .kill [#entityId] (no id: your target)");
                return;
            }

            if (entityId == 0)
            {
                Say(gm, "Target a creature first, or give its #entityId.");
                return;
            }

            if (!EntityManager.Instance.Creatures.TryGetValue(entityId, out var creature) || creature.MapContextId != player.MapContextId)
            {
                Say(gm, $"{entityId} is not a creature on your map. .kill is for creatures only.");
                return;
            }

            if (creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
            {
                Say(gm, "It is already dead.");
                return;
            }

            var health = creature.Attributes.TryGetValue(Attributes.Health, out var h) ? h.Current : 0;
            var armor = creature.Attributes.TryGetValue(Attributes.Armor, out var a) ? a.Current : 0;
            var blow = Math.Max(1, (health + armor) * 10);

            ActorManager.Instance.Damage(player.MapChannel, creature, blow, player, out var outcome, DamageType.Physical);

            Say(gm, creature.State == CharacterState.Dead || creature.State == CharacterState.Dying
                ? $"Killed {creature.Name ?? "creature"} ({entityId})."
                : outcome.Immune
                    ? $"{creature.Name ?? "It"} is immune right now (invulnerable or leashing)."
                    : $"{creature.Name ?? "It"} survived ({(creature.Attributes.TryGetValue(Attributes.Health, out var left) ? left.Current : 0)} health left).");
        }

        /// <summary>
        /// .freeze &lt;player&gt;: holds them where they stand. Their client is blocked from moving by
        /// the net gun's immobilize effect (which also puts its icon on their buff bar), the
        /// server refuses any move away (ManifestationManager), and they are told in a yellow
        /// ADMIN MESSAGE they can't turn off. .unfreeze lets them go. Not saved: logging out
        /// ends it.
        /// </summary>
        public static void Freeze(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".freeze <familyName or #characterId>");
            if (target == null || Refuse(gm, target, "freeze yourself"))
                return;

            if (target.Player.GmFrozenAt != null)
            {
                Say(gm, $"{DisplayName(target)} is already frozen. .unfreeze to let them go.");
                return;
            }

            target.Player.GmFrozenAt = target.Player.Position;
            ShowFrozen(target);
            CommunicatorManager.Instance.AdminMessage(target, "A game master has frozen you in place. Please wait; they will be with you shortly.");
            Say(gm, $"{DisplayName(target)} is frozen. .unfreeze {target.Player.FamilyName} to let them go.");
        }

        public static void Unfreeze(Client gm, string[] parts)
        {
            var target = Target(gm, parts, ".unfreeze <familyName or #characterId>");
            if (target == null)
                return;

            if (target.Player.GmFrozenAt == null)
            {
                Say(gm, $"{DisplayName(target)} is not frozen.");
                return;
            }

            target.Player.GmFrozenAt = null;
            if (target.Player.MapChannel != null)
                CellManager.Instance.CellCallMethod(target.Player.MapChannel, target.Player, new GameEffectDetachedPacket { EffectId = FrozenEffectId });
            CommunicatorManager.Instance.AdminMessage(target, "A game master has unfrozen you. You can move again.");
            Say(gm, $"{DisplayName(target)} can move again.");
        }

        /// <summary>
        /// A frozen player has arrived on a map - summoned, sent to a hospital, through a door:
        /// they are held where they now are, not where they were frozen, and their new client is
        /// told again, directly, as they may not be in the cells yet for the broadcast to reach.
        /// </summary>
        public static void ArrivedFrozen(Client client)
        {
            var player = client?.Player;
            if (player?.GmFrozenAt == null)
                return;

            player.GmFrozenAt = player.Position;
            client.CallMethod(player.EntityId, FrozenPacket(player));
            ShowFrozen(client);
        }

        /// <summary>
        /// A frozen player's immobilize effect, to everyone around them: on their own client it
        /// blocks their movement and shows on their buff bar, and at level 2 everyone sees the
        /// net on them (gameeffectdata.specialFX has (125, 2); level 1 has no visual). Sent again
        /// on every map they arrive on (EffectCarry.Restore).
        /// </summary>
        public static void ShowFrozen(Client client)
        {
            var player = client?.Player;
            if (player?.GmFrozenAt == null)
                return;

            if (player.MapChannel == null)
                return;

            CellManager.Instance.CellCallMethod(player.MapChannel, player, FrozenPacket(player));
        }

        internal static GameEffectAttachedPacket FrozenPacket(Structures.Manifestation player) => new GameEffectAttachedPacket
        {
            EffectTypeId = FrozenEffectTypeId,
            EffectId = FrozenEffectId,
            EffectLevel = 2,
            SourceId = player.EntityId,
            Announced = true,
            IsActive = true,
            IsDebuff = true
        };

        #endregion

        #region PAU

        /// <summary>
        /// .pau &lt;angel|vulcan|mech|grendel|orson&gt; / .pau off: you as that
        /// Personal Armored Unit, for 300 s or until you right-click its effect or .pau off.
        /// A test of the vehicles (AbilityManager.Paus): their weapons, and the AFS Mech's and
        /// Grendel's Rockets and Ground Slam.
        /// </summary>
        public static void Pau(Client gm, string[] parts)
        {
            var names = string.Join(", ", AbilityManager.Paus.Keys);

            if (parts.Length < 2)
            {
                Say(gm, $"Usage: .pau <{names}> or .pau off");
                return;
            }

            if (parts[1].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                Say(gm, AbilityManager.EndMorph(gm) ? "Out of the PAU." : "You are not in a PAU.");
                return;
            }

            if (!AbilityManager.Paus.TryGetValue(parts[1], out var variant))
            {
                Say(gm, $"No PAU '{parts[1]}'. One of: {names}.");
                return;
            }

            Say(gm, AbilityManager.Instance.GmMorph(gm, variant)
                ? $"You are a {variant.Name} for 5 minutes. Right-click its effect or .pau off to get out."
                : "You can't do that right now.");
        }

        #endregion

        #region Inventory

        private static readonly string[] Sections = { "backpack", "equipped", "lockbox", "inbox" };

        public static void ShowInventory(Client gm, string[] parts)
        {
            const string usage = ".inventory <familyName or #characterId> [backpack/equipped/lockbox/inbox]";
            var target = Target(gm, parts, usage);
            if (target == null)
                return;

            var section = parts.Length > 2 ? parts[2].ToLowerInvariant() : null;
            if (section != null && !Sections.Contains(section))
            {
                Say(gm, $"Usage: {usage}");
                return;
            }

            var inventory = target.Player.Inventory;
            var credits = target.Player.Credits.TryGetValue(CurencyType.Credits, out var c) ? c : 0;
            var prestige = target.Player.Credits.TryGetValue(CurencyType.Prestige, out var p) ? p : 0;

            var lines = new List<string>();

            foreach (var (name, items) in new[]
            {
                ("backpack", inventory.PersonalInventory),
                ("equipped", inventory.EquippedInventory.Concat(inventory.WeaponDrawer).ToList()),
                ("lockbox", inventory.HomeInventory),
                ("inbox", inventory.InboxItems),
            })
            {
                if (section != null && section != name)
                    continue;

                var held = items.Where(id => id != 0).Select(id => EntityManager.Instance.GetItem(id)).Where(item => item != null).ToList();
                lines.Add($"{name} ({held.Count}):");
                lines.AddRange(held.Select(item => "  " + Describe(item)));
            }

            SayList(gm, $"{DisplayName(target)}: {credits:N0} credits, {prestige:N0} prestige, {target.Player.LockboxCredits:N0} in the lockbox", lines, ".inventory");
        }

        internal static string Describe(Item item)
        {
            var name = EntityClassManager.Instance.GetClassInfo(item.ItemTemplate?.Class ?? 0)?.ClassName
                       ?? $"template {item.ItemTemplate?.ItemTemplateId}";

            return $"{name}{(item.StackSize > 1 ? $" x{item.StackSize}" : "")} (template {item.ItemTemplate?.ItemTemplateId}, id {item.EntityId})";
        }

        #endregion

        #region Petitions

        public static void Petition(Client gm, string[] parts)
        {
            const string usage = "Usage: .petition list [open/resolved/cancelled/all], show <id>, resolve <id> [what you did]";
            var action = parts.Length > 1 ? parts[1].ToLowerInvariant() : "list";

            switch (action)
            {
                case "list":
                {
                    PetitionStatus? status = PetitionStatus.Open;
                    if (parts.Length > 2)
                    {
                        if (parts[2].Equals("all", StringComparison.OrdinalIgnoreCase))
                            status = null;
                        else if (Enum.TryParse<PetitionStatus>(parts[2], true, out var named))
                            status = named;
                        else
                        {
                            Say(gm, usage);
                            return;
                        }
                    }

                    var petitions = PetitionManager.Instance.List(status, 50);
                    if (petitions.Count == 0)
                    {
                        Say(gm, status == null ? "No petitions have been filed." : $"No {status} petitions.");
                        return;
                    }

                    SayList(gm, $"{petitions.Count} petition(s), newest first" + (status == null ? ":" : $", {status}:"),
                        petitions.Select(petition =>
                            $"#{petition.Id} {(PetitionType)petition.Type} {petition.CreatedAt:MM-dd HH:mm} - {petition.Summary}").ToList(),
                        ".petition");
                    return;
                }

                case "show":
                {
                    if (parts.Length < 3 || !uint.TryParse(parts[2].TrimStart('#'), out var id))
                    {
                        Say(gm, usage);
                        return;
                    }

                    var petition = PetitionManager.Instance.Get(id);
                    if (petition == null)
                    {
                        Say(gm, $"There is no petition #{id}.");
                        return;
                    }

                    var filer = Online().FirstOrDefault(client => client.Player.Id == petition.CharacterId);
                    Say(gm,
                        $"Petition #{petition.Id} ({(PetitionType)petition.Type}, {(PetitionStatus)petition.Status})\n"
                        + $"Filed {petition.CreatedAt:yyyy-MM-dd HH:mm} UTC by character {petition.CharacterId}"
                        + (filer != null ? $" ({DisplayName(filer)}, in the world)" : "") + "\n"
                        + $"Map {petition.MapContextId} at {petition.PosX:0.#}, {petition.PosY:0.#}, {petition.PosZ:0.#}\n"
                        + $"{petition.Summary}\n{petition.Body}"
                        + (string.IsNullOrWhiteSpace(petition.Resolution) ? "" : $"\nAnswer: {petition.Resolution}"));
                    return;
                }

                case "resolve":
                {
                    if (parts.Length < 3 || !uint.TryParse(parts[2].TrimStart('#'), out var id))
                    {
                        Say(gm, usage);
                        return;
                    }

                    var note = string.Join(" ", parts.Skip(3));
                    Say(gm, PetitionManager.Instance.Resolve(id, note)
                        ? $"Petition #{id} resolved; the player has been told."
                        : $"Petition #{id} is not open, or does not exist.");
                    return;
                }

                default:
                    Say(gm, usage);
                    return;
            }
        }

        #endregion

        #region Helpers

        /// <summary>The player named in parts[1], by family name or #characterId; says why when there is none.</summary>
        private static Client Target(Client gm, string[] parts, string usage)
        {
            if (parts.Length < 2)
            {
                Say(gm, $"Usage: {usage}");
                return null;
            }

            var target = Find(parts[1]);
            if (target == null)
                Say(gm, $"{parts[1]} is not in the world.");

            return target;
        }

        internal static Client Find(string name)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
                return null;

            if (name.StartsWith("#"))
                return uint.TryParse(name.Substring(1), out var characterId)
                    ? Online().FirstOrDefault(client => client.Player.Id == characterId)
                    : null;

            return Online().FirstOrDefault(client =>
                string.Equals(client.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool Refuse(Client gm, Client target, string what)
        {
            if (target != gm)
                return false;

            Say(gm, $"You can't {what}.");
            return true;
        }

        private static IEnumerable<Client> Online()
        {
            IReadOnlyList<Client> clients;
            if (OnlineClients != null)
                clients = OnlineClients();
            else
                lock (Server.Clients)
                    clients = Server.Clients.ToList();

            return clients.Where(client => client?.State == ClientState.Ingame && client.Player != null);
        }

        /// <summary>A heading and up to <see cref="ChatLines"/> lines in chat; everything in the console.</summary>
        private static void SayList(Client gm, string heading, List<string> lines, string tag)
        {
            var shown = lines.Take(ChatLines).ToList();
            if (lines.Count > shown.Count)
                shown.Add($"... and {lines.Count - shown.Count} more in the server console.");

            Say(gm, heading + "\n" + string.Join("\n", shown));

            if (lines.Count > ChatLines)
            {
                Logger.WriteLog(LogType.Command, $"[{tag}] {heading}");
                foreach (var line in lines)
                    Logger.WriteLog(LogType.Command, $"[{tag}] {line}");
            }
        }

        private static string DisplayName(Client client) =>
            $"{client.Player.Name} {client.Player.FamilyName}".Trim();

        private static string ClassName(Client client) =>
            Enum.IsDefined(typeof(CharacterClass), client.Player.Class)
                ? ((CharacterClass)client.Player.Class).ToString()
                : $"class {client.Player.Class}";

        private static string MapName(Client client) => client.Player.MapChannel?.MapInfo?.MapName;

        private static void Say(Client client, string text) =>
            CommunicatorManager.Instance.SystemMessage(client, text);

        #endregion
    }
}
