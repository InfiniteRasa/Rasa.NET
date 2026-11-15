using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using static Rasa.Services.Preloader.DynamicDropshipsContent;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Ligo Ashen Desert's control points as the Plateau's (<see cref="PlateauDynamicDropshipsContent"/>),
    /// the Pools' (<see cref="PoolsDynamicDropshipsContent"/>), the Marshes' (<see cref="MarshesDynamicDropshipsContent"/>)
    /// and the Crucible's (<see cref="CrucibleDynamicDropshipsContent"/>): an AFS garrison by AFS dropship at a point the
    /// AFS hold, timed Bane assaults by dropship on it, an AFS relief ship; AFS sorties against a point the Bane hold,
    /// which cannot take it. White Oasis Post (CP 17) holds at the start (World DB control_point default owner 1,
    /// ControlPointSeed); Bane Conscription Garrison (CP 16, default owner 0) gets its AFS garrison and a Bane
    /// counter-attack after a capture, as the Marshes' Bane Assertion Camp. Both get a plan, as every point of the
    /// earlier zones.
    ///
    /// Ids: block 692400-692499 (World DB: free; the Plateau's 692001-692099, the Pools' 692101-692199, the
    /// Marshes' 692200-692299, the Crucible's 692300-692399; not the Ashen quests' 790xxx). AFS rows 692401-692403
    /// (creature, creature_action, creature_stat; creature_appearance 692401-692402); garrison pools and their
    /// control_point_link rows 692401-692417. Also, outside the block: Lieutenant Dania Fontecilla's pool 510012
    /// linked to White Oasis Post as a service pool (kind 7, see below).
    ///
    /// Migration 20271114000000 sorts after the Crucible's dropships (20271109000000) and after every Ashen quest
    /// migration (20271110000000-20271110002200), so it may merge before or after them: the SQLite boot migrator
    /// (RasaDbContextBase.Initialize) runs Migrate(pending[i]) one by one, which rolls back every applied
    /// migration that sorts after the pending one (seen on a local server, 2026-11-03 and 2026-11-09).
    ///
    /// Sources:
    ///  - the two points: TaRapedia "Control Point" lists Bane Conscription Garrison and White Oasis Post for the
    ///    Ashen Desert; "Defending White Oasis Post" (YouTube, JuustoKakku, 2009-02-05) shows Bane waves on a
    ///    walled post with turrets; the client has CP01 and CP02 tokens for the zone (CP_Token_Ashen_CP01/CP02,
    ///    26614/26615, 26640/26641, each with an item template), so neither is singled out as the Crucible's fort was;
    ///  - White Oasis Post is the zone's quest hub: Captain Daris Linier 510007 and Lieutenant Dania Fontecilla
    ///    510012 are linked to it (kind 2), and the Ashen quests (PR #228) add Doctor Harold Meeks and Agent Hale
    ///    there (AshenWhiteOasisNpcsV1: pools 790400 (-498, 282.4, 164) and 790401 (-516, 282.8, 176), unlinked;
    ///    Fontecilla moved off the point's console to (-519, 283.71, 164), Linier 2 m to (-507.4, 282.73, 162.4)). Every one of them with a mission, a
    ///    package or a vendor is a service NPC of the point (ControlPoints.ServicePointOf: by its link, or, unlinked,
    ///    the nearest point within ServiceNpcRadius, 130 m): an assault never fights it
    ///    (LandingZoneAssaultTargeting.IsServiceTarget in BehaviorManager.MayFight) and never counts it as garrison;
    ///    a Bane capture hides it until players take the point back, as at the Crucible's Prometheus Outpost.
    ///    Upstream Fontecilla has no vendor, package or mission row (World DB), so an assault would count her as
    ///    garrison, kill her and hold her dead: she gets a service link (kind 7), as Tiersky and Madias at
    ///    Prometheus Outpost. Linier has a package (npc_package 1476), so he is a service NPC already. The hub gets
    ///    the longer period, as Prometheus Outpost;
    ///  - Bane Conscription Garrison: "Levels= 45+", a forcefield (TaRapedia, Conscription Garrison and
    ///    Updates/2008-03-07 (test)); upstream its Bane garrison is the Thrax pool 531033 on the point and the five
    ///    mortar mounts round its walls (kind 1, BaneMortars), all suspended while the AFS hold it;
    ///  - the attackers are "2 to 4 levels over the Zone average" (zerias): the Ashen Desert's Bane are 39-40
    ///    (World DB rows 531008-531011, commander 531012 level 42), so the waves are the level 41-42 Thrax of the
    ///    Abyss (531001-531004) and its level 44 Thrax Commander 531005, 2 levels over (the lowest choice in the
    ///    range; the Crucible's 42-43 and the Howling Maw's 43-44 would be 3 and 4). The zone has no Kael row (DB).
    /// Retail gives no timers, counts or places for this zone: they are ESTIMATE, as the Plateau's.
    /// Assaults run only while a player is on the map: an assault overdue by a whole period is put off
    /// to the first delay again (LandingZoneAssaults).
    /// </summary>
    public static class AshenDynamicDropshipsContent
    {
        public const uint MapId = 1734;
        public const uint ConscriptionGarrison = 16, WhiteOasisPost = 17;

        public const uint IdMin = 692400, IdMax = 692499;

        // Level 41-42 Thrax and the level 44 commander (the Abyss' rows, World DB). No Kael, no Shield
        // Drones (see the Plateau's waves).
        public const uint ThraxSoldier = 531001, ThraxGrenadier = 531002, ThraxTechnician = 531003,
            Caretaker = 531004, ThraxCommander = 531005;

        public const uint Rifleman = 692401, MachineGunner = 692402, Mech = 692403;

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

        // As the Plateau's, the Pools', the Marshes' and the Crucible's (ESTIMATE there): 3 riflemen and a machine
        // gunner; the sortie "a single AFS Mech and a fire team of 3-4 flunkies" (IGN Vault / planettr, 2007-12).
        private static readonly Troop[] Relief = { new(Rifleman, 3, 3), new(MachineGunner, 1, 1) };
        // The fire team one over retail's 3-4, 4 riflemen and a machine gunner (ESTIMATE; see the Marshes').
        private static readonly Troop[] SortieSquad = { new(Mech, 1, 1), new(Rifleman, 4, 4), new(MachineGunner, 1, 1) };

        private static LandingZoneAssaultContent.Drop Toward(Vector3 at, Vector3 point) =>
            new(at, Math.Atan2(-(point.X - at.X), -(point.Z - at.Z)));

        // The points' positions (World DB control_point).
        private static readonly Vector3 GarrisonAt = new(429.175f, 273.099f, -59.088f), OasisAt = new(-511.082f, 283.43f, 164.351f);

        // White Oasis Post: first assault 37.5 min after the start, then every 45 min (ESTIMATE: the Crucible's
        // quest hub Prometheus Outpost's, the Plateau's longer period). Bane Conscription Garrison: a captured
        // garrison is counter-attacked after 10 min (as the Plateau's and the Marshes' Bane points, TaRapedia "after
        // a delay"), then every 30 min (ESTIMATE: the Crucible's Fort Intrepid's and the Marshes' Research Point's,
        // for the zone's point that is not the hub). Sorties: 7.5 min after the Bane hold a point (the server start
        // for the garrison), then every 30 min (ESTIMATE, the Marshes').
        public static readonly Assault[] Assaults =
        {
            new()
            {
                MapId = AshenDynamicDropshipsContent.MapId, PointId = WhiteOasisPost, FirstDelaySeconds = 2250, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 2700, WaveSeconds = 90, LifetimeSeconds = 900,
                // The post's fence (client ArchHumOutpostFence*, x -533 to -441, z 114 to 218) has its gate on the
                // east side (ArchHumOutpostFenceGateV02 at (-441, 282, 158)), with the two AFS turrets on the fence
                // bases either side of it (World DB 551183/551185 at (-433.1, 288.3, 154) and (-433.1, 288.3, 178),
                // FenceTurretBases). Drop 1 east of the gate: the tester's spot A (-411.1, 280.3, 164.3; walked out
                // and back in the real client, NOTES-ashen-dropships-scout 2026-11-14) moved 11 m further east, so
                // that it lands 34 m and more from the turrets (spot A: 24 m; the Marshes' troops set down 16-18 m
                // from turrets went for them); 111 m from the point. Drop 2 north-east of the fence: the tester's
                // spot B as it is (walked out and back), 100 m from the point, 58 m from the turrets, 32 m from the
                // fence corner's unused mount (ControlPointTurrets 551187). No hostile pools within 130 m of the
                // post (World DB). Places ESTIMATE, on the navmesh, no wall within 8 m (the cover file).
                Drops = new[] { Toward(new(-400.0f, 280.0f, 165.0f), OasisAt), Toward(new(-440.4f, 283.8f, 235.1f), OasisAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // No pad at the post (client map): the open ground in the east of the yard, inside the gate, 52 m
                // from the point, 39 m and more from the quest givers (Linier and Fontecilla upstream and moved,
                // Meeks, Hale) and from the hospital and the token banker, 33 m from the post's waypoint 317
                // (-479.9, 281.5, 127.1; teleporter table) and 27 m from the gate turrets. Not the south-east of the
                // yard: 5 m from the waypoint, where players arrive (audit 2026-11-14). The relief post 15 m and more
                // from the givers, off the hospital (ESTIMATE). Every drop, pad, post and garrison post here and at the garrison: the zone
                // readiness probes (branch zone_readiness: PadWalls, no wall from 6 m in to the 2 m middle; ClientFloor.Walk,
                // the client's own floor walked to the point and back) pass, 2026-11-14.
                ReliefPad = Toward(new(-460.0f, 283.3f, 153.5f), OasisAt), ReliefPost = new(-487.0f, 281.6f, 153.5f)
            },
            new()
            {
                MapId = AshenDynamicDropshipsContent.MapId, PointId = ConscriptionGarrison, FirstDelaySeconds = 600, SortieFirstDelaySeconds = 450,
                PeriodSeconds = 1800, WaveSeconds = 90, LifetimeSeconds = 900,
                // The garrison is a walled Bane camp (client ArchBaneOutpostWallMinor*, x 386-470, z -92 to -23) with
                // a south entrance at (432, 272, -92) and a north one at (432, 272, -22.7) (ArchBaneOutpostWallMinor-
                // EntranceV01). Its Bane garrison's mortar mounts (BaneMortars, linked kind 1): 592197, 592198, 592200
                // and 592201 outside its four corners, 592196 about 120 m north-west of the point (592199 is not
                // linked). Drop 1 south of
                // the south entrance, 66 m from the point; drop 2 north-west of the camp, 85 m from it (the troops go
                // round to the north entrance); 33 m and more from every mortar mount. Places ESTIMATE, on the
                // navmesh, no wall within 8 m (the cover file).
                Drops = new[] { Toward(new(430.0f, 274.7f, -125.0f), GarrisonAt), Toward(new(380.0f, 279.7f, 10.0f), GarrisonAt) },
                Waves = Waves, Boss = ThraxCommander, Relief = Relief, Sortie = SortieSquad,
                // No pad at the camp (client map): the flat court inside the walls (the tester's ring scan: flat at
                // y 271.6-272.1 to 30 m and more), its west side, 29 m from the point and 47 m and more from the
                // hospital and the token banker. Not the tester's NE point (450.4, 271.8, -37.9): it is 7 m from the
                // token banker 500015 (an assault goes for the relief where it lands, LandingZoneBreach.Order). The
                // post 9 m west of the point (ESTIMATE).
                ReliefPad = Toward(new(400.0f, 271.8f, -55.0f), GarrisonAt), ReliefPost = new(420.0f, 272.0f, -60.0f)
            }
        };

        /// <summary>The AFS garrison pools: id, point, what, where. All come and come back by AFS dropship.</summary>
        public static readonly (uint Id, uint Point, Troop Troop, Vector3 At, int RespawnTenths)[] Garrison =
        {
            // White Oasis Post: 8 riflemen, 2 machine gunners, 2 mechs, as Prometheus Outpost and a Plateau AFS
            // point; two AFS turrets stand by its gate (control_point_link kind 2, 551183/551185). Places inside the
            // fence on open ground (the yard's middle has trenches and sandbags), 12 m and more from the quest
            // givers (Linier, both of Fontecilla's spots, Meeks, Hale) and 8 m and more from the hospital and the
            // token banker; ESTIMATE, on the navmesh. Not by the gate (-457, 283.6, 151): the client walk from there to the
            // point meets the yard's sandbag ring (ClientFloor.Walk, knee height at (-476.5, 282.4, 156.6)).
            (692401, WhiteOasisPost, new(Rifleman, 3, 3), new(-492.0f, 281.9f, 176.0f), 3000),
            (692402, WhiteOasisPost, new(Rifleman, 3, 3), new(-479.5f, 281.6f, 151.0f), 3000),
            (692403, WhiteOasisPost, new(Rifleman, 2, 2), new(-482.0f, 281.6f, 181.0f), 3000),
            (692404, WhiteOasisPost, new(MachineGunner, 1, 1), new(-487.0f, 282.0f, 178.5f), 3000),
            (692405, WhiteOasisPost, new(MachineGunner, 1, 1), new(-479.5f, 281.5f, 141.0f), 3000),
            (692406, WhiteOasisPost, new(Mech, 1, 1), new(-477.0f, 281.6f, 146.0f), 6000),
            (692407, WhiteOasisPost, new(Mech, 1, 1), new(-464.5f, 281.7f, 166.0f), 6000),
            // A captured Bane Conscription Garrison: 10 riflemen, 2 machine gunners, 4 mechs, as a captured
            // Plateau or Marshes Bane point: no AFS turrets there (ControlPointTurrets has no mounts for CP 16).
            // Places in the court inside the walls, 8 m and more from the point, the hospital (450.7, 271.6,
            // -67.6) and the token banker (457.2, 271.6, -40.9); ESTIMATE, on the navmesh.
            (692408, ConscriptionGarrison, new(Rifleman, 3, 3), new(410.0f, 271.7f, -40.0f), 3000),
            (692409, ConscriptionGarrison, new(Rifleman, 3, 3), new(440.0f, 271.8f, -35.0f), 3000),
            (692410, ConscriptionGarrison, new(Rifleman, 2, 2), new(415.0f, 271.8f, -80.0f), 3000),
            (692411, ConscriptionGarrison, new(Rifleman, 2, 2), new(440.0f, 271.8f, -80.0f), 3000),
            (692412, ConscriptionGarrison, new(MachineGunner, 1, 1), new(420.0f, 271.8f, -35.0f), 3000),
            (692413, ConscriptionGarrison, new(MachineGunner, 1, 1), new(455.0f, 271.7f, -55.0f), 3000),
            (692414, ConscriptionGarrison, new(Mech, 1, 1), new(430.0f, 271.8f, -80.0f), 6000),
            (692415, ConscriptionGarrison, new(Mech, 1, 1), new(430.0f, 271.7f, -40.0f), 6000),
            (692416, ConscriptionGarrison, new(Mech, 1, 1), new(445.0f, 271.7f, -40.0f), 6000),
            (692417, ConscriptionGarrison, new(Mech, 1, 1), new(410.0f, 271.8f, -75.0f), 6000)
        };

        /// <summary>White Oasis Post's quest giver with no vendor, package or mission row upstream: Lieutenant Dania Fontecilla.</summary>
        public static readonly uint[] ServicePools = { 510012 };

        /// <summary>
        /// White Oasis Post's quest givers and turn-ins: Captain Daris Linier 510007 and Lieutenant Dania Fontecilla
        /// 510012 (World DB, linked kind 2), their spots after the Ashen quests move them (AshenCantDrinkAshV1 moves
        /// Linier 2 m inside the post, AshenWhiteOasisNpcsV1 moves Fontecilla off the console), Doctor Harold Meeks
        /// 790400 and Agent Hale 790401 (the Ashen quests, PR #228, AshenWhiteOasisNpcsV1; not on this branch's World
        /// DB, so their places are given here).
        /// </summary>
        public static readonly (string Name, Vector3 At)[] QuestNpcs =
        {
            ("Captain Daris Linier 510007 (upstream)", new(-507.4f, 282.3f, 164.4f)),
            ("Captain Daris Linier 510007 (Ashen quests)", new(-507.4f, 282.73f, 162.4f)),
            ("Lieutenant Dania Fontecilla 510012 (upstream)", new(-511.1f, 281.8f, 164.4f)),
            ("Lieutenant Dania Fontecilla 510012 (Ashen quests)", new(-519.0f, 283.71f, 164.0f)),
            ("Doctor Harold Meeks 790400 (Ashen quests)", new(-498.0f, 282.4f, 164.0f)),
            ("Agent Hale 790401 (Ashen quests)", new(-516.0f, 282.8f, 176.0f))
        };

        /// <summary>The migration's rows, each insert only where its key is not there yet (DynamicDropshipsContent.Guarded): safe to run twice.</summary>
        public static IEnumerable<string> Up() => DynamicDropshipsContent.Guarded(Rows());

        private static IEnumerable<string> Rows()
        {
            // Balance (ESTIMATE): the Marshes' AFS times the Abyss' Thrax over the Mires' (the Marshes' waves):
            // health 6878 / 4460 and damage 152-200 / 98-130, 1.54 each (World DB); armour times 195 / 170. Rifle
            // 277-412 -> 427-634, machine gun 170-277 -> 262-427, minigun 170-255 -> 262-393; health 11960 -> 18418,
            // mech 50310 -> 77477; armour 554 -> 635, mech 1166 -> 1337. Levels 42 and 44: the Marshes' 37 and 39
            // plus the 5 levels from the Mires' Thrax to the Abyss'. Looks and names as the Plateau's (classes
            // 29423, 29433, 10235; names 8717, 8719, 10736).
            yield return "INSERT INTO creature_action (id,description,action_id,action_arg_id,range_min,range_max,cooldown,windup,min_damage,max_damage,damage_type) VALUES " +
                "(692401,'Ashen AFS rifle',1,134,1,35,800,0,427,634,1)," +
                "(692402,'Ashen AFS machine gun',149,1,1,30,500,0,262,427,1)," +
                "(692403,'Ashen AFS mech minigun',1,224,1,30,400,0,262,393,1);";
            yield return "INSERT INTO creature (id,comment,class_id,faction,level,max_hp,name_id,run_speed,walk_speed,action1,action2,action3,action4,action5,action6,action7,action8) VALUES " +
                "(692401,'Ashen control point AFS Rifleman',29423,1,42,18418,8717,7,2,692401,0,0,0,0,0,0,0)," +
                "(692402,'Ashen control point AFS Machine Gunner',29433,1,42,18418,8719,7,2,692402,0,0,0,0,0,0,0)," +
                "(692403,'Ashen control point AFS Mech Defender',10235,1,44,77477,10736,9,5,692403,0,0,0,0,0,0,0);";
            yield return "INSERT INTO creature_stat (id,body,mind,spirit,health,armor) VALUES " +
                "(692401,15,15,15,18418,635),(692402,15,15,15,18418,635),(692403,30,30,30,77477,1337);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) VALUES (692401,13,27220,1),(692402,13,20535,1);";
            foreach (var pool in Garrison)
                yield return "INSERT INTO spawnpool (id,mode,anim_type,respown_time,pos_x,pos_y,pos_z,rotation,map_context_id," +
                    "creature_1_Id,creature_1_min_count,creature_1_max_count,creature_2_Id,creature_2_min_count,creature_2_max_count," +
                    "creature_3_Id,creature_3_min_count,creature_3_max_count,creature_4_Id,creature_4_min_count,creature_4_max_count," +
                    "creature_5_Id,creature_5_min_count,creature_5_max_count,creature_6_Id,creature_6_min_count,creature_6_max_count,radius) VALUES " +
                    $"({pool.Id},1,2,{pool.RespawnTenths},{N(pool.At.X)},{N(pool.At.Y)},{N(pool.At.Z)},0,{MapId},{pool.Troop.Creature},{pool.Troop.Min},{pool.Troop.Max}," +
                    "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,3);";
            yield return "INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES " +
                string.Join(",", Garrison.Select(p => $"({p.Point},2,{p.Id})")) + ";";
            yield return "INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES " +
                string.Join(",", ServicePools.Select(p => $"({WhiteOasisPost},7,{p})")) + ";";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM control_point_link WHERE kind=2 AND object_id BETWEEN {IdMin} AND {IdMax};";
            yield return $"DELETE FROM control_point_link WHERE control_point_id={WhiteOasisPost} AND kind=7 AND object_id IN ({string.Join(",", ServicePools)});";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {IdMin} AND {IdMax};";
            yield return "DELETE FROM creature_appearance WHERE id BETWEEN 692401 AND 692403;";
            yield return "DELETE FROM creature_stat WHERE id BETWEEN 692401 AND 692403;";
            yield return "DELETE FROM creature WHERE id BETWEEN 692401 AND 692403;";
            yield return "DELETE FROM creature_action WHERE id BETWEEN 692401 AND 692403;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
