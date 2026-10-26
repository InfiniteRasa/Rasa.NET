using System.Collections.Generic;
namespace Rasa.Services.Preloader
{
    /// <summary>Retire the two original mini-turret pools that overlap Landing Zone's standard front guns.</summary>
    public static class LandingZoneBreachContent
    {
        public static readonly uint[] FrontlinePools = { LandingZoneGarrisonContent.MechPool, 551121, 551123 };
        public static IEnumerable<string> Up()
        {
            yield return "UPDATE spawnpool SET mode=2,creature_1_min_count=0,creature_1_max_count=0 WHERE id IN (106,107) AND map_context_id=1220 AND creature_1_Id=9 AND mode=0;";
            yield return "DELETE FROM control_point_link WHERE control_point_id=4 AND kind=2 AND object_id IN (106,107) AND EXISTS " +
                "(SELECT 1 FROM spawnpool p WHERE p.id=control_point_link.object_id AND p.map_context_id=1220 AND p.creature_1_Id=9 AND p.mode=2);";
        }
        public static IEnumerable<string> Down()
        {
            yield return "UPDATE spawnpool SET mode=0,creature_1_min_count=1,creature_1_max_count=1 WHERE id IN (106,107) AND map_context_id=1220 AND creature_1_Id=9 AND mode=2;";
            foreach (var id in new[] { 106, 107 })
                yield return $"INSERT INTO control_point_link (control_point_id,kind,object_id) SELECT 4,2,{id} FROM spawnpool p " +
                    $"WHERE p.id={id} AND p.map_context_id=1220 AND p.creature_1_Id=9 AND p.mode=0 " +
                    $"AND NOT EXISTS (SELECT 1 FROM control_point_link l WHERE l.control_point_id=4 AND l.kind=2 AND l.object_id={id});";
        }
    }
}
