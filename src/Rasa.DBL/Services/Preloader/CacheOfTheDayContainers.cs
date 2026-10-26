using System.Collections.Generic;
using System.Globalization;
namespace Rasa.Services.Preloader
{
    /// <summary>Cache of the Day already owns ten damageable mission objects. Remove only their duplicate scenery.</summary>
    public static class CacheOfTheDayContainers
    {
        private static string N(double value) => value.ToString("R",CultureInfo.InvariantCulture);
        public static IEnumerable<string> Up()
        {
            for(var i=0;i<WildernessPostNpcs.Barrels.Length;i++)
            {
                var b=WildernessPostNpcs.Barrels[i];
                yield return $"DELETE FROM world_destructible WHERE id={WildernessPostNpcs.BarrelId(i)} AND map_context_id=1220 AND class_id={WildernessPostNpcs.BaneBarrel} " +
                    $"AND ABS(pos_x-({N(b.X)}))<0.0001 AND ABS(pos_y-({N(b.Y)}))<0.0001 AND ABS(pos_z-({N(b.Z)}))<0.0001 " +
                    $"AND ABS(rotation-({N(b.Rotation)}))<0.0001 AND hit_points=0 AND creature_id=0 AND creature_count=0 AND side=0 " +
                    "AND comment='Bane barrel in the yard of Sgt. Pierre''s cache';";
            }
        }
        public static IEnumerable<string> Down() => WildernessPostNpcs.CacheBarrelStatements();
    }
}
