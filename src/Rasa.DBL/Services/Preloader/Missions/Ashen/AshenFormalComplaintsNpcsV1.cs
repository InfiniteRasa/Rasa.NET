using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 6 NPCs, Ligo Ashen Desert (map 1734), for Formal Complaints and its AFS follow-ups: Scavenger Vakiel
    /// Latan (Formal Complaints 1254, A Bribe for Your Thoughts 1259, the receiver of The Human Perspective 1258), Shinichiro Ito
    /// (1258) and Megumi Yamamoto (Dearly Departed 1285), all three new. Captain Akio Yamamoto (set 1, package 1414) and Colonel
    /// Mason Linier (upstream 510010, package 1389, moved by set 1) are used where they stand.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 6 790200-790239): 790200 Latan, 790201 Ito, 790202 Megumi
    /// (creature, creature_appearance, creature_stat, npc_package, spawn pool each).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9277 "Scavenger Vakiel Latan", 9272 "Shinichiro Ito", 9295 "Megumi
    ///    Yamamoto". Latan's package 1449 (client objectiveconversation 1254,1-3); Ito and Megumi have no objective conversation
    ///    in the client (they only give): server-only packages (their ids), as set 1's Ames.
    ///  - Latan is Brann: the 1254 offer is a Labbna speaker ("Your AFS has been good to me and my Labbna", client text 10051);
    ///    Yamamoto calls her "that fork-faced harlot" (10099). Her look is a copy of Mechanic Raji Lasat (World DB 510014, the
    ///    Ashoka Settlement's Brann, class 7776 NPC_Brann_Swapset_Male: every Brann NPC of the World DB has that class).
    ///  - Places (TaRapedia, 0.5): Ito "Ashoka Settlement, a side-room in the middle section", /loc 556, 248, -320, level 43
    ///    (TaRapedia Shinichiro Ito); Megumi "Shadow's Edge Post, in eastern tent", /loc -195, 269, -898, level 42 (TaRapedia
    ///    Megumi Yamamoto); Latan "Ashoka" (TaRapedia Formal Complaints, A Bribe for Your Thoughts; no coordinates).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Ito 4 m from his TaRapedia loc on the lower level of Ashoka (the Ashoka Settlement hospital's floor, y 248.5); Latan 16 m
    ///    east of him on that floor (no source: "near Ito", triage set 6). The lower level is joined on the server navmesh to the
    ///    hospital, the Ashoka Upper teleporter (613) and the market with the armour and weapon vendors (y 264.5); the market's
    ///    ramp ends 32 m east of the Ashoka Settlement waypoint, where the client's floor goes on to the waypoint (steps of
    ///    0.9 m at most, probe of 2026-11-09) and the server navmesh does not (upstream N18).
    ///  - Megumi: her TaRapedia loc is in the barracks rooms under the post (the client's blankets and corridors at y 264-269
    ///    south of the HQ entrance), a navmesh island no waypoint walks to, and the post has no tent: she stands on the post's
    ///    floor in its south enclosure (the fences at x -170..-82, z -856..-916), 29 m from the loc, as set 1 put Mason Linier at
    ///    the HQ entrance.
    ///  - Ito a copy of Mason Linier's row and look (AFS class 3846), Megumi of Ida Hayner's (the female AFS class 3848, as set
    ///    1's Caldwell), Latan of Raji Lasat's; Latan level 40 (no source: the missions' band). 4500 hit points and armour 300,
    ///    friendly, standing still, rotation 0 (as set 1).
    ///  - Each spot: on the client's floor and the server navmesh, 2 m or more from a wall, nothing over the head, seen from 16 of
    ///    16 places 2 m round it, 7 m or more from every waypoint and teleporter, 22 m or more outside every hostile pool's circle
    ///    (probe of 2026-11-09, client files and navmesh/adv_arieki_ligo_ashendesert).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows. No upstream row
    /// changes.
    /// </summary>
    public static class AshenFormalComplaintsNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Latan = 790200, LatanName = 9277, LatanPackage = 1449, LatanLevel = 40;
        public const uint Ito = 790201, ItoName = 9272, ItoPackage = 790201, ItoLevel = 43;
        public const uint Megumi = 790202, MegumiName = 9295, MegumiPackage = 790202, MegumiLevel = 42;
        public const uint NextFreeId = 790203;

        /// <summary>The World DB rows the new NPCs copy: Mechanic Raji Lasat (Brann, Ashoka), Mason Linier (AFS), Ida Hayner (AFS, female).</summary>
        public const uint RajiLasat = 510014, MasonLinier = AshenLeadInNpcsV1.MasonLinier, IdaHayner = AshenLeadInNpcsV1.IdaHayner;

        public static readonly (double X, double Y, double Z) LatanAt = (576, 248.65, -321);
        public static readonly (double X, double Y, double Z) ItoAt = (560, 248.65, -320);
        public static readonly (double X, double Y, double Z) MegumiAt = (-166, 296.98, -895);

        /// <summary>The new NPCs: id, comment, npc_package comment (varchar(50)), the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, string PackageComment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Latan, "Scavenger Vakiel Latan - Ashen Desert, Ashoka Settlement (1254)", "Scavenger Vakiel Latan (1254)", RajiLasat, LatanName, LatanLevel, LatanPackage, LatanAt),
            (Ito, "Shinichiro Ito - Ashen Desert, Ashoka Settlement (1258)", "Shinichiro Ito (1258)", MasonLinier, ItoName, ItoLevel, ItoPackage, ItoAt),
            (Megumi, "Megumi Yamamoto - Ashen Desert, Shadow's Edge Post (1285)", "Megumi Yamamoto (1285)", IdaHayner, MegumiName, MegumiLevel, MegumiPackage, MegumiAt)
        };

        /// <summary>Where an NPC of set 6 stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) => New.Single(npc => npc.Id == pool).At;

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, packageComment, copy, name, level, package, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (id, _, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
