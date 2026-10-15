using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The NPCs whose death fails the mission they are escorted in (Escort_npc_stats), each with
    /// 4000 health and 300 armour:
    ///
    ///  - Matthew Corman (creature 123, spawnpool 201), "Walking Wounded" (697), who walks the
    ///    Ranja Caverns to the camp. He had no creature_stat row, and a creature without one is
    ///    spawned with 100 health and 100 armour (CreatureManager): a couple of hits from anything
    ///    in the caverns killed him, as the three Xanx killed Ranger Anjuhi (AnjuhiStats);
    ///  - Sgt. Pierre (630070), "Escape Velocity" (666), about 560 m from the Bane cache to the
    ///    Landing Zone, 1000 health;
    ///  - Ranger Milpas (630010), the Alia branch (1390), 800 health;
    ///  - the Alia escort ranger (630001), "Solis" (1407), 600 health.
    ///
    /// The last three had the row WildernessSpawnStatisticsV1 gives the reconstructed Wilderness
    /// creatures: body, mind and spirit 15, the creature row's max_hp as health and 100 armour.
    /// About one Xanx's health, which a pack met on the way takes in seconds. 4000 and 300 is
    /// close to a minute against three of them.
    ///
    /// Guarded: Matthew's row goes in only if he has none, and the others change only from the
    /// values they were seeded with, so a row set by hand is kept; Down puts back only a row still
    /// as Up left it. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class EscortNpcStats
    {
        public const uint Matthew = 123;
        public const uint Pierre = 630070;
        public const uint Milpas = 630010;
        public const uint Ranger = 630001;

        public const int Attribute = 15;
        public const int Health = 4000;
        public const int Armor = 300;
        public const int SeededArmor = 100;

        /// <summary>The seeded rows that are raised: creature and health (armour 100, body, mind and spirit 15).</summary>
        public static readonly (uint Creature, int Health)[] Raised = { (Pierre, 1000), (Milpas, 800), (Ranger, 600) };

        public static IEnumerable<uint> Creatures
        {
            get
            {
                yield return Matthew;
                foreach (var (creature, _) in Raised)
                    yield return creature;
            }
        }

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                $"SELECT {Matthew}, {Attribute}, {Attribute}, {Attribute}, {Health}, {Armor} FROM creature c " +
                $"WHERE c.id = {Matthew} AND NOT EXISTS (SELECT 1 FROM creature_stat s WHERE s.id = {Matthew});";

            foreach (var (creature, health) in Raised)
                yield return $"UPDATE creature_stat SET health = {Health}, armor = {Armor} WHERE id = {creature} " +
                    $"AND body = {Attribute} AND mind = {Attribute} AND spirit = {Attribute} AND health = {health} AND armor = {SeededArmor};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {Matthew} AND body = {Attribute} AND mind = {Attribute} " +
                $"AND spirit = {Attribute} AND health = {Health} AND armor = {Armor};";

            foreach (var (creature, health) in Raised)
                yield return $"UPDATE creature_stat SET health = {health}, armor = {SeededArmor} WHERE id = {creature} " +
                    $"AND body = {Attribute} AND mind = {Attribute} AND spirit = {Attribute} AND health = {Health} AND armor = {Armor};";
        }
    }
}
