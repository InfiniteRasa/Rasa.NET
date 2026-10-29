using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// The Mad Bomber (native mission 856), Retread Caves (squad instance 1694), Valverde Pools: Ellen Lenee wants the
    /// saboteur who bombed the AFS supply lines on the Plateau dead; he hides in a cave of the Retread Caves.
    ///
    /// Native: texts 4441-4444, 7358 (missionconversation): log "Find and kill the Mad Bomber.", offer "He was the one who
    /// set up the explosives on the Plateau. ... He's holed up down here somewhere", turn-in "You've done a great service
    /// for us all today."; objective 1 "Kill the Mad Bomber" 4445/7359 (missionobjective); no objective conversation.
    ///
    /// Sourced: TaRapedia The Mad Bomber: giver and receiver Ellen Lenee (Retread Caves); requires Total Slaughter; 60,000
    /// XP, 7,000 credits; one of Prodigy Hazmat Armor Gloves lvl 34, Titan Motor Assist Armor Gloves lvl 33, Astra
    /// Reflective Armor Gloves lvl 34, Teleract Stealth Armor Legs lvl 35; the last mission of the caves' story; "in a
    /// cave he has wired to blow up if he's cornered". Ten Ton Hammer Retread Caves guide: from the main passage's second
    /// light (-34, -134) a winding passage to the Eastern Fringe, where the Mad Bomber "has left little explosive presents
    /// for his guests"; a level 36 polarity gunner. TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: the boss only, no proximity mines): the Mad Bomber
    /// (PoolsRetreadCavesNpcsV1, personal, hostile) stands in the Eastern Fringe from the accept; his death completes 1 and
    /// pays. Rewards the lowest Uncommon templates of the bands that hold those levels, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsMadBomberV1
    {
        public const uint MissionId = 856;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Ellen = PoolsRetreadCavesNpcsV1.Ellen;
        public const uint Kill = 1;
        public const uint BomberKilled = 5, BomberDown = 1;
        public const string BomberRole = "mad-bomber";
        public static readonly ScenePosition BomberAt = new(-128f, 58.21f, -48f);
        public const double BomberRadius = 15;

        public const uint Experience = 60000;
        public const uint Credits = 7000;
        public const uint HazmatGloves = 20484;         // Armor_T2_Hazmat_V02_UNC_Gloves_34_to_38
        public const uint MotorAssistGloves = 28767;    // Armor_T1_MotorAssist_V04_UNC_Gloves_33_to_37
        public const uint ReflectiveGloves = 35720;     // Armor_T2_Reflective_V02_UNC_Gloves_34_to_38
        public const uint StealthLegs = 39191;          // Armor_T3_Stealth_V02_UNC_Legs_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "The Mad Bomber", 4441, Ellen, Ellen, Level);
            mission.Reward(Experience, Credits, new[] { HazmatGloves, MotorAssistGloves, ReflectiveGloves, StealthLegs });
            mission.Prerequisite(PoolsTotalSlaughterV1.MissionId);

            mission.Objective(Kill, 4445, 7359, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(BomberKilled, "The Mad Bomber killed", 1);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.ScenarioEvent, 1, null, counter: BomberKilled);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                DefeatSequences = new() { [BomberRole] = BomberDown }
            };
            scene.Actors[BomberRole] = new SceneActorDefinition(BomberRole, SceneActorKind.Creature, PoolsRetreadCavesNpcsV1.MadBomber,
                BomberAt, Orientation: 1.5708, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-bomber", BomberRole) } };
            scene.Sequences[BomberDown] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, BomberKilled, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/856",
                "Native1 'Kill the Mad Bomber' 4445/7359; no objectiveconversation. Texts 4441-4444, 7358. Name 7085 The Mad Bomber.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Mad_Bomber",
                "Giver/receiver Ellen Lenee (Retread Caves); requires Total Slaughter; 60000XP, 7000 credits; one of Prodigy Hazmat " +
                "Armor Gloves34, Titan Motor Assist Armor Gloves33, Astra Reflective Armor Gloves34, Teleract Stealth Armor Legs35. " +
                "Ten Ton Hammer: the Eastern Fringe, mines, a level36 polarity gunner. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMadBomberV1",
                "Boss only (user), no mines: the personal Mad Bomber 760229 in the Eastern Fringe from the accept, a pistol " +
                "(no polarity gun attack). Items 20484, 28767, 35720, 39191, one to choose. Place an estimate. Indicators in the instance too.", 0.3);

            mission.Indicator(Kill, 1, BomberAt.X, BomberAt.Y, BomberAt.Z, BomberRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
