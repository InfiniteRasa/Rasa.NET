using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Ranger Anjuhi (creature 98, Stone Anvil, spawnpool 176), whom three Xanx attack in
    /// "Childhood's End" (682), had no creature_stat row. A creature without one is spawned with
    /// 100 health and 100 armour (CreatureManager), and the three Xanx (creature 87, 750 health
    /// and 175 armour each) killed him in seconds, which fails the mission before the player can
    /// kill them (Anjuhi_stats).
    ///
    /// He is given 4500 health and 300 armour: about a minute against all three with no help, so
    /// a player who fights them keeps him alive and one who walks away still loses him. Body,
    /// mind and spirit 15, as the other rows have them.
    ///
    /// Guarded: a row put in by hand is kept, and Down takes away only a row still as it was put
    /// in. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class AnjuhiStats
    {
        public const uint Anjuhi = 98;
        public const int Attribute = 15;
        public const int Health = 4500;
        public const int Armor = 300;

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                $"SELECT {Anjuhi}, {Attribute}, {Attribute}, {Attribute}, {Health}, {Armor} FROM creature c " +
                $"WHERE c.id = {Anjuhi} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {Anjuhi});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {Anjuhi} AND body = {Attribute} AND mind = {Attribute} " +
                $"AND spirit = {Attribute} AND health = {Health} AND armor = {Armor};";
        }
    }
}
