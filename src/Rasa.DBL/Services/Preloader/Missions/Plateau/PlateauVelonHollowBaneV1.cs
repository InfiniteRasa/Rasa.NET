using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Velon Hollow (squad instance 2136, client map adv_foreas_howlingmaw_deathburrow) for Elder Hundra's
    /// Ancient Mysteries (1978) and Outpost Cmdr. Russ's Mountain Spring Toxins (1979): the Bane outpost's Bane
    /// at the instance's level, the Bane officers and the Mox who carry the Velon Artifacts, and Receptive
    /// Officer Markin, the other hand-in of 1978. Elder Hundra gets her mission package.
    ///
    /// Sourced: gamecontext 2136, levels 30-32. The server's outpost pools 531224 (412.5, 50.4, 109.8),
    /// 531225 (322.2, 34.5, 43.4) and 531226 (255.5, 50.9, -34.4) hold Howling Maw Bane at level 43-46 (531029
    /// Thrax Soldier, 531030 Thrax Grenadier, 531035 Miasma, 531031 Thrax Technician, 531032 Caretaker, 531033
    /// Thrax Commander, 531034 Shield Drone). 1978 objective 1 (missionobjective 20835/20836):
    /// "Recover the artifacts from Bane officers in Velon Hollow"; TaRapedia Ancient Mysteries: "Seem to be on
    /// all bosses in instance - Thrax leaders, Forean infected and a mox too", 0/10. Objectiveconversation
    /// 1978,3 on packages 1003 (Hundra: "I can't tell you how much this means to me") and 2515 (Markin's AFS
    /// "ship these off to High Command"); creaturenamelanguage 10515 "Receptive Officer Markin". Client map: the
    /// outpost's entrance ArchBaneIndustrialEntranceV01 (358.7, 32.6, 51.9), its tunnels and the control room
    /// ArchBaneIndustrialRoomControl24mV14 (455, 32.6, 103).
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - The three outpost pools keep their places and counts, their creatures at level 31 (the user):
    ///    <see cref="Soldier"/>, <see cref="Grenadier"/> and <see cref="Miasma"/>, hit points and armor as the
    ///    Ustor Yard's level 31 Thrax (2914, 145), the attacks of the server's level 30 Pools rows (531089,
    ///    531090, 540004); the server's own level 31 Pools Technician, Caretaker and Shield Drone (531091,
    ///    531092, 531094); <see cref="Commander"/>, the Thrax Commander between the Plateau's (level 30, 6187,
    ///    280) and the Pools' (level 33, 8024, 310) at level 31 (6799, 290), the Plateau one's attacks. Down puts
    ///    the Howling Maw creatures back.
    ///  - <see cref="Officer"/>: a boss, level 32, three times the hit points of a level 32 Thrax (3177), the
    ///    level 33 Thrax's attacks, named 7682 "Thrax Infantry Warrant Officer" as Ustor's; two, each with two
    ///    soldiers (slot 2): one in the control room, one in the entrance tunnel. Respawn 600 s.
    ///  - <see cref="MoxBoss"/>: the Plateau's Mox Alpha Male (520053: class 22977, level 32, attacks, armor) at
    ///    three times its hit points, by the dirty pool west of the outpost. Respawn 600 s.
    ///  - Markin: class 3846 NPC_Human_Swapset_Male in Comm Officer Devinchy's outfit (510178), level 30, at New
    ///    Velon 4 m east of Hundra (the user: by her), on the navmesh's ground.
    ///  - The outpost's inside is not joined to its front on the navmesh (the entrance at 358, 46 and the tunnel
    ///    at 376, 69 are not linked): its Bane hold their ground. Playtest.
    ///
    /// Ids: creatures 750070-750074, 750079 (the commander) and 750078 (Markin), pools 750075-750077 and 750078 (Markin's, as his creature
    /// id), free before. Plain SQL, valid on SQLite and MySQL; down takes out only these rows and gives the three
    /// outpost pools their creatures back.
    /// </summary>
    public static class PlateauVelonHollowBaneV1
    {
        public const uint MapContextId = 2136;
        public const uint Plateau = PlateauMissionDataV1.MapContextId;

        public const uint Hundra = 510181, HundraPackage = 1003;
        public const uint Markin = 750078, MarkinPool = 750078, MarkinPackage = 2515, MarkinName = 10515, MarkinLevel = 30;
        public const uint MaleClass = 3846;             // NPC_Human_Swapset_Male
        public const uint OutfitOf = 510178;            // Comm Officer Devinchy - Wedge Rock Outpost

        public const uint ThraxClass = 20757, GrenadierClass = 9354, MiasmaClass = 6170, AlphaMoxClass = 22977, CommanderClass = 10504;
        public const uint ThraxName = 237, GrenadierName = 0, MiasmaName = 7908, OfficerName = 7682, MoxName = 8797, CommanderName = 10629;
        public const uint Soldier = 750070, Grenadier = 750071, Miasma = 750072, Officer = 750073, MoxBoss = 750074, Commander = 750079;
        public const uint Technician = 531091, Caretaker = 531092, ShieldDrone = 531094;   // Pools, level 31
        public const uint CommanderHitPoints = 6799, CommanderArmor = 290;
        public const uint CommanderKick = 53132, CommanderWeapon = 53133;       // Thrax Commander - Plateau
        public const uint ControlRoomPool = 750075, TunnelPool = 750076, MoxPool = 750077, Guards = 2;
        public const uint BossRespawnSeconds = 600;

        public const uint Level = 31, HitPoints = 2914, Armor = 145;
        public const uint BossLevel = 32, OfficerHitPoints = 3177 * 3, OfficerArmor = 150;
        public const uint MoxHitPoints = 3700 * 3, MoxArmor = 360;
        public const uint ThraxKick30 = 53137, ThraxWeapon30 = 53138;           // Thrax Soldier - Pools (level 30)
        public const uint GrenadierKick30 = 53139, GrenadierWeapon30 = 53140;   // Thrax Grenadier - Pools
        public const uint MiasmaTouch30 = 55005, MiasmaCloud30 = 68089, MiasmaBurst30 = 56011;   // Miasma - Pools
        public const uint ThraxKick33 = 53068, ThraxWeapon33 = 53069;           // Thrax Soldier - Marshes (level 33)
        public const uint MoxBite = 77004, MoxCharge = 57001;                   // Mox Alpha Male - Plateau

        /// <summary>The outpost's pools and the Howling Maw creatures they held: (pool, slot, old creature, new).</summary>
        public static readonly (uint Pool, int Slot, uint Old, uint New)[] Outpost =
        {
            (531224, 1, 531029, Soldier), (531224, 2, 531030, Grenadier), (531224, 3, 531031, Technician),
            (531224, 4, 531032, Caretaker), (531224, 5, 531033, Commander), (531224, 6, 531034, ShieldDrone),
            (531225, 1, 531029, Soldier), (531225, 2, 531035, Miasma), (531225, 3, 531031, Technician), (531225, 4, 531032, Caretaker),
            (531226, 1, 531029, Soldier), (531226, 2, 531030, Grenadier), (531226, 3, 531031, Technician), (531226, 4, 531032, Caretaker),
        };

        public static readonly (double X, double Y, double Z) ControlRoomAt = (450, 32.9, 110);
        public static readonly (double X, double Y, double Z) TunnelAt = (376, 34.3, 69);
        public static readonly (double X, double Y, double Z) MoxAt = (216, 36.69, -13);
        public static readonly (double X, double Y, double Z) MarkinAt = (343.5, 364.57, 643.2);

        public static IEnumerable<string> Up()
        {
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Hundra}, {HundraPackage}, 'Elder Hundra');";

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Markin}, 'Receptive Officer Markin - New Velon Village', {MaleClass}, 1, {MarkinLevel}, 1000, {MarkinName}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Markin}, slot_id, Class_id, color FROM creature_appearance WHERE id = {OutfitOf};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Markin}, {MarkinPackage}, 'Receptive Officer Markin');";
            yield return Pool(MarkinPool, MarkinAt.X, MarkinAt.Y, MarkinAt.Z, Plateau, Markin, 1, 1, 20, 0);

            yield return Creature(Soldier, "Thrax Soldier - Velon Hollow", ThraxClass, Level, HitPoints, ThraxName, ThraxKick30, ThraxWeapon30);
            yield return Stat(Soldier, HitPoints, Armor);
            yield return Creature(Grenadier, "Thrax Grenadier - Velon Hollow", GrenadierClass, Level, HitPoints, GrenadierName,
                GrenadierKick30, GrenadierWeapon30);
            yield return Stat(Grenadier, HitPoints, Armor);
            yield return Creature(Miasma, "Miasma - Velon Hollow", MiasmaClass, Level, HitPoints, MiasmaName,
                MiasmaTouch30, MiasmaCloud30, MiasmaBurst30);
            yield return Stat(Miasma, HitPoints, Armor);
            yield return Creature(Officer, "Bane Officer - Velon Hollow, Bane outpost", ThraxClass, BossLevel, OfficerHitPoints, OfficerName,
                ThraxKick33, ThraxWeapon33);
            yield return Stat(Officer, OfficerHitPoints, OfficerArmor);
            yield return Creature(MoxBoss, "Mox Alpha Male - Velon Hollow", AlphaMoxClass, BossLevel, MoxHitPoints, MoxName, MoxBite, MoxCharge);
            yield return Stat(MoxBoss, MoxHitPoints, MoxArmor);
            yield return Creature(Commander, "Thrax Commander - Velon Hollow", CommanderClass, Level, CommanderHitPoints, CommanderName,
                CommanderKick, CommanderWeapon);
            yield return Stat(Commander, CommanderHitPoints, CommanderArmor);

            foreach (var (pool, at) in new[] { (ControlRoomPool, ControlRoomAt), (TunnelPool, TunnelAt) })
            {
                yield return PoolSeconds(pool, at.X, at.Y, at.Z, MapContextId, Officer, 1, 1, BossRespawnSeconds, 4);
                yield return $"UPDATE spawnpool SET creature_2_Id = {Soldier}, creature_2_min_count = {Guards}, creature_2_max_count = {Guards} " +
                             $"WHERE id = {pool};";
            }
            yield return PoolSeconds(MoxPool, MoxAt.X, MoxAt.Y, MoxAt.Z, MapContextId, MoxBoss, 1, 1, BossRespawnSeconds, 0);

            foreach (var (pool, slot, old, replacement) in Outpost)
                yield return $"UPDATE spawnpool SET creature_{slot}_Id = {replacement} WHERE id = {pool} AND creature_{slot}_Id = {old};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (pool, slot, old, replacement) in Outpost)
                yield return $"UPDATE spawnpool SET creature_{slot}_Id = {old} WHERE id = {pool} AND creature_{slot}_Id = {replacement};";
            yield return $"DELETE FROM spawnpool WHERE id IN ({ControlRoomPool}, {TunnelPool}, {MoxPool}, {MarkinPool});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Soldier}, {Grenadier}, {Miasma}, {Officer}, {MoxBoss}, {Commander});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Hundra}, {Markin});";
            yield return $"DELETE FROM creature_appearance WHERE id = {Markin};";
            yield return $"DELETE FROM creature WHERE id IN ({Soldier}, {Grenadier}, {Miasma}, {Officer}, {MoxBoss}, {Commander}, {Markin});";
        }
    }
}
