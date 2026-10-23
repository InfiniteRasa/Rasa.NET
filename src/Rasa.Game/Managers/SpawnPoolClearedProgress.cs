using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// "Secure the outer Bane Outpost": an objective complete when some spawn pools of a squad's
    /// instance are all dead at once (MissionProgressEventKind.SpawnPoolCleared). Only a squad's
    /// instance keeps which of its pools are dead (SquadInstanceState), so only there is a pool
    /// cleared. The pools' own random counts are no matter: a pool is cleared when nothing of it
    /// is alive, queued or on its way.
    ///
    /// The players in the instance hear of a pool cleared on the spawn pools' next pass over the
    /// map (SpawnPoolManager.SpawnPoolWorker), after the kill that cleared it has been credited,
    /// not inside it. A player who comes into the instance after its pools died hears of every
    /// pool dead there on arriving (ManifestationManager); one who takes the mission there has
    /// them counted on taking it (MissionApplication's existing facts).
    /// </summary>
    internal static class SpawnPoolClearedProgress
    {
        /// <summary>Where the progress is recorded: the server's missions, or a test's.</summary>
        internal static Func<MissionApplication> Missions { get; set; } = () => MissionApplication.Instance;

        /// <summary>The pools of the instance cleared since the last pass: told to everyone in it.</summary>
        internal static void Announce(MapChannel map)
        {
            var cleared = map?.SquadState?.TakeNewlyCleared();

            if (cleared == null || cleared.Count == 0)
                return;

            // One that has come back since it died is not cleared any more.
            var pools = cleared.Where(pool => pool.ClearedAtUtcMs != 0 && pool.DbId != 0)
                .Select(pool => pool.DbId).Distinct().ToArray();

            if (pools.Length == 0)
                return;

            foreach (var client in map.ClientList.ToArray())
                Record(client, pools);
        }

        /// <summary>A player has come into a squad's instance: every pool dead there, for their missions.</summary>
        internal static void Arrived(Client client)
        {
            var pools = MissionApplication.ClearedSpawnPools(client);

            if (pools.Count > 0)
                Record(client, pools.OrderBy(pool => pool).ToArray());
        }

        private static void Record(Client client, uint[] pools)
        {
            var missions = Missions();

            if (missions == null || client?.Player == null)
                return;

            try
            {
                missions.RecordProgress(client, pools.Select(MissionProgressEvent.SpawnPoolCleared).ToArray());
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Cleared spawn pools {string.Join(", ", pools)}, character {client.Player.Id}: {e.Message}");
            }
        }
    }
}
