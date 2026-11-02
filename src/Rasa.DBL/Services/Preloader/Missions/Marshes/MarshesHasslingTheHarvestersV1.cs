using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Hassling the Harvesters (native mission 890), Valverde Marshes: Retread Kline at Paludos has no time for the AFS's
    /// tests - he wants the Bane gas harvesters gone.
    ///
    /// Native: texts 4672-4675 and 9805 (missionconversation): log "Destroy 5 Bane Gas Harvesters, then check in with Retread
    /// Kline in Paludos.", offer "those gas harvesters aren't here so the Bane can make us pretty little balloon animals from
    /// the swamp gas ... Get. Rid. Of. Them. You've got a gun, soldier - go use it!", turn-in "I didn't actually think you'd go
    /// through with that ... stop in and see Vincent"; objective 1 "Destroy the Bane Gas Harvesters" 4684/9806 counter 4685
    /// "Gas Harvesters Destroyed:" (missionobjective); no objective conversation. Object 7906
    /// UsableInertDestBaneGasHarvesterV01 "Gas Harvester" (entityclass: an inert destructible, augmentation 41; states 110
    /// intact, 2 destroyed, usabledata). Indicator name 106 "Bane Harvester" (missionobjectiveindicatorlanguage).
    ///
    /// Sourced: TaRapedia Hassling the Harvesters: giver and receiver Retread Kline (Paludos); requires Reaping the Harvest;
    /// level 37; 65,000 XP, 5,400 credits; four items of level 35, not named. World DB: Kline 510166 (pool at -326, 218, -5,
    /// an upstream placeholder spot).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Five personal destructible harvesters (7906, 500 hit points) at the first five of 881's places
    ///    (MarshesHarvestersNpcsV1.Harvesters); each destroyed counts once (scene result set, counter 1). One marker, the
    ///    client's "Bane Harvester", round the field.
    ///  - The rewards (no names on any source, the triage's Q37): the lowest Uncommon template of the band that holds level 35
    ///    for each of the four T3 armour kinds (Bio, Graviton, Mech, Stealth: PoolsLookingForLogosV1's set), one to choose.
    /// </summary>
    public static class MarshesHasslingTheHarvestersV1
    {
        public const uint MissionId = 890;
        public const uint Level = 37;
        public const uint Kline = 510166;
        public const uint Destroy = 1;
        public const uint Destroyed = 1;                // the scenario: one event per harvester
        public const uint Count = 5;

        public const uint HarvesterClass = 7906;        // UsableInertDestBaneGasHarvesterV01
        public const uint HarvesterHitPoints = 500, IntactState = 110, DestroyedState = 2;
        public const uint BaneHarvester = MarshesReapingTheHarvestV1.BaneHarvester;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint BioBoots = 18750;             // Armor_T3_Bio_V02_UNC_Boots_35_to_39
        public const uint GravitonBoots = 23549;        // Armor_T3_Graviton_V02_UNC_Boots_35_to_39
        public const uint MechHelmet = 8855;            // Armor_T3_Mech_V03_UNC_Helmet_35_to_39
        public const uint StealthVest = 10637;          // Armor_T3_Stealth_V02_UNC_Vest_35_to_39

        public static string HarvesterRole(int index) => $"gas-harvester-{index + 1}";
        public static uint HarvesterSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Hassling the Harvesters", 4672, Kline, Kline, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechHelmet, StealthVest });
            mission.Prerequisite(MarshesReapingTheHarvestV1.MissionId);

            mission.Objective(Destroy, 4684, 9806, 0, MissionObjectiveState.Incomplete, counterTextId: 4685);
            mission.Scenario(Destroyed, "A gas harvester destroyed", Count);
            mission.Transition(Destroy);
            for (var id = 1U; id <= Count; id++)
                mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, id, Count, counter: Destroyed, trigger: id);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);

            var field = MarshesHarvestersNpcsV1.FieldCentre;
            mission.NamedIndicator(Destroy, BaneHarvester, field.X, field.Y, field.Z, MarshesHarvestersNpcsV1.FieldRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/890",
                "Native1 'Destroy the Bane Gas Harvesters' 4684/9806 counter4685 'Gas Harvesters Destroyed:'; no " +
                "objectiveconversation. Texts 4672-4675, 9805 (log: destroy 5, Kline in Paludos). Object 7906 " +
                "UsableInertDestBaneGasHarvesterV01 'Gas Harvester' (augmentation 41, states 110/2). Indicator name106.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Hassling_the_Harvesters",
                "Giver/receiver Retread Kline (Paludos); requires Reaping the Harvest; level37; 65000XP, 5400 credits; four items " +
                "of level35, no names.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesHasslingTheHarvestersV1",
                "5 personal destructibles 7906 (500HP) at the first 5 of 881's harvester places east of Falcon Hold; each counts " +
                "once. One 'Bane Harvester' marker round the field. Items 18750, 23549, 8855, 10637 (lowest Uncommon of the band " +
                "holding 35 for Bio, Graviton, Mech, Stealth), one to choose.", 0.3);

            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = MarshesMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Count; index++)
            {
                var role = HarvesterRole(index);
                var sequence = HarvesterSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, HarvesterClass,
                    MarshesHarvestersNpcsV1.Harvesters[index], Orientation: index * 0.9, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Destroy, sequence, HarvesterHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Destroyed, (uint)index + 1) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
