using System;
using System.Collections.Generic;
using Rasa.Data;
using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Structures.Char;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Ashen
{
    /// <summary>
    /// The Ashen tests' reload and history seed. WildernessRanjaGorgeTests.Reload reads the missions back but not the player
    /// flags (a login does, CharacterManager), so an "after a reload" check of a flag gate would read the flags still in
    /// memory; Reload here also reads them back from the character database, as BootcampRuntimeTestHarness's reload does.
    /// </summary>
    internal static class AshenRuntime
    {
        /// <summary>A reload of the character's missions, progress, inventory and player flags from the character database.</summary>
        internal static void Reload(WildernessRuntimeTestHarness harness)
        {
            WildernessRanjaGorgeTests.Reload(harness);
            using var unit = harness.CreateChar();
            harness.Client.Player.PlayerFlags = new Dictionary<uint, uint>(unit.CharacterFlags.Get(harness.Client.Player.Id));
        }

        /// <summary>Completed missions on the Ashen revision in the character's history (as AshenMaunaBarrensTests), then a reload.</summary>
        internal static void SeedCompleted(WildernessRuntimeTestHarness harness, params uint[] completed)
        {
            using (var unit = harness.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    foreach (var mission in completed)
                        unit.CharacterMissions.Runtime.Add(new CharacterMissionHistoryEntry
                        {
                            CharacterId = harness.Client.Player.Id,
                            MissionId = mission,
                            AssignmentId = Guid.NewGuid().ToString("N"),
                            AssignmentGeneration = 1,
                            ContentRevision = AshenMissionDataV1.Revision,
                            CompletedAtUtc = harness.UtcNow,
                            RewardedAtUtc = harness.UtcNow,
                            Outcome = (uint)MissionState.Completed,
                            Rewarded = true
                        });
                });
            Reload(harness);
        }
    }
}
