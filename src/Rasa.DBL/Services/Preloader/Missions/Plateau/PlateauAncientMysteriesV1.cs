using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Ancient Mysteries (native mission 1978), Plateau: Elder Hundra at New Velon Village sends the player into
    /// Velon Hollow (squad instance 2136) for the Order of Velon's artifacts the Bane took; the player gives them
    /// to the Foreans (Hundra) or to the AFS (Receptive Officer Markin), not to both.
    ///
    /// Native: texts 20818-20822 (missionconversation); objectives (missionobjective) 1 "Recover Velon Artifacts"
    /// 20835/20836 ("Recover the artifacts from Bane officers in Velon Hollow before they are lost forever."), 2
    /// "Explore Velon Hollow" 20837/20838 ("Travel to Velon Hollow."), 3 "Return to Receptive Markin OR Elder
    /// Hundra" 20929/20930 ("You can not give them to both."); no counters. Objectiveconversation 1978,3 on
    /// package 1003 (Hundra, 20932) and on 2515 (Markin, 20931). Item: Velon Artifact (class 29313, template
    /// 122240, Hollow_Mission_Forean_Artifact).
    ///
    /// Sourced: TaRapedia Ancient Mysteries: giver and reward giver Elder Hundra, New Velon Village; requirement
    /// Village Of The Damned; 4,800 credits, 45,000 (in the item field: read as XP), no item; objectives
    /// explore, recover 0/10, return to Markin or Hundra; walkthrough "Seem to be on all bosses in instance -
    /// Thrax leaders, Forean infected and a mox too".
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - Level 32, as Deadly Silence (the instance's top).
    ///  - 2 first: done on arriving in Velon Hollow (the whole map). It opens 1.
    ///  - 1: ten artifacts, a personal drop of two at every kill while 1 is open, from the bosses: the two Bane
    ///    officers and the Mox Alpha Male (PlateauVelonHollowBaneV1) and the three corrupted leaders
    ///    (PlateauVelonElderBossesV1); six bosses, twelve at most, ten kept.
    ///  - 3: a talk to Hundra (transition 1) or to Markin (transition 2), who stands by her (the user); the
    ///    talk takes the ten artifacts. The turn-in is at Hundra in both cases (TaRapedia's reward giver).
    ///  - The reward, by the user's rule for a mission with no item: two Class VI Standard Med Packs (45058),
    ///    as Results Are In (level 32). No indicators: the goals are in an instance.
    /// </summary>
    public static class PlateauAncientMysteriesV1
    {
        public const uint MissionId = 1978;
        public const uint Level = 32;
        public const uint Hundra = PlateauVelonHollowBaneV1.Hundra;
        public const uint HundraPackage = PlateauVelonHollowBaneV1.HundraPackage, MarkinPackage = PlateauVelonHollowBaneV1.MarkinPackage;
        public const uint Recover = 1, Explore = 2, Return = 3;
        public const uint VelonHollow = 1;
        public const uint ToHundra = 1, ToMarkin = 2;

        public const uint Artifact = 122240, ArtifactClass = 29313, Artifacts = 10, PerKill = 2;
        public const string ArtifactKey = "velon-artifact";

        public static readonly uint[] Bosses =
        {
            PlateauVelonHollowBaneV1.Officer, PlateauVelonHollowBaneV1.MoxBoss,
            PlateauVelonElderBossesV1.Minash.Id, PlateauVelonElderBossesV1.Kadali.Id, PlateauVelonElderBossesV1.Taosari.Id
        };

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint MedPack = PlateauDefiantOnesV1.MedPack, MedPacks = PlateauDefiantOnesV1.MedPacks;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Ancient Mysteries", 20818, Hundra, Hundra, Level);
            mission.Reward(Experience, Credits, new (uint, uint)[0], new[] { (MedPack, MedPacks) });
            mission.Prerequisite(PlateauVillageOfTheDamnedV1.MissionId);

            mission.Objective(Explore, 20837, 20838, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Recover, 20835, 20836, 1, MissionObjectiveState.Inactive);
            mission.Objective(Return, 20929, 20930, 2, MissionObjectiveState.Inactive);
            mission.MapArea(VelonHollow, PlateauVelonHollowBaneV1.MapContextId);

            mission.Transition(Explore);
            mission.EnterArea(Explore, VelonHollow);
            PlateauSilentButDeadlyV1.Open(mission, Explore, 1, Recover);

            mission.Transition(Recover);
            mission.Progress(Recover, MissionProgressEventKind.ItemAcquired, ArtifactClass, Artifacts);
            PlateauSilentButDeadlyV1.Open(mission, Recover, 1, Return);

            // 3: to the Foreans or to the AFS; either talk takes the artifacts.
            foreach (var (transition, package, to) in new[] { (ToHundra, HundraPackage, "hundra"), (ToMarkin, MarkinPackage, "markin") })
            {
                mission.Transition(Return, transition, transition);
                mission.Conversation(Return, package, 1, transition);
                mission.Action(Return, 1, MissionActionKind.ConsumeMissionItem, transition: transition,
                    itemIntent: new ConsumeMissionItemIntent($"artifacts-to-{to}", MissionId, ArtifactKey, Artifacts, MissionItemScope.CharacterOwned));
                mission.Action(Return, 2, MissionActionKind.GrantReward, reward: 1, transition: transition);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1978",
                "Native1 artifacts 20835/20836, 2 explore 20837/20838, 3 Markin or Hundra 20929/20930, no counters; " +
                "objectiveconversation 1978,3 on 1003 (Hundra, 20932) and 2515 (Markin, 20931). Texts 20818-20822. Velon " +
                "Artifact class29313/template122240. Name 10515 Receptive Officer Markin.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Ancient_Mysteries",
                "Giver and reward giver Elder Hundra, New Velon Village; requirement Village Of The Damned; 4800 credits, 45000 " +
                "(item field, read as XP); recover 0/10; walkthrough: on all bosses - Thrax leaders, Forean infected, a mox.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauAncientMysteriesV1",
                "Level32. 2 on arriving in Velon Hollow (map area); 1 ten artifacts, two per kill of the six bosses (officers " +
                "650073 x2, Mox 650074, leaders 650053-650055) while 1 is open; 3 a talk to Hundra or Markin (650078, by Hundra), " +
                "which takes the ten; turn-in at Hundra. Two Class VI Standard Med Packs 45058. No indicator (instance).", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Recover },
                Items = new()
                {
                    new MissionItemBinding(ArtifactKey, Artifact, MissionItemScope.CharacterOwned, Artifacts,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(Bosses, Recover, PlateauVelonHollowBaneV1.MapContextId, 100, PerKill))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
