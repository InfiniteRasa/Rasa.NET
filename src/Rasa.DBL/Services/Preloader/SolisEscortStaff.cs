using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>The personal ranger copied Forean spearman 37 without its equipped staff.</summary>
    public static class SolisEscortStaff
    {
        public static IEnumerable<string> Up()
        {
            yield return "CREATE TABLE solis_escort_staff_repair (creature_id INTEGER NOT NULL PRIMARY KEY);";
            yield return "INSERT INTO solis_escort_staff_repair (creature_id) SELECT id FROM creature WHERE id=630001 AND class_id=7034 " +
                "AND NOT EXISTS (SELECT 1 FROM creature_appearance WHERE id=630001 AND slot_id=13);";
            yield return "INSERT INTO creature_appearance (id,slot_id,class_id,color) SELECT creature_id,13,10532,1 FROM solis_escort_staff_repair;";
        }
        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM creature_appearance WHERE id=630001 AND slot_id=13 AND class_id=10532 AND color=1 " +
                "AND EXISTS (SELECT 1 FROM solis_escort_staff_repair WHERE creature_id=630001);";
            yield return "DROP TABLE solis_escort_staff_repair;";
        }
    }
}
