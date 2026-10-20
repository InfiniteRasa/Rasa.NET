using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The constant-fire weapons marked autofire (Constant_fire_autofire): windup -1
    /// (ItemTemplateWeaponEntry.AutofireWindup) on every itemtemplate_weapon row whose item class
    /// attacks with one of the client's ConstantFireAttack actions, the one value the tooltip
    /// prints "Autofire" for (client/ui/tooltipwindow.py, windupTime == -1).
    ///
    /// Those weapons fire for as long as the trigger is held: one windup that loops, with the
    /// shots as the ticks of a ConstantFireEffect and no windup or recovery per shot
    /// (client/actions/weapons/constantfire.py; Managers.ConstantFire). By attack action, the
    /// weaponclass column the client picks its action module from:
    ///
    ///  - 140 WEAPON_FLAMETHROWER, weapons.flamethrower: propellant guns;
    ///  - 149 WEAPON_MACHINEGUN, weapons.machinegun: the chainguns, and the Series 2 and 3 laser
    ///    pistols, which fire as chainguns do (CF_PISTOL_S2_EFFECT, _S3_);
    ///  - 179 WEAPON_DENSITYGUN, weapons.densitygun: leech guns;
    ///  - 249 WEAPON_POLARITYGUN, weapons.polaritygun: polarity guns.
    ///
    /// Which weapons the live game marked is not in anything we have: the client takes the windup
    /// from the server, and every data set that survives has the 800 placeholder on every row.
    /// This is the reading of the client above, with the strategy guide's Propellant Gun (no
    /// firing speed, "a constant stream") for the one that is surest. The other weapons keep 800.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Up changes only rows still holding the
    /// placeholder, so a windup a game master has set is left alone, and can run again; Down puts
    /// the placeholder back on those weapons' rows that are -1.
    /// </summary>
    public static class ConstantFireAutofire
    {
        /// <summary>The placeholder windup every weapon row was seeded with.</summary>
        public const int PlaceholderWindup = 800;

        /// <summary>The client's ConstantFireAttack weapon actions: flamethrower, machine gun, density gun, polarity gun.</summary>
        public static readonly uint[] ConstantFireActions = { 140, 149, 179, 249 };

        /// <summary>The item templates whose class attacks with one of ConstantFireActions.</summary>
        public static string ConstantFireTemplates =>
            "SELECT ic.itemTemplateId FROM itemtemplate_itemclass ic JOIN weaponclass wc ON wc.id = ic.itemClassId " +
            $"WHERE wc.attack_action_id IN ({string.Join(", ", ConstantFireActions)})";

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE itemtemplate_weapon SET windup = {Structures.World.ItemTemplateWeaponEntry.AutofireWindup} " +
                $"WHERE windup = {PlaceholderWindup} AND id IN ({ConstantFireTemplates});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE itemtemplate_weapon SET windup = {PlaceholderWindup} " +
                $"WHERE windup = {Structures.World.ItemTemplateWeaponEntry.AutofireWindup} AND id IN ({ConstantFireTemplates});";
        }
    }
}
