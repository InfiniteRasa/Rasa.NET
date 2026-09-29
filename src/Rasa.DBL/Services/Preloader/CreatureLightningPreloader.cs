using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The lightning attacks Wire_creature_lightning gives out, one row per (creature, ability): the
    /// client's CR_* lightning at the argument named for the creature's rank, with its range, reuse
    /// and windup, and damage from the argument's DAMAGE_AMOUNT_MIN/MAX scaled to the creature's
    /// level (x 2^((level - 1) / 8), x 0.25; a boss not on a boss argument x 0.5).
    /// </summary>
    public class CreatureLightningPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 57001, "Mox boss 520053 mox_energy_attack", 209, 2, 1.0, 40.0, 4000, 825, 139, 205, 13 };
            yield return new object[] { 57002, "Warnet Queen boss 520016 warnet_queen", 210, 1, 1.0, 40.0, 4000, 1200, 130, 389, 13 };
            yield return new object[] { 57003, "Warnet Queen boss 520016 warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 65, 195, 13 };
            yield return new object[] { 57004, "Warnet Queen boss 520046 warnet_queen", 210, 1, 1.0, 40.0, 4000, 1200, 259, 778, 13 };
            yield return new object[] { 57005, "Warnet Queen boss 520046 warnet_zap", 189, 1, 1.0, 20.0, 4000, 630, 130, 389, 13 };
            yield return new object[] { 57006, "Beam Manta boss 520049 beammanta_lightning", 248, 2, 1.0, 20.0, 4000, 2966, 279, 279, 13 };
            yield return new object[] { 57007, "Thrax Soldier boss 520003 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 654, 1297, 13 };
            yield return new object[] { 57008, "Thrax Soldier boss 520004 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 654, 1297, 13 };
            yield return new object[] { 57009, "Thrax Soldier boss 520005 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 654, 1297, 13 };
            yield return new object[] { 57010, "Thrax Soldier boss 520006 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 654, 1297, 13 };
            yield return new object[] { 57011, "Thrax Soldier boss 520007 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 654, 1297, 13 };
            yield return new object[] { 57012, "Thrax Soldier boss 520011 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 37, 74, 13 };
            yield return new object[] { 57013, "Thrax Soldier boss 520013 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 848, 1682, 13 };
            yield return new object[] { 57014, "Thrax Soldier boss 520014 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 848, 1682, 13 };
            yield return new object[] { 57015, "Thrax Soldier boss 520019 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 82, 162, 13 };
            yield return new object[] { 57016, "Thrax Soldier boss 520021 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 924, 1834, 13 };
            yield return new object[] { 57017, "Thrax Soldier boss 520023 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 550, 1091, 13 };
            yield return new object[] { 57018, "Thrax Soldier boss 520024 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 550, 1091, 13 };
            yield return new object[] { 57019, "Thrax Soldier boss 520025 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 550, 1091, 13 };
            yield return new object[] { 57020, "Thrax Soldier boss 520026 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 550, 1091, 13 };
            yield return new object[] { 57021, "Thrax Soldier boss 520027 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 550, 1091, 13 };
            yield return new object[] { 57022, "Thrax Soldier boss 520029 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 389, 771, 13 };
            yield return new object[] { 57023, "Thrax Soldier boss 520030 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 389, 771, 13 };
            yield return new object[] { 57024, "Thrax Soldier boss 520032 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 389, 771, 13 };
            yield return new object[] { 57025, "Thrax Soldier boss 520033 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 389, 771, 13 };
            yield return new object[] { 57026, "Thrax Soldier boss 520034 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 389, 771, 13 };
            yield return new object[] { 57027, "Thrax Soldier boss 520035 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 389, 771, 13 };
            yield return new object[] { 57028, "Thrax Soldier boss 520043 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 163, 324, 13 };
            yield return new object[] { 57029, "Thrax Soldier boss 520048 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 462, 917, 13 };
            yield return new object[] { 57030, "Thrax Soldier boss 520054 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 231, 459, 13 };
            yield return new object[] { 57031, "Thrax Soldier boss 520058 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 300, 595, 13 };
            yield return new object[] { 57032, "Thrax Soldier boss 520060 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 300, 595, 13 };
            yield return new object[] { 57033, "Thrax Soldier boss 520062 thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 300, 595, 13 };
        }
    }
}
