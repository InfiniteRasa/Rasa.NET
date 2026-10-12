using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;

    /// <summary>
    /// The Proving Grounds' pistol and machine gun soldiers fire their guns. A creature's attack is
    /// played by the client as the action and argument it is performed with (actiondata
    /// actionArguments and actorActionFXFamily): the recovery animation and the shot's effect
    /// and sound. 510218, the attack of Capture the Flag's escorts and of the two Infantrymen at
    /// the sandbag post, was WEAPON_ATTACK argument 1, the Thrax pistol's (Weapon_Creature_Bane_
    /// Pistol and its variants; recovery animation family 1028, "Weapon - Thrax Pistol -
    /// Resolve", effect family 276): the soldiers aimed and did damage, but no human plays that
    /// animation and nothing was fired. Their pistol,
    /// Weapon_Human_Redshirt_Pistol_Physical (6271), attacks with WEAPON_ATTACK 133 (weaponclass:
    /// animation family 490, effect family 346), so 510218 is that.
    ///
    /// The Field Gunner's Weapon_Human_Redshirt_MachineGun_Physical (20535) attacks with
    /// WEAPON_MACHINEGUN (149) argument 1 (windup 638, recovery 637, effects 266 and 267); he
    /// was given the AFS light soldier's attack (2, WEAPON_ATTACK 133). He gets 510219, his
    /// machine gun's.
    ///
    /// Down puts both back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Fix_bootcamp_soldier_weapon_attacks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update creature_action set action_arg_id = {BootcampSoldierWeapons.PistolArg} where id = {BootcampSoldierWeapons.Pistol};");
            migrationBuilder.Sql(
                "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                $"values ({BootcampSoldierWeapons.MachineGun}, 'Bootcamp Field Gunner machine gun', {BootcampSoldierWeapons.MachineGunAction}, {BootcampSoldierWeapons.MachineGunArg}, 1, 20, 1000, 0, 10, 15, 1);");
            migrationBuilder.Sql($"update creature set action1 = {BootcampSoldierWeapons.MachineGun} where id = {BootcampBaseNpcs.FieldGunnerId};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update creature set action1 = 2 where id = {BootcampBaseNpcs.FieldGunnerId};");
            migrationBuilder.Sql($"delete from creature_action where id = {BootcampSoldierWeapons.MachineGun};");
            migrationBuilder.Sql($"update creature_action set action_arg_id = 1 where id = {BootcampSoldierWeapons.Pistol};");
        }
    }
}
