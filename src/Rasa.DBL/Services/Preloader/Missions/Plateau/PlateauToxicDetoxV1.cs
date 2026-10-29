using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Toxic Detox (native mission 1053), Plateau: Delta Squad Commander Locke in Martyr's Canyon
    /// asks the player to destroy the five Bane Toxic Waste Pumps at the Septic Bog.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5916-5919 and 6473 (missionconversation);
    /// objective 1 "Destroy 5 Pumps." 5920/6474, counter 5921 "Bane Toxic Waste Pumps Destroyed"
    /// (missionobjective). The Septic Bog is map marker 30 (587, 445, -562); the client map has a
    /// Bane goo lake (TerraBaneGooLake128mV01, 656, 441, -430) and three Bane atmosphere
    /// generators (617, 440, -504) there, and no pump.
    ///
    /// Sourced (TaRapedia Toxic Detox): giver and receiver Locke, 50,000 XP, 4,950 credits, one
    /// of four green items - Dynamic Radial Repair Tool 30, ChiTech Sonic Disperser 32, Vextronics
    /// Rifle 31, Vitalius Sonic Torqueshell Rifle 33; Elite Guards round the pumps; requirement
    /// "Incommunicado (one objective)".
    ///
    /// Reconstructed: level 30, as Incommunicado; offered once the player delivered Locke's
    /// communicator (flag <see cref="DeliveredFlag"/>, ESTIMATE); the pumps are five personal
    /// destroyable scene objects of UsableInertDestBaneRawFuelPumpHouseV02 (21007, the client's
    /// destroyable Bane pump house, augmentation 41 as Cache of the Day's containers), 100 hit
    /// points, on the goo's shore at the navmesh's ground, each its own receipt as Cache of the
    /// Day (WildernessLandingZoneV1); the Septic Bog packs of PlateauMaligoGuardsV1 guard them;
    /// "Radial" Repair Tool read as the Area Repair Tool; the items are the Uncommon base
    /// templates of the bands that hold those levels, no module; one indicator over the pumps.
    /// </summary>
    public static class PlateauToxicDetoxV1
    {
        public const uint MissionId = 1053;
        public const uint Level = 30;
        public const uint Locke = PlateauReconSquadsV1.Locke;
        public const uint DeliveredFlag = PlateauIncommunicadoFlagsV1.LockeFlag;

        public const uint PumpClass = 21007;            // UsableInertDestBaneRawFuelPumpHouseV02
        public const uint PumpHitPoints = 100;
        public const uint IntactState = 110;            // IdesStateIntact
        public const uint DestroyedState = 2;           // StateDestroyed

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint AreaRepairTool = 120434;      // Tool_Avatar_Field_Repair_Area_UNC_30_to_34
        public const uint SonicDisperser = 48788;       // Weapon_Avatar_PropellantGun_Sonic_UNC_32_to_36
        public const uint Rifle = 14610;                // Weapon_Avatar_Rifle_Physical_UNC_31_to_35
        public const uint SonicTorqueshellRifle = 49652; // Weapon_Avatar_Torqueshell_Rifle_Sonic_UNC_33_to_37

        /// <summary>The pumps, on the shore of the goo, at the navmesh's ground.</summary>
        public static readonly ScenePosition[] Pumps =
        {
            new(545, 442.55f, -510),
            new(585, 441.79f, -530),
            new(625, 441.87f, -490),
            new(570, 442.02f, -465),
            new(600, 441.35f, -500),
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Toxic Detox", 5916, Locke, Locke, Level);
            mission.Reward(Experience, Credits, new[] { AreaRepairTool, SonicDisperser, Rifle, SonicTorqueshellRifle });
            mission.FlagPrerequisite(DeliveredFlag, 1, "Locke's communicator delivered (Incommunicado objective 3)");

            mission.Objective(1, 5920, 6474, 0, MissionObjectiveState.Incomplete, counterTextId: 5921);
            mission.Scenario(1, "Destroy five distinct Bane Toxic Waste Pumps", (uint)Pumps.Length);
            mission.Transition(1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition { Script = "data.sequence" };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Pumps.Length; index++)
            {
                var id = (uint)index + 1;
                var role = $"pump-{id}";
                mission.Progress(1, MissionProgressEventKind.ScenarioEvent, id, (uint)Pumps.Length, counter: 1, trigger: id);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, PumpClass,
                    Pumps[index], Orientation: index * 1.2566370614, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, 1, id, PumpHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World = new()
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = new() { new SceneMissionSignal(MissionId, 1, id) }
                };
            }

            mission.Indicator(1, 1, 585, 442, -500, 50);       // the five pumps

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1053",
                "Native1 'Destroy 5 Pumps' (5920/6474), counter5921; texts 5916-5919, 6473. Septic Bog marker30; client map " +
                "has a Bane goo lake and atmosphere generators there, no pump. Class21007 is a destroyable (augmentation41) Bane pump house.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Toxic_Detox",
                "Giver/receiver Delta Squad Commander Locke; requirement 'Incommunicado (one objective)'; 50000XP, 4950 " +
                "credits; one of green Dynamic Radial Repair Tool30/ChiTech Sonic Disperser32/Vextronics Rifle31/Vitalius " +
                "Sonic Torqueshell Rifle33; Elite Guards round the pumps.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauToxicDetoxV1",
                "Level30. Offered after Locke's delivery (flag1040003). Five personal 100HP pumps (class21007) on the goo's " +
                "shore with distinct receipts, as Cache of the Day; places and guards are estimates. Radial read as Area " +
                "Repair Tool. Uncommon base items, no module: 120434, 48788, 14610, 49652. Indicator r50 over the pumps.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
