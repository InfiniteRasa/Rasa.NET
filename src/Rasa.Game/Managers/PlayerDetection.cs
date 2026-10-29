using System;
using System.Linq;

namespace Rasa.Managers
{
    using Game;
    using Structures;

    /// <summary>
    /// "Avoid Detection While Uploading Virus" (1187 Silent But Deadly): a mission that reacts to
    /// the player being detected (MissionProgressEventKind.PlayerDetected). Detected is a creature
    /// of a spawn pool starting to fight the player (BehaviorManager.TrySetActionFighting): it
    /// noticed them in its aggro scan, or they hit it first - shooting a guard gives you away
    /// too. No sight check of its own: what keeps a creature from noticing keeps the player
    /// undetected - Stealth Armor's shorter range (Manifestation.DetectionRangePercent), Cloak
    /// Wave, a creature blinded by a mag flash (Detection).
    ///
    /// Shared with the player's squad as a kill is (GroupCreditService): in a squad's instance,
    /// one of them seen is the squad seen.
    /// </summary>
    internal static class PlayerDetection
    {
        /// <summary>Where the progress is recorded: the server's missions, or a test's.</summary>
        internal static Func<MissionApplication> Missions { get; set; } = () => MissionApplication.Instance;

        /// <summary>
        /// The creature has just turned on this target - a fight started, or a new target in
        /// one. A player, by a creature of a pool: they are detected by that pool. Shared from where
        /// the creature stands, as a kill is.
        /// </summary>
        internal static void Fighting(MapChannel map, Creature creature, ulong targetEntityId)
        {
            var pool = creature?.SpawnPool;

            if (map == null || pool == null || pool.DbId == 0)
                return;

            var client = map.ClientList.FirstOrDefault(entry => entry?.Player?.EntityId == targetEntityId);
            var missions = Missions();

            // Out of sight - a cloak a damage over time or a pull turned the creature on - is not seen.
            if (client == null || missions == null || Detection.UnnoticedByCreatures(client.Player))
                return;

            try
            {
                missions.Credit.Record(client, MissionProgressEvent.PlayerDetected(pool.DbId), creature.Position);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Detected by spawn pool {pool.DbId}, character {client.Player.Id}: {e.Message}");
            }
        }
    }
}
