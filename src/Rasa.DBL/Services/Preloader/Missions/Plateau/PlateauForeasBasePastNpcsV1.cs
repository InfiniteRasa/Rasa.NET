using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Foreas Base in the past (the Temporal Chamber's west part, squad instance 2029) for Foreas Base Past
    /// (1178): Colonel Fonseca in the AFS HQ, the templates of the mission's scene (Diplomatic Liaison Villaran,
    /// the AFS defenders, the two Bane waves, Taskmaster Xykryl) and the teleporter pair on Veryin's left. The
    /// client's map places none of them, and the server had none.
    ///
    /// Sourced:
    ///  - Names (creaturenamelanguage, the block 9079-9108 of this mission): 9081 Colonel Fonseca, 9085 Diplomatic
    ///    Liaison Villaran, 9089 AFS 1st Infantry, 9099 Thrax Cavalryman, 9102 Thrax Recon Soldier, 9104 Thrax
    ///    Special Forces, 9108 Taskmaster Xykryl. Package 1238 (objectiveconversation 1178, 1/4/6).
    ///  - Client 1178 texts: 8847 "Go through the teleporter to Veryin's left"; 8848 "the Forean diplomat,
    ///    Villaran"; 8858 "take him to the drop-ship outside"; 8958 "the teleporter north of HQ".
    ///  - Client map 2029: ArchHumanBaseHeadquartersForeas (-474, 226, 431), its door barricades at z 442, the
    ///    gate wall along z 490, barricades round an open place north-west of HQ (x -504..-466, z 526..535).
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-24; places on the navmesh, checked in
    /// PlateauTemporalChamberTests):
    ///  - Fonseca inside HQ; the chamber pad on Veryin's left (as Hudson's is on the right); the past pad north of HQ,
    ///    between its door and the gate wall.
    ///  - Villaran a Forean (NPC_Forean_Unarmed), 4000 health and 300 armour as the server's other escorts
    ///    (EscortNpcStats), with Ranger Milpas's Forean lightning (row 6) to fight back on his walk.
    ///  - The defenders: Clean Slate's Light Soldier with the redshirt pistol, level 30. The waves: Thrax as Ustor
    ///    Yard's (level 30 and 31), Striders of the plain Strider class at level 30 with the Strider boss's weapons at
    ///    about 60% damage, Xykryl a Thrax rifle boss at level 32, three times a Thrax's health.
    ///
    /// Ids: creatures 750096-750102, Fonseca's pool 750096, teleporters 903-904 (waypoint ids stay below 1000), creature actions
    /// 750031-750033, free before. Plain SQL, valid on SQLite and MySQL.
    /// </summary>
    public static class PlateauForeasBasePastNpcsV1
    {
        public const uint MapContextId = PlateauTemporalChamberNpcsV1.MapContextId;

        public const uint Fonseca = 750096, FonsecaPool = 750096, FonsecaPackage = 1238, FonsecaName = 9081;
        public const uint Villaran = 750097, VillaranName = 9085, VillaranHealth = EscortNpcStats.Health, VillaranArmor = EscortNpcStats.Armor;

        /// <summary>
        /// Villaran's attack: creature_action row 6, CR_FOREAN_LIGHTNING, as Ranger Milpas (630010) of the same unarmed Forean
        /// class 26833, whose model the client animates it on (Fix_creature_attack_animations). He fights back when hit on
        /// his walk (playtest 2026-10-25: he had no attack and walked on under fire).
        /// </summary>
        public const uint VillaranAttack = 6;
        public const uint Defender = 750098, DefenderName = 9089, DefenderHitPoints = 2600, DefenderArmor = 140;
        public const uint Recon = 750099, ReconName = 9102, ReconLevel = 30, ReconHitPoints = 2800, ReconArmor = 140;
        public const uint Cavalryman = 750100, CavalrymanName = 9099, CavalrymanLevel = 30, CavalrymanHitPoints = 4000, CavalrymanArmor = 200;
        public const uint Xykryl = 750101, XykrylName = 9108, XykrylLevel = 32, XykrylHitPoints = 3177 * 3, XykrylArmor = 150;
        public const uint SpecialForces = 750102, SpecialForcesName = 9104, SpecialForcesLevel = PlateauUstorYardV1.SecurityLevel,
            SpecialForcesHitPoints = PlateauUstorYardV1.SecurityHitPoints, SpecialForcesArmor = PlateauUstorYardV1.SecurityArmor;

        public const uint HumanLevel = 30;
        public const uint MaleClass = PlateauMaligoBaseV1.MaleClass;
        public const uint OutfitOf = PlateauMaligoBaseV1.Altera;    // Peterson's officer uniform
        public const uint ForeanClass = 26833;          // NPC_Forean_Unarmed
        public const uint SoldierClass = PlateauPerduNpcsV1.SoldierClass;
        public const uint ThraxClass = PlateauUstorYardV1.ThraxClass;
        public const uint StriderClass = 9804;          // Vehicle_Bane_Strider_V1
        public const uint ThraxBossClass = 10504;       // Bane_Thrax_Soldier_Rifle_Boss
        public const uint ThraxLightning = 57030;       // thrax_lightning of the Thrax Soldier boss 520054
        public const uint ThraxBossRifle = 34;          // the Thrax boss rifle (1/79) of the same class's Plateau boss 520054

        public const uint DefenderPistol = 750031, StriderWeapon = 750032, StriderLaser = 750033;

        public const uint ChamberPad = 903, PastPad = 904;
        public const uint PadClass = PlateauTemporalChamberNpcsV1.PadClass;
        public const uint LocalTeleporterType = PlateauTemporalChamberNpcsV1.LocalTeleporterType;

        public static readonly (double X, double Y, double Z) FonsecaAt = (-474, 230.45, 414);
        public static readonly (double X, double Y, double Z) ChamberPadAt = (-10, 248.16, -34);
        public static readonly (double X, double Y, double Z) PastPadAt = (-474, 226.25, 470);

        public static IEnumerable<string> Up()
        {
            yield return Friend(Fonseca, "Colonel Fonseca - Temporal Chamber, Foreas Base past", MaleClass, FonsecaName, 1000, 0, 0, 0);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Fonseca}, slot_id, Class_id, color FROM creature_appearance WHERE id = {OutfitOf};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Fonseca}, {FonsecaPackage}, 'Colonel Fonseca');";
            yield return Pool(FonsecaPool, FonsecaAt.X, FonsecaAt.Y, FonsecaAt.Z, MapContextId, Fonseca, 1, 1, 20, 0);

            yield return Friend(Villaran, "Diplomatic Liaison Villaran - Foreas Base past (Foreas Base Past scene)", ForeanClass,
                VillaranName, VillaranHealth, 7, 2, VillaranAttack);
            yield return Stat(Villaran, VillaranHealth, VillaranArmor);

            yield return Action(DefenderPistol, "AFS 1st Infantry - Foreas Base past pistol 1/133", 1, 133, 0.5, 24, 1300, 0, 95, 140, 1);
            yield return Friend(Defender, "AFS 1st Infantry - Foreas Base past (Foreas Base Past scene)", SoldierClass, DefenderName,
                DefenderHitPoints, 9, 5, DefenderPistol);
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Defender}, 13, 6271, 1);";
            yield return Stat(Defender, DefenderHitPoints, DefenderArmor);

            yield return Creature(Recon, "Thrax Recon Soldier - Foreas Base past (Foreas Base Past scene)", ThraxClass, ReconLevel,
                ReconHitPoints, ReconName, PlateauUstorYardV1.ThraxKick30, PlateauUstorYardV1.ThraxWeapon30);
            yield return Stat(Recon, ReconHitPoints, ReconArmor);

            yield return Action(StriderWeapon, "Thrax Cavalryman - Foreas Base past strider weapon 1/204", 1, 204, 1, 20, 1000, 0, 210, 315, 1);
            yield return Action(StriderLaser, "Thrax Cavalryman - Foreas Base past strider_laser_beam", 270, 1, 1, 40, 15000, 2733, 515, 630, 2);
            yield return Creature(Cavalryman, "Thrax Cavalryman - Foreas Base past (Foreas Base Past scene)", StriderClass, CavalrymanLevel,
                CavalrymanHitPoints, CavalrymanName, StriderWeapon, StriderLaser);
            yield return Stat(Cavalryman, CavalrymanHitPoints, CavalrymanArmor);

            yield return Creature(Xykryl, "Taskmaster Xykryl - Foreas Base past (Foreas Base Past scene)", ThraxBossClass, XykrylLevel,
                XykrylHitPoints, XykrylName, PlateauUstorYardV1.ThraxKick33, ThraxBossRifle, ThraxLightning);
            yield return Stat(Xykryl, XykrylHitPoints, XykrylArmor);

            yield return Creature(SpecialForces, "Thrax Special Forces - Foreas Base past (Foreas Base Past scene)", ThraxClass,
                SpecialForcesLevel, SpecialForcesHitPoints, SpecialForcesName, PlateauUstorYardV1.ThraxKick30, PlateauUstorYardV1.ThraxWeapon30);
            yield return Stat(SpecialForces, SpecialForcesHitPoints, SpecialForcesArmor);

            yield return Pad(ChamberPad, "Local Teleporter: Temporal Chamber to Foreas Base past", ChamberPadAt);
            yield return Pad(PastPad, "Local Teleporter: Foreas Base past to Temporal Chamber", PastPadAt);
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({ChamberPad}, {PastPad});";
            yield return $"INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) VALUES ({PastPad}, {ChamberPad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id IN ({ChamberPad}, {PastPad});";
            yield return $"DELETE FROM teleporter WHERE id IN ({ChamberPad}, {PastPad});";
            yield return $"DELETE FROM spawnpool WHERE id = {FonsecaPool};";
            yield return $"DELETE FROM npc_package WHERE id = {Fonseca};";
            yield return $"DELETE FROM creature_stat WHERE id BETWEEN {Fonseca} AND {SpecialForces};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Fonseca} AND {SpecialForces};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Fonseca} AND {SpecialForces};";
            yield return $"DELETE FROM creature_action WHERE id BETWEEN {DefenderPistol} AND {StriderLaser};";
        }

        /// <summary>A friendly (AFS) creature of level <see cref="HumanLevel"/>.</summary>
        private static string Friend(uint id, string comment, uint classId, uint nameId, uint hitPoints, uint run, uint walk, uint action1) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"VALUES ({id}, '{comment}', {classId}, 1, {HumanLevel}, {hitPoints}, {nameId}, {run}, {walk}, {action1}, 0, 0, 0, 0, 0, 0, 0);";

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"VALUES ({id}, {PadClass}, {LocalTeleporterType}, '{description}', {F(at.X)}, {F(at.Y)}, {F(at.Z)}, 0, {MapContextId});";
    }
}
