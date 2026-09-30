using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The cooldowns the server holds (Actor.ActionReuseUntil, set by AbilityManager.StartCooldown)
    /// told to the client and kept across a logout.
    ///
    /// - On every arrival - login, map change, dropship - the running ones go to the client in
    ///   ActionReuseTimes. The client keeps its timers on its actor, which each map makes afresh,
    ///   so without this a player who changed maps with a five-minute wave cooling down saw it
    ///   ready and was refused it until the server's timer ran out.
    /// - On leaving the world (logout or a dropped connection) the ones with at least
    ///   MinSavedMs left are written to character_action_reuse, with the wall-clock time they
    ///   end; loading the character reads them back into ActionReuseUntil and removes the rows.
    ///   A relog no longer resets the hour-long account rewards, the class waves or Adrenaline Boost.
    /// </summary>
    public static class ActionReuse
    {
        /// <summary>Less than this left at logout is gone by the time anyone is back in; not saved.</summary>
        public const long MinSavedMs = 5000;

        /// <summary>
        /// The actor's cooldowns still running at <paramref name="nowTick"/>, with the time each
        /// has left. Read only: the ability requests on the client's own thread are what write
        /// the table, and a spent entry is as good as none to them.
        /// </summary>
        public static List<(ActionId ActionId, int RemainingMs)> Running(Actor actor, long nowTick)
        {
            return actor.ActionReuseUntil.ToArray()
                .Where(e => e.Value > nowTick)
                .Select(e => (e.Key, (int)Math.Min(e.Value - nowTick, int.MaxValue)))
                .OrderBy(r => r.Key)
                .ToList();
        }

        /// <summary>The player's running cooldowns, to their own client. Nothing is sent when there are none.</summary>
        public static void SendTo(Client client)
        {
            var player = client.Player;
            var running = Running(player, Environment.TickCount64);

            if (running.Count > 0)
                client.CallMethod(player.EntityId, new ActionReuseTimesPacket(running));
        }

        /// <summary>The rows to save for a character leaving the world at wall-clock <paramref name="nowUnixMs"/>.</summary>
        public static List<CharacterActionReuseEntry> ToSave(uint characterId, Actor actor, long nowTick, long nowUnixMs)
        {
            return Running(actor, nowTick)
                .Where(r => r.RemainingMs >= MinSavedMs)
                .Select(r => new CharacterActionReuseEntry(characterId, (uint)r.ActionId, nowUnixMs + r.RemainingMs))
                .ToList();
        }

        /// <summary>Saved rows back onto a freshly loaded character, those still running at <paramref name="nowUnixMs"/>.</summary>
        public static void Restore(Actor actor, IEnumerable<CharacterActionReuseEntry> rows, long nowTick, long nowUnixMs)
        {
            foreach (var row in rows)
            {
                var left = row.ReadyAt - nowUnixMs;

                if (left > 0)
                    actor.ActionReuseUntil[(ActionId)row.ActionId] = nowTick + left;
            }
        }

        /// <summary>Writes the cooldowns of a player leaving the world. Replaces whatever was saved before.</summary>
        public static void Save(Client client)
        {
            var player = client.Player;

            if (player == null || player.Id == 0)
                return;

            var rows = ToSave(player.Id, player, Environment.TickCount64, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // RemovePlayer goes on to hand the client back to character selection; a database
            // that cannot be reached costs the cooldowns, not that.
            try
            {
                using var unitOfWork = Server.GameUnitOfWorkFactory.CreateChar();
                unitOfWork.CharacterActionReuses.Replace(player.Id, rows);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Could not save the cooldowns of {player.FamilyName}: {e.Message}");
            }
        }
    }
}
