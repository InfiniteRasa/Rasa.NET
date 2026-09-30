using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The explosions Wire_creature_bombs gives out, one row per (creature, ability): the client's
    /// CR_* action at the argument named for the creature's rank, damage from the argument's
    /// DAMAGE_AMOUNT_MIN/MAX scaled to the creature's level (x 2^((level - 1) / 8), x 0.25; a boss not
    /// on a boss argument x 0.5). Death actions and the self-destruct have a range of 0-0: the
    /// fighting loop never picks them.
    /// </summary>
    public class CreatureBombPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 60001, "Warden Bot Crucible warden_bot_death", 480, 1, 0.0, 0.0, 0, 0, 714, 951, 4 };
            yield return new object[] { 60002, "Warden Bot Incline warden_bot_death", 480, 1, 0.0, 0.0, 0, 0, 463, 617, 4 };
            yield return new object[] { 60003, "Warden Bot Thunderhead warden_bot_death", 480, 1, 0.0, 0.0, 0, 0, 600, 800, 4 };
            yield return new object[] { 60004, "Howler Palisades howler_death", 514, 1, 0.0, 0.0, 0, 0, 841, 1009, 13 };
            yield return new object[] { 60005, "Howler Pools howler_death", 514, 1, 0.0, 0.0, 0, 0, 1542, 1851, 13 };
            yield return new object[] { 60006, "Predator boss 520022 predator_death", 407, 1, 0.0, 0.0, 0, 0, 872, 1309, 1 };
            yield return new object[] { 60007, "Predator boss 520057 predator_death", 407, 1, 0.0, 0.0, 0, 0, 476, 714, 1 };
            yield return new object[] { 60008, "Bane Fithik fithik_self_destruct", 180, 1, 0.0, 0.0, 0, 4333, 66, 80, 1 };
            yield return new object[] { 60009, "Fithik Crucible fithik_self_destruct", 180, 1, 0.0, 0.0, 0, 4333, 1640, 1963, 1 };
            yield return new object[] { 60010, "Fithik Palisades fithik_self_destruct", 180, 1, 0.0, 0.0, 0, 4333, 316, 378, 1 };
            yield return new object[] { 60011, "Fithik Palisades fithik_self_destruct", 180, 1, 0.0, 0.0, 0, 4333, 316, 378, 1 };
            yield return new object[] { 60012, "Fithik Ashen Desert fithik_self_destruct", 180, 1, 0.0, 0.0, 0, 4333, 1265, 1514, 1 };
            yield return new object[] { 60013, "Fithik Plains fithik_self_destruct", 180, 1, 0.0, 0.0, 0, 4333, 894, 1070, 1 };
            yield return new object[] { 60014, "Linker Abyss linker_groundblast", 264, 1, 1.0, 40.0, 5000, 2633, 349, 523, 1 };
            yield return new object[] { 60015, "Linker Ashen Desert linker_groundblast", 264, 1, 1.0, 40.0, 5000, 2633, 293, 440, 1 };
            yield return new object[] { 60016, "Atropos (Linker boss) linker_groundblast", 264, 2, 1.0, 40.0, 5000, 2633, 67, 101, 1 };
        }
    }
}
