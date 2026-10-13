using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The rows of Add_world_wormholes, as SQL.
    ///
    ///  - The Ligo Crucible wormhole, teleporter 358. The client has a name for it in its
    ///    waypoint table ("Wormhole: Ligo Crucible") and a map marker ("Wormhole: Outpost
    ///    Intrepid") on adv_arieki_ligo_burningsteps, map 1993. Its row was a placeholder, of
    ///    no type and on no map, "Wormhole: Ligo Crucible Dupe see 86" (86 is Outpost Aurora's
    ///    waypoint on Ligo Thunderhead, which has no wormhole). It is made the wormhole the
    ///    marker shows: where the marker is, on the floor of the sunken square in the middle of
    ///    Outpost Intrepid's hall, and of the class the other world wormholes are, 25408
    ///    (UsableTwoStateHumWormhole).
    ///  - The Guardian Prominence Personal Wormhole, teleporter 425, had class 28478, which is
    ///    MisPalisadesItemDatabladedeployment, a mission item: it is given 28474,
    ///    UsableTwoStateHumPersonalWormholeV01.
    ///  - The wormhole network's one lock: from the Concordia Divide wormhole (343) to the
    ///    three on Arieki - Torden Plains (348), Ligo Crucible (358) and Torden Abyss (400) -
    ///    only once the player has taken mission 1038, "Arieki" ("Go through the wormhole to
    ///    Arieki."), at least once.
    /// </summary>
    public static class WorldWormholesSeed
    {
        public const uint CrucibleWormholeId = 358;
        public const uint CrucibleMapContextId = 1993;
        public const uint WormholeClassId = 25408;

        public const uint GuardianProminenceWormholeId = 425;
        public const uint GuardianProminenceWrongClassId = 28478;
        public const uint PersonalWormholeClassId = 28474;

        public const uint DivideWormholeId = 343;
        public const uint TordenPlainsWormholeId = 348;
        public const uint TordenAbyssWormholeId = 400;
        public const uint AriekiMissionId = 1038;

        private static readonly string[] TeleporterColumns =
            { "id", "class_id", "type", "description", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id" };

        public static readonly object[] CrucibleWormhole =
            { CrucibleWormholeId, WormholeClassId, (byte)3, "Wormhole: Ligo Crucible", 966.44, 140.0, 59.95, 0.0, CrucibleMapContextId };

        /// <summary>What row 358 was: a placeholder.</summary>
        public static readonly object[] CruciblePlaceholder =
            { CrucibleWormholeId, 0u, (byte)0, "Wormhole: Ligo Crucible Dupe see 86", 0.0, 0.0, 0.0, 0.0, 0u };

        private static readonly string[] LockColumns =
            { "id", "from_teleporter_id", "to_teleporter_id", "kind", "value", "comment" };

        public static readonly object[][] Locks =
        {
            new object[] { 1u, DivideWormholeId, TordenPlainsWormholeId, WormholeLockEntry.KindMissionAccepted, AriekiMissionId, "Divide -> Torden Plains: mission 1038 Arieki taken" },
            new object[] { 2u, DivideWormholeId, CrucibleWormholeId, WormholeLockEntry.KindMissionAccepted, AriekiMissionId, "Divide -> Ligo Crucible: mission 1038 Arieki taken" },
            new object[] { 3u, DivideWormholeId, TordenAbyssWormholeId, WormholeLockEntry.KindMissionAccepted, AriekiMissionId, "Divide -> Torden Abyss: mission 1038 Arieki taken" }
        };

        /// <summary>
        /// The id the migration had first. Two migrations were numbered 20261207000000 - this
        /// one and Add_control_point_turrets - so it was given 20261208000000. A world that ran
        /// it under the first id has its table and rows; Add_world_wormholes is written so that
        /// running it again changes nothing, and takes the first id off the history
        /// (<see cref="ForgetFirstIdStatement"/>).
        /// </summary>
        public const string FirstMigrationId = "20261207000000_Add_world_wormholes";

        /// <summary>The history row of a world that ran the migration under <see cref="FirstMigrationId"/>.</summary>
        public static string ForgetFirstIdStatement =>
            $"delete from __EFMigrationsHistory where MigrationId = '{FirstMigrationId}';";

        /// <summary>
        /// The rows Up adds and the class it puts right, in order; plain SQL either provider
        /// takes, and the same whether or not they have been run before: 358 is set to the
        /// wormhole, 425's class only changes from the wrong one, and the locks are put in
        /// anew.
        /// </summary>
        public static IEnumerable<string> InsertStatements
        {
            get
            {
                yield return Update(CrucibleWormhole);
                yield return $"update {TeleporterEntry.TableName} set class_id = {PersonalWormholeClassId} where id = {GuardianProminenceWormholeId} and class_id = {GuardianProminenceWrongClassId};";
                yield return $"delete from {WormholeLockEntry.TableName} where id between {Locks.Min(row => (uint)row[0])} and {Locks.Max(row => (uint)row[0])};";
                yield return Insert(WormholeLockEntry.TableName, LockColumns, Locks);
            }
        }

        /// <summary>What Down takes out and puts back, before the table is dropped.</summary>
        public static IEnumerable<string> DeleteStatements
        {
            get
            {
                yield return Update(CruciblePlaceholder);
                yield return $"update {TeleporterEntry.TableName} set class_id = {GuardianProminenceWrongClassId} where id = {GuardianProminenceWormholeId} and class_id = {PersonalWormholeClassId};";
            }
        }

        /// <summary>The teleporter row with the id of <paramref name="row"/> set to the rest of it.</summary>
        private static string Update(object[] row)
        {
            return $"update {TeleporterEntry.TableName} set "
                + string.Join(", ", TeleporterColumns.Skip(1).Select((column, i) => $"{column} = {Literal(row[i + 1])}"))
                + $" where id = {Literal(row[0])};";
        }

        private static string Insert(string table, string[] columns, object[][] rows)
        {
            return $"insert into {table} ({string.Join(", ", columns)}) values "
                + string.Join(", ", rows.Select(row => "(" + string.Join(", ", row.Select(Literal)) + ")"))
                + ";";
        }

        private static string Literal(object value)
        {
            return value switch
            {
                string text => "'" + text.Replace("'", "''") + "'",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                _ => System.Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}
