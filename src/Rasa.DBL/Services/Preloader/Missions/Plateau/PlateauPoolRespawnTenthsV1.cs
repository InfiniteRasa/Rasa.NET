using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Plateau's hostile pools and their respawn: spawnpool.respown_time is in tenths of a second (SpawnPoolManager:
    /// RespawnTime * 100 ms), so each area's migration writes a respawn in seconds × 10 (PlateauCreatureSqlV1.PoolSeconds,
    /// <see cref="Tenths"/>), as upstream does for Wilderness (WildernessRespawns). This list is what the tests check
    /// them by.
    ///
    /// Not here: Zyrutha's and Kythura's pools, already in tenths (PlateauMaligoOfficerSquadRespawnV1, 600 = 60 s);
    /// the mission NPCs' pools written 20, which stay at 20 as a mission NPC's pool is (MissionNpcSpawnpoolPreloader,
    /// WildernessRespawns); the Giant Killer squad, whose npc_squad_spawn.respawn_seconds is in seconds (NpcSquads).
    /// </summary>
    public static class PlateauPoolRespawnTenthsV1
    {
        public const uint TenthsPerSecond = 10;

        /// <summary>Each pool, its respawn in seconds as its migration wrote it, and what it holds.</summary>
        public static readonly IReadOnlyList<(uint Pool, uint Seconds, string What)> Pools =
            PlateauMaligoGuardsV1.Packs.Select(pack => (pack.Id, PlateauMaligoGuardsV1.RespawnSeconds, "Maligo Elite Guards"))
                .Concat(PlateauBaneStalkersV1.Pairs.Select(pair => (pair.Id, PlateauBaneStalkersV1.RespawnSeconds, "Bane Stalkers")))
                .Concat(PlateauTombMiasmasV1.Pools.Select(pool => (pool.Id, PlateauTombMiasmasV1.RespawnSeconds, "Tomb Miasmas")))
                .Concat(PlateauInfectedForeansV1.Pools.Select(pool => (pool.Id, PlateauInfectedForeansV1.RespawnSeconds, "Infected Foreans")))
                .Append((PlateauPrototypeJuggernautV1.Pool, PlateauPrototypeJuggernautV1.RespawnSeconds, "Prototype Juggernaut"))
                .Concat(PlateauMoxLairV1.Pools.Select(pool => (pool.Id, PlateauMoxLairV1.RespawnSeconds, "Mox lair")))
                .Concat(PlateauSurveillanceSentinelsV1.Sentinels.Select(sentinel =>
                    (sentinel.Pool, PlateauSurveillanceSentinelsV1.RespawnSeconds, "Surveillance Sentinel")))
                .Concat(PlateauVelonElderBossesV1.All.Select(boss => (boss.Pool, PlateauVelonElderBossesV1.RespawnSeconds, boss.Name)))
                .Append((PlateauUstorYardV1.OfficerPool, PlateauUstorYardV1.OfficerRespawnSeconds, "Ustor Yard officer"))
                .Append((PlateauVelonHollowBaneV1.ControlRoomPool, PlateauVelonHollowBaneV1.BossRespawnSeconds, "Velon Hollow control room officer"))
                .Append((PlateauVelonHollowBaneV1.TunnelPool, PlateauVelonHollowBaneV1.BossRespawnSeconds, "Velon Hollow tunnel officer"))
                .Append((PlateauVelonHollowBaneV1.MoxPool, PlateauVelonHollowBaneV1.BossRespawnSeconds, "Velon Hollow Mox boss"))
                .ToArray();

        public static uint Tenths(uint seconds) => seconds * TenthsPerSecond;
    }
}
