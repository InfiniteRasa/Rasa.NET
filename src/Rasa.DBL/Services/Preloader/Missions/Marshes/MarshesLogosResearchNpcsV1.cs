using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 13 (Logos Research) NPCs and creatures: in the Logos Research Facility (squad instance 1700) Specialist
    /// Hax and Captain Sakai by the facility's hospital, the newbie medic of A Taste of Their Own Medicine (546), the Thrax
    /// Commander of Specimen Control (548), Coroner Larvax, Dissector Xynnr and Overseer Mesgel (718), the cell block prisoners
    /// (548), Lieutenant Russell and Special Ops Officer Brock (550); on the Marshes the Eloh Jumna in the chamber under the
    /// lake by Retread City and the two local teleporters between the lake and the chamber (1674).
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-03 against the World DB and the code of all worktrees:
    /// 760362-760399 free): 760362 Hax (creature, creature_appearance, creature_stat, npc_package, pool), 760363 Sakai
    /// (creature, creature_appearance, creature_stat, npc_package, pool), 760364 the newbie medic (creature,
    /// creature_appearance, creature_stat), 760365 the Thrax Commander (creature, creature_stat), 760366-760368 Larvax, Xynnr
    /// and Mesgel (creature, creature_stat), 760369 the prisoner (creature, creature_appearance), 760370 Russell and 760371
    /// Brock (creature, creature_appearance, npc_package), 760372 Jumna (creature, npc_package, pool). Teleporters 907 (the
    /// lake) and 908 (the chamber) and their two local_teleporter_link rows (teleporter ids stay below 1000; 907-951 free in
    /// the World DB and the code, MarshesRetreadArenaNpcsV1 took 905-906).
    ///
    /// Sourced: client names 8562 "Specialist Hax", 8563 "Captain Sakai", 8572 "Coroner Larvax", 8574 "Dissector Xynnr", 8573
    /// "Overseer Mesgel", 3135 "Lieutenant Russell", 10133 "Special Ops Officer Brock", 9951 "Jumna", 10117 "Prisoner", 8721
    /// "Field Medic", 10629 "Thrax Commander" (creaturenamelanguage); packages 960 (Hax, objectiveconversation 546,5), 284
    /// (Russell, 550,7), 2203 (550,10, Brock's text 17331), 2036 (Jumna, 1674,1). The LRF map (client
    /// adv_foreas_valverde_marshes_logosresearchfacility): its hospital (teleporters 450 and 587, the two Logos Research Field
    /// Medics 500262/500263 at 820, 321.6, -104), the Security Checkpoint (map_region 20052, 760, 320, -136), Specimen Control
    /// (20057, 728.5, 297.5, -176, its suspended command chair 728.1, 297.2, -181.9). The Marshes map: the Eloh chamber
    /// (ArchElohTemplesRoomSpiritV01 -160, 136, -796, its floor at y 112 from -808 to -930, the Eloh plates at -160, 111.9,
    /// -808) and the Eloh lily glows on the lake over it (-158.5 to -164.3, 214.2, -864.8 to -870). TaRapedia: Jumna "Eloh,
    /// male, level 45", "Retread City Subterranean Chamber", /loc -160, 111, -902; "we've got a secret teleporter that will
    /// take you to the Eloh chamber" (1674's offer 16111); 548's key "from a boss in the control room" (MMO DB tip). World
    /// DB: Sergeant Fitz 510170, Colonel Bosley 510176, Lieutenant Holloway 510199, Special Agent Perdu 510193, Field Medic
    /// DuBois 510161, the Maligo Base Prisoner 750105, Veryin 750051 (NPC_Holographic_Eloh), the Marshes Thrax Commander
    /// 531049 and Caretaker 531048, the Plains Thrax Commander 531078 (level 38, 12375).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower; the triage's choices of 2026-11-02):
    ///  - Hax a copy of Fitz's row and look, Sakai of Bosley's (a captain), both with the escort stats (they stand in the
    ///    missions' scenes too) and public in the facility, 6-8 m west of the hospital's medics: Hax gives 548 and 718 there,
    ///    Sakai 550. Each is also his mission's scene actor (the same creature: the turn-in), which hides the public one from
    ///    its owner while it stands. Sakai's package is a server-only one (his id: the client has no talk on him).
    ///  - The newbie medic a copy of DuBois' row and look, name "Field Medic", the escort stats.
    ///  - The Thrax Commander a copy of the Marshes one at level 38 with the Plains level 38 one's hit points; Larvax, Xynnr
    ///    and Mesgel copies of the Marshes Caretaker at level 38 with 10000, 12500 and 15000 hit points (the triage's
    ///    10000-15000). The prisoner a friendly copy of the Maligo Base Prisoner with the client's name "Prisoner", no attack.
    ///  - Russell a copy of Holloway's row and look, Brock of Perdu's; scene actors only (no pool).
    ///  - Jumna a copy of Veryin's row (the hologram Eloh: the client has no other Eloh NPC class with a body) at level 45,
    ///    public, at TaRapedia's /loc on the chamber's floor (an island on the navmesh).
    ///  - The secret teleporter: two public local teleporters (ArchHumTeleporterV02, as MarshesRetreadArenaNpcsV1's), one on
    ///    the rock under the lily glows (-160, 213.68, -868: walked to from Retread City) and one on the chamber's plates
    ///    (-160, 112.43, -808), each linked to the other (a player set down on a pad is not sent back until he steps off it,
    ///    DynamicObjectManager.LocalTeleporterWorker). Not only for 1674's players: the client has no gate on it.
    /// </summary>
    public static class MarshesLogosResearchNpcsV1
    {
        public const uint MapContextId = 1700;          // adv_foreas_valverde_marshes_logosresearchfacility, a squad instance
        public const uint Marshes = MarshesMissionDataV1.MapContextId;
        public const uint Fitz = 510170, Bosley = 510176, Holloway = 510199, Perdu = 510193, DuBois = 510161;
        public const uint MaligoPrisoner = 750105, Veryin = 750051, ThraxCommander = 531049, Caretaker = 531048;
        public const uint EscortHealth = MarshesRetreadCityNpcsV1.EscortHealth, EscortArmor = MarshesRetreadCityNpcsV1.EscortArmor;

        public const uint Hax = 760362, HaxName = 8562, HaxPackage = 960, HaxLevel = 35;
        public const uint Sakai = 760363, SakaiName = 8563, SakaiPackage = Sakai, SakaiLevel = 36;
        public const uint Medic = 760364, MedicName = 8721, MedicLevel = 33;
        public const uint Commander = 760365, CommanderName = 10629, BossLevel = 38, CommanderHitPoints = 12375, CommanderArmor = 340;
        public const uint Larvax = 760366, LarvaxName = 8572, LarvaxHitPoints = 10000;
        public const uint Xynnr = 760367, XynnrName = 8574, XynnrHitPoints = 12500;
        public const uint Mesgel = 760368, MesgelName = 8573, MesgelHitPoints = 15000;
        public const uint CaretakerArmor = 240;
        public const uint Prisoner = 760369, PrisonerName = 10117, PrisonerLevel = 34;
        public const uint Russell = 760370, RussellName = 3135, RussellPackage = 284;
        public const uint Brock = 760371, BrockName = 10133, BrockPackage = 2203;
        public const uint Jumna = 760372, JumnaName = 9951, JumnaPackage = 2036, JumnaLevel = 45;

        public static readonly (double X, double Y, double Z) HaxAt = (814, 321.77, -110);
        /// <summary>On the hospital's client floor (321.61), 5 m from Hax; (814, -100) was on the floor's edge (to-fix 32).</summary>
        public static readonly (double X, double Y, double Z) SakaiAt = (814, 321.77, -105);
        public static readonly (double X, double Y, double Z) JumnaAt = (-160, 112.11, -902);

        public const uint LakePad = 907, ChamberPad = 908;
        public const uint PadClass = MarshesRetreadArenaNpcsV1.PadClass, LocalTeleporter = MarshesRetreadArenaNpcsV1.LocalTeleporter;
        public static readonly (double X, double Y, double Z) LakePadAt = (-160, 213.68, -868);
        public static readonly (double X, double Y, double Z) ChamberPadAt = (-160, 112.43, -808);
        public const double LakePadYaw = 0, ChamberPadYaw = 3.1416;   // the chamber's pad faces south, to Jumna

        public static IEnumerable<string> Up()
        {
            // Hax and Sakai: public in the facility, the escort stats.
            foreach (var (id, comment, row, name, level, package, at) in new[]
            {
                (Hax, "Specialist Hax - Logos Research Facility (546, 548, 718)", Fitz, HaxName, HaxLevel, HaxPackage, HaxAt),
                (Sakai, "Captain Sakai - Logos Research Facility (548, 550)", Bosley, SakaiName, SakaiLevel, SakaiPackage, SakaiAt)
            })
            {
                yield return Copy(id, comment, row, name, level, EscortHealth.ToString(), "1", true);
                yield return Looks(id, row);
                yield return Stat(id, EscortHealth, EscortArmor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Logos Research');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            yield return Copy(Medic, "Field Medic - Logos Research Facility (546, the newbie medic)", DuBois, MedicName, MedicLevel,
                EscortHealth.ToString(), "1", true);
            yield return Looks(Medic, DuBois);
            yield return Stat(Medic, EscortHealth, EscortArmor);

            // The bosses: Bane, their rows' attacks.
            foreach (var (id, comment, row, name, health, armor) in new[]
            {
                (Commander, "Thrax Commander - Logos Research Facility, Specimen Control (548)", ThraxCommander, CommanderName,
                    CommanderHitPoints, CommanderArmor),
                (Larvax, "Coroner Larvax - Logos Research Facility (718)", Caretaker, LarvaxName, LarvaxHitPoints, CaretakerArmor),
                (Xynnr, "Dissector Xynnr - Logos Research Facility (718)", Caretaker, XynnrName, XynnrHitPoints, CaretakerArmor),
                (Mesgel, "Overseer Mesgel - Logos Research Facility (718)", Caretaker, MesgelName, MesgelHitPoints, CaretakerArmor)
            })
            {
                yield return Copy(id, comment, row, name, BossLevel, health.ToString(), "faction", true);
                yield return Stat(id, health, armor);
            }

            yield return Copy(Prisoner, "Prisoner - Logos Research Facility, the cell blocks (548)", MaligoPrisoner, PrisonerName,
                PrisonerLevel, "max_hp", "1", false);
            yield return Looks(Prisoner, MaligoPrisoner);

            foreach (var (id, comment, row, name, package) in new[]
            {
                (Russell, "Lieutenant Russell - Logos Research Facility, the entrance (550)", Holloway, RussellName, RussellPackage),
                (Brock, "Special Ops Officer Brock - Logos Research Facility, the entrance (550)", Perdu, BrockName, BrockPackage)
            })
            {
                yield return Copy(id, comment, row, name, 36, "max_hp", "1", true);
                yield return Looks(id, row);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Logos Research');";
            }

            // Jumna (the hologram Eloh has no creature_appearance rows: Veryin has none) and the two teleporters.
            yield return Copy(Jumna, "Jumna - Marshes, the Eloh chamber under the lake (1674)", Veryin, JumnaName, JumnaLevel, "max_hp", "1", true);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Jumna}, {JumnaPackage}, 'Jumna - Marshes');";
            yield return Pool(Jumna, JumnaAt.X, JumnaAt.Y, JumnaAt.Z, Marshes, Jumna, 1, 1, 20, 0);
            yield return Pad(LakePad, "Local Teleporter: the lake by Retread City (Eloh chamber)", LakePadAt, LakePadYaw);
            yield return Pad(ChamberPad, "Local Teleporter: the Eloh chamber (the lake)", ChamberPadAt, ChamberPadYaw);
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({LakePad}, {ChamberPad});";
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({ChamberPad}, {LakePad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({LakePad}, {ChamberPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({LakePad}, {ChamberPad});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({Hax}, {Sakai}, {Jumna});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Hax}, {Sakai}, {Russell}, {Brock}, {Jumna});";
            yield return $"DELETE FROM creature_stat WHERE id BETWEEN {Hax} AND {Mesgel};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Hax} AND {Jumna};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Hax} AND {Jumna};";
        }

        /// <summary>A copy of <paramref name="row"/> with its own name, level, hit points and faction; its attacks kept or none.</summary>
        private static string Copy(uint id, string comment, uint row, uint name, uint level, string health, string faction, bool attacks) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, '{comment.Replace("'", "''")}', class_id, {faction}, {level}, {health}, {name}, run_speed, walk_speed, " +
            (attacks ? "action1, action2, action3, action4, action5, action6, action7, action8" : "0, 0, 0, 0, 0, 0, 0, 0") +
            $" FROM creature WHERE id = {row};";

        private static string Looks(uint id, uint row) =>
            "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
            $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at, double rotation) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"VALUES ({id}, {PadClass}, {LocalTeleporter}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, {F(rotation)}, {Marshes});";
    }
}
