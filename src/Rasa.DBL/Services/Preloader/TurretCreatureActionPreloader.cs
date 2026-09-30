using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The AFS turrets' guns, one row per (turret, zone): WEAPON_ATTACK at the pair of the turret's own
    /// weapon - 1/87 Weapon_Creature_AFS_Turret, 1/242 Weapon_Creature_AFS_Mini_Turret - 0 to 60 m as
    /// the client's rows reach, once a second, Electrical (the client's ENERGY); a shot 8% (standard) or
    /// 6% (light) of a same-level player's base health, +-20%, the 4:3 of the client's 3000 and 2250.
    /// </summary>
    public class TurretCreatureActionPreloader : PreloaderBase, IPreloader
    {
        private static readonly string[] Columns =
        {
            "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage", "damage_type"
        };

        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, Columns);
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 59001, "AFS Turret Ashen Desert", 1, 87, 0.0, 60.0, 1000, 0, 761, 1142, 13 };
            yield return new object[] { 59002, "AFS Turret Crucible", 1, 87, 0.0, 60.0, 1000, 0, 987, 1481, 13 };
            yield return new object[] { 59003, "AFS Light Turret Crucible", 1, 242, 0.0, 60.0, 1000, 0, 740, 1110, 13 };
            yield return new object[] { 59004, "AFS Turret Thunderhead", 1, 87, 0.0, 60.0, 1000, 0, 830, 1245, 13 };
            yield return new object[] { 59005, "AFS Light Turret Thunderhead", 1, 242, 0.0, 60.0, 1000, 0, 622, 934, 13 };
            yield return new object[] { 59006, "AFS Light Turret Abyss", 1, 242, 0.0, 60.0, 1000, 0, 679, 1018, 13 };
            yield return new object[] { 59007, "AFS Turret Abyss", 1, 87, 0.0, 60.0, 1000, 0, 905, 1358, 13 };
            yield return new object[] { 59008, "AFS Turret Incline", 1, 87, 0.0, 60.0, 1000, 0, 640, 960, 13 };
            yield return new object[] { 59009, "AFS Turret Mires", 1, 87, 0.0, 60.0, 1000, 0, 587, 880, 13 };
            yield return new object[] { 59010, "AFS Light Turret Mires", 1, 242, 0.0, 60.0, 1000, 0, 440, 660, 13 };
            yield return new object[] { 59011, "AFS Turret Plains", 1, 87, 0.0, 60.0, 1000, 0, 538, 807, 13 };
            yield return new object[] { 59012, "AFS Light Turret Plains", 1, 242, 0.0, 60.0, 1000, 0, 404, 605, 13 };
            yield return new object[] { 59013, "AFS Light Turret Bootcamp", 1, 242, 0.0, 60.0, 1000, 0, 19, 29, 13 };
            yield return new object[] { 59014, "AFS Light Turret Earth", 1, 242, 0.0, 60.0, 1000, 0, 960, 1440, 13 };
            yield return new object[] { 59015, "AFS Turret Divide", 1, 87, 0.0, 60.0, 1000, 0, 95, 143, 13 };
            yield return new object[] { 59016, "AFS Turret Palisades", 1, 87, 0.0, 60.0, 1000, 0, 190, 285, 13 };
            yield return new object[] { 59017, "AFS Light Turret Wilderness", 1, 242, 0.0, 60.0, 1000, 0, 46, 69, 13 };
            yield return new object[] { 59018, "AFS Turret Wilderness", 1, 87, 0.0, 60.0, 1000, 0, 62, 93, 13 };
            yield return new object[] { 59019, "AFS Turret Howling Maw", 1, 87, 0.0, 60.0, 1000, 0, 1076, 1615, 13 };
            yield return new object[] { 59020, "AFS Light Turret Howling Maw", 1, 242, 0.0, 60.0, 1000, 0, 807, 1211, 13 };
            yield return new object[] { 59021, "AFS Light Turret Descent", 1, 242, 0.0, 60.0, 1000, 0, 404, 605, 13 };
            yield return new object[] { 59022, "AFS Light Turret Marshes", 1, 242, 0.0, 60.0, 1000, 0, 339, 509, 13 };
            yield return new object[] { 59023, "AFS Turret Marshes", 1, 87, 0.0, 60.0, 1000, 0, 453, 679, 13 };
            yield return new object[] { 59024, "AFS Turret Plateau", 1, 87, 0.0, 60.0, 1000, 0, 269, 404, 13 };
            yield return new object[] { 59025, "AFS Light Turret Plateau", 1, 242, 0.0, 60.0, 1000, 0, 202, 303, 13 };
            yield return new object[] { 59026, "AFS Light Turret Pools", 1, 242, 0.0, 60.0, 1000, 0, 262, 393, 13 };
            yield return new object[] { 59027, "AFS Turret Pools", 1, 87, 0.0, 60.0, 1000, 0, 349, 523, 13 };
        }
    }
}
