using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 17 creatures and pads, Avernus Outpost (squad instance 2110, adv_arieki_ligo_ashendesert_avernusoutpost):
    /// Captain Sera Karter at the outpost's entrance (Overly Sensitive 1473), Commander Drazen in the cavern south of the outpost
    /// (Petrovich's Last Stand 1533), Captain Petrovich in the AFS Graveyard (1533's receiver, the giver of Petrovich's Run 1535) and
    /// at the top of the tunnel (1535's receiver), Petrovich's squad (an officer and soldiers, 1535's scene only), and the Atta
    /// Teleporter that takes a player from Drazen's cavern to the graveyard and back, and the Avernus Invaders' pools in the
    /// outpost. The World DB has none of them.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 17 790640-790679): 790640 Karter (creature, look, stats, package,
    /// pool), 790641 Drazen, 790642 Petrovich in the graveyard, 790643 Petrovich at the top (each the same), 790644 the Avernus Outpost
    /// Officer and 790645 the Avernus Outpost Soldier (creature, look, stats; no pool: 1535's scene brings them), 790646 the Avernus
    /// Invader (creature, stats) and its pools 790647-790649. The pads: local
    /// teleporters 915 (Drazen's cavern) and 916 (the graveyard), free in the World DB and the code (fix H's 913/914 the last).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9281 "Captain Sera Karter", 9671 "Commander Drazen", 9672 "Captain Petrovich",
    ///    9673 "Avernus Outpost Soldier", 9674 "Avernus Outpost Officer". Package (client objectiveconversation, 1.0): 1803 Petrovich
    ///    (1535,3). The client gives 1473 and 1533 no objective conversation: Karter and Drazen have server-only packages (their ids),
    ///    as set 2's Karter (AshenMaunaBarrensNpcsV1).
    ///  - Places (TaRapedia, /loc, 0.5): Karter "Captain Sera Karter (Avernus Outpost)" (80.3, 528.5, 99.5), at the entrance from the
    ///    Ashen Desert (map_link 59's arrival (83.99, 528.5, 107.67)); Drazen (31.0, 479.3, -356.5); Petrovich (952.0, 274.3, -275.0) in
    ///    the AFS Graveyard. 1533 (client 13697/13698): "go through the Atta Teleporter and find Captain Petrovich", Drazen's men "were
    ///    holding off the bugs"; TaRapedia: "enter the nearby Atta Teleporter and talk to Captain Petrovich just ahead". 1535 (13755):
    ///    "follow him up the Tunnel"; TaRapedia: clear the room, up the tunnel, rest at the big turn, the ambush, the top.
    ///  - The pad's look: the client's UsableTwoStateAttaTeleporter (class 25302, mesh 39557); the client map places none.
    ///  - The Avernus Invaders: name 9675 "Avernus Invader" (creaturenamelanguage); TaRapedia (Overly Sensitive, cache): "Atta called
    ///    Avernus Invaders, usually in groups of two or three, and an occasional elite commanding unit".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The rows copy Captain Flynn Harris (AFS) and, for Karter, Ida Hayner (the female AFS class), as set 2's. Levels: Karter 48
    ///    (set 2's), Drazen and Petrovich 44, the squad 42 (no source; the missions' band 41-42).
    ///  - Karter 0.5 m east of TaRapedia's place (seen from all round). Drazen moved 50.9 m north-east of TaRapedia's place (1.4 m outside the Atta pool 531055's
    ///    circle) to (57.5, 477.97, -313), beside his pad: 38.4 m outside 531055's circle; 35.7 m in 3D but 31.8 m on the flat
    ///    outside 531061's (18.6 m above him at the ramp's top; a rule break of the 34 m place rule on the flat, docs). Petrovich in the graveyard moved to
    ///    the graveyard's middle (909, 273.86, -246), 19 m from the pad, 36 m outside every hostile pool's circle (TaRapedia's place
    ///    lies inside 531050's). Petrovich at the top: the east end of the Food Storage shelf (645.5, 352.67, -246.5), where the tunnel
    ///    comes out (54 m outside every hostile pool's circle); 1535's walk ends there.
    ///  - The Atta Teleporter: a pair of local teleporters (as fix H's 913/914): 915 in Drazen's cavern, 916 in the graveyard, linked
    ///    both ways. The client floor has no walk from the outpost to the graveyard (the probe's flood, 2026-11-12): the pad is the
    ///    only way to the hive, for every player (the client has no gate on a local teleporter).
    ///  - The Avernus Invader: a copy of the Atta Soldier 531015 (class 7081, its actions and stats; no warcry), level 41 (1473's).
    ///    Three pools of two or three, respawn 1200 tenths (the outpost's Atta pools'), circle 5 m. The places: the only reachable
    ///    client floor 35 m or more off every walk of the set, 39 m from every place and 60 m outside every other hostile pool's
    ///    circle (AshenAvernusOutpostTests): two in the north ravine (on a navmesh island of their own: they do not chase into the
    ///    base), one in the south-west yard. The elite commander (9677 "Atta Invader Chief") is not built: no level, stats or place.
    /// </summary>
    public static class AshenAvernusOutpostNpcsV1
    {
        public const uint Outpost = 2110;   // adv_arieki_ligo_ashendesert_avernusoutpost, a squad instance (map_link 59 from 1734)
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Karter = 790640, KarterName = AshenMaunaBarrensNpcsV1.KarterName, KarterPackage = 790640, KarterLevel = 48;
        public const uint Drazen = 790641, DrazenName = 9671, DrazenPackage = 790641, DrazenLevel = 44;
        public const uint Petrovich = 790642, PetrovichTop = 790643, PetrovichName = 9672, PetrovichPackage = 1803, PetrovichLevel = 44;
        public const uint Officer = 790644, OfficerName = 9674, Soldier = 790645, SoldierName = 9673, SquadLevel = 42;
        public const uint Invader = 790646, InvaderName = 9675, InvaderLevel = 41;
        public const uint NextFreeId = 790650;

        /// <summary>The row the Avernus Invaders copy: the Atta Soldier (531015, Ashen Desert; no warcry).</summary>
        public const uint AttaSoldier = 531015;

        /// <summary>The Invaders' pools: two or three each (TaRapedia), the outpost's respawn (its Atta pools' 1200 tenths), a small circle.</summary>
        public const uint InvaderMin = 2, InvaderMax = 3, InvaderRespawnTenths = 1200, InvaderRadius = 5;

        /// <summary>The Invaders' pools in the outpost (790647-790649): each off the set's walks and places (AshenAvernusOutpostTests).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] InvaderPools =
        {
            (790647, (158, 528.25, 17)),        // the north ravine, west end
            (790648, (205, 515.85, 74)),        // the north ravine, east
            (790649, (-26, 528.87, -176))       // the south-west yard, past the landing strip
        };

        /// <summary>The World DB rows the new ones copy: Captain Flynn Harris (AFS), Ida Hayner (the female AFS class).</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, IdaHayner = AshenLeadInNpcsV1.IdaHayner;

        public static readonly (double X, double Y, double Z) KarterAt = (80.8, 528.5, 99.5);
        public static readonly (double X, double Y, double Z) DrazenAt = (57.5, 477.97, -313);
        public static readonly (double X, double Y, double Z) PetrovichAt = (909, 273.86, -246);
        public static readonly (double X, double Y, double Z) PetrovichTopAt = (645.5, 352.67, -246.5);

        /// <summary>The Atta Teleporter: a local teleporter pair (type 1), class 25302 UsableTwoStateAttaTeleporter.</summary>
        public const uint CavernPad = 915, GraveyardPad = 916, LocalTeleporter = 1, PadClass = 25302;
        public const string CavernDescription = "Atta Teleporter: Avernus Outpost, Drazen's cavern (set 17)";
        public const string GraveyardDescription = "Atta Teleporter: Avernus Outpost, AFS Graveyard (set 17)";
        public static readonly (double X, double Y, double Z) CavernPadAt = (64, 480.73, -312), GraveyardPadAt = (890, 278.11, -232);

        /// <summary>The new friendly creatures with a pool: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] Friends =
        {
            (Karter, "Captain Sera Karter - Avernus Outpost, the entrance (1473)", IdaHayner, KarterName, KarterLevel, KarterPackage, KarterAt),
            (Drazen, "Commander Drazen - Avernus Outpost, the south cavern (1533)", Harris, DrazenName, DrazenLevel, DrazenPackage, DrazenAt),
            (Petrovich, "Captain Petrovich - Avernus Outpost, AFS Graveyard (1533, 1535)", Harris, PetrovichName, PetrovichLevel,
                PetrovichPackage, PetrovichAt),
            (PetrovichTop, "Captain Petrovich - Avernus Outpost, the tunnel's top (1535)", Harris, PetrovichName, PetrovichLevel,
                PetrovichPackage, PetrovichTopAt)
        };

        /// <summary>Petrovich's squad: scene-only rows (1535), no package, no pool.</summary>
        public static readonly (uint Id, string Comment, uint Name)[] Squad =
        {
            (Officer, "Avernus Outpost Officer - Petrovich's squad (1535)", OfficerName),
            (Soldier, "Avernus Outpost Soldier - Petrovich's squad (1535)", SoldierName)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, level, package, at) in Friends)
            {
                yield return Creature(id, comment, copy, name, level);
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                // npc_package.comment is varchar(50): the name and the missions only ("Captain Petrovich (1533, 1535)").
                var packageComment = comment[..comment.IndexOf(" - ", StringComparison.Ordinal)] + comment[comment.LastIndexOf(" (", StringComparison.Ordinal)..];
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                yield return Pool(id, at.X, at.Y, at.Z, Outpost, id, 1, 1, 20, 0);
            }
            foreach (var (id, comment, name) in Squad)
            {
                yield return Creature(id, comment, Harris, name, SquadLevel);
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Harris};";
                yield return Stat(id, HitPoints, Armor);
            }
            // The Avernus Invaders: the Atta Soldier's row (class, look, actions, stats) under the client's name, at the set's level.
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Invader}, 'Avernus Invader - Avernus Outpost (1473)', class_id, faction, {InvaderLevel}, max_hp, {InvaderName}, " +
                         $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {AttaSoldier};";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                         $"SELECT {Invader}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {AttaSoldier};";
            foreach (var (pool, at) in InvaderPools)
                yield return Pool(pool, at.X, at.Y, at.Z, Outpost, Invader, InvaderMin, InvaderMax, InvaderRespawnTenths, InvaderRadius);
        }

        /// <summary>The Atta Teleporter's pads and links: each INSERT guarded already (only where the row is not there), as fix H's.</summary>
        public static IEnumerable<string> PadsUp()
        {
            yield return Pad(CavernPad, CavernDescription, CavernPadAt);
            yield return Pad(GraveyardPad, GraveyardDescription, GraveyardPadAt);
            yield return Link(CavernPad, GraveyardPad);
            yield return Link(GraveyardPad, CavernPad);
        }

        public static IEnumerable<string> PadsDown()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {GraveyardPad} AND to_teleporter_id = {CavernPad};";
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {CavernPad} AND to_teleporter_id = {GraveyardPad};";
            yield return $"DELETE FROM teleporter WHERE id IN ({GraveyardPad}, {CavernPad}) AND map_context_id = {Outpost} AND type = {LocalTeleporter};";
        }

        public static IEnumerable<string> Down()
        {
            var all = string.Join(", ", Creatures.Select(id => id.ToString(CultureInfo.InvariantCulture)));
            var pooled = string.Join(", ", Friends.Select(row => row.Id.ToString(CultureInfo.InvariantCulture)));
            var invaders = string.Join(", ", InvaderPools.Select(row => row.Pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({invaders});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({pooled});";
            yield return $"DELETE FROM npc_package WHERE id IN ({pooled});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({all});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({all});";
            yield return $"DELETE FROM creature WHERE id IN ({all});";
        }

        /// <summary>Where a pooled NPC of set 17 stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) => Friends.Single(npc => npc.Id == pool).At;

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).Concat(Squad.Select(row => row.Id)).Append(Invader).ToArray();

        private static string Creature(uint id, string comment, uint copy, uint name, uint level) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, run_speed, walk_speed, " +
            $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"SELECT {id}, {PadClass}, {LocalTeleporter}, {S(description)}, {F(at.X)}, {F(at.Y)}, {F(at.Z)}, 0, m.map_context_id " +
            $"FROM map_info m WHERE m.map_context_id = {Outpost} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {id});";

        private static string Link(uint from, uint to) =>
            "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
            $"SELECT t.id, {to} FROM teleporter t WHERE t.id = {from} AND EXISTS (SELECT 1 FROM teleporter u WHERE u.id = {to}) " +
            $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {from});";

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
