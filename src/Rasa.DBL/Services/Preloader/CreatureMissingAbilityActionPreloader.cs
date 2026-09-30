using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The rows Wire_missing_abilities adds: the abilities the spawning creatures were missing
    /// (68001-68093), and the summoned Necromite's bite and self-destruct at level 42 (68094-68095).
    /// </summary>
    public class CreatureMissingAbilityActionPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 68001, "Thrax Soldier boss 520003 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 322, 654, 1 };
            yield return new object[] { 68002, "Thrax Soldier boss 520003 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 654, 1297, 7 };
            yield return new object[] { 68003, "Thrax Soldier boss 520004 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 322, 654, 1 };
            yield return new object[] { 68004, "Thrax Soldier boss 520004 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 654, 1297, 7 };
            yield return new object[] { 68005, "Thrax Soldier boss 520005 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 322, 654, 1 };
            yield return new object[] { 68006, "Thrax Soldier boss 520005 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 654, 1297, 7 };
            yield return new object[] { 68007, "Thrax Soldier boss 520006 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 322, 654, 1 };
            yield return new object[] { 68008, "Thrax Soldier boss 520006 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 654, 1297, 7 };
            yield return new object[] { 68009, "Thrax Soldier boss 520007 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 322, 654, 1 };
            yield return new object[] { 68010, "Thrax Soldier boss 520007 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 654, 1297, 7 };
            yield return new object[] { 68011, "Thrax Soldier boss 520011 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 18, 37, 1 };
            yield return new object[] { 68012, "Thrax Soldier boss 520011 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 37, 74, 7 };
            yield return new object[] { 68013, "Thrax Soldier boss 520013 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 417, 848, 1 };
            yield return new object[] { 68014, "Thrax Soldier boss 520013 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 848, 1682, 7 };
            yield return new object[] { 68015, "Thrax Soldier boss 520014 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 417, 848, 1 };
            yield return new object[] { 68016, "Thrax Soldier boss 520014 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 848, 1682, 7 };
            yield return new object[] { 68017, "Thrax Soldier boss 520019 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 40, 82, 1 };
            yield return new object[] { 68018, "Thrax Soldier boss 520019 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 82, 162, 7 };
            yield return new object[] { 68019, "Thrax Soldier boss 520021 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 455, 924, 1 };
            yield return new object[] { 68020, "Thrax Soldier boss 520021 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 924, 1834, 7 };
            yield return new object[] { 68021, "Thrax Soldier boss 520023 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 270, 550, 1 };
            yield return new object[] { 68022, "Thrax Soldier boss 520023 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 550, 1091, 7 };
            yield return new object[] { 68023, "Thrax Soldier boss 520024 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 270, 550, 1 };
            yield return new object[] { 68024, "Thrax Soldier boss 520024 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 550, 1091, 7 };
            yield return new object[] { 68025, "Thrax Soldier boss 520025 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 270, 550, 1 };
            yield return new object[] { 68026, "Thrax Soldier boss 520025 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 550, 1091, 7 };
            yield return new object[] { 68027, "Thrax Soldier boss 520026 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 270, 550, 1 };
            yield return new object[] { 68028, "Thrax Soldier boss 520026 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 550, 1091, 7 };
            yield return new object[] { 68029, "Thrax Soldier boss 520027 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 270, 550, 1 };
            yield return new object[] { 68030, "Thrax Soldier boss 520027 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 550, 1091, 7 };
            yield return new object[] { 68031, "Thrax Soldier boss 520029 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 191, 389, 1 };
            yield return new object[] { 68032, "Thrax Soldier boss 520029 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 389, 771, 7 };
            yield return new object[] { 68033, "Thrax Soldier boss 520030 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 191, 389, 1 };
            yield return new object[] { 68034, "Thrax Soldier boss 520030 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 389, 771, 7 };
            yield return new object[] { 68035, "Thrax Soldier boss 520032 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 191, 389, 1 };
            yield return new object[] { 68036, "Thrax Soldier boss 520032 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 389, 771, 7 };
            yield return new object[] { 68037, "Thrax Soldier boss 520033 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 191, 389, 1 };
            yield return new object[] { 68038, "Thrax Soldier boss 520033 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 389, 771, 7 };
            yield return new object[] { 68039, "Thrax Soldier boss 520034 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 191, 389, 1 };
            yield return new object[] { 68040, "Thrax Soldier boss 520034 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 389, 771, 7 };
            yield return new object[] { 68041, "Thrax Soldier boss 520035 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 191, 389, 1 };
            yield return new object[] { 68042, "Thrax Soldier boss 520035 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 389, 771, 7 };
            yield return new object[] { 68043, "Thrax Soldier boss 520043 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 80, 163, 1 };
            yield return new object[] { 68044, "Thrax Soldier boss 520043 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 163, 324, 7 };
            yield return new object[] { 68045, "Thrax Soldier boss 520048 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 227, 462, 1 };
            yield return new object[] { 68046, "Thrax Soldier boss 520048 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 462, 917, 7 };
            yield return new object[] { 68047, "Thrax Soldier boss 520054 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 114, 231, 1 };
            yield return new object[] { 68048, "Thrax Soldier boss 520054 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 231, 459, 7 };
            yield return new object[] { 68049, "Thrax Soldier boss 520058 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 147, 300, 1 };
            yield return new object[] { 68050, "Thrax Soldier boss 520058 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 300, 595, 7 };
            yield return new object[] { 68051, "Thrax Soldier boss 520060 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 147, 300, 1 };
            yield return new object[] { 68052, "Thrax Soldier boss 520060 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 300, 595, 7 };
            yield return new object[] { 68053, "Thrax Soldier boss 520062 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 147, 300, 1 };
            yield return new object[] { 68054, "Thrax Soldier boss 520062 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 300, 595, 7 };
            yield return new object[] { 68055, "Thraxus Machina 520001 thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 382, 777, 1 };
            yield return new object[] { 68056, "Thraxus Machina 520001 thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 777, 1542, 7 };
            yield return new object[] { 68057, "Thraxus Machina 520001 thrax_tectonic_strike", 458, 1, 1.0, 40.0, 10000, 500, 777, 1542, 1 };
            yield return new object[] { 68058, "Thrax Grenadier boss 520008 thrax_tectonic_strike", 458, 1, 1.0, 40.0, 10000, 500, 19, 37, 1 };
            yield return new object[] { 68059, "Thrax Grenadier boss 520008 thrax_necromite", 488, 1, 0.0, 40.0, 30000, 2100, 0, 0, 1 };
            yield return new object[] { 68060, "Thrax Grenadier boss 520061 thrax_tectonic_strike", 458, 1, 1.0, 40.0, 10000, 500, 300, 595, 1 };
            yield return new object[] { 68061, "Thrax Grenadier boss 520061 thrax_necromite", 488, 1, 0.0, 40.0, 30000, 2100, 0, 0, 1 };
            yield return new object[] { 68062, "Thrax Technician 47 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68063, "Thrax Technician boss 520009 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68064, "Thrax Technician boss 520010 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68065, "Thrax Technician boss 520037 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68066, "Thrax Technician boss 520041 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68067, "Thrax Technician boss 520042 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68068, "Thrax Technician boss 520045 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68069, "Thrax Technician boss 520052 technician_revive", 400, 2, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68070, "Thrax Technician 530003 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68071, "Thrax Technician 531003 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68072, "Thrax Technician 531010 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68073, "Thrax Technician 531020 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68074, "Thrax Technician 531026 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68075, "Thrax Technician 531031 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68076, "Thrax Technician 531042 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68077, "Thrax Technician 531047 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68078, "Thrax Technician 531057 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68079, "Thrax Technician 531065 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68080, "Thrax Technician 531076 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68081, "Thrax Technician 531084 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68082, "Thrax Technician 531091 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68083, "Thrax Technician 531103 technician_revive", 400, 1, 0.0, 20.0, 30000, 3335, 0, 0, 1 };
            yield return new object[] { 68084, "Forean Shaman 52 forean_lifeforce_funnel", 255, 1, 0.0, 0.0, 10000, 1500, 0, 0, 1 };
            yield return new object[] { 68085, "Forean Shaman 52 forean_decay", 448, 1, 0.0, 5.0, 6000, 1666, 19, 38, 4 };
            yield return new object[] { 68086, "Forean Gunner 51 forean_chaff", 204, 1, 0.0, 40.0, 60000, 1500, 0, 0, 1 };
            yield return new object[] { 68087, "Miasma 88 miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 17, 21, 3 };
            yield return new object[] { 68088, "Miasma 531035 miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 381, 476, 3 };
            yield return new object[] { 68089, "Miasma 540004 miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 123, 154, 3 };
            yield return new object[] { 68090, "Miasma 540015 miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 95, 119, 3 };
            yield return new object[] { 68091, "Miasma 540034 miasma_coalesce", 485, 1, 0.0, 10.0, 30000, 0, 381, 476, 3 };
            yield return new object[] { 68092, "AFS Soldier 39 human_rushing_blow", 504, 1, 1.0, 20.0, 5000, 1598, 34, 46, 1 };
            yield return new object[] { 68093, "AFS Soldier 97 human_rushing_blow", 504, 1, 1.0, 20.0, 5000, 1598, 34, 46, 1 };
            yield return new object[] { 68094, "Necromite (Thrax's) weapon 1/109", 1, 109, 0.5, 3.0, 3000, 0, 320, 480, 1 };
            yield return new object[] { 68095, "Necromite (Thrax's) necromite_self_destruct", 489, 1, 0.0, 5.0, 1000, 800, 733, 1448, 1 };
        }
    }
}
