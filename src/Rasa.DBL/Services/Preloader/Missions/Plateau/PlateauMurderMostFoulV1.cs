using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Murder Most Foul (native mission 895), Plateau: Security Guard Norton has been murdered. Provost passes
    /// the case to Detective Crais, who has the player scan the crime scene with a Crimecorder; an Unknown
    /// Forean Suspect is nosing around it; the CID Mainframe Computer reads the scan; Crais hears it all.
    /// It opens Looking For Clues (928).
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4698-4701 and 6419 (missionconversation); objectives 1
    /// "Speak to Commander Provost." 4722/6420, 2 "Speak to Det. Crais." 4723/6421, 3 "Scan the crime scene."
    /// 4724/6422, 4 "Return to Det. Crais." 4725/6426, 5 "A Forean Suspect?" 4729/6423, 6 "Upload the
    /// Crimecorder data." 4730/6425, 7 "Tell Crais about the suspect." 4734/6424, no counters
    /// (missionobjective); the talks (objectiveconversation): 1 Provost 962 (4726), 2 Crais 963 (4727), 3
    /// Norton 959 (4728 "CRIMECORDER ACTIVATED"), 4 Crais 963 (4846), 5 the suspect 966 (4736), 6 the
    /// Mainframe 964 (4733: the weapon was issued to Private Remick), 7 Crais 963 (4735). The Mainframe is
    /// creature name 8576 "CID Mainframe Computer"; the Crimecorder class 21082 (MisPlateauItemCrimecorder).
    ///
    /// Sourced: TaRapedia Murder Most Foul: giver Security Guard Norton, reward from CID Detective Crais,
    /// requirement Blue Flu, follow-up Looking For Clues, 40000 XP; "The crimecorder doesn't appear in
    /// inventory anywhere". Ten Ton Hammer (Fort Defiance part 1): the order Provost, Crais, scan at the body,
    /// the Unknown Forean Suspect, "the Batcomputer" up in Provost's area, Crais; "4650 credits and your choice
    /// of HellStorm Stealth Armor Helmet, Vitalius Gravitron Armor Gloves, Wellcare Hazmat Gloves, or Pulsar
    /// Motor Assist Armor Gloves". TaRapedia NPC: Unknown Forean Suspect /loc -158, 398, 1011.
    ///
    /// Reconstructed: the reward is the server's consecutive Rare templates 116447-116450, those four pieces
    /// in that order. ESTIMATE: level 30; no Crimecorder item (the scan is Norton's talk); the objectives one
    /// after another 1, 2, 3, 5, 6, 4, 7; the suspect is the player's own, there from the scan until the
    /// player speaks to him; the Mainframe is the player's own usable computer (UsableNPCCormanComputerV01,
    /// 7123) by the CID table (-229.7, 421, 929), at (-228, 421, 931).
    /// </summary>
    public static class PlateauMurderMostFoulV1
    {
        public const uint MissionId = 895;
        public const uint Level = 30;
        public const uint Provost = 1, Crais = 2, Scan = 3, Report = 4, Suspect = 5, Upload = 6, Tell = 7;
        public const uint Norton = PlateauCidNpcsV1.Norton, CraisNpc = PlateauCidNpcsV1.Crais;
        public const uint MainframeClass = 7123, MainframePackage = 964;
        public const uint SuspectAppears = 1, SuspectLeaves = 2;

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint StealthHelmet = 116447, GravitonGloves = 116448, HazmatGloves = 116449, MotorAssistGloves = 116450;

        public static readonly ScenePosition SuspectAt = new(-158, 398.75f, 1011);
        public static readonly ScenePosition MainframeAt = new(-228, 421, 931);

        /// <summary>The objectives in their order: (objective, name, body, package).</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Package)[] Steps =
        {
            (Provost, 4722, 6420, PlateauCidNpcsV1.ProvostPackage),
            (Crais, 4723, 6421, PlateauCidNpcsV1.CraisPackage),
            (Scan, 4724, 6422, PlateauCidNpcsV1.NortonPackage),
            (Suspect, 4729, 6423, PlateauCidNpcsV1.SuspectPackage),
            (Upload, 4730, 6425, MainframePackage),
            (Report, 4725, 6426, PlateauCidNpcsV1.CraisPackage),
            (Tell, 4734, 6424, PlateauCidNpcsV1.CraisPackage),
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Murder Most Foul", 4698, Norton, CraisNpc, Level);
            mission.Reward(Experience, Credits, new[] { StealthHelmet, GravitonGloves, HazmatGloves, MotorAssistGloves });
            mission.Prerequisite(PlateauBlueFluV1.MissionId);
            mission.Scenario(SuspectAppears, "The Unknown Forean Suspect comes to the crime scene", 0);
            mission.Scenario(SuspectLeaves, "The Unknown Forean Suspect goes", 0);

            var provost = PlateauCidNpcsV1.ProvostAt;
            var crais = PlateauCidNpcsV1.Moves[1].At;
            var norton = PlateauCidNpcsV1.Moves[0].At;
            var places = new Dictionary<uint, (double X, double Y, double Z)>
            {
                [Provost] = provost, [Crais] = crais, [Scan] = norton, [Suspect] = (SuspectAt.X, SuspectAt.Y, SuspectAt.Z),
                [Upload] = (MainframeAt.X, MainframeAt.Y, MainframeAt.Z), [Report] = crais, [Tell] = crais
            };
            for (var index = 0; index < Steps.Length; index++)
                mission.Objective(Steps[index].Objective, Steps[index].Name, Steps[index].Body, (uint)index,
                    index == 0 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive);
            for (var index = 0; index < Steps.Length; index++)
            {
                var step = Steps[index];
                mission.Transition(step.Objective);
                mission.Conversation(step.Objective, step.Package);
                if (index + 1 < Steps.Length)
                {
                    mission.Action(step.Objective, 1, MissionActionKind.RevealObjective, targetObjective: Steps[index + 1].Objective);
                    mission.Action(step.Objective, 2, MissionActionKind.ActivateObjective, targetObjective: Steps[index + 1].Objective,
                        state: MissionObjectiveState.Incomplete);
                }
                else
                    mission.Action(step.Objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = places[step.Objective];
                mission.Indicator(step.Objective, (uint)index + 1, at.X, at.Y, at.Z);
            }
            mission.Action(Scan, 3, MissionActionKind.StartScenario, scenario: SuspectAppears);
            mission.Action(Suspect, 3, MissionActionKind.StartScenario, scenario: SuspectLeaves);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Actors = new()
                {
                    ["suspect"] = new SceneActorDefinition("suspect", SceneActorKind.Creature, PlateauCidNpcsV1.Suspect,
                        SuspectAt, Orientation: 0),
                    ["mainframe"] = new SceneActorDefinition("mainframe", SceneActorKind.Object, MainframeClass, MainframeAt,
                        Conversation: new SceneObjectConversation(MissionId, Upload, MainframePackage, Upload))
                },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Upload, MainframePackage, 1, transitionId: 1)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("ensure-mainframe", "mainframe") } };
            scene.Sequences[SuspectAppears] = new SceneSequenceDefinition { World = { new EnsureActorIntent("suspect-comes", "suspect") } };
            scene.Sequences[SuspectLeaves] = new SceneSequenceDefinition { World = { new RemoveActorIntent("suspect-goes", "suspect") } };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/895",
                "Native1-7 (4722/6420, 4723/6421, 4724/6422, 4725/6426, 4729/6423, 4730/6425, 4734/6424), no counters; " +
                "objectiveconversation 895: 1/962 4726, 2/963 4727, 3/959 4728, 4/963 4846, 5/966 4736, 6/964 4733, 7/963 4735. " +
                "Texts 4698-4701, 6419. Mainframe name8576, suspect name8577, Crimecorder class21082.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Murder_Most_Foul",
                "Giver Security Guard Norton, reward from CID Detective Crais; requirement Blue Flu; follow-up Looking For Clues; " +
                "40000XP; the Crimecorder is not in the inventory. Ten Ton Hammer Fort Defiance part 1: order Provost, Crais, " +
                "scan, suspect, Batcomputer, Crais; 4650 credits; HellStorm Stealth Helmet, Vitalius Graviton Gloves, Wellcare " +
                "Hazmat Gloves, Pulsar Motor Assist Gloves. Suspect /loc -158, 398, 1011.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMurderMostFoulV1",
                "Rewards: consecutive Rare templates 116447-116450. Level30; objectives in order 1,2,3,5,6,4,7; personal suspect " +
                "from the scan to his talk; personal Mainframe 7123 at (-228, 421, 931); no Crimecorder item.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
