using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Thrax and Maw that fired another family's weapon now fire their own: creature_action rows
    /// 70001-70031, each creature's own, put in the slot of the row it replaces. The client has a
    /// weapon class for each family (generated.client.weaponclass), and it names the attack pair:
    ///
    ///  - Thrax Technicians - the thirteen world rows, the base table's and the Divide's, and the
    ///    seven Technician bosses: Weapon_Creature_Thrax_Technician, WEAPON_ATTACK 1/296, EMP,
    ///    20 m, where they fired the Soldier's rifle 1/79 (or, the base table's and the Divide's,
    ///    the Hunter's rifle 1/244 and a 174/33 swing no weapon class plays, now
    ///    Weapon_Creature_Thrax_Melee 174/46).
    ///  - Thraxus Machina (520001): Weapon_Creature_Thrax_Machina 1/250, Electrical, 40 m.
    ///  - The Grenadier bosses, Karem Zul and Orax: Weapon_Creature_Bane_Grenade 1/229, Fire,
    ///    beside the rifle they keep - the pair every world Grenadier carries.
    ///  - Seymour, the Maw boss (520036): Weapon_Creature_Maw 174/9, 4 m, where it swung the
    ///    Thrax soldier's melee.
    ///  - The pistol Thrax Soldiers of the base table, the Divide and the Wilderness squads:
    ///    Weapon_Creature_Bane_Pistol 1/1, Laser, 20 m, where they fired 1/133, a player pistol's
    ///    pair.
    ///
    /// Damage, cooldown and the near end of the range are the replaced row's, to be retuned with
    /// the damage curves. Down puts each slot back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Fix_creature_weapons : Migration
    {
        private const uint ActionIdMin = 70001;
        private const uint ActionIdMax = 70999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureWeaponFixPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action1 = 70001 where id = 3;");   // Thrax Soldier
            migrationBuilder.Sql($"update {creatures} set action1 = 70002 where id = 47;");   // Bane Thrax Technician
            migrationBuilder.Sql($"update {creatures} set action2 = 70003 where id = 47;");   // Bane Thrax Technician
            migrationBuilder.Sql($"update {creatures} set action1 = 70004 where id = 520001;");   // Thraxus Machina Super Soldier
            migrationBuilder.Sql($"update {creatures} set action5 = 70005 where id = 520008;");   // Karem Zul - Bootcamp
            migrationBuilder.Sql($"update {creatures} set action1 = 70006 where id = 520009;");   // The Collector - Bootcamp
            migrationBuilder.Sql($"update {creatures} set action1 = 70007 where id = 520010;");   // The Dissector - Bootcamp
            migrationBuilder.Sql($"update {creatures} set action1 = 70008 where id = 520036;");   // Seymour - Marshes
            migrationBuilder.Sql($"update {creatures} set action1 = 70009 where id = 520037;");   // Davinx - Palisades
            migrationBuilder.Sql($"update {creatures} set action1 = 70010 where id = 520041;");   // Lawfoid - Palisades
            migrationBuilder.Sql($"update {creatures} set action1 = 70011 where id = 520042;");   // Mirtanz - Palisades
            migrationBuilder.Sql($"update {creatures} set action1 = 70012 where id = 520045;");   // Sinatrix - Palisades
            migrationBuilder.Sql($"update {creatures} set action1 = 70013 where id = 520052;");   // Inquisitor Krakatus - Plateau
            migrationBuilder.Sql($"update {creatures} set action5 = 70014 where id = 520061;");   // Orax - Pools
            migrationBuilder.Sql($"update {creatures} set action1 = 70015 where id = 530001;");   // Thrax Soldier - Divide
            migrationBuilder.Sql($"update {creatures} set action1 = 70016 where id = 530003;");   // Thrax Technician - Divide
            migrationBuilder.Sql($"update {creatures} set action2 = 70017 where id = 530003;");   // Thrax Technician - Divide
            migrationBuilder.Sql($"update {creatures} set action1 = 70018 where id = 531003;");   // Thrax Technician - Abyss
            migrationBuilder.Sql($"update {creatures} set action1 = 70019 where id = 531010;");   // Thrax Technician - Ashen Deser
            migrationBuilder.Sql($"update {creatures} set action1 = 70020 where id = 531020;");   // Thrax Technician - Crucible
            migrationBuilder.Sql($"update {creatures} set action1 = 70021 where id = 531026;");   // Thrax Technician - Descent
            migrationBuilder.Sql($"update {creatures} set action1 = 70022 where id = 531031;");   // Thrax Technician - Howling Maw
            migrationBuilder.Sql($"update {creatures} set action1 = 70023 where id = 531042;");   // Thrax Technician - Incline
            migrationBuilder.Sql($"update {creatures} set action1 = 70024 where id = 531047;");   // Thrax Technician - Marshes
            migrationBuilder.Sql($"update {creatures} set action1 = 70025 where id = 531057;");   // Thrax Technician - Mires
            migrationBuilder.Sql($"update {creatures} set action1 = 70026 where id = 531065;");   // Thrax Technician - Palisades
            migrationBuilder.Sql($"update {creatures} set action1 = 70027 where id = 531076;");   // Thrax Technician - Plains
            migrationBuilder.Sql($"update {creatures} set action1 = 70028 where id = 531084;");   // Thrax Technician - Plateau
            migrationBuilder.Sql($"update {creatures} set action1 = 70029 where id = 531091;");   // Thrax Technician - Pools
            migrationBuilder.Sql($"update {creatures} set action1 = 70030 where id = 531103;");   // Thrax Technician - Thunderhead
            migrationBuilder.Sql($"update {creatures} set action1 = 70031 where id = 560001;");   // Thrax Soldier - Wilderness
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action1 = 2 where id = 3 and action1 = 70001;");
            migrationBuilder.Sql($"update {creatures} set action1 = 9 where id = 47 and action1 = 70002;");
            migrationBuilder.Sql($"update {creatures} set action2 = 10 where id = 47 and action2 = 70003;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520001 and action1 = 70004;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520008 and action5 = 70005;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520009 and action1 = 70006;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520010 and action1 = 70007;");
            migrationBuilder.Sql($"update {creatures} set action1 = 1 where id = 520036 and action1 = 70008;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520037 and action1 = 70009;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520041 and action1 = 70010;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520042 and action1 = 70011;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520045 and action1 = 70012;");
            migrationBuilder.Sql($"update {creatures} set action1 = 34 where id = 520052 and action1 = 70013;");
            migrationBuilder.Sql($"update {creatures} set action5 = 0 where id = 520061 and action5 = 70014;");
            migrationBuilder.Sql($"update {creatures} set action1 = 2 where id = 530001 and action1 = 70015;");
            migrationBuilder.Sql($"update {creatures} set action1 = 9 where id = 530003 and action1 = 70016;");
            migrationBuilder.Sql($"update {creatures} set action2 = 10 where id = 530003 and action2 = 70017;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53005 where id = 531003 and action1 = 70018;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53016 where id = 531010 and action1 = 70019;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53031 where id = 531020 and action1 = 70020;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53041 where id = 531026 and action1 = 70021;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53049 where id = 531031 and action1 = 70022;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53064 where id = 531042 and action1 = 70023;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53072 where id = 531047 and action1 = 70024;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53088 where id = 531057 and action1 = 70025;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53101 where id = 531065 and action1 = 70026;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53118 where id = 531076 and action1 = 70027;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53130 where id = 531084 and action1 = 70028;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53141 where id = 531091 and action1 = 70029;");
            migrationBuilder.Sql($"update {creatures} set action1 = 53159 where id = 531103 and action1 = 70030;");
            migrationBuilder.Sql($"update {creatures} set action1 = 2 where id = 560001 and action1 = 70031;");

            migrationBuilder.Sql($"delete from {actions} where id between {ActionIdMin} and {ActionIdMax};");
        }
    }
}
