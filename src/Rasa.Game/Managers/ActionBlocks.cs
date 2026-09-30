using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Actions a player may not perform, and why - and the client told, with ActionBlockChange, so
    /// it greys them out and refuses them itself instead of winding up for a refusal.
    ///
    /// Each action can be blocked for any number of reasons at once; it is blocked while it has
    /// one. The client counts blocks too (BlockAction / UnblockAction), so it is only told when an
    /// action goes from no reasons to some and back: its count is one or none, whatever the
    /// server's reasons. Its counts live on its manifestation, which a map change recreates, so
    /// <see cref="Resend"/> gives a newly placed manifestation every block again, and changes made
    /// while the player is out of the world are only recorded, to go out with that.
    ///
    /// AbilityManager.RequestPerformAbility refuses a blocked action whatever the client does.
    /// </summary>
    public static class ActionBlocks
    {
        /// <summary>An ability the player's skills grant that this server cannot perform yet.</summary>
        public const string Unimplemented = "not implemented on this server";

        /// <summary>Crab Mines with the player's three mines in play.</summary>
        public const string CrabMineLimit = "crab mine limit";

        /// <summary>A GM's .blockaction.</summary>
        public const string Gm = "GM";

        public static bool IsBlocked(Manifestation player, ActionId actionId)
        {
            return player != null && player.ActionBlocks.TryGetValue(actionId, out var reasons) && reasons.Count > 0;
        }

        /// <summary>The reasons an action is blocked for, empty when it is not.</summary>
        public static IReadOnlyCollection<string> ReasonsFor(Manifestation player, ActionId actionId)
        {
            return player != null && player.ActionBlocks.TryGetValue(actionId, out var reasons) ? reasons : (IReadOnlyCollection<string>)new string[0];
        }

        /// <summary>
        /// Adds or takes away one reason for one action, telling the client when that blocks or
        /// unblocks it. <paramref name="send"/> false records it only, for a caller that sends
        /// everything afterwards with <see cref="Resend"/>.
        /// </summary>
        public static void Set(Client client, ActionId actionId, string reason, bool blocked, bool send = true)
        {
            var player = client?.Player;

            if (player == null)
                return;

            var was = IsBlocked(player, actionId);

            if (blocked)
            {
                if (!player.ActionBlocks.TryGetValue(actionId, out var reasons))
                    player.ActionBlocks[actionId] = reasons = new HashSet<string>();

                reasons.Add(reason);
            }
            else if (player.ActionBlocks.TryGetValue(actionId, out var reasons))
            {
                reasons.Remove(reason);

                if (reasons.Count == 0)
                    player.ActionBlocks.Remove(actionId);
            }

            var now = IsBlocked(player, actionId);

            if (send && was != now && client.IsInWorld)
                client.CallMethod(player.EntityId, new ActionBlockChangePacket(actionId, now));
        }

        /// <summary>
        /// Makes <paramref name="actionIds"/> exactly the actions blocked for
        /// <paramref name="reason"/>: the rest lose that reason, these gain it.
        /// </summary>
        public static void SetAll(Client client, string reason, IEnumerable<ActionId> actionIds, bool send = true)
        {
            var player = client?.Player;

            if (player == null)
                return;

            var wanted = new HashSet<ActionId>(actionIds);
            var had = player.ActionBlocks.Where(e => e.Value.Contains(reason)).Select(e => e.Key).ToList();

            foreach (var actionId in had)
                if (!wanted.Contains(actionId))
                    Set(client, actionId, reason, false, send);

            foreach (var actionId in wanted)
                Set(client, actionId, reason, true, send);
        }

        /// <summary>
        /// Every blocked action, to a manifestation the client has just (re)created - on map entry,
        /// whose client starts with no blocks at all.
        /// </summary>
        public static void Resend(Client client)
        {
            var player = client?.Player;

            if (player == null)
                return;

            foreach (var entry in player.ActionBlocks)
                if (entry.Value.Count > 0)
                    client.CallMethod(player.EntityId, new ActionBlockChangePacket(entry.Key, true));
        }
    }
}
