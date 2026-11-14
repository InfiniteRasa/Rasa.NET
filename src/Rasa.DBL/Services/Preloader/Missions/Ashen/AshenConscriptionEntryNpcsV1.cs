using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rasa.Services.Preloader.Missions.Plateau;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 19 creatures, field and pads, the Bane Conscription Facility (squad instance 1988), its entry and
    /// biolab: Capt. Peacock just inside the facility (Lockdown the Conscription Facility 1500, Raid the Conscription Facility
    /// 1497), Slocombe (Blast the Biolab 1498, no pool: only 1498's copy), the place of the front force field (1497's "Bomb
    /// the Front Forcefield": each member's own scene object, AshenRaidTheConscriptionFacilityV1; no world_destructible row,
    /// so no copy holds a public field) and a Bane teleporter pair from the HQ's hall to the biolab's service room. The World
    /// DB has none of them.
    ///
    /// Ids (the Ashen block 790000-790999; set 18 took 790650-790658): 790659 Peacock (creature, look, stats, package, pool),
    /// 790660 Slocombe (creature, look, stats, package). The pads: local teleporters 921/922, free in the World DB and the code (set
    /// 18's 917-920 the last).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9740 "Capt. Peacock", 9745 "Slocombe". Packages (client
    ///    objectiveconversation, 1.0): 1928 Peacock (1500,1), 1933 Slocombe (1498,4).
    ///  - Places: TaRapedia 1500 "Capt. Peacock is just inside the instance" (0.5); the door map_link 57 Ashen Desert (427.74,
    ///    305.61, 206.80) to the facility (7.25, 153.5, -670.25); the facility's entry chunnel, its hospital teleporter 442
    ///    (7.27, 156.17, -636.68) and medics 500019/500020 (World DB). The field: the client map's only way from the yard into
    ///    the facility, the HQ tunnel (ArchBaneIndustrialTunnelStraight16mV03 (6.5, 136, -409.2) and 018mV01 (6.5, 136,
    ///    -417.2)), and its class UsableForceFieldBaneOutpostWallMajorV01 9572 (the client's one Bane outpost field).
    ///  - The pads' look: the client's UsableTwoStateBaneTeleporterV01 (class 8044), running in 56 (MapUsables); the client map
    ///    places none in the facility.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Peacock and Slocombe copy Captain Flynn Harris (AFS, class 3846); levels 45 (a captain) and 44.
    ///  - Peacock 24 m on from the arrival, 9 m before the hospital's medics, at (6, 153.36, -646): the client floor and the navmesh
    ///    agree, he is seen from all round, 150 m and more from every Bane pool (AshenConscriptionEntryTests).
    ///  - The field across the HQ tunnel at <see cref="FieldAt"/>, turned as the tunnel (yaw 0), 5 m south of the bulkhead's
    ///    jambs (-401.2): every client-floor line along the tunnel through it, wall to wall, is stopped
    ///    (AshenConscriptionEntryTests.ThePlacesAreWalkedToOnTheClientsFloor, every 0.25 m across);
    ///    its foot 2 m under the tunnel's lowest floor there (131.5, the east gutter), so nobody walks under it. Past it, the HQ's
    ///    round hall (floor 135.94, x -8..22, z -354..-382) is walked from the tunnel both ways.
    ///  - The pads: the client floor has no walk from the hall (or Central Operations) to the Atta Laboratory's wing (the
    ///    probe's floods, 2026-11-12: 71000 cells from Central Operations; the nearest, the west wing's chunnel end (-215, 97.6,
    ///    -255)); the lab's wing is walked from its service room (-161.5, 89, -241). So, as set 18's Atta Teleporters, a public
    ///    pair, linked both ways: 921 on the hall's east side (17, 135.94, -370), 922 in the service room (-161.5, 89, -245).
    /// </summary>
    public static class AshenConscriptionEntryNpcsV1
    {
        public const uint Facility = 1988;              // adv_arieki_ligo_ashendesert_baneconscriptfacility
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Peacock = 790659, PeacockName = 9740, PeacockPackage = 1928, PeacockLevel = 45;
        public const uint Slocombe = 790660, SlocombeName = 9745, SlocombePackage = 1933, SlocombeLevel = 44;
        public const uint NextFreeId = 790661;

        /// <summary>The row the new ones copy: Captain Flynn Harris (AFS).</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris;

        public static readonly (double X, double Y, double Z) PeacockAt = (6, 153.36, -646);

        /// <summary>The front force field's class and place: UsableForceFieldBaneOutpostWallMajorV01 across the HQ tunnel (1497's scene object).</summary>
        public const uint FieldClass = 9572;
        public static readonly (double X, double Y, double Z) FieldAt = (6.5, 129.5, -405);
        public const double FieldYaw = 0;

        /// <summary>The Bane teleporters: a local teleporter pair (type 1), class 8044 UsableTwoStateBaneTeleporterV01.</summary>
        public const uint HallPad = 921, ServicePad = 922;
        public const uint LocalTeleporter = AshenAvernusOutpostNpcsV1.LocalTeleporter, PadClass = 8044;
        public static readonly (double X, double Y, double Z) HallPadAt = (17, 135.94, -370), ServicePadAt = (-161.5, 89, -245);

        /// <summary>The pads: id, description, place; linked both ways.</summary>
        public static readonly (uint Id, string Description, (double X, double Y, double Z) At, uint To)[] Pads =
        {
            (HallPad, "Bane Teleporter: Conscription Facility, the HQ hall (set 19)", HallPadAt, ServicePad),
            (ServicePad, "Bane Teleporter: Conscription Facility, the biolab's service room (set 19)", ServicePadAt, HallPad)
        };

        /// <summary>The new friendly creatures: id, comment, name, level, package; Peacock with a public pool.</summary>
        public static readonly (uint Id, string Comment, uint Name, uint Level, uint Package)[] Friends =
        {
            (Peacock, "Capt. Peacock - Conscription Facility, the entry (1500)", PeacockName, PeacockLevel, PeacockPackage),
            (Slocombe, "Slocombe - Conscription Facility, the HQ hall (1498)", SlocombeName, SlocombeLevel, SlocombePackage)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, level, package) in Friends)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, run_speed, walk_speed, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Harris};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Harris};";
                yield return Stat(id, HitPoints, Armor);
                // npc_package.comment is varchar(50): the name and the mission only.
                var packageComment = comment[..comment.IndexOf(" - ", StringComparison.Ordinal)] + comment[comment.LastIndexOf(" (", StringComparison.Ordinal)..];
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
            }
            // Respawn 20 tenths (2 s): ESTIMATE, no source for Peacock; the value every Ashen quest NPC pool uses (Ashoka's Chase,
            // the Boneyard's Infanger, the Avernus Outpost and hive NPCs).
            yield return Pool(Peacock, PeacockAt.X, PeacockAt.Y, PeacockAt.Z, Facility, Peacock, 1, 1, 20, 0);
        }

        /// <summary>The pads and links: each INSERT guarded already (only where the row is not there), as set 18's.</summary>
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
            yield return $"DELETE FROM teleporter WHERE id IN ({string.Join(", ", Pads.Select(pad => pad.Id))}) AND map_context_id = {Facility} AND type = {LocalTeleporter};";
        }

        public static IEnumerable<string> Down()
        {
            var all = string.Join(", ", Creatures.Select(id => id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({Peacock});";
            yield return $"DELETE FROM npc_package WHERE id IN ({all});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({all});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({all});";
            yield return $"DELETE FROM creature WHERE id IN ({all});";
        }

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).ToArray();

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"SELECT {id}, {PadClass}, {LocalTeleporter}, {S(description)}, {R(at.X)}, {R(at.Y)}, {R(at.Z)}, 0, m.map_context_id " +
            $"FROM map_info m WHERE m.map_context_id = {Facility} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {id});";

        private static string Link(uint from, uint to) =>
            "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
            $"SELECT t.id, {to} FROM teleporter t WHERE t.id = {from} AND EXISTS (SELECT 1 FROM teleporter u WHERE u.id = {to}) " +
            $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {from});";

        private static string R(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
