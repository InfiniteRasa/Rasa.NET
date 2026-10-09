using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The battle cry package of each creature class that has one, and of the one creature row
    /// that has its own (creature_battlecry; the game server's Battlecries).
    ///
    /// Reconstructed: the assignment was the 2009 server's, and the client holds only what a
    /// package sounds like - generated/client/battlecrypackages, (package, cry type) to an audio
    /// set. A row is seeded where the audio set names the creature and the entity class name
    /// carries the same name:
    ///
    ///  - "Creature Thrax Soldier Agro" ... (package 12): classes named Thrax_Soldier.
    ///  - "Creature Thrax Technician ..." (13): Thrax_Technician.
    ///  - "Creature Lightbender Alert" (10), "Creature Bane Linker Alert" (14), "Creature
    ///    Caretaker Alert" (15), "Creature Treelurker Alert" (3), "Creature Treemite Alert" (7).
    ///  - "Battlecry Brann Female ..." (21) and "Battlecry Brann Male ..." (22): classes named
    ///    Brann with that sex.
    ///
    /// 58 classes, turrets left out. Not seeded, because nothing says whose they are: the Thrax
    /// Officer's (11) - no class is named Officer - and the six human voices (8, 9, 16 to 19),
    /// which are one NPC's each and not a class's; a Thrax Grenadier and a Thrax Machina have no
    /// package of their name. Packages 2, 5 and 6 are placeholders (an avatar's grunts, a mining
    /// laser, a door) and 20 is 14 again.
    ///
    /// The one creature row: the Proving Grounds' Training Officer (Add_npc_patrols) has the
    /// drill sergeant's package (4), which is two patrol cries and nothing else - "Bark Drill
    /// Seargent Start Patrol" and "... Stop Patrol".
    /// </summary>
    public class CreatureBattlecryPreloader : PreloaderBase, IPreloader
    {
        public const uint DrillSergeant = 4;
        public const uint Treelurker = 3;
        public const uint Treemite = 7;
        public const uint Lightbender = 10;
        public const uint ThraxSoldier = 12;
        public const uint ThraxTechnician = 13;
        public const uint Linker = 14;
        public const uint Caretaker = 15;
        public const uint BrannFemale = 21;
        public const uint BrannMale = 22;

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureBattlecryEntry.TableName, new[] { "scope", "target_id", "package_id" });
        }

        /// <summary>The rows, as (scope, target id, package id).</summary>
        public static IEnumerable<object[]> Rows()
        {
            yield return Class(3762, ThraxSoldier);    // Bane_Thrax_Soldier_Pistol
            yield return Class(4047, ThraxSoldier);    // Bane_Thrax_Soldier_Rifle
            yield return Class(10503, ThraxSoldier);   // Bane_Thrax_Soldier_Pistol_Boss
            yield return Class(10504, ThraxSoldier);   // Bane_Thrax_Soldier_Rifle_Boss
            yield return Class(20757, ThraxSoldier);   // Bane_Thrax_Soldier_Pistol_NoBeaminBirth
            yield return Class(20768, ThraxSoldier);   // Bane_Thrax_Soldier_Melee
            yield return Class(21827, ThraxSoldier);   // Holographic_Thrax_Soldier
            yield return Class(24558, ThraxSoldier);   // Holographic_Thrax_Soldier_Boss
            yield return Class(25580, ThraxSoldier);   // Bane_Thrax_Soldier_Pistol_NO_XP
            yield return Class(25581, ThraxSoldier);   // Bane_Thrax_Soldier_Rifle_NO_XP
            yield return Class(29059, ThraxSoldier);   // Bane_Thrax_Soldier_Pistol_V2_CP_Boss
            yield return Class(29426, ThraxSoldier);   // Test_Bane_Thrax_Soldier_Pistol_Jolene
            yield return Class(29769, ThraxSoldier);   // Bane_Thrax_Soldier_Grunt_Pistol
            yield return Class(29773, ThraxSoldier);   // Bane_Thrax_Soldier_Grunt_Rifle
            yield return Class(29774, ThraxSoldier);   // Bane_Thrax_Soldier_Grunt_Rifle_TEST
            yield return Class(30417, ThraxSoldier);   // EPIC_Thrax_Soldier_Pistol
            yield return Class(30418, ThraxSoldier);   // EPIC_Thrax_Soldier_Rifle

            yield return Class(7043, ThraxTechnician);    // Bane_Thrax_Technician
            yield return Class(10502, ThraxTechnician);   // Bane_Thrax_Technician_Boss
            yield return Class(11320, ThraxTechnician);   // NPC_Bane_Thrax_Technician
            yield return Class(21826, ThraxTechnician);   // Holographic_Thrax_Technician
            yield return Class(24556, ThraxTechnician);   // Holographic_Thrax_Technician_Boss
            yield return Class(26353, ThraxTechnician);   // NPC_Bane_Thrax_Technician_Boss
            yield return Class(30419, ThraxTechnician);   // EPIC_Thrax_Technician

            yield return Class(7120, Lightbender);    // Bane_Lightbender_Standard
            yield return Class(10333, Lightbender);   // Bane_Lightbender_Standard_Boss
            yield return Class(10857, Lightbender);   // Bane_Lightbender_Alternate
            yield return Class(21828, Lightbender);   // Holographic_Lightbender
            yield return Class(24077, Lightbender);   // Bane_Lightbender_Alternate_Boss

            yield return Class(9751, Linker);    // Bane_Linker
            yield return Class(10335, Linker);   // Bane_Linker_Boss
            yield return Class(29058, Linker);   // Bane_Linker_V3_CP_Boss
            yield return Class(29778, Linker);   // Bane_Linker_Grunt

            yield return Class(9244, Caretaker);    // Bane_Caretaker
            yield return Class(10321, Caretaker);   // Bane_Caretaker_Boss
            yield return Class(21829, Caretaker);   // Holographic_Caretaker
            yield return Class(24557, Caretaker);   // Holographic_Caretaker_Boss
            yield return Class(29951, Caretaker);   // Bane_Caretaker_Grunt
            yield return Class(30415, Caretaker);   // EPIC_Caretaker

            yield return Class(6039, Treelurker);    // Creature_Treelurker
            yield return Class(10347, Treelurker);   // Creature_Treelurker_Boss
            yield return Class(29429, Treelurker);   // Test_Creature_Thrax_Treelurker_Jolene

            yield return Class(6040, Treemite);    // Creature_Treemite
            yield return Class(10348, Treemite);   // Creature_Treemite_Boss

            yield return Class(7254, BrannFemale);    // Redshirt_Brann_Female
            yield return Class(7775, BrannFemale);    // NPC_Brann_Swapset_Female
            yield return Class(24067, BrannFemale);   // Vendor_Brann_Female
            yield return Class(24560, BrannFemale);   // Redshirt_Brann_Female_Boss
            yield return Class(24562, BrannFemale);   // NPC_Brann_Swapset_Female_Boss
            yield return Class(25611, BrannFemale);   // Redshirt_Brann_Incurable_Female
            yield return Class(25617, BrannFemale);   // Redshirt_Brann_Incurable_Female_Boss

            yield return Class(7253, BrannMale);    // Redshirt_Brann_Male
            yield return Class(7776, BrannMale);    // NPC_Brann_Swapset_Male
            yield return Class(23191, BrannMale);   // Redshirt_Brann_Incurable_Male
            yield return Class(24068, BrannMale);   // Vendor_Brann_Male
            yield return Class(24561, BrannMale);   // Redshirt_Brann_Male_Boss
            yield return Class(24564, BrannMale);   // NPC_Brann_Swapset_Male_Boss
            yield return Class(25547, BrannMale);   // Redshirt_Brann_Incurable_Male_Boss

            yield return new object[] { CreatureBattlecryEntry.ScopeCreature, BootcampTrainingOfficer.CreatureId, DrillSergeant };
        }

        protected override IEnumerable<object[]> GetRows() => Rows();

        private static object[] Class(uint classId, uint packageId) =>
            new object[] { CreatureBattlecryEntry.ScopeClass, classId, packageId };
    }
}
