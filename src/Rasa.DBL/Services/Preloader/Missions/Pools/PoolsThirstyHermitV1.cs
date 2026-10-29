using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Thirsty Hermit (native mission 1328), Valverde Pools: Supply Chief Riggins sends the player to the Hermit, the
    /// Retreads' go-between, by way of Dr. Hester, whose "medicine" opens the Hermit's door.
    ///
    /// Native: texts 10848-10852 (missionconversation): offer "we had entered an agreement with the Retreads for them to,
    /// shall we say, 'acquire' what ever it is that we needed... we've barely received anything over the last few weeks",
    /// turn-in (the Hermit) "I do love my good friend Dr. Hester's 'Medicine'. Looks like a fine vintage."; objectives 1
    /// "Speak with the Hermit" 10853/10854, 2 "Speak with Dr. Hester" 10856/10857, 3 "Collect \"Medicine\"" 10858/10859, no
    /// counters (missionobjective); talks (objectiveconversation) 1328,1 on package 325 (the Hermit, text 10855) and
    /// 1328,2 on package 1129 (Hester, text 10860 "you want to meet up with my good friend the hermit eh?"). Item:
    /// "Medicine" class 25589 (template 97514). Usable name override 117 "Dr. Hester's \"Medicine\" Cabinet"; the client's
    /// map places no usable cabinet (Hester's medic tent and crates are static).
    ///
    /// Sourced: TaRapedia The Thirsty Hermit: giver Supply Chief Riggins, reward from the Hermit; requires Death Patrol;
    /// Medicinal Measures; follow-up A Cult Divided; 27,500 XP, 3,400 credits, 2 Class VII Advanced Med Packs, 2 Class VII
    /// Adrenaline Boosters, 4 Class VII Concussion Grenades, 4 Class VII Fragmentation Grenades (client classes 22545,
    /// 28468, 26268, 22963). TR MMO DB: level 33. World DB: the Hermit 750050 (PlateauBaileyNpcsV1) on Fractured Butte.
    ///
    /// Reconstructed (the user's choices of 2026-10-25, ESTIMATE): 1328 is built, not its client twin 1327; it requires both
    /// Death Patrol and Medicinal Measures. Order 2, 3, 1 (the offer and Hester's line): Hester's talk opens 3; the cabinet
    /// is a personal AFS item crate (class 11078: no usable cabinet class exists) by Hester's medical crates, usable while
    /// 3 is open, and gives the "medicine"; 3 completes when it is opened and opens 1; the Hermit's talk takes it. The reward items as
    /// the lowest template of each client class, all given. The markers on Hester, the cabinet and the Hermit.
    /// </summary>
    public static class PoolsThirstyHermitV1
    {
        public const uint MissionId = 1328;
        public const uint Level = 33;
        public const uint Riggins = PoolsSnakePitNpcsV1.Riggins;
        public const uint Hermit = Plateau.PlateauBaileyNpcsV1.Hermit, HermitPackage = Plateau.PlateauBaileyNpcsV1.HermitPackage;
        public const uint HesterPackage = PoolsSnakePitNpcsV1.HesterPackage;
        public const uint SpeakHermit = 1, SpeakHester = 2, Collect = 3;

        public const uint MedicineTemplate = 97514, MedicineClass = 25589;
        public const string MedicineKey = "hesters-medicine";
        public const uint CabinetClass = 11078;         // UsableItemDisp_item_crate_hum_v01
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;
        public static readonly ScenePosition CabinetAt = new(-963, 919.4f, 466);
        public static readonly (double X, double Y, double Z) HermitAt = (-430, 920.56, 115);

        public const uint Experience = 27500;
        public const uint Credits = 3400;
        public const uint MedPack = 45062, MedPacks = 2;                        // Class VII Advanced Med Pack
        public const uint AdrenalineBooster = 118814, AdrenalineBoosters = 2;   // Class VII Adrenaline Booster
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 4;   // Class VII Concussion Grenade
        public const uint FragGrenade = 45446, FragGrenades = 4;                // Class VII Fragmentation Grenade

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "The Thirsty Hermit", 10848, Riggins, Hermit, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[]
            {
                (MedPack, MedPacks), (AdrenalineBooster, AdrenalineBoosters), (ConcussionGrenade, ConcussionGrenades), (FragGrenade, FragGrenades)
            });
            mission.Prerequisite(PoolsDeathPatrolV1.MissionId, 1);
            mission.Prerequisite(PoolsMedicinalMeasuresV1.MissionId, 2);

            mission.Objective(SpeakHester, 10856, 10857, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Collect, 10858, 10859, 1, MissionObjectiveState.Inactive);
            mission.Objective(SpeakHermit, 10853, 10854, 2, MissionObjectiveState.Inactive);

            // 2: Hester knows the Hermit; her medicine is in the cabinet.
            mission.Transition(SpeakHester);
            mission.Conversation(SpeakHester, HesterPackage);
            Open(mission, SpeakHester, 1, Collect);

            // 3: the "medicine" from the cabinet.
            mission.Transition(Collect);
            mission.Progress(Collect, MissionProgressEventKind.ScenarioEvent, 1, null, counter: 1);   // the cabinet opened
            Open(mission, Collect, 1, SpeakHermit);

            // 1: the Hermit takes it and opens up.
            mission.Transition(SpeakHermit);
            mission.Conversation(SpeakHermit, HermitPackage);
            mission.Action(SpeakHermit, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("hermit-takes-medicine", MissionId, MedicineKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(SpeakHermit, 2, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(SpeakHester, 1, PoolsSnakePitNpcsV1.HesterAt.X, PoolsSnakePitNpcsV1.HesterAt.Y, PoolsSnakePitNpcsV1.HesterAt.Z);
            mission.Indicator(Collect, 2, CabinetAt.X, CabinetAt.Y, CabinetAt.Z);
            mission.Indicator(SpeakHermit, 3, HermitAt.X, HermitAt.Y, HermitAt.Z);
            mission.Scenario(1, "Hester's medicine taken", 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(MedicineKey, MedicineTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors["cabinet"] = new SceneActorDefinition("cabinet", SceneActorKind.Object, CabinetClass, CabinetAt,
                Orientation: 0, InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Collect, 1, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-cabinet", "cabinet"));
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("open-cabinet", "cabinet", TakenState),
                    new SetInteractionIntent("disable-cabinet", "cabinet", false)
                },
                Character = { new IssueMissionItemIntent("medicine-from-cabinet", MissionId, MedicineKey, MedicineTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, 1, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1328",
                "Native1 'Speak with the Hermit' 10853/10854, 2 'Speak with Dr. Hester' 10856/10857, 3 'Collect \"Medicine\"' " +
                "10858/10859, no counters; objectiveconversation 1328,1 package325 text10855, 1328,2 package1129 text10860. Texts " +
                "10848-10852. Medicine class25589/template97514. Usable name override 117 (cabinet); no usable cabinet on the map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Thirsty_Hermit",
                "Giver Supply Chief Riggins, reward from the Hermit; requires Death Patrol; Medicinal Measures; follow-up A Cult " +
                "Divided; 27500XP, 3400 credits, 2 Class VII Advanced Med Pack, 2 Class VII Adrenaline Booster, 4 Class VII " +
                "Concussion Grenade, 4 Class VII Fragmentation Grenade. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsThirstyHermitV1",
                "1328 built, not 1327. Requires 1315 and 1066. Order 2,3,1; the cabinet a personal crate (class11078) by Hester at " +
                "-963,919.4,466; the Hermit takes the medicine. Items 45062x2, 118814x2, 111018x4, 45446x4.", 0.3);

            mission.Enable(scene);
        }

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(PoolsMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
