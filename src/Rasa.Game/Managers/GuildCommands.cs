using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Clan.Client;

    /// <summary>
    /// The client's guild slash commands, which are its clan commands under the name guilds had
    /// before they were clans. generated.client.slashcommand still lists them - /guildinvite,
    /// /addguild and /guinvite (InviteGuild), /guildkick and /gukick (KickGuild), /guildleave and
    /// /guleave (LeaveGuild), /guilddisband and /gudisband (DisbandGuild) - but communicator.py
    /// has no function by those names, so ProcessSlashCommand passes each one on as
    /// PrivilegedCommand(command, arg), with whatever was typed after it (nothing is required).
    /// They were answered "Unknown command".
    ///
    /// Each does what its clan command does, through the same ClanManager entry point, for the
    /// player's own clan (Manifestation.ClanId): /guildinvite is /claninvite, /guildkick
    /// /clankick, /guildleave /clanleave, /guilddisband /clandisband. These are a player's
    /// commands, not a game master's: they are not on the GM audit log.
    ///
    /// /clanleave and /clandisband ask in a window first (clan.py OnLeaveClan, OnDisbandClan);
    /// the server cannot open that window, so leaving or disbanding takes the command typed
    /// twice within <see cref="ConfirmSeconds"/> s. One that would be refused anyway - no clan,
    /// a leader leaving, anyone but the leader disbanding - is passed straight on, and the clan
    /// code refuses it as it refuses the window's.
    /// </summary>
    internal static class GuildCommands
    {
        /// <summary>How long a /guildleave or /guilddisband waits for the same command again.</summary>
        internal const int ConfirmSeconds = 15;

        /// <summary>The clock a confirmation is timed by; a test puts its own here.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        private static readonly Dictionary<string, Action<Client, string, string>> Commands = new Dictionary<string, Action<Client, string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["guildinvite"] = Invite,
            ["addguild"] = Invite,
            ["guinvite"] = Invite,
            ["guildkick"] = Kick,
            ["gukick"] = Kick,
            ["guildleave"] = Leave,
            ["guleave"] = Leave,
            ["guilddisband"] = Disband,
            ["gudisband"] = Disband,
        };

        /// <summary>A character's leave or disband waiting for its second command: which, and when it was asked.</summary>
        private static readonly Dictionary<uint, (string Kind, long At)> Pending = new Dictionary<uint, (string, long)>();
        private static readonly object PendingLock = new object();

        /// <summary>True when the command is one of the guild commands, run for this client.</summary>
        internal static bool TryRun(Client client, string command, string args)
        {
            if (client?.Player == null || command == null || !Commands.TryGetValue(command, out var run))
                return false;

            run(client, command.ToLowerInvariant(), (args ?? "").Trim());
            return true;
        }

        /// <summary>Forgets every pending confirmation (tests).</summary>
        internal static void Reset()
        {
            lock (PendingLock)
                Pending.Clear();

            Now = () => Environment.TickCount64;
        }

        private static void Invite(Client client, string command, string name)
        {
            if (name.Length == 0)
            {
                Say(client, $"usage: /{command} <family name>");
                return;
            }

            ClanManager.Instance.InviteToClanByName(client, new InviteToClanByNamePacket { FamilyName = name });
        }

        private static void Kick(Client client, string command, string name)
        {
            if (name.Length == 0)
            {
                Say(client, $"usage: /{command} <family name>");
                return;
            }

            ClanManager.Instance.KickPlayerFromClanByName(client, new KickPlayerFromClanByNamePacket { Name = name, ClanId = client.Player.ClanId });
        }

        private static void Leave(Client client, string command, string _)
        {
            var player = client.Player;
            var member = player.ClanId == 0 ? null : ClanManager.Instance.GetClanMember(player.ClanId, player.Id);

            if (member != null && member.Rank != ClanRank.Leader && !Confirmed(client, "leave", command, $"leave {ClanName(player.ClanId)}"))
                return;

            ClanManager.Instance.LeaveClan(client, new LeaveClanPacket { CharacterId = player.Id, ClanId = player.ClanId });
        }

        private static void Disband(Client client, string command, string _)
        {
            var player = client.Player;
            var member = player.ClanId == 0 ? null : ClanManager.Instance.GetClanMember(player.ClanId, player.Id);

            if (member != null && member.Rank == ClanRank.Leader && !Confirmed(client, "disband", command, $"disband {ClanName(player.ClanId)}"))
                return;

            ClanManager.Instance.DisbandClan(client, new DisbandClanPacket { ClanId = player.ClanId });
        }

        /// <summary>
        /// True on the second of two of the same kind within <see cref="ConfirmSeconds"/> s; on
        /// the first, the player is told to type it again.
        /// </summary>
        private static bool Confirmed(Client client, string kind, string command, string what)
        {
            var now = Now();
            var characterId = client.Player.Id;

            lock (PendingLock)
            {
                if (Pending.TryGetValue(characterId, out var pending) && pending.Kind == kind && now - pending.At <= ConfirmSeconds * 1000L)
                {
                    Pending.Remove(characterId);
                    return true;
                }

                Pending[characterId] = (kind, now);
            }

            Say(client, $"Type /{command} again within {ConfirmSeconds} seconds to {what}.");
            return false;
        }

        private static string ClanName(uint clanId) => ClanManager.Instance.ClanNameOf(clanId) ?? "your clan";

        private static void Say(Client client, string text) => CommunicatorManager.Instance.SystemMessage(client, text);
    }
}
