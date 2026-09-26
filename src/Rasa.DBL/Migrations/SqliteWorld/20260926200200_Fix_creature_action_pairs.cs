using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// Points thirteen creature_action rows at the attack they describe.
    ///
    /// A row names its attack as an (action, argument) pair, and that pair is what the client
    /// resolves to pick the animation, the FX and the timings. Two kinds of mistake are in the
    /// shipped table. Eight rows name a pair the client has no row for at all, which does not
    /// fail: client/actions/__init__.py hands back a default ActorActionInfo with no windup
    /// animation and no FX families, so the attack lands its damage and draws nothing. Five more
    /// resolve, but to a weapon belonging to some other creature - the Caretaker was firing
    /// DELETEME_Weapon_Avatar_Rifle and the Filcher a Khrab's gun - because action ids and weapon
    /// argument ids share a numeric range, so an ability id left in the argument column usually
    /// lands on something rather than nothing.
    ///
    /// Three of the thirteen had the argument right and the action wrong: their own descriptions
    /// say melee and the argument belongs to WEAPON_MELEE (174). In the other ten the number in
    /// the argument column is an ability's own ACTION id, with action left at 1.
    ///
    /// Every replacement is named after the creature that performs it, and each agrees with the
    /// strategy guide's Enemy Intel entry for that creature: the Boargar "attempts to stun and
    /// charge" (CR_BOARGAR_STUN, CR_BOARGAR_KNOCKBACK) and "rends" in close (Weapon_Creature_
    /// Boargar); the Mox has "a lightning sock at range or a lethal tail swipe"
    /// (CR_MOX_ENERGY_ATTACK, Weapon_Creature_Mox); the Amoeboid closes and "spits various
    /// slimes"; the Caretaker uses "natural AoE abilities" at short range. Two sources that
    /// share no ids, agreeing on the roster.
    ///
    /// Ranges and cooldowns move only where the row contradicted the action it now names - a
    /// melee swing advertised at fifteen metres, a three-metre drone attack advertised at thirty.
    /// Damage columns are left alone: they are this server's own numbers, and an ability that
    /// resolves through its action data brings the client's instead.
    ///
    /// Also adds the Shield Drone's heal. The guide gives it "Offense: None" and a shield that
    /// "protects and heals Bane within its area of effect", and the client has both halves -
    /// CR_SHIELD_DRONE_HEAL restores 500 within a radius of 60 every five seconds. The reflect
    /// itself is a game effect rather than an action and is not in this migration.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Fix_creature_action_pairs : Migration
    {
        private const uint HealRow = 45;
        private const uint ShieldDrone = 85;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            // The pairs.
            migrationBuilder.Sql($"update {actions} set action_id = 174, action_arg_id = 48 where id = 1;");   // Weapon_Creature_Thrax_Melee_Soldier
            migrationBuilder.Sql($"update {actions} set action_id = 174, action_arg_id = 10 where id = 4;");   // Weapon_Creature_Boargar
            migrationBuilder.Sql($"update {actions} set action_id = 174, action_arg_id = 13 where id = 22;");   // Weapon_Creature_Mox, and the guide's "lethal tail swipe"
            migrationBuilder.Sql($"update {actions} set action_id = 203, action_arg_id = 1 where id = 6;");   // CR_FOREAN_LIGHTNING; 194 is AA_RECRUIT_LIGHTNING, a player ability
            migrationBuilder.Sql($"update {actions} set action_id = 431, action_arg_id = 1 where id = 11;");   // CR_AMOEBOID_MELEE
            migrationBuilder.Sql($"update {actions} set action_id = 211, action_arg_id = 1 where id = 12;");   // CR_AMOEBOID_SLIME, the guide's "spitting various slimes"
            migrationBuilder.Sql($"update {actions} set action_id = 395, action_arg_id = 1 where id = 16;");   // CR_CARETAKER_ATTACK; 265 drew DELETEME_Weapon_Avatar_Rifle
            migrationBuilder.Sql($"update {actions} set action_id = 207, action_arg_id = 1 where id = 21;");   // CR_FILCHER_MELEE; 207 under action 1 drew Weapon_Creature_Khrab
            migrationBuilder.Sql($"update {actions} set action_id = 209, action_arg_id = 1 where id = 23;");   // CR_MOX_ENERGY_ATTACK; 209 under action 1 drew Weapon_Creature_Beam_Manta
            migrationBuilder.Sql($"update {actions} set action_id = 274, action_arg_id = 1 where id = 24;");   // CR_AMOEBOID_V1_VOMIT; 431 was copied off row 11, which is the melee
            migrationBuilder.Sql($"update {actions} set action_id = 182, action_arg_id = 1 where id = 25;");   // CR_BOARGAR_STUN, the guide's "attempt to stun"; drew a player sonic rifle
            migrationBuilder.Sql($"update {actions} set action_id = 235, action_arg_id = 1 where id = 26;");   // CR_BOARGAR_KNOCKBACK, the guide's "charge"
            migrationBuilder.Sql($"update {actions} set action_id = 396, action_arg_id = 1 where id = 42;");   // CR_SHIELD_DRONE_ATTACK

            // Reach, where the row disagreed with the action it now names.
            migrationBuilder.Sql($"update {actions} set range_min = 0.5, range_max = 2.0 where id = 12;");   // CR_AMOEBOID_SLIME reaches 2; the row claimed 14
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 2.0 where id = 21;");   // CR_FILCHER_MELEE reaches 1; the row claimed 15
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 3.0 where id = 22;");   // a melee swing at 15 metres
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 3.0 where id = 25;");   // CR_BOARGAR_STUN reaches 3; the row claimed 10
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 3.0 where id = 26;");   // CR_BOARGAR_KNOCKBACK reaches 1; the row claimed 5
            migrationBuilder.Sql($"update {actions} set range_min = 0.5, range_max = 3.0 where id = 42;");   // CR_SHIELD_DRONE_ATTACK reaches 3; the row claimed 30

            // Cooldown, where the client states a longer reuse than the row did.
            migrationBuilder.Sql($"update {actions} set cooldown = 4000 where id = 6;");
            migrationBuilder.Sql($"update {actions} set cooldown = 4000 where id = 12;");
            migrationBuilder.Sql($"update {actions} set cooldown = 4000 where id = 21;");
            migrationBuilder.Sql($"update {actions} set cooldown = 4000 where id = 23;");
            migrationBuilder.Sql($"update {actions} set cooldown = 10000 where id = 24;");
            migrationBuilder.Sql($"update {actions} set cooldown = 4000 where id = 25;");
            migrationBuilder.Sql($"update {actions} set cooldown = 10000 where id = 26;");
            migrationBuilder.Sql($"update {actions} set cooldown = 1000 where id = 42;");

            // A stated damage type wins over anything worked out, so a moved pair needs it re-stated.
            migrationBuilder.Sql($"update {actions} set damage_type = 13 where id = 6;");   // CR_FOREAN_LIGHTNING carries DAMAGE_TYPE 13
            migrationBuilder.Sql($"update {actions} set damage_type = 13 where id = 22;");   // Weapon_Creature_Mox is ENERGY

            // The Shield Drone's heal: 500 to Bane within 60, every five seconds
            // (CR_SHIELD_DRONE_HEAL 245/1, RADIUS_AROUND_SOURCE 60, HEAL_AMOUNT 500).
            migrationBuilder.Sql($"insert into {actions} " +
                "(id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                $"values ({HealRow}, 'Bane Shield Drone heal aura', 245, 1, 0, 60, 5000, 0, 0, 0, 0);");

            migrationBuilder.Sql($"update {creatures} set action2 = {HealRow} where id = {ShieldDrone};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var actions = CreatureActionEntry.TableName;
            var creatures = CreatureEntry.TableName;

            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 48 where id = 1;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 10 where id = 4;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 13 where id = 22;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 194 where id = 6;");
            migrationBuilder.Sql($"update {actions} set action_id = 174, action_arg_id = 431 where id = 11;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 211 where id = 12;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 265 where id = 16;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 207 where id = 21;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 209 where id = 23;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 431 where id = 24;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 182 where id = 25;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 235 where id = 26;");
            migrationBuilder.Sql($"update {actions} set action_id = 1, action_arg_id = 45 where id = 42;");
            migrationBuilder.Sql($"update {actions} set range_min = 3, range_max = 14 where id = 12;");
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 15.0 where id = 21;");
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 15.0 where id = 22;");
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 10.0 where id = 25;");
            migrationBuilder.Sql($"update {actions} set range_min = 1.0, range_max = 5.0 where id = 26;");
            migrationBuilder.Sql($"update {actions} set range_min = 0.5, range_max = 30.0 where id = 42;");
            migrationBuilder.Sql($"update {actions} set cooldown = 2800 where id = 6;");
            migrationBuilder.Sql($"update {actions} set cooldown = 2800 where id = 12;");
            migrationBuilder.Sql($"update {actions} set cooldown = 800 where id = 21;");
            migrationBuilder.Sql($"update {actions} set cooldown = 800 where id = 23;");
            migrationBuilder.Sql($"update {actions} set cooldown = 2800 where id = 24;");
            migrationBuilder.Sql($"update {actions} set cooldown = 2500 where id = 25;");
            migrationBuilder.Sql($"update {actions} set cooldown = 2500 where id = 26;");
            migrationBuilder.Sql($"update {actions} set cooldown = 800 where id = 42;");
            migrationBuilder.Sql($"update {actions} set damage_type = 1 where id = 6;");
            migrationBuilder.Sql($"update {actions} set damage_type = 1 where id = 22;");

            migrationBuilder.Sql($"update {creatures} set action2 = 0 where id = {ShieldDrone};");
            migrationBuilder.Sql($"delete from {actions} where id = {HealRow};");
        }
    }
}
