using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The self and ally actions Wire_creature_buffs gives out, one row per (creature, ability): the
    /// client's CR_* action at the argument named for the creature's rank, with its reuse and windup,
    /// a range for when to use it, and for Scourge a tick's damage from the argument scaled to the
    /// creature's level (x 2^((level - 1) / 8), x 0.25).
    /// </summary>
    public class CreatureBuffPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 61001, "Thrax Soldier boss 520003 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61002, "Thrax Soldier boss 520003 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 41, 93, 1 };
            yield return new object[] { 61003, "Thrax Soldier boss 520004 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61004, "Thrax Soldier boss 520004 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 41, 93, 1 };
            yield return new object[] { 61005, "Thrax Soldier boss 520005 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61006, "Thrax Soldier boss 520005 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 41, 93, 1 };
            yield return new object[] { 61007, "Thrax Soldier boss 520006 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61008, "Thrax Soldier boss 520006 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 41, 93, 1 };
            yield return new object[] { 61009, "Thrax Soldier boss 520007 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61010, "Thrax Soldier boss 520007 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 41, 93, 1 };
            yield return new object[] { 61011, "Thrax Soldier boss 520011 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61012, "Thrax Soldier boss 520011 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 2, 5, 1 };
            yield return new object[] { 61013, "Thrax Soldier boss 520013 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61014, "Thrax Soldier boss 520013 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 54, 121, 1 };
            yield return new object[] { 61015, "Thrax Soldier boss 520014 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61016, "Thrax Soldier boss 520014 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 54, 121, 1 };
            yield return new object[] { 61017, "Thrax Soldier boss 520019 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61018, "Thrax Soldier boss 520019 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 5, 12, 1 };
            yield return new object[] { 61019, "Thrax Soldier boss 520021 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61020, "Thrax Soldier boss 520021 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 59, 132, 1 };
            yield return new object[] { 61021, "Thrax Soldier boss 520023 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61022, "Thrax Soldier boss 520023 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 35, 79, 1 };
            yield return new object[] { 61023, "Thrax Soldier boss 520024 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61024, "Thrax Soldier boss 520024 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 35, 79, 1 };
            yield return new object[] { 61025, "Thrax Soldier boss 520025 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61026, "Thrax Soldier boss 520025 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 35, 79, 1 };
            yield return new object[] { 61027, "Thrax Soldier boss 520026 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61028, "Thrax Soldier boss 520026 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 35, 79, 1 };
            yield return new object[] { 61029, "Thrax Soldier boss 520027 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61030, "Thrax Soldier boss 520027 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 35, 79, 1 };
            yield return new object[] { 61031, "Thrax Soldier boss 520029 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61032, "Thrax Soldier boss 520029 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 25, 56, 1 };
            yield return new object[] { 61033, "Thrax Soldier boss 520030 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61034, "Thrax Soldier boss 520030 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 25, 56, 1 };
            yield return new object[] { 61035, "Thrax Soldier boss 520032 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61036, "Thrax Soldier boss 520032 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 25, 56, 1 };
            yield return new object[] { 61037, "Thrax Soldier boss 520033 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61038, "Thrax Soldier boss 520033 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 25, 56, 1 };
            yield return new object[] { 61039, "Thrax Soldier boss 520034 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61040, "Thrax Soldier boss 520034 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 25, 56, 1 };
            yield return new object[] { 61041, "Thrax Soldier boss 520035 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61042, "Thrax Soldier boss 520035 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 25, 56, 1 };
            yield return new object[] { 61043, "Thrax Soldier boss 520043 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61044, "Thrax Soldier boss 520043 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 10, 23, 1 };
            yield return new object[] { 61045, "Thrax Soldier boss 520048 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61046, "Thrax Soldier boss 520048 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 29, 66, 1 };
            yield return new object[] { 61047, "Thrax Soldier boss 520054 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61048, "Thrax Soldier boss 520054 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 15, 33, 1 };
            yield return new object[] { 61049, "Thrax Soldier boss 520058 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61050, "Thrax Soldier boss 520058 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 19, 43, 1 };
            yield return new object[] { 61051, "Thrax Soldier boss 520060 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61052, "Thrax Soldier boss 520060 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 19, 43, 1 };
            yield return new object[] { 61053, "Thrax Soldier boss 520062 thrax_rage", 454, 1, 0.0, 40.0, 30000, 500, 0, 0, 1 };
            yield return new object[] { 61054, "Thrax Soldier boss 520062 thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 19, 43, 1 };
            yield return new object[] { 61055, "Atta Harvester Ashen Desert atta_harvester_warcry", 477, 1, 0.0, 40.0, 7500, 1000, 0, 0, 1 };
            yield return new object[] { 61056, "Atta Harvester Incline atta_harvester_warcry", 477, 1, 0.0, 40.0, 7500, 1000, 0, 0, 1 };
            yield return new object[] { 61057, "Atta Harvester Plains atta_harvester_warcry", 477, 1, 0.0, 40.0, 7500, 1000, 0, 0, 1 };
            yield return new object[] { 61058, "Atta Harvester Thunderhead atta_harvester_warcry", 477, 1, 0.0, 40.0, 7500, 1000, 0, 0, 1 };
            yield return new object[] { 61059, "Phuumz (Atta Harvester boss) atta_harvester_warcry", 477, 1, 0.0, 40.0, 7500, 1000, 0, 0, 1 };
        }
    }
}
