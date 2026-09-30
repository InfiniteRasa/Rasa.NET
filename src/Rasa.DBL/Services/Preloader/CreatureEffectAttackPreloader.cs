using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The attacks Wire_creature_effect_attacks gives out, one row per (creature, ability): the
    /// client's CR_* action at the argument named for the creature's rank, with its range, reuse and
    /// windup, and damage from the argument's DAMAGE_AMOUNT_MIN/MAX scaled to the creature's level
    /// (x 2^((level - 1) / 8), x 0.25). A damage over time's damage is a tick's, the nanites' an
    /// explosion's; the effect-only attacks have none. See that migration for who gets what.
    /// </summary>
    public class CreatureEffectAttackPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 56001, "Treeback Marshes treeback_swarm", 191, 1, 1.0, 40.0, 20000, 2799, 67, 130, 4 };
            yield return new object[] { 56002, "Treeback Palisades treeback_swarm", 191, 1, 1.0, 40.0, 20000, 2799, 28, 55, 4 };
            yield return new object[] { 56003, "Bane Xanx xanx_v1_web", 238, 1, 1.0, 40.0, 30000, 1060, 21, 42, 4 };
            yield return new object[] { 56004, "Xanx Divide xanx_v1_web", 238, 1, 1.0, 40.0, 30000, 1060, 42, 84, 4 };
            yield return new object[] { 56005, "Arioch (Xanx boss) xanx_v2_web", 440, 2, 1.0, 40.0, 30000, 1060, 84, 168, 4 };
            yield return new object[] { 56006, "Atta Harvester Ashen Desert harvester_pheromone", 476, 1, 0.5, 10.0, 12000, 600, 0, 0, 1 };
            yield return new object[] { 56007, "Atta Harvester Incline atta_harvester_pheromone", 476, 1, 0.5, 10.0, 12000, 600, 0, 0, 1 };
            yield return new object[] { 56008, "Atta Harvester Plains atta_harvester_pheromone", 476, 1, 0.5, 10.0, 12000, 600, 0, 0, 1 };
            yield return new object[] { 56009, "Atta Harvester Thunderhead harvester_pheromone", 476, 1, 0.5, 10.0, 12000, 600, 0, 0, 1 };
            yield return new object[] { 56010, "Phuumz atta_harvester_pheromone", 476, 2, 0.5, 10.0, 12000, 600, 0, 0, 1 };
            yield return new object[] { 56011, "Bane Miasma miasma_gas_cloud", 208, 1, 1.0, 10.0, 15000, 600, 0, 0, 1 };
            yield return new object[] { 56012, "Miasma Howling Maw miasma_gas_cloud", 208, 1, 1.0, 10.0, 15000, 600, 0, 0, 1 };
            yield return new object[] { 56013, "Thrax Technician boss 520009 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 3, 6, 4 };
            yield return new object[] { 56014, "Thrax Technician boss 520009 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56015, "Thrax Technician boss 520010 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 3, 6, 4 };
            yield return new object[] { 56016, "Thrax Technician boss 520010 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56017, "Thrax Technician boss 520037 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 23, 49, 4 };
            yield return new object[] { 56018, "Thrax Technician boss 520037 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56019, "Thrax Technician boss 520041 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 23, 49, 4 };
            yield return new object[] { 56020, "Thrax Technician boss 520041 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56021, "Thrax Technician boss 520042 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 23, 49, 4 };
            yield return new object[] { 56022, "Thrax Technician boss 520042 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56023, "Thrax Technician boss 520045 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 23, 49, 4 };
            yield return new object[] { 56024, "Thrax Technician boss 520045 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56025, "Thrax Technician boss 520052 thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 33, 70, 4 };
            yield return new object[] { 56026, "Thrax Technician boss 520052 thrax_polarity_field", 456, 5, 1.0, 60.0, 15000, 500, 0, 0, 1 };
            yield return new object[] { 56027, "Thrax Grenadier boss 520008 explosive_nanites", 457, 1, 1.0, 30.0, 15000, 500, 4, 9, 6 };
            yield return new object[] { 56028, "Thrax Grenadier boss 520061 explosive_nanites", 457, 1, 1.0, 30.0, 15000, 500, 62, 147, 6 };
            yield return new object[] { 56029, "Bane Hunter 41 hunter_net", 403, 1, 1.0, 40.0, 10000, 900, 6, 11, 13 };
            yield return new object[] { 56030, "Bane Hunter 45 hunter_net", 403, 1, 1.0, 40.0, 10000, 900, 5, 10, 13 };
            yield return new object[] { 56031, "Bane Hunter 530007 hunter_net", 403, 1, 1.0, 40.0, 10000, 900, 11, 21, 13 };
            yield return new object[] { 56032, "Bane Hunter 530009 hunter_net", 403, 1, 1.0, 40.0, 10000, 900, 14, 27, 13 };
        }
    }
}
