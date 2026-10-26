using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>Combat Technicians use the non-dialogue variant of the same client model.</summary>
    public static class CombatTechnicianClasses
    {
        public const uint OldClass = 11320;
        public const uint CombatClass = 7043;
        public static readonly uint[] Creatures = { 47, 530003, 566015, 660021 };
        private const string Guard = "c.id IN (47,530003,566015,660021) AND c.class_id=11320 AND c.faction=0 " +
            "AND NOT EXISTS (SELECT 1 FROM npc_package n WHERE n.id=c.id) " +
            "AND NOT EXISTS (SELECT 1 FROM vendor v WHERE v.id=c.id) " +
            "AND NOT EXISTS (SELECT 1 FROM npc_greeting g WHERE g.id=c.id) " +
            "AND NOT EXISTS (SELECT 1 FROM npc_mission m WHERE m.giver_id=c.id OR m.reciver_id=c.id) " +
            "AND NOT EXISTS (SELECT 1 FROM mission_content_definition m WHERE m.giver_id=c.id OR m.receiver_id=c.id)";

        public static IEnumerable<string> Up()
        {
            // Remember only rows actually converted: rollback must preserve an existing manual fix.
            yield return "CREATE TABLE IF NOT EXISTS combat_technician_class_repair (creature_id INTEGER NOT NULL PRIMARY KEY);";
            yield return "INSERT INTO combat_technician_class_repair (creature_id) SELECT c.id FROM creature c WHERE " + Guard +
                " AND NOT EXISTS (SELECT 1 FROM combat_technician_class_repair r WHERE r.creature_id=c.id);";
            yield return "UPDATE creature SET class_id=7043 WHERE class_id=11320 AND faction=0 " +
                "AND id IN (SELECT creature_id FROM combat_technician_class_repair);";
        }
        public static IEnumerable<string> Down()
        {
            yield return "UPDATE creature SET class_id=11320 WHERE class_id=7043 AND faction=0 " +
                "AND id IN (SELECT creature_id FROM combat_technician_class_repair);";
            yield return "DROP TABLE combat_technician_class_repair;";
        }
    }
}
