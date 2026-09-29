using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// Gives spawning creatures their explosions (CreatureBombs): creature_action rows 60001-60016
    /// and the slots that point at them.
    ///
    ///  - Warden bots (Crucible, Incline, Thunderhead): CR_WARDEN_BOT_DEATH 480/1
    ///    (_SHARED_MINION), 15 m of virulent damage when one dies.
    ///  - Howlers (Palisades, Pools): CR_HOWLER_DEATH 514/1, 15 m of electrical damage when one
    ///    dies. The client takes an exploded Howler away, so it leaves no corpse to loot; the
    ///    Howler boss (Kennilaxx) is left without it for that reason.
    ///  - Predator bosses (520022, 520057): CR_PREDATOR_DEATH 407/1, a bomb on the wreck that
    ///    goes off 3 s after it falls, 10 m.
    ///  - Fithiks (the base table's Bane Fithik 1, Crucible, Palisades, Ashen Desert, Plains):
    ///    CR_FITHIK_SELF_DESTRUCT 180/1 (_MINION), 15 m after a 4.3 s windup, once one is down to a
    ///    fifth of its health. The Fithik bosses do not.
    ///  - Linkers (Abyss, Ashen Desert): CR_LINKER_GROUNDBLAST 264/1 (_SHARED_MINION); the Linker
    ///    boss Atropos 264/2 (_SHARED_BOSS). A bomb on the player hit, 5 m.
    ///
    /// The death actions and the self-destruct sit on a range of 0-0, which the fighting loop
    /// never picks; they are found on the creature when it dies, or when its health falls. Damage
    /// as the rows before them: the argument's DAMAGE_AMOUNT_MIN/MAX x 2^((level - 1) / 8) x 0.25,
    /// a boss not on a boss argument x 0.5 (the Predators' 407/1). These are the client's
    /// proportions: a Howler's death is 500-600 at base against its bite's 19-25, and it hits
    /// accordingly.
    ///
    /// Not given out, as nothing that has them spawns: CR_BOT_DEATH_EXPLOSION, CR_SEEKER_DETONATION,
    /// CR_NECROMITE_SELF_DESTRUCT / _CORPSE_EXPLOSION. Left for later: CR_STALKER_EGG_DROP, whose
    /// EFFECT_RADIUS is 60 m, for the one Stalker that spawns.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wire_creature_bombs : Migration
    {
        private const uint IdMin = 60001;
        private const uint IdMax = 60999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var creatures = CreatureEntry.TableName;

            new CreatureBombPreloader().Preload(migrationBuilder);

            migrationBuilder.Sql($"update {creatures} set action3 = 60001 where id = 531023;");   // Warden Bot Crucible
            migrationBuilder.Sql($"update {creatures} set action3 = 60002 where id = 531044;");   // Warden Bot Incline
            migrationBuilder.Sql($"update {creatures} set action3 = 60003 where id = 540001;");   // Warden Bot Thunderhead
            migrationBuilder.Sql($"update {creatures} set action3 = 60004 where id = 540008;");   // Howler Palisades
            migrationBuilder.Sql($"update {creatures} set action3 = 60005 where id = 540009;");   // Howler Pools
            migrationBuilder.Sql($"update {creatures} set action2 = 60006 where id = 520022;");   // Predator boss 520022
            migrationBuilder.Sql($"update {creatures} set action2 = 60007 where id = 520057;");   // Predator boss 520057
            migrationBuilder.Sql($"update {creatures} set action2 = 60008 where id = 1;");   // Bane Fithik
            migrationBuilder.Sql($"update {creatures} set action3 = 60009 where id = 531022;");   // Fithik Crucible
            migrationBuilder.Sql($"update {creatures} set action3 = 60010 where id = 531068;");   // Fithik Palisades
            migrationBuilder.Sql($"update {creatures} set action3 = 60011 where id = 540006;");   // Fithik Palisades
            migrationBuilder.Sql($"update {creatures} set action3 = 60012 where id = 540018;");   // Fithik Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action3 = 60013 where id = 540027;");   // Fithik Plains
            migrationBuilder.Sql($"update {creatures} set action3 = 60014 where id = 531007;");   // Linker Abyss
            migrationBuilder.Sql($"update {creatures} set action3 = 60015 where id = 531014;");   // Linker Ashen Desert
            migrationBuilder.Sql($"update {creatures} set action2 = 60016 where id = 78;");   // Atropos (Linker boss)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531023 and action3 = 60001;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531044 and action3 = 60002;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540001 and action3 = 60003;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540008 and action3 = 60004;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540009 and action3 = 60005;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520022 and action2 = 60006;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 520057 and action2 = 60007;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 1 and action2 = 60008;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531022 and action3 = 60009;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531068 and action3 = 60010;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540006 and action3 = 60011;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540018 and action3 = 60012;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 540027 and action3 = 60013;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531007 and action3 = 60014;");
            migrationBuilder.Sql($"update {creatures} set action3 = 0 where id = 531014 and action3 = 60015;");
            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = 78 and action2 = 60016;");

            migrationBuilder.Sql($"delete from {actions} where id between {IdMin} and {IdMax};");
        }
    }
}
