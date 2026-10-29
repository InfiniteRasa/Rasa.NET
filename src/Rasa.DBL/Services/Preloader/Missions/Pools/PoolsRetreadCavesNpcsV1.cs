using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Retread Caves (squad instance 1694) for their nine missions (Stick Your Neck Out 1329, the Mox Clearing chain
    /// 1132, 1135, 1133, 1134, 1131, 1136, 1138, 856): the Retreads Ellen Lenee, Kaethi Rose, Dr. Donius and H.P. Miles at
    /// the Retread Camp inside, the mission creatures (the Alpha Mox, the mutated, docile and pump miasmas, the Mad
    /// Bomber) and public Mox and mutated miasma pools. The server had only the Retread Medical Area, one AFS turret and
    /// the Maw, Miasma, Howler and Mox pools there (one Mox pool of 2-3 in the Western Grotto).
    ///
    /// Sourced: client names 7083 "Ellen Lenee", 4377 "Kaethi Rose", 7078 "Dr. Donius", 7080 "H.P. Miles", 9028 "Alpha
    /// Mox", 9037 "Mutated Miasma", 9038 "Docile Miasma", 7913 "Actinium Miasma", 7085 "The Mad Bomber"
    /// (creaturenamelanguage); packages 336 (Kaethi), 849 (Donius), 851 (Miles) from objectiveconversation 1329,1, 1135,2
    /// and 1136,3-11; Ellen has none. Ten Ton Hammer Retread Caves guide: the Retreads are Cormans; the dumbbell of the
    /// first room, the main passage and the Retread Camp, the Western Grotto off the passage, the Eastern Fringe off the
    /// passage, the Mox Lair off the camp; Kaethi Rose at the Retread Camp; H.P. Miles "next to Gun Dealer Doris (the
    /// lowest level of the Retread Camp next to the fire pit)"; three miasma kinds in the Mox Lair (Actinium, Thorium,
    /// Mutated), the mutated ones with the fewest hit points; the Alpha Mox level 36 with "a really high health
    /// regeneration rate"; the Mad Bomber a level 36 polarity gunner in the Eastern Fringe, armor that strips quickly and
    /// health that takes longer, though "no where near as long as the Alpha Mox". TaRapedia Mox Clearing: Ellen "not too
    /// far in", Kaethi "stands next to Ellen". The door sets a player down at (19.3, 82.5, -4.4) (map_link 30).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The navmesh of the caves is in four parts with no path between them: the first room, main passage and Eastern
    ///    Fringe; the Retread Camp and the branch above it; the Western Grotto; the Mox Lair. A creature walks only in its
    ///    own part, so each mission puts its creatures in one part. A player walks on the client's ground and is not held
    ///    to it.
    ///  - The Retreads: Cormans (Amee Corman's class 6340 for the women, Dr. Samual Corman's 6339 for the men, each with
    ///    that NPC's look), friendly, level 36, on the upper floor of the camp by the medical area; Miles on its lowest
    ///    floor. Ellen's package is a server-only id (her own); the others the client's.
    ///  - Five public pools of three Mox in the Western Grotto, so that Mox Clearing's twelve are there to kill (the
    ///    user's choice of 2026-10-25), and four public pools of three Mutated Miasmas in the Mox Lair for Miasma
    ///    Mutations' ten samples; respawn 120 s, as the cave pools (in the squad's copy the squad clock governs). Their Mox
    ///    a copy of the Pools Mox (540011, also in the Pools zone) at level 33, so that only the caves' Mox count.
    ///  - The Alpha Mox: a copy of the Velon Hollow Mox Alpha Male at level 36 with more hit points (no regeneration);
    ///    the Mutated Miasma a copy of the Pools Miasma at level 33 with fewer hit points; the Docile Miasma friendly, no
    ///    attack; the pump miasmas "Actinium Miasma" copies of the Pools Miasma at level 34; the Mad Bomber a hostile AFS
    ///    soldier body (the Clean Slate berserk soldier's) at level 36 with a pistol shot at level 36 damage (no polarity
    ///    gun attack for that body). All but the pools' creatures are personal (their missions' scenes).
    ///
    /// Ids: creatures 760221-760230, pools 760221-760224 (the Retreads) and 760230-760238, action 760040, free before.
    /// Plain SQL, valid on SQLite and MySQL; down takes out only these rows.
    /// </summary>
    public static class PoolsRetreadCavesNpcsV1
    {
        public const uint MapContextId = 1694;          // the Retread Caves
        public const uint FemaleClass = 6340, MaleClass = 6339;    // NPC_Corman_Swapset_Female / _Male
        public const uint FemaleLook = PoolsRetreadCampNpcsV1.Amee, MaleLook = 95;   // Amee Corman, Dr. Samual Corman
        public const uint RetreadLevel = 36;

        public const uint Ellen = 760221, EllenName = 7083, EllenPackage = 760221;
        public const uint Kaethi = 760222, KaethiName = 4377, KaethiPackage = 336;
        public const uint Donius = 760223, DoniusName = 7078, DoniusPackage = 849;
        public const uint Miles = 760224, MilesName = 7080, MilesPackage = 851;

        public const uint AlphaMox = 760225, AlphaMoxName = 9028, AlphaMoxLevel = 36;
        public const uint AlphaMoxHitPoints = 16000, AlphaMoxArmor = 360;
        public const uint MutatedMiasma = 760226, MutatedMiasmaName = 9037, MutatedMiasmaLevel = 33;
        public const uint MutatedMiasmaHitPoints = 2400, MutatedMiasmaArmor = 130;
        public const uint DocileMiasma = 760227, DocileMiasmaName = 9038, DocileMiasmaLevel = 33;
        public const uint PumpMiasma = 760228, PumpMiasmaName = 7913, PumpMiasmaLevel = 34;
        public const uint PumpMiasmaHitPoints = 3000, PumpMiasmaArmor = 160;
        public const uint MadBomber = 760229, MadBomberName = 7085, MadBomberLevel = 36;
        public const uint MadBomberHitPoints = 8000, MadBomberArmor = 300;
        public const uint MadBomberPistol = 760040;

        public const uint Mox = 760230, MoxName = 8797, MoxLevel = 33;
        public const uint MoxHitPoints = 4200, MoxArmor = 230;
        public const uint MoxClass = 6021, MiasmaClass = 6170, AlphaClass = 22977;
        public const uint SoldierClass = Plateau.PlateauPerduNpcsV1.SoldierClass;
        public const uint PoolRespawn = 1200;           // spawnpool.respown_time, tenths of a second: 120 s, as the cave pools

        public static readonly (double X, double Y, double Z) DoorArrival = (19.321, 82.5442, -4.3964);
        public static readonly (double X, double Y, double Z) EllenAt = (-40, 62.85, -184);
        public static readonly (double X, double Y, double Z) KaethiAt = (-40.3, 62.85, -176.3);
        public static readonly (double X, double Y, double Z) DoniusAt = (-48, 62.95, -192);
        public static readonly (double X, double Y, double Z) MilesAt = (0, 47.49, -200);

        /// <summary>The Western Grotto's Mox pools, three Mox each.</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] MoxPools =
        {
            (760230, 104, 9.23, -40),
            (760231, 152, 12.65, -56),
            (760232, 176, 10.46, 16),
            (760233, 64, 4.41, -72),
            (760234, 168, 8.54, 0),
        };

        /// <summary>The Mox Lair's Mutated Miasma pools, three each.</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] MutatedPools =
        {
            (760235, 104, 55.43, -288),
            (760236, 144, 47.65, -296),
            (760237, 128, 51.44, -312),
            (760238, 96, 55.82, -238.8),
        };

        public const uint PerPool = 3;

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Retread(Ellen, "Ellen Lenee - Retread Caves", FemaleClass, FemaleLook, EllenName, EllenPackage, EllenAt))
                yield return sql;
            foreach (var sql in Retread(Kaethi, "Kaethi Rose - Retread Caves", FemaleClass, FemaleLook, KaethiName, KaethiPackage, KaethiAt))
                yield return sql;
            foreach (var sql in Retread(Donius, "Dr. Donius - Retread Caves", MaleClass, MaleLook, DoniusName, DoniusPackage, DoniusAt))
                yield return sql;
            foreach (var sql in Retread(Miles, "H.P. Miles - Retread Caves", MaleClass, MaleLook, MilesName, MilesPackage, MilesAt))
                yield return sql;

            // The Alpha Mox: the Velon Hollow Mox Alpha Male's attacks.
            yield return Creature(AlphaMox, "Alpha Mox - Retread Caves (A Mox On Both Our Houses)", AlphaClass, AlphaMoxLevel,
                AlphaMoxHitPoints, AlphaMoxName, 77004, 57001);
            yield return Stat(AlphaMox, AlphaMoxHitPoints, AlphaMoxArmor);
            // The caves' Mox: the Pools Mox's attacks.
            yield return Creature(Mox, "Mox - Retread Caves (Mox Clearing)", MoxClass, MoxLevel, MoxHitPoints, MoxName, 55017, 55018);
            yield return Stat(Mox, MoxHitPoints, MoxArmor);
            // The miasmas: the Pools Miasma's attacks.
            yield return Creature(MutatedMiasma, "Mutated Miasma - Retread Caves (Miasma Mutations)", MiasmaClass, MutatedMiasmaLevel,
                MutatedMiasmaHitPoints, MutatedMiasmaName, 55005, 68089, 56011);
            yield return Stat(MutatedMiasma, MutatedMiasmaHitPoints, MutatedMiasmaArmor);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({DocileMiasma}, 'Docile Miasma - Retread Caves (Miasma Testing)', {MiasmaClass}, 1, {DocileMiasmaLevel}, " +
                         $"1000, {DocileMiasmaName}, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return Creature(PumpMiasma, "Actinium Miasma - Retread Caves (Total Slaughter)", MiasmaClass, PumpMiasmaLevel,
                PumpMiasmaHitPoints, PumpMiasmaName, 55005, 68089, 56011);
            yield return Stat(PumpMiasma, PumpMiasmaHitPoints, PumpMiasmaArmor);
            // The Mad Bomber: the Clean Slate berserk soldier's body and pistol.
            yield return Action(MadBomberPistol, "The Mad Bomber - Retread Caves pistol 1/133", 1, 133, 0.5, 24, 1300, 0, 120, 180, 1);
            yield return Creature(MadBomber, "The Mad Bomber - Retread Caves (The Mad Bomber)", SoldierClass, MadBomberLevel,
                MadBomberHitPoints, MadBomberName, MadBomberPistol);
            yield return Stat(MadBomber, MadBomberHitPoints, MadBomberArmor);
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {MadBomber}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Plateau.PlateauPerduNpcsV1.Berserk};";

            foreach (var (id, x, y, z) in MoxPools)
                yield return Pool(id, x, y, z, MapContextId, Mox, PerPool, PerPool, PoolRespawn, 6);
            foreach (var (id, x, y, z) in MutatedPools)
                yield return Pool(id, x, y, z, MapContextId, MutatedMiasma, PerPool, PerPool, PoolRespawn, 6);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (id, _, _, _) in MutatedPools)
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
            foreach (var (id, _, _, _) in MoxPools)
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
            foreach (var id in new[] { Mox, MadBomber, PumpMiasma, DocileMiasma, MutatedMiasma, AlphaMox })
            {
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
            yield return $"DELETE FROM creature_action WHERE id = {MadBomberPistol};";
            foreach (var id in new[] { Miles, Donius, Kaethi, Ellen })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        /// <summary>A Retread of the caves: a friendly Corman with the look of <paramref name="look"/>, on the caves' map.</summary>
        private static IEnumerable<string> Retread(uint id, string comment, uint classId, uint look, uint name, uint package,
            (double X, double Y, double Z) at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, '{comment}', {classId}, 1, {RetreadLevel}, 1000, {name}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {look};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
            yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
        }
    }
}
