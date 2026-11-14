using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 18 creatures and pads, Avernus Outpost's hive (squad instance 2110): Captain Tananda at the Cave In
    /// (Assessing The Atta 1536), Sgt. Frogga by Petrovich in the Food Storage chamber, Colonel Vandeweghe over the Queen's
    /// Chamber (God Save the Queen 1537), the Eloh Zuriel in the Eloh Sanctuary (The Big Secret 1581), the Bane in the Queen's
    /// Chamber (1537's "Sweep the Room") and two Atta Teleporters: the southern tunnel to the Queen's Chamber, the Food Storage's
    /// west side to the Eloh Sanctuary. The World DB has none of them.
    ///
    /// Ids (the Ashen block 790000-790999; set 17 took 790640-790649, set 18 790650-790658): 790650 Tananda, 790651 Frogga, 790652
    /// Vandeweghe (no pool), 790653 Zuriel (creature, look, stats, package, pool), 790654-790657 the Bane (a Caretaker, a
    /// Fithik, an Altered Miasma, a Thrax Tunneler: creature and stats) and their pool 790658. The pads: local teleporters 917/918 (the southern
    /// tunnel, the Queen's Chamber) and 919/920 (the Food Storage's west side, the Eloh Sanctuary), free in the World DB and the
    /// code (set 17's 915/916 the last).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9728 "Captain Tananda", 9731 "Sgt. Frogga", 9733 "Colonel Vandeweghe", 9676
    ///    "Zuriel", 9892 "Thrax Tunneler". Packages (client objectiveconversation, 1.0): 1914 Tananda (1536,2), 1925 Vandeweghe
    ///    (1537,3/5/7), 2075 Zuriel (1581,2). Frogga has none: a server-only package (his id), as set 17's Karter.
    ///  - Places (TaRapedia, /loc, 0.5): Tananda "Cave In" (467.4, 384.1, -97.5) (client map marker Cave In (465, 384, -98));
    ///    Vandeweghe "Queen's Chamber" (342.0, 231.8, -267.8); Zuriel (628.6, 276.0, -677.8) (the Eloh Sanctuary's obelisks and
    ///    Logos signs (628, 282-287, -660), client map). TaRapedia: 1536 "the northern 'Cave In' tunnel", 1537 "the southern tunnel
    ///    and ... the Atta teleporter there", 1581 "the west side of the Food Storage chamber and ... the Atta teleporter there. You
    ///    will find yourself at the entrance of an Eloh Sanctuary"; the Queen's Chamber holds "Caretakers, Fithik, Altered Miasmas,
    ///    and Thrax Tunnelers".
    ///  - The pads' look: the client's UsableTwoStateAttaTeleporter (class 25302), as set 17's; the client map places none.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Tananda copies Ida Hayner (the female AFS class), Frogga and Vandeweghe Captain Flynn Harris (AFS), Zuriel the Eloh Jumna
    ///    (760372, the hologram Eloh; the client has no other Eloh NPC with a body). Levels: Tananda and Frogga 44, Vandeweghe 46
    ///    (a colonel), Zuriel 50 (the set plan).
    ///  - Places on the client floor (AshenAvernusHiveTests): Tananda moved 55 m north of TaRapedia's place (that lies inside the
    ///    Atta pool 531051's circle) to the tunnel's end, the cave-in (449, 391.46, -45; the client floor and the navmesh within
    ///    0.3 m): the farthest floor from 531051 she is seen from all round at (20 m outside its circle: a rule break). Frogga 10.7 m north-east of Petrovich's top row (652, 353.32, -238).
    ///    Vandeweghe moved 10.5 m north-east of TaRapedia's place (6 m outside 531058's circle) up the ramp, to (349, 233.11, -260)
    ///    (13.6 m outside it: the best place seen from all round, a rule break). He has no public pool: only 1537's copy (an
    ///    actor, invulnerable) stands there. The ramp's tip is the only floor round TaRapedia's place, and no spot on it 15 m
    ///    (SpawnPoolManager.SafeClearance) outside 531058's circle is seen from all round (the probe, 2026-11-12): a public NPC
    ///    there would fail the start-up safe-ground check. Zuriel at TaRapedia's place.
    ///  - The Bane: copies of the Ashen Caretaker Doctor (531011), Fithik Guardian (540018) and Thrax Soldier (531008) and the
    ///    Polonium Miasma (531035, the nearest level), each its row's class, actions and stats, at 1537's level 43. Names: the
    ///    copies' own for the Caretaker and the Fithik; 9892 "Thrax Tunneler" for the Thrax; the client has no "Altered Miasma":
    ///    9037 "Mutated Miasma" (the nearest name). None has a warcry. One pool of the four, one each, circle 5 m, respawn 1200
    ///    tenths (the outpost's Atta pools'; in the squad's copy, the only place it stands, a cleared pool comes back 45 min after
    ///    the last of it died, SquadInstanceState's RespawnMinutes, not after 120 s), on the Queen's Chamber's west ledge (147, 194.11, -590): the client floor and the
    ///    navmesh agree there, and it is 100 m and more outside every Atta (warcry) pool's circle.
    ///  - The Atta Teleporters: two local teleporter pairs (as fix H's and set 17's), linked both ways, public (the client has no
    ///    gate on a local teleporter): 917 near the southern tunnel's end (509, 368.37, -506), 918 on the ramp over the Queen's
    ///    Chamber (340, 238.11, -252); 919 at the Food Storage's west side (470, 385.83, -324), 920 in the Eloh Sanctuary's entrance
    ///    corridor (628, 275.98, -639). The client floor has no walk from the hive to either (the probe's flood, 2026-11-12).
    ///  - The pad's body (prop_atta_teleporter_v01.geo, the probe, 2026-11-12): a platform 1.1-1.22 m over its origin (x within
    ///    1.25 m, z -2.5 to +2), its only way up a ramp on the south (-z) side down to about z -5.6; the other sides drop. All four
    ///    stand at rotation 0, and every walk to or from one goes up or down that ramp along its length (AshenAvernusHiveTests).
    ///    917 moved 4.3 m north-east of its first place (505, 369.87, -512) at the tunnel's end: the floor ends 1 m south of that
    ///    (the endcap), so its ramp met the wall; at its new place the platform's north edge touches the rock wall
    ///    (arch_arieki_atta_queens_obj01_v08), seen only.
    /// </summary>
    public static class AshenAvernusHiveNpcsV1
    {
        public const uint Outpost = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Tananda = 790650, TanandaName = 9728, TanandaPackage = 1914, TanandaLevel = 44;
        public const uint Frogga = 790651, FroggaName = 9731, FroggaPackage = 790651, FroggaLevel = 44;
        public const uint Vandeweghe = 790652, VandewegheName = 9733, VandeweghePackage = 1925, VandewegheLevel = 46;
        public const uint Zuriel = 790653, ZurielName = 9676, ZurielPackage = 2075, ZurielLevel = 50;
        public const uint Caretaker = 790654, Fithik = 790655, Miasma = 790656, Tunneler = 790657, BanePool = 790658;
        public const uint MiasmaName = 9037, TunnelerName = 9892, BaneLevel = 43;
        public const uint NextFreeId = 790659;

        /// <summary>The rows the new ones copy: Captain Flynn Harris (AFS), Ida Hayner (the female AFS class), the Eloh Jumna.</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, IdaHayner = AshenLeadInNpcsV1.IdaHayner, Jumna = 760372;

        /// <summary>The Bane's rows: the Ashen Caretaker Doctor, the Ashen Fithik Guardian, the Polonium Miasma, the Ashen Thrax Soldier.</summary>
        public const uint CaretakerRow = 531011, FithikRow = 540018, MiasmaRow = 531035, ThraxRow = 531008;

        /// <summary>The Bane's pool: one of each, a small circle, the outpost's respawn.</summary>
        public const uint BaneRespawnTenths = 1200, BaneRadius = 5;
        public static readonly (double X, double Y, double Z) BaneAt = (147, 194.11, -590);

        public static readonly (double X, double Y, double Z) TanandaAt = (449, 391.46, -45);
        public static readonly (double X, double Y, double Z) FroggaAt = (652, 353.32, -238);
        public static readonly (double X, double Y, double Z) VandewegheAt = (349, 233.11, -260);
        public static readonly (double X, double Y, double Z) ZurielAt = (628.6, 276, -677.8);

        /// <summary>The Atta Teleporters: local teleporter pairs (type 1), class 25302 UsableTwoStateAttaTeleporter.</summary>
        public const uint TunnelPad = 917, QueenPad = 918, WestPad = 919, SanctuaryPad = 920;
        public const uint LocalTeleporter = AshenAvernusOutpostNpcsV1.LocalTeleporter, PadClass = AshenAvernusOutpostNpcsV1.PadClass;
        public static readonly (double X, double Y, double Z) TunnelPadAt = (509, 368.37, -506), QueenPadAt = (340, 238.11, -252);
        public static readonly (double X, double Y, double Z) WestPadAt = (470, 385.83, -324), SanctuaryPadAt = (628, 275.98, -639);

        /// <summary>The pads: id, description, place; linked in pairs both ways.</summary>
        public static readonly (uint Id, string Description, (double X, double Y, double Z) At, uint To)[] Pads =
        {
            (TunnelPad, "Atta Teleporter: Avernus Outpost, the southern tunnel (set 18)", TunnelPadAt, QueenPad),
            (QueenPad, "Atta Teleporter: Avernus Outpost, over the Queen's Chamber (set 18)", QueenPadAt, TunnelPad),
            (WestPad, "Atta Teleporter: Avernus Outpost, Food Storage west (set 18)", WestPadAt, SanctuaryPad),
            (SanctuaryPad, "Atta Teleporter: Avernus Outpost, the Eloh Sanctuary (set 18)", SanctuaryPadAt, WestPad)
        };

        /// <summary>The new friendly creatures with a pool: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] Friends =
        {
            (Tananda, "Captain Tananda - Avernus Outpost, the Cave In (1536)", IdaHayner, TanandaName, TanandaLevel, TanandaPackage, TanandaAt),
            (Frogga, "Sgt. Frogga - Avernus Outpost, Food Storage (1536)", Harris, FroggaName, FroggaLevel, FroggaPackage, FroggaAt),
            (Vandeweghe, "Colonel Vandeweghe - Avernus Outpost, over the Queen's Chamber (1537)", Harris, VandewegheName, VandewegheLevel,
                VandeweghePackage, VandewegheAt),
            (Zuriel, "Zuriel - Avernus Outpost, the Eloh Sanctuary (1581)", Jumna, ZurielName, ZurielLevel, ZurielPackage, ZurielAt)
        };

        /// <summary>The Bane: id, comment, the row they copy, the name (null: the copy's own).</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint? Name)[] Bane =
        {
            (Caretaker, "Caretaker - Avernus Outpost, the Queen's Chamber (1537)", CaretakerRow, null),
            (Fithik, "Fithik - Avernus Outpost, the Queen's Chamber (1537)", FithikRow, null),
            (Miasma, "Altered Miasma - Avernus Outpost, the Queen's Chamber (1537)", MiasmaRow, MiasmaName),
            (Tunneler, "Thrax Tunneler - Avernus Outpost, the Queen's Chamber (1537)", ThraxRow, TunnelerName)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, level, package, at) in Friends)
            {
                yield return Creature(id, comment, copy, name, level);
                // The hologram Eloh has no creature_appearance rows (Jumna, Veryin): none to copy for Zuriel.
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                // npc_package.comment is varchar(50): the name and the mission only.
                var packageComment = comment[..comment.IndexOf(" - ", StringComparison.Ordinal)] + comment[comment.LastIndexOf(" (", StringComparison.Ordinal)..];
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                if (id != Vandeweghe)
                    yield return Pool(id, at.X, at.Y, at.Z, Outpost, id, 1, 1, 20, 0);
            }
            foreach (var (id, comment, copy, name) in Bane)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, faction, {BaneLevel}, max_hp, {(name.HasValue ? name.Value.ToString(CultureInfo.InvariantCulture) : "name_id")}, " +
                             $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                             $"SELECT {id}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {copy};";
            }
            yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                         "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                         "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                         "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                         $"VALUES ({BanePool}, 0, 0, {BaneRespawnTenths}, {F(BaneAt.X)}, {F(BaneAt.Y)}, {F(BaneAt.Z)}, 0, {Outpost}, " +
                         $"{Caretaker}, 1, 1, {Fithik}, 1, 1, {Miasma}, 1, 1, {Tunneler}, 1, 1, 0, 0, 0, 0, 0, 0, {BaneRadius});";
        }

        /// <summary>The Atta Teleporters' pads and links: each INSERT guarded already (only where the row is not there), as set 17's.</summary>
        public static IEnumerable<string> PadsUp()
        {
            foreach (var (id, description, at, _) in Pads)
                yield return Pad(id, description, at);
            foreach (var (id, _, _, to) in Pads)
                yield return Link(id, to);
        }

        public static IEnumerable<string> PadsDown()
        {
            foreach (var (id, _, _, to) in Pads.Reverse())
                yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {id} AND to_teleporter_id = {to};";
            yield return $"DELETE FROM teleporter WHERE id IN ({string.Join(", ", Pads.Select(pad => pad.Id))}) AND map_context_id = {Outpost} AND type = {LocalTeleporter};";
        }

        public static IEnumerable<string> Down()
        {
            var all = string.Join(", ", Creatures.Select(id => id.ToString(CultureInfo.InvariantCulture)));
            var packaged = string.Join(", ", Friends.Select(row => row.Id.ToString(CultureInfo.InvariantCulture)));
            var pooled = string.Join(", ", Pooled.Select(id => id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({BanePool});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({pooled});";
            yield return $"DELETE FROM npc_package WHERE id IN ({packaged});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({all});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({all});";
            yield return $"DELETE FROM creature WHERE id IN ({all});";
        }

        /// <summary>Where a pooled NPC of set 18 stands (Vandeweghe too: his copy's place).</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool == BanePool ? BaneAt : Friends.Single(npc => npc.Id == pool).At;

        /// <summary>The friendly NPCs with a public pool: Vandeweghe has none (only 1537's copy, AshenGodSaveTheQueenV1).</summary>
        public static uint[] Pooled => Friends.Select(row => row.Id).Where(id => id != Vandeweghe).ToArray();

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).Concat(Bane.Select(row => row.Id)).ToArray();

        private static string Creature(uint id, string comment, uint copy, uint name, uint level) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, run_speed, walk_speed, " +
            $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"SELECT {id}, {PadClass}, {LocalTeleporter}, {S(description)}, {R(at.X)}, {R(at.Y)}, {R(at.Z)}, 0, m.map_context_id " +
            $"FROM map_info m WHERE m.map_context_id = {Outpost} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {id});";

        private static string Link(uint from, uint to) =>
            "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
            $"SELECT t.id, {to} FROM teleporter t WHERE t.id = {from} AND EXISTS (SELECT 1 FROM teleporter u WHERE u.id = {to}) " +
            $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {from});";

        private static string R(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
