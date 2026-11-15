using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using static Rasa.Services.Preloader.DynamicDropshipsContent;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Ligo Crucible's control points as the Plateau's (<see cref="PlateauDynamicDropshipsContent"/>),
    /// the Pools' (<see cref="PoolsDynamicDropshipsContent"/>) and the Marshes' (<see cref="MarshesDynamicDropshipsContent"/>):
    /// an AFS garrison by AFS dropship at a point the AFS hold, timed Bane assaults by dropship on it, an
    /// AFS relief ship; AFS sorties against a point the Bane hold, which cannot take it. Both points hold
    /// at the start (World DB control_point default owner 1, ControlPointSeed): Fort Intrepid (CP 30) and
    /// Prometheus Outpost (CP 31). Both get a plan, as every point of the earlier zones.
    ///
    /// Ids: block 692300-692399 (World DB: free; the Plateau's 692001-692099, the Pools' 692101-692199,
    /// the Marshes' 692200-692299; not the Crucible quests' 770000-770099). AFS rows 692301-692303
    /// (creature, creature_action, creature_stat; creature_appearance 692301-692302); garrison pools and
    /// their control_point_link rows 692301-692317. Also, outside the block: the Thrax Commander's pool
    /// 520014 linked to Fort Intrepid as its Bane garrison (kind 1), Captain Tiersky's and Larai Zupa
    /// Madias's pools 510021 and 510029 linked to Prometheus Outpost as service pools (kind 7), and
    /// Executioner Derge's pool 520013 moved onto the rampart (see below), and Prometheus Outpost's turret
    /// pools 551265/551266 moved 1 m (see the garrison).
    ///
    /// Migration 20271109000000 sorts after the Crucible quests' (20271108000000-20271108002600) and the
    /// creature tints' (20271107*), so it may merge before or after them: the SQLite boot migrator
    /// (RasaDbContextBase.Initialize) runs Migrate(pending[i]) one by one, which rolls back every applied
    /// migration that sorts after the pending one (seen on a local server, 2026-11-09).
    ///
    /// Sources:
    ///  - the client names and makes items of Fort Intrepid's control point tokens only
    ///    (CP_Token_Cruci_CP01_Assault 26612 "Fort Intrepid Assault Token", CP_Token_Cruci_CP01_Defense
    ///    26638, each with an item template); the CP02 tokens are entity classes only (26613, 26639: no
    ///    name, no item template). So Fort Intrepid is taken as the zone's contested point (ESTIMATE) and
    ///    gets the shorter period; Prometheus Outpost, the zone's quest hub
    ///    (Captain Tiersky 510021, Larai Zupa Madias 510029, Major Keplinger 510031 are linked to it, kind
    ///    2), the Plateau's longer one. A Bane capture of the outpost hides those quest givers, as every
    ///    kind-2 link (ControlPoints), until players take it back. Tiersky and Madias have no vendor,
    ///    package or mission row upstream (ControlPoints.IsServiceNpc in ControlPointServices.cs is false), so an assault would count
    ///    them as garrison, kill them and hold them dead: they get service links (kind 7), which make them
    ///    service NPCs of the point (shut while the Bane hold it, never a defender);
    ///  - Fort Intrepid: upstream, Executioner Derge 520013 and the Thrax Commander 520014 (level 47, pools
    ///    of mode 0) stand on the point, unlinked; the AFS garrison would kill them at every spawn. "Is not
    ///    there when the Bane control the CP or when the AFS control the CP" (TaRapedia, 2008-01-28) is a
    ///    note of a bug that patch 1.4 fixed: after it Derge is there under either owner, on the ramparts
    ///    ("he spawns at 859 188 476 which is on top of a very tall wall", IGN / patch 1.4). So his pool
    ///    moves to (859, 188.68, 476), unlinked, the same UPDATE as the Crucible quests' (1241,
    ///    CrucibleMinefieldAndSalvageNpcsV1.DergeAt), so whichever comes first moves him and the other's is
    ///    a no-op; 125 m from the point and 18 m over it, out of the garrison's reach. The commander's pool
    ///    becomes the fort's Bane garrison (kind 1): it stands while the Bane hold the fort (ESTIMATE). Its
    ///    respawn goes from 50 s to 120 s, the fort's own Bane pool 531098's, so that the fort's capture
    ///    window (ControlPoints.ReturnMs, the shortest Bane pool respawn) stays 120 s as the other planned
    ///    points' 120-300 s (ESTIMATE); Down sets 50 s back;
    ///  - the attackers are "2 to 4 levels over the Zone average" (zerias): the Crucible's Bane are 42-43
    ///    (World DB rows 531018-531021). The World DB has no Thrax rows over 44 but the commanders: the
    ///    highest are the Howling Maw's (531029-531032, level 43-44) and its level 46 Thrax Commander
    ///    531033, so the waves are those, 1 level over the Crucible's (not 2-4), and the commander 3-4.
    ///    The Crucible has no Kael row (DB).
    /// Retail gives no timers, counts or places for this zone: they are ESTIMATE, as the Plateau's.
    /// Assaults run only while a player is on the map: an assault overdue by a whole period is put off
    /// to the first delay again (LandingZoneAssaults).
    /// </summary>
    public static class CrucibleDynamicDropshipsContent
    {
        public const uint MapId = 1993;
        public const uint FortIntrepid = 30, PrometheusOutpost = 31;

        public const uint IdMin = 692300, IdMax = 692399;

        // Level 43-44 Thrax and the level 46 commander (the Howling Maw's rows, World DB). No Kael, no
        // Shield Drones (see the Plateau's waves).
        public const uint ThraxSoldier = 531029, ThraxGrenadier = 531030, ThraxTechnician = 531031,
            Caretaker = 531032, ThraxCommander = 531033;

        public const uint Rifleman = 692301, MachineGunner = 692302, Mech = 692303;

        // The Pools' wave table (ESTIMATE): 28-33 Bane with the commander.
        private static readonly Troop[][][] Waves =
        {
            new[] { new[] { new Troop(ThraxSoldier, 3, 4), new Troop(ThraxGrenadier, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 3, 3), new Troop(ThraxGrenadier, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 2, 2), new Troop(Caretaker, 1, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 1, 1), new Troop(ThraxTechnician, 1, 1) } },
            new[] { new[] { new Troop(ThraxSoldier, 4, 4), new Troop(Caretaker, 1, 1), new Troop(ThraxGrenadier, 0, 1) },
                    new[] { new Troop(ThraxSoldier, 2, 3), new Troop(ThraxGrenadier, 2, 2), new Troop(ThraxTechnician, 1, 1) } }
        };

        // As the Plateau's, the Pools' and the Marshes' (ESTIMATE there): 3 riflemen and a machine gunner;
        // the sortie "a single AFS Mech and a fire team of 3-4 flunkies" (IGN Vault / planettr, 2007-12).
        private static readonly Troop[] Relief = { new(Rifleman, 3, 3), new(MachineGunner, 1, 1) };
        // The fire team one over retail's 3-4, 4 riflemen and a machine gunner (ESTIMATE; see the Marshes').
        private static readonly Troop[] SortieSquad = { new(Mech, 1, 1), new(Rifleman, 4, 4), new(MachineGunner, 1, 1) };

        private static LandingZoneAssaultContent.Drop Toward(Vector3 at, Vector3 point) =>
            new(at, Math.Atan2(-(point.X - at.X), -(point.Z - at.Z)));

        // The points' positions (World DB control_point).
        private static readonly Vector3 FortAt = new(757.066f, 171.121f, 549.128f), OutpostAt = new(400.142f, 170.353f, 387.916f);

        // Fort Intrepid: first assault 15 min after the start, then every 30 min (ESTIMATE: the Pools' and
        // the Marshes' Research Point's period, for the zone's contested point). Prometheus Outpost: first
        // assault after 37.5 min, then every 45 min (ESTIMATE, the Plateau's: the first delays as the
        // Plateau's two AFS points, about 22 min apart; the longer period for the quest hub). Sorties: 7.5
        // min after the Bane take a point, then every 30 min (ESTIMATE, the Marshes').
        public static readonly Assault[] Assaults =
        {
            new()
            {
                MapId = CrucibleDynamicDropshipsContent.MapId, PointId = FortIntrepid, FirstDelaySeconds = 900, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 1800, WaveSeconds = 90, LifetimeSeconds = 900,
                // The control point is a walled Bane camp (client ArchBaneOutpostWallMinor*, x 711-783, z 511-583,
                // entrances at (751, 169, 518) and (751, 169, 576)) in the ruins of the fort (client
                // ArchHumFortWall*/ArchHumanBaseWall*, x 664-866, z 452-692). Drop 1 lands south of the camp,
                // in the line of its south entrance by the fort's south wall, 66 m from the point; drop 2 west of
                // it, between the camp's west wall and the fort's, 60 m from the point (the troops go round to an
                // entrance). Not north or
                // east: the twenty AFS corpses of The Departed (1143) lie there, and the Fithik pools 531117 and
                // 531118 (764, 174, 627) and (822, 174, 628); 59 m and more from the corpses, 62 m from the
                // Bane Smart Mines west of the fort (1232: (635, 168, 557)), 31 m and 48 m from the nearest
                // Bane mortar mount (BaneMortars 592281-592284). The point has no AFS turrets. Places ESTIMATE,
                // on the navmesh.
                Drops = new[] { Toward(new(747.1f, 168.4f, 484.1f), FortAt), Toward(new(697.1f, 168.3f, 549.1f), FortAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // Outside the camp's south entrance, 41 m from the point, 21 m and more from the mortar mounts and
                // 25 m from drop 1 (ESTIMATE). Not the fort's own pad (client ArchHumDropshipLandingpadV02 at
                // (868, 168, 618)): 131 m out, and an assault goes for the relief where it lands
                // (LandingZoneBreach.Order). The post in the camp, 15 m south of the point (ESTIMATE).
                ReliefPad = Toward(new(750.0f, 168.5f, 509.0f), FortAt), ReliefPost = new(757.1f, 168.5f, 534.1f)
            },
            new()
            {
                MapId = CrucibleDynamicDropshipsContent.MapId, PointId = PrometheusOutpost, FirstDelaySeconds = 2250, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 2700, WaveSeconds = 90, LifetimeSeconds = 900,
                // The outpost's fence (client ArchHumOutpostFence*) closes it to the south (z 340-352, the gate
                // ArchHumOutpostFenceGateV01 at (392, 168, 340)), lava walls close it to the north
                // (TerraAriekiLigoLavaWall*), the ground falls away east and west (no navmesh). The way in is
                // the yard south of the fence, down to the lava river (TerraAriekiLavaRiver*, z 240-305) and
                // its bridge (410, 166, 286): drop 1 there, south of the gate, drop 2 south-east of the fence;
                // each 91-92 m from the point, 40-41 m from the nearest fence turret (ControlPointTurrets), 15 m
                // and more from the lava, 70 m and more from the Flaregasher pool 540142 (445, 167, 254).
                // Places ESTIMATE, on the navmesh.
                Drops = new[] { Toward(new(390.1f, 168.5f, 297.9f), OutpostAt), Toward(new(470.1f, 167.7f, 327.9f), OutpostAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // No pad at the outpost (client map): the yard inside the gate, 26 m from the point and 19 m and
                // more from the quest givers; the post 15 m east of the point, off the givers' spots (ESTIMATE).
                ReliefPad = Toward(new(405.1f, 168.3f, 362.9f), OutpostAt), ReliefPost = new(415.1f, 168.7f, 387.9f)
            }
        };

        /// <summary>The AFS garrison pools: id, point, what, where. All come and come back by AFS dropship.</summary>
        public static readonly (uint Id, uint Point, Troop Troop, Vector3 At, int RespawnTenths)[] Garrison =
        {
            // Fort Intrepid: 10 riflemen, 2 machine gunners, 4 mechs, as the Pools' Retread Outpost: it has
            // no AFS turrets (ControlPointTurrets has no mounts for CP 30; its mounts are the Bane mortars).
            // Places inside the camp's walls, 8 m and more from the point and from the hospital (731, 168,
            // 567) and the token banker (731, 168, 525); ESTIMATE, on the navmesh.
            (692301, FortIntrepid, new(Rifleman, 3, 3), new(737.1f, 168.5f, 539.1f), 3000),
            (692302, FortIntrepid, new(Rifleman, 3, 3), new(772.1f, 168.4f, 549.1f), 3000),
            (692303, FortIntrepid, new(Rifleman, 2, 2), new(762.1f, 168.3f, 524.1f), 3000),
            (692304, FortIntrepid, new(Rifleman, 2, 2), new(752.1f, 168.5f, 569.1f), 3000),
            (692305, FortIntrepid, new(MachineGunner, 1, 1), new(727.1f, 168.5f, 544.1f), 3000),
            (692306, FortIntrepid, new(MachineGunner, 1, 1), new(777.1f, 168.7f, 534.1f), 3000),
            (692307, FortIntrepid, new(Mech, 1, 1), new(767.1f, 168.3f, 524.1f), 6000),
            (692308, FortIntrepid, new(Mech, 1, 1), new(742.1f, 168.7f, 564.1f), 6000),
            (692309, FortIntrepid, new(Mech, 1, 1), new(777.1f, 169.1f, 559.1f), 6000),
            (692310, FortIntrepid, new(Mech, 1, 1), new(722.1f, 168.8f, 549.1f), 6000),
            // Prometheus Outpost: 8 riflemen, 2 machine gunners, 2 mechs, as a Plateau AFS point and the
            // Marshes' Research Point: four AFS turrets stand at its fence corners (control_point_link kind 2,
            // 551263-551269). ControlPointTurrets seeds six spots for CP 31 (pools 551263-551274); FenceTurretBases removes
            // the 5th and 6th (551271-551274) and moves the 3rd and 4th (551267-551270) onto the fence's bases; the
            // first two (551263-551266) stand where ControlPointTurrets put them, at the fence corners, so four
            // stand. The second of them (AFS 551265, Bane 551266, upstream ControlPointTurrets, PR #158) stood at
            // (416.27, 177.40, 339.56) with its eye in a small closed box of the cover mesh (floor 177.15, roof
            // 179.80): it saw nothing, gave up every fight and healed out of one, and the Bane turret could not
            // be killed after a Bane capture (playtest 2026-11-09 row 84, cover-mesh probe). Moved 1 m west and
            // 1 m south, to (415.27, 177.40, 338.56) on the wall top (177.10), where it sees 92 of 144 ground
            // points 10-60 m round it (ESTIMATE as to the best spot; CrucibleDynamicDropshipsTests). Places inside the fence, 9 m and more from the quest givers, the vendors and
            // the waypoint (World DB, both the upstream and the moved spots of Tiersky, Madias and Keplinger);
            // ESTIMATE, on the navmesh.
            (692311, PrometheusOutpost, new(Rifleman, 3, 3), new(390.1f, 168.4f, 357.9f), 3000),
            (692312, PrometheusOutpost, new(Rifleman, 3, 3), new(425.1f, 168.3f, 377.9f), 3000),
            (692313, PrometheusOutpost, new(Rifleman, 2, 2), new(365.1f, 168.5f, 392.9f), 3000),
            (692314, PrometheusOutpost, new(MachineGunner, 1, 1), new(410.1f, 168.4f, 357.9f), 3000),
            (692315, PrometheusOutpost, new(MachineGunner, 1, 1), new(430.1f, 168.3f, 397.9f), 3000),
            (692316, PrometheusOutpost, new(Mech, 1, 1), new(370.1f, 168.4f, 372.9f), 6000),
            (692317, PrometheusOutpost, new(Mech, 1, 1), new(420.1f, 168.3f, 407.9f), 6000)
        };

        /// <summary>The migration's rows, each insert only where its key is not there yet (DynamicDropshipsContent.Guarded): safe to run twice.</summary>
        public static IEnumerable<string> Up() => DynamicDropshipsContent.Guarded(Rows());

        /// <summary>The Thrax Commander's pool at Fort Intrepid (upstream, mode 0, on the point): the fort's Bane garrison.</summary>
        public const uint CommanderPool = 520014;

        /// <summary>The commander pool's respawn in tenths of a second: upstream 500, after Up 1200 (pool 531098's).</summary>
        public const int CommanderRespawnBefore = 500, CommanderRespawn = 1200;

        /// <summary>Executioner Derge's pool (upstream 520013) and its rampart spot, as CrucibleMinefieldAndSalvageNpcsV1.DergeAt.</summary>
        public const uint DergePool = 520013;
        public static readonly Vector3 DergeAt = new(859f, 188.68f, 476f);

        /// <summary>Prometheus Outpost's quest givers with no vendor, package or mission row upstream: Captain Tiersky, Larai Zupa Madias.</summary>
        public static readonly uint[] ServicePools = { 510021, 510029 };

        /// <summary>Prometheus Outpost's boxed-in turret spot (AFS 551265, Bane 551266) and where it goes.</summary>
        public static readonly uint[] BoxedTurretPools = { 551265, 551266 };
        public static readonly Vector3 BoxedTurretWas = new(416.27f, 177.40f, 339.56f), BoxedTurretAt = new(415.27f, 177.40f, 338.56f);

        /// <summary>The turret move, guarded on the place it leaves (each run moves only a pool still there): Up, or Down with the places swapped.</summary>
        public static string MoveTurret(Vector3 from, Vector3 to) =>
            $"UPDATE spawnpool SET pos_x = {N(Math.Round(to.X, 2))}, pos_z = {N(Math.Round(to.Z, 2))} WHERE id IN ({string.Join(", ", BoxedTurretPools)}) AND map_context_id = {MapId} " +
            $"AND abs(pos_x - {N(Math.Round(from.X, 2))}) < 0.01 AND abs(pos_y - {N(Math.Round(from.Y, 2))}) < 0.01 AND abs(pos_z - {N(Math.Round(from.Z, 2))}) < 0.01;";

        private static IEnumerable<string> Rows()
        {
            // Balance (ESTIMATE): the Marshes' AFS times the Howling Maw's Thrax over the Mires' (the
            // Marshes' waves): health 8179 / 4460 and damage 181-238 / 98-130, 1.83 each (World DB); armour
            // times 205 / 170. Rifle 277-412 -> 507-754, machine gun 170-277 -> 311-507, minigun 170-255 ->
            // 311-467; health 11960 -> 21887, mech 50310 -> 92067; armour 554 -> 668, mech 1166 -> 1406.
            // Levels 44 and 46: the Marshes' 37 and 39 plus the 7 levels from the Mires' Thrax to the Howling
            // Maw's. Looks and names as the Plateau's (classes 29423, 29433, 10235; names 8717, 8719, 10736).
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(692301,'Crucible AFS rifle',1,134,1,35,800,0,507,754,1)," +
                "(692302,'Crucible AFS machine gun',149,1,1,30,500,0,311,507,1)," +
                "(692303,'Crucible AFS mech minigun',1,224,1,30,400,0,311,467,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(692301,'Crucible control point AFS Rifleman',29423,1,44,21887,8717,7,2,692301,0,0,0,0,0,0,0)," +
                "(692302,'Crucible control point AFS Machine Gunner',29433,1,44,21887,8719,7,2,692302,0,0,0,0,0,0,0)," +
                "(692303,'Crucible control point AFS Mech Defender',10235,1,46,92067,10736,9,5,692303,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES " +
                "(692301,15,15,15,21887,668),(692302,15,15,15,21887,668),(692303,30,30,30,92067,1406);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (692301,13,27220,1),(692302,13,20535,1);";
            foreach (var pool in Garrison)
                yield return "INSERT INTO spawnpool (id,mode,anim_type,respown_time,pos_x,pos_y,pos_z,rotation,map_context_id," +
                    "creature_1_Id,creature_1_min_count,creature_1_max_count,creature_2_Id,creature_2_min_count,creature_2_max_count," +
                    "creature_3_Id,creature_3_min_count,creature_3_max_count,creature_4_Id,creature_4_min_count,creature_4_max_count," +
                    "creature_5_Id,creature_5_min_count,creature_5_max_count,creature_6_Id,creature_6_min_count,creature_6_max_count,radius) VALUES " +
                    $"({pool.Id},1,2,{pool.RespawnTenths},{N(pool.At.X)},{N(pool.At.Y)},{N(pool.At.Z)},0,{MapId},{pool.Troop.Creature},{pool.Troop.Min},{pool.Troop.Max}," +
                    "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3);";
            yield return "INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES " +
                string.Join(",", Garrison.Select(p => $"({p.Point},2,{p.Id})")) + ";";
            yield return $"INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES ({FortIntrepid},1,{CommanderPool});";
            yield return "INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES " +
                string.Join(",", ServicePools.Select(p => $"({PrometheusOutpost},7,{p})")) + ";";
            // Not an insert: the same values as the Crucible quests' (1241), so it is the same row either way.
            yield return $"UPDATE spawnpool SET pos_x = {N(859)}, pos_y = {N(188.68)}, pos_z = {N(476)} WHERE id = {DergePool};";
            // The commander's respawn as the fort's Bane pool 531098's (the Crucible quests leave the column alone).
            yield return $"UPDATE spawnpool SET respown_time = {CommanderRespawn} WHERE id = {CommanderPool};";
            yield return MoveTurret(BoxedTurretWas, BoxedTurretAt);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM control_point_link WHERE kind=2 AND object_id BETWEEN {IdMin} AND {IdMax};";
            yield return $"DELETE FROM control_point_link WHERE control_point_id={FortIntrepid} AND kind=1 AND object_id={CommanderPool};";
            yield return $"DELETE FROM control_point_link WHERE control_point_id={PrometheusOutpost} AND kind=7 AND object_id IN ({string.Join(",", ServicePools)});";
            yield return $"UPDATE spawnpool SET respown_time = {CommanderRespawnBefore} WHERE id = {CommanderPool};";
            yield return MoveTurret(BoxedTurretAt, BoxedTurretWas);
            // Derge stays on the rampart: the Crucible quests (1241) need him there, and with them applied
            // a move back would put him on the point again under their mission. Up's UPDATE is not undone.
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {IdMin} AND {IdMax};";
            yield return "DELETE FROM creature_appearance WHERE id BETWEEN 692301 AND 692303;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 692301 AND 692303;";
            yield return "DELETE FROM creature WHERE id BETWEEN 692301 AND 692303;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 692301 AND 692303;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
