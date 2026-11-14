using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Petrovich's Last Stand (native mission 1533), Avernus Outpost (squad instance 2110): Commander Drazen sends the player
    /// through the Atta Teleporter to find Captain Petrovich in the AFS Graveyard.
    ///
    /// Native: texts 13696-13700 (missionconversation): title "Petrovich's Last Stand", log "Commander Drazen wants you to go
    /// through the Atta Teleporter and find Captain Petrovich.", offer "Petrovich took the survivors and headed through the Atta
    /// Teleporter there ...", turn-in "Oh, man, somebody else lived through that? Good thing we left Drazen back there to mop up,
    /// huh?" (Petrovich's Run's 13757 too), 13700 "none". The client has no objective of 1533 (missionobjective,
    /// objectiveconversation, missionobjectiveindicatorlanguage): a mission handed in at its receiver as soon as it is taken.
    /// Names 9671 Commander Drazen, 9672 Captain Petrovich.
    ///
    /// Sourced: TaRapedia Petrovich's Last Stand: giver Commander Drazen, reward giver Captain Petrovich (Avernus Outpost); "enter
    /// the nearby Atta Teleporter and talk to Captain Petrovich just ahead"; 90,000 XP, 6,150 credits; Class VIII EMP Grenade (3),
    /// Res Trauma Kit, Class VIII Concussion Grenade (3), Class VIII Basic Med Pack (6), one to choose (0.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (triage D5, the Avernus Outpost band). No prerequisite (no source; the triage's G1).
    ///  - As Information is Key (Crucible 1525): one hidden objective, not required (the server needs one row): the mission is
    ///    completeable at the accept; the objective completes in the graveyard (an area round Petrovich, AreaRadius) and holds the
    ///    reward. No marker (a hidden objective has none; the client draws the receiver of a completeable mission itself).
    ///  - The way: Drazen's pad (the Atta Teleporter, AshenAvernusOutpostNpcsV1 915) to the graveyard's (916), Petrovich 19 m on.
    ///  - The items by their client names, each a stack, one to choose: Class VIII EMP Grenade (111029) x3, Res Trauma Kit (47600)
    ///    x1, Class VIII Concussion Grenade (111019) x3, Class VIII Basic Med Pack (45063) x6.
    /// </summary>
    public static class AshenPetrovichsLastStandV1
    {
        public const uint MissionId = 1533;
        public const uint Level = 41;
        public const uint Outpost = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint Drazen = AshenAvernusOutpostNpcsV1.Drazen, Petrovich = AshenAvernusOutpostNpcsV1.Petrovich;
        public const uint InTheGraveyard = 1;
        public const uint GraveyardArea = 1;
        public const double AreaRadius = 15;     // ESTIMATE (no source gives the radius)

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                 // Class VIII EMP Grenade (class 26279)
        public const uint ResTraumaKit = 47600, ResTraumaKits = 1;              // Res Trauma Kit
        public const uint ConcussionGrenade = 111019, ConcussionGrenades = 3;   // Class VIII Concussion Grenade
        public const uint BasicMedPack = 45063, BasicMedPacks = 6;              // Class VIII Basic Med Pack

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Petrovich's Last Stand", 13696, Drazen, Petrovich, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades),
                    (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);
            // Hidden, not required: the mission is completeable at the accept; the client sees no objective.
            mission.Objective(InTheGraveyard, 13697, 13698, 0, MissionObjectiveState.Incomplete, required: false);
            var at = AshenAvernusOutpostNpcsV1.PetrovichAt;
            mission.Area(GraveyardArea, Outpost, at.X, at.Y, at.Z, AreaRadius);
            mission.Transition(InTheGraveyard);
            mission.EnterArea(InTheGraveyard, GraveyardArea);
            mission.Action(InTheGraveyard, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionconversation/1533",
                "Texts 13696-13700 (log 'Commander Drazen wants you to go through the Atta Teleporter and find Captain Petrovich.'); " +
                "no missionobjective, objectiveconversation or indicator rows for 1533. Names 9671 Commander Drazen, 9672 Captain Petrovich.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Petrovich%27s_Last_Stand",
                "Giver Commander Drazen, reward giver Captain Petrovich (Avernus Outpost); enter the nearby Atta Teleporter and talk to " +
                "Petrovich just ahead; 90000XP, 6150 credits; Class VIII EMP Grenade (3), Res Trauma Kit, Class VIII Concussion " +
                "Grenade (3), Class VIII Basic Med Pack (6).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenPetrovichsLastStandV1",
                "Level41, no prerequisite. One hidden objective, not required (the server needs a row; the client has none): " +
                "completeable at the accept, handed in at Petrovich 790642 in the AFS Graveyard; the objective completes in a 15 m " +
                "area round him and holds the reward. The Atta Teleporter pads 915/916. Items 111029x3, 47600x1, 111019x3, 45063x6, " +
                "one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = Outpost,
                HiddenObjectiveIds = new() { InTheGraveyard }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
