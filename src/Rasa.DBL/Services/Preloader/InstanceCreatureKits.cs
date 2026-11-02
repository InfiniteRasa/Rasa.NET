using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// What two instances' rows were put in without, that every creature of their family the
    /// world spawns has (Instance_creature_kits):
    ///  - Linker Commander Casso (the Energy Weapon Center's 710023, a Linker boss): the Linkers'
    ///    channel, linker_channel (creature_action 66001, CR_ 410), in his empty fourth action
    ///    slot (Family_abilities).
    ///  - The Hunters of Timora Mines (569015) and Crater Lake (675012): their shield,
    ///    Weapon_Creature_Hunter, in the weapon slot, as HunterShields gave the others. Without
    ///    it a Hunter never draws it and has no shield to reflect with.
    ///
    /// Plain SQL for both providers. Guarded: an action slot that is not empty, a weapon slot that
    /// is filled, are left as they are; Down takes off only what this put on.
    /// </summary>
    public static class InstanceCreatureKits
    {
        public const uint Casso = EnergyWeaponCenterContent.Casso, CassoName = 9380;
        public const uint LinkerChannel = EnergyWeaponCenterContent.LinkerChannel;

        public static readonly uint[] Hunters = { TimoraMinesContent.Hunter, CraterLakeContent.Hunter };

        private static string Ids => string.Join(", ", Hunters);

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE creature SET action4 = {LinkerChannel} WHERE id = {Casso} AND name_id = {CassoName} AND action4 = 0 " +
                $"AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {LinkerChannel});";

            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                $"SELECT c.id, {HunterShields.WeaponSlot}, {HunterShields.Shield}, {HunterShields.Color} FROM creature c " +
                $"WHERE c.id IN ({Ids}) AND c.class_id = 10166 " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id AND a.slot_id = {HunterShields.WeaponSlot});";
        }

        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM creature_appearance " +
                $"WHERE id IN ({Ids}) AND slot_id = {HunterShields.WeaponSlot} AND Class_id = {HunterShields.Shield};";

            yield return $"UPDATE creature SET action4 = 0 WHERE id = {Casso} AND name_id = {CassoName} AND action4 = {LinkerChannel};";
        }
    }
}
