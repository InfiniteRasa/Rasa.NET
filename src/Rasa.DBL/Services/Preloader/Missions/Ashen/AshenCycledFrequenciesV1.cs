using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Cycled Frequencies (native mission 1275), Ligo Ashen Desert: Captain Marcus Emmons on Stonewall Ridge wants the Bane radar
    /// frequency codes from one of the Bane dropships downed in the Boneyard, so the AFS radars can listen in.
    ///
    /// Native: texts 10248-10252 (missionconversation); objective (missionobjective) 1 "Recover Bane Radar Frequency Codes"
    /// 10263/10264, no counter. No objective conversation. Item class 25473 "Bane Radar Frequency Codes" (template 97475). Usable
    /// name 66 "Bane Dropship Salvage" (usablenameoverride LIGO_ASHEN_RADARCRATE). The client map: five wrecks
    /// BaneVehicleDropshipV01Destroyed in the Boneyard, (537, 282, -279) among them (statics).
    ///
    /// Sourced: TaRapedia Cycled Frequencies: giver and reward giver Captain Marcus Emmons; follow-up On The Same Frequency (1277);
    /// 95,000 XP, 6,300 credits; ChiTech Sonic Rifle, Vextronics Endothermic Polarity Gun, Sonic RPG Launcher, Pulse Shotgun;
    /// "one of the downed dropships".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (triage D5: the zone band). No requirement (the triage's chain: 1275 first).
    ///  - One personal Bane crate (UsableTreasureDispBaneCrateV02 7904; closed 200, opened 201; usabledata None: the client sends 1),
    ///    named "Bane Dropship Salvage" (66), by the wreck at (537, 282, -279), the one nearest the Boneyard's middle (the triage);
    ///    on the client's floor and the navmesh, walked to from the Ashoka waypoint. Its use gives the codes and completes 1
    ///    (a scene result); Emmons takes them at the turn-in, an abandon takes them and the crate.
    ///  - One marker on the crate.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 40 (client entityclass names): Rifle Sonic
    ///    14658 (36-40), Polarity Gun Ice 48712 (38-42, "Endothermic"), Rocket Launcher Sonic 49172 (39-43, "RPG"), Shotgun EMP
    ///    15025 (37-41, "Pulse"); one to choose.
    /// </summary>
    public static class AshenCycledFrequenciesV1
    {
        public const uint MissionId = 1275;
        public const uint Level = 40;
        public const uint Emmons = AshenLeadInNpcsV1.Emmons;
        public const uint Recover = 1;
        public const uint Found = 1;                    // the scenario of the crate's use: its step 1
        public const uint CrateSequence = 1;

        public const uint CodesTemplate = 97475, CodesClass = 25473;
        public const string CodesKey = "ashen-bane-radar-frequency-codes";
        public const uint CrateClass = 7904;            // UsableTreasureDispBaneCrateV02
        public const uint CrateName = 66;               // usablenameoverride LIGO_ASHEN_RADARCRATE "Bane Dropship Salvage"
        public const uint UseArgument = 1;              // usabledata lookup[class][0] is None: the client sends 1
        public const uint ClosedState = 200, OpenedState = 201;     // USE_TD_STATE_CLOSED, USE_TD_STATE_OPENED
        public const string CrateRole = "boneyard-dropship-salvage";

        /// <summary>The wreck the crate lies by (client static BaneVehicleDropshipV01Destroyed).</summary>
        public static readonly ScenePosition WreckAt = new(537f, 282f, -279f);
        /// <summary>The crate (the client floor's height), 12 m off the wreck's middle on its +z side (ESTIMATE; seen from all 32
        /// places round it, the probe of 2026-11-10).</summary>
        public static readonly ScenePosition CrateAt = new(537f, 283.86f, -267f);
        public const double CrateOrientation = 1.5708;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint SonicRifle = 14658;           // Weapon_Avatar_Rifle_Sonic_UNC_36_to_40
        public const uint EndothermicPolarityGun = 48712;   // Weapon_Avatar_PolarityGun_Ice_UNC_38_to_42
        public const uint SonicRpgLauncher = 49172;     // Weapon_Avatar_RocketLauncher_Sonic_UNC_39_to_43
        public const uint PulseShotgun = 15025;         // Weapon_Avatar_Shotgun_EMP_UNC_37_to_41

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Cycled Frequencies", 10248, Emmons, Emmons, Level);
            mission.Reward(Experience, Credits, new[] { SonicRifle, EndothermicPolarityGun, SonicRpgLauncher, PulseShotgun });
            mission.Scenario(Found, "The Bane Radar Frequency Codes found in the dropship salvage", 1);

            mission.Objective(Recover, 10263, 10264, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Recover);
            mission.Progress(Recover, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Found);
            mission.Action(Recover, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Recover, 1, CrateAt.X, CrateAt.Y, CrateAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1275",
                "Native1 'Recover Bane Radar Frequency Codes' 10263/10264, no counter; no objectiveconversation. Texts 10248-10252. " +
                "Item class25473 Bane Radar Frequency Codes (template97475). Usable name 66 'Bane Dropship Salvage'. Boneyard " +
                "wrecks BaneVehicleDropshipV01Destroyed (537, 282, -279) and four more (statics).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Cycled_Frequencies",
                "Giver/reward giver Captain Marcus Emmons; follow-up On The Same Frequency; 95000XP, 6300 credits; ChiTech Sonic " +
                "Rifle, Vextronics Endothermic Polarity Gun, Sonic RPG Launcher, Pulse Shotgun; one of the downed dropships.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenCycledFrequenciesV1",
                "Level40 estimated, no requirement. One personal Bane crate 7904 (name66; arg1; 200->201) by the wreck at (537, " +
                "-279); its use gives the codes and completes 1; Emmons takes them. One marker. Items 14658, 48712, 49172, 15025 " +
                "(lowest Uncommon, band of level 40), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own salvage crate by the Boneyard wreck, searched once for the codes.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(CodesKey, CodesTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Actors[CrateRole] = new SceneActorDefinition(CrateRole, SceneActorKind.Object, CrateClass, CrateAt,
                Orientation: CrateOrientation, InitialObjectState: ClosedState, NameOverrideId: CrateName,
                UseAction: new SceneObjectAction(MissionId, Recover, CrateSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{CrateRole}", CrateRole) } };
            scene.Sequences[CrateSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{CrateRole}-searched", CrateRole, OpenedState),
                    new SetInteractionIntent($"{CrateRole}-done", CrateRole, false)
                },
                Character = { new IssueMissionItemIntent($"{CrateRole}-codes", MissionId, CodesKey, CodesTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, Found, 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
