using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The history rows of the ids two migrations had before they were restamped
    /// (Forget_restamped_migration_ids), as the earlier restamps take theirs off
    /// (WildernessMortarLaunchers.ForgetOldId and the rest).
    ///
    /// Logos_shrine_heights was 20270213000000, then 20270217000000, and is 20270224000000;
    /// Wilderness_urn_enigma_regions was 20270306000000, then 20270307000000, and is
    /// 20270601000000. A World that ran either under an old id ran it again under the new one,
    /// as a no-op - its statements are guarded - and kept the old id's history row, which names
    /// a migration there no longer is. Those rows go; a World that never had them is unchanged.
    ///
    /// Down puts nothing back: the old ids are no migration's. Plain SQL, valid on SQLite and
    /// MySQL alike.
    /// </summary>
    public static class RestampedMigrationIds
    {
        public static readonly string[] OldIds =
        {
            "20270213000000_Logos_shrine_heights",
            "20270217000000_Logos_shrine_heights",
            "20270306000000_Wilderness_urn_enigma_regions",
            "20270307000000_Wilderness_urn_enigma_regions"
        };

        public static IEnumerable<string> Up() =>
            OldIds.Select(id => $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{id}';");
    }
}
