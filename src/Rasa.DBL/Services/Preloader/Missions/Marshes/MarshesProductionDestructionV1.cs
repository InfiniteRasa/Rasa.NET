using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Production Destruction (native missions 916 and 1033), Valverde Marshes: the follow-ups of Highway Robbery (914). 916
    /// is Retread Vincent's (the theft), 1033 Lieutenant Holloway's (the telling, with an AFS Mech squadmate): destroy the
    /// Bane production kiosks at the Bane Repair Station and kill its Overseers Vesh and Koorva.
    ///
    /// Native: 916 texts 4895-4898 (missionconversation), objectives 1 "Destroy the Production Matrix in the Strider Bay"
    /// 5756/6979 counter 5757 "Production Kiosks Destroyed:", 2 "Eliminate Overseer Vesh" 5758/6980 counter 5759, 3
    /// "Eliminate Factory Overseer Koorva" 5760/6981 counter 5761; 1033 texts 5762-5765,
    /// objectives 5766/6416 counter 5767, 5768/6417 counter 5769, 5770/6418 counter 5771 (missionobjective); no
    /// objectiveconversation. Object 10159 UsableInertDestBaneMachineV01 (augmentation 41, states 110/2); usable name 46
    /// "Production Kiosk" (usablenameoverride).
    ///
    /// Sourced: TaRapedia Production Destruction: level 35; 150,000 XP, 7,600 credits; 916 Retread Vincent, 1033 Lieutenant
    /// Holloway ("you now have a Mech squadmate ... if you let him bite the bullet you are on your own"); TaRapedia history: the
    /// steal branch of Highway Robbery gives Vincent's, the rat-out branch Holloway's. World DB: Overseer Vesh 520035 (pool
    /// 350, 217.5, -525), Koorva 520033 (320, 218.1, -525). The rewards the client's item blocks of each mission (916
    /// 119318-119321, 1033 120009-120012: the four T3 armour kinds V04, level 35-39).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - 5 personal destructible kiosks (10159, 500 HP) round Vesh and Koorva, 15-35 m apart (the triage: client 5, TTH-RC
    ///    "five of the consoles"); each counts once. Kills by anyone's Vesh/Koorva count (the public pools, as 968).
    ///  - 916 needs flag 914001 = 1 (stolen), 1033 = 2 (told): the triage's Q16.
    ///  - 1033's mech (MarshesRetreadCityNpcsV1.Mech) comes up by Holloway at the accept and follows the player (an escort leg
    ///    with no objective, MarshesEscortScene); his death fails nothing; he goes at the turn-in. Markers round the kiosks
    ///    (r40), at Vesh and Koorva (r15).
    /// </summary>
    public static class MarshesProductionDestructionV1
    {
        public const uint VincentMissionId = 916, HollowayMissionId = 1033;
        public const uint Level = 35;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent, Holloway = MarshesRetreadCityNpcsV1.Holloway;
        public const uint Destroy = 1, KillVesh = 2, KillKoorva = 3;
        public const uint Destroyed = 1;                // the scenario: one event per kiosk
        public const uint Vesh = 520035, Koorva = 520033;
        public const double BossRadius = 15, KioskRadius = 40;

        public const uint KioskClass = 10159;           // UsableInertDestBaneMachineV01
        public const uint KioskName = 46;               // usablenameoverride "Production Kiosk"
        public const uint KioskHitPoints = 500, IntactState = 110, DestroyedState = 2;

        public static readonly ScenePosition[] Kiosks =
        {
            new(300f, 217.88f, -500f), new(335f, 217.35f, -500f), new(360f, 217.46f, -505f), new(340f, 217.58f, -555f),
            new(310f, 217.57f, -540f)
        };
        public static readonly (double X, double Y, double Z) KiosksCentre = (330, 217.6, -525);
        public static readonly (double X, double Y, double Z) VeshAt = (350, 217.54, -525), KoorvaAt = (320, 218.12, -525);
        /// <summary>The mech's place at the accept: beside Holloway.</summary>
        public static readonly ScenePosition MechAt = new(-283f, 217.09f, -751f);

        public const uint Experience = 150000;
        public const uint Credits = 7600;
        public static readonly uint[] VincentRewards = { 119318, 119319, 119320, 119321 };
        public static readonly uint[] HollowayRewards = { 120009, 120010, 120011, 120012 };

        /// <summary>The mission's ids and texts: (mission, giver, first text, objectives' name/body/counter, rewards, the flag value).</summary>
        public sealed record Variant(uint MissionId, uint Giver, uint TextId, (uint Name, uint Body, uint Counter)[] Objectives,
            uint[] Rewards, uint ChoiceValue, string Choice);

        public static readonly Variant VincentVariant = new(VincentMissionId, Vincent, 4895,
            new[] { (5756U, 6979U, 5757U), (5758U, 6980U, 5759U), (5760U, 6981U, 5761U) }, VincentRewards,
            MarshesHighwayRobberyV1.StoleFlag, "the supplies stolen");
        public static readonly Variant HollowayVariant = new(HollowayMissionId, Holloway, 5762,
            new[] { (5766U, 6416U, 5767U), (5768U, 6417U, 5769U), (5770U, 6418U, 5771U) }, HollowayRewards,
            MarshesHighwayRobberyV1.ToldFlag, "the AFS told");

        public static string KioskRole(int index) => $"production-kiosk-{index + 1}";
        public static uint KioskSequence(int index) => 1 + (uint)index;
        public static string MechRole => MarshesEscortScene.Role(1, 1);

        public static void Up(MigrationBuilder migration)
        {
            Build(migration, VincentVariant, new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId
            });
            var escort = MarshesEscortsNpcsV1.Scene(HollowayMissionId,
                new MarshesEscortsNpcsV1.Leg(0, 0, 0, null, MarshesRetreadCityNpcsV1.Mech, new[] { MechAt }));
            Build(migration, HollowayVariant, escort);
        }

        private static void Build(MigrationBuilder migration, Variant variant, MissionSceneDefinition scene)
        {
            var id = variant.MissionId;
            var mission = new MarshesMissionDataV1(migration, id, "Production Destruction", variant.TextId, variant.Giver, variant.Giver, Level);
            mission.Reward(Experience, Credits, variant.Rewards);
            mission.Prerequisite(MarshesHighwayRobberyV1.MissionId);
            mission.FlagPrerequisite(MarshesHighwayRobberyV1.ChoiceFlag, variant.ChoiceValue,
                $"Highway Robbery's choice: {variant.Choice}", 2);

            var (name, body, counter) = variant.Objectives[0];
            mission.Objective(Destroy, name, body, 0, MissionObjectiveState.Incomplete, counterTextId: counter);
            mission.Scenario(Destroyed, "A production kiosk destroyed", (uint)Kiosks.Length);
            mission.Transition(Destroy);
            for (var kiosk = 1U; kiosk <= Kiosks.Length; kiosk++)
                mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, kiosk, (uint)Kiosks.Length, counter: Destroyed, trigger: kiosk);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Destroy, 1, KiosksCentre.X, KiosksCentre.Y, KiosksCentre.Z, KioskRadius);

            foreach (var (objective, boss, at) in new[] { (KillVesh, Vesh, VeshAt), (KillKoorva, Koorva, KoorvaAt) })
            {
                (name, body, counter) = variant.Objectives[objective - 1];
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete, counterTextId: counter);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, boss, 1, counter: 0);
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, BossRadius);
            }

            var vincent = id == VincentMissionId;
            mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{id}",
                $"Native1 'Destroy the Production Matrix in the Strider Bay' {variant.Objectives[0].Name}/{variant.Objectives[0].Body} counter" +
                $"{variant.Objectives[0].Counter}, 2 'Eliminate Overseer Vesh' {variant.Objectives[1].Name}/{variant.Objectives[1].Body} " +
                $"counter{variant.Objectives[1].Counter}, 3 'Eliminate Factory Overseer Koorva' {variant.Objectives[2].Name}/" +
                $"{variant.Objectives[2].Body} counter{variant.Objectives[2].Counter}; no objectiveconversation. Texts " +
                $"{variant.TextId}-{variant.TextId + 3}. Object 10159 UsableInertDestBaneMachineV01 (aug41, states 110/2), name46. " +
                $"Item block {variant.Rewards[0]}-{variant.Rewards[^1]}.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Production_Destruction",
                $"Giver {(vincent ? "Retread Vincent" : "Lieutenant Holloway (Mech squadmate)")}; level35; 150000XP, 7600 " +
                "credits; the follow-up of Highway Robbery's " + (vincent ? "steal" : "rat-out") + " branch (history).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesProductionDestructionV1",
                "5 personal destructibles 10159 (500HP) round the server's Vesh 520035 and Koorva 520033 (the public pools). " +
                $"Flag914001 = {variant.ChoiceValue}." +
                (vincent ? "" : " The mech 760341 follows from the accept (no objective; his death fails nothing; gone at the turn-in)."),
                0.3);

            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Kiosks.Length; index++)
            {
                var role = KioskRole(index);
                var sequence = KioskSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, KioskClass, Kiosks[index],
                    Orientation: index * 1.2, InitialObjectState: IntactState, NameOverrideId: KioskName,
                    Destruction: new SceneObjectDestruction(id, Destroy, sequence, KioskHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(id, Destroyed, (uint)index + 1) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration)
        {
            MarshesMissionDataV1.Remove(migration, HollowayMissionId);
            MarshesMissionDataV1.Remove(migration, VincentMissionId);
        }
    }
}
