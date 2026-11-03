using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Landing Zone's AFS Mech Defenders fire their missiles (Landing_zone_mech_missiles).
    /// Landing_zone_garrison made the defender (creature 680001, Vehicle_AFS_Mech) with the
    /// Mech's minigun alone, creature_action 13 - the Mech the world already had, creature 40
    /// (NPC_Vehicle_AFS_Mech, pools 113 and 114), has the minigun and its missiles, 15
    /// (WEAPON_ROCKETLAUNCHER 141, argument 17, out to 40 m). The report: they only ever used
    /// their minigun and never their rocket launchers.
    ///
    /// Up gives the defender the missiles in its second slot, only while that slot is empty and
    /// the missiles' row is there. Down empties the slot again, only while it still holds them.
    /// </summary>
    public static class LandingZoneMechMissiles
    {
        public const uint Defender = LandingZoneGarrisonContent.Mech;
        public const uint Minigun = 13, Missiles = 15;

        /// <summary>The id the migration first had, 20270925000000, which Gate_field_nooks shares.</summary>
        public const string OldId = "20270925000000_Landing_zone_mech_missiles";

        /// <summary>The history row a World that ran it under OldId still has.</summary>
        public static readonly string ForgetOldId =
            $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{OldId}';";

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE creature SET action2 = {Missiles} WHERE id = {Defender} AND action1 = {Minigun} AND action2 = 0 " +
                $"AND EXISTS (SELECT 1 FROM creature_action WHERE id = {Missiles});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE creature SET action2 = 0 WHERE id = {Defender} AND action2 = {Missiles};";
        }
    }
}
