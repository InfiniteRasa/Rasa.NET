using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Landing Zone assault's Thrax - the Infantry Trainee, Private and Sergeant
    /// (Bane_Thrax_Soldier_Rifle 4047) and the boss (Bane_Thrax_Soldier_Rifle_Boss 10504) - were
    /// made with no weapon row, and the client drew them with empty hands. Each is given
    /// Weapon_Creature_Bane_Rifle (3878), whose weaponclass attack (1/79) is the one their
    /// creature_action rows shoot, as Old Scratch (creature 80, of the boss's class) carries.
    /// The Hunter has its own (LandingZoneHunterWeapon); a Linker none, as every Linker row.
    /// </summary>
    public static class LandingZoneThraxWeapons
    {
        public const uint Rifle = 3878;

        public static readonly uint[] Thrax =
        {
            LandingZoneAssaultContent.Trainee, LandingZoneAssaultContent.Private,
            LandingZoneAssaultContent.Sergeant, LandingZoneAssaultContent.Boss
        };

        public static IEnumerable<string> Up()
        {
            foreach (var id in Thrax)
                yield return "INSERT INTO creature_appearance (id,slot_id,Class_id,color) " +
                    $"SELECT c.id,13,{Rifle},1 FROM creature c WHERE c.id={id} AND c.class_id IN (4047,10504) " +
                    "AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id=c.id AND a.slot_id=13);";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var id in Thrax)
                yield return $"DELETE FROM creature_appearance WHERE id={id} AND slot_id=13 AND Class_id={Rifle} AND color=1 " +
                    $"AND EXISTS (SELECT 1 FROM creature c WHERE c.id={id} AND c.comment LIKE 'Landing Zone %');";
        }
    }
}
