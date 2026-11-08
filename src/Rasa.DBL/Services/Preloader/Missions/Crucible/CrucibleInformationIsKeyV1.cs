using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Information is Key (native mission 1525), the Chaukas Robotics Facility (squad instance 2146) and Ligo Crucible: Cipher
    /// Specialist Brian sends the player with the facility's data back to Information Analyst Ruben at Outpost Intrepid.
    ///
    /// Native: texts 13601-13605 (missionconversation): title "Information is Key", log "Report to Information Analyist Ruben."
    /// (the client's spelling), offer "Take the information we recovered here to Information Analyst Ruben. She can use it to
    /// help us create our own fighting robots like these Warden Bots.", turn-in "Well done soldier. This information may prove
    /// to be valuable in the fight against the Bane.", 13605 "none". The client has no objective of 1525 (missionobjective,
    /// objectiveconversation, missionobjectiveindicatorlanguage): a mission handed in at its receiver as soon as it is taken.
    ///
    /// Sourced: TaRapedia Information is Key: giver Brian, reward giver Information Analyst Ruben ("back in Outpost Intrepid
    /// outside the instance"); requires Factory Fortification; 42,500 XP, 6,000 credits; Class VIII Standard Med Pack (3),
    /// Incendiary Grenade (3), Advanced Med Pack (2), EMP Grenade (3); no objective (0.5). TR MMO DB: level 35. PlanetTR-2842
    /// (0.3): Ruben by the Front Lines exit; the poster hands 1525 in there.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The server needs one objective row (MissionContentValidator "missing-objective-id"). One hidden objective, not
    ///    required (MissionRuntime.IsComplete counts only required ones): the mission is completeable at the accept, as the
    ///    client has it, and the client is sent no objective (scene HiddenObjectiveIds). It completes when the player is on the
    ///    Crucible (a map area), and its transition holds the reward (the catalog's one GrantReward). No marker: the server has
    ///    none for a hidden objective, and the client draws the receiver of a completeable mission itself (MissionContacts).
    ///  - The scene's map the Crucible's (the receiver's), not bound: taken in the squad's Chaukas, handed in on the Crucible.
    ///  - The items by their client names, each a stack, one to choose (as Sadistic Sabotage 1445): 22547 Class VIII Standard
    ///    Med Pack (45064) x3, 26289 Class VIII Incendiary Grenade (111039) x3, 22548 Class VIII Advanced Med Pack (45065) x2,
    ///    26279 Class VIII EMP Grenade (111029) x3; "Class VIII" for all four (TaRapedia writes it once).
    /// </summary>
    public static class CrucibleInformationIsKeyV1
    {
        public const uint MissionId = 1525;
        public const uint Level = 35;
        public const uint Brian = CrucibleChaukasNpcsV1.Brian, Ruben = CrucibleChaukasNpcsV1.Ruben;
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint BackOnTheCrucible = 1;
        public const uint CrucibleArea = 1;

        public const uint Experience = 42500;
        public const uint Credits = 6000;
        public const uint StandardMedPack = 45064, StandardMedPacks = 3;        // Class VIII Standard Med Pack (class 22547)
        public const uint IncendiaryGrenade = 111039, IncendiaryGrenades = 3;   // Class VIII Incendiary Grenade (class 26289)
        public const uint AdvancedMedPack = 45065, AdvancedMedPacks = 2;        // Class VIII Advanced Med Pack (class 22548)
        public const uint EmpGrenade = 111029, EmpGrenades = 3;                 // Class VIII EMP Grenade (class 26279)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Information is Key", 13601, Brian, Ruben, Level);
            mission.Reward(Experience, Credits,
                new[] { (StandardMedPack, StandardMedPacks), (IncendiaryGrenade, IncendiaryGrenades), (AdvancedMedPack, AdvancedMedPacks),
                    (EmpGrenade, EmpGrenades) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleFactoryFortificationV1.MissionId);

            // Hidden, not required: the mission is completeable at the accept; the client sees no objective.
            mission.Objective(BackOnTheCrucible, 13602, 13603, 0, MissionObjectiveState.Incomplete, required: false);
            mission.MapArea(CrucibleArea, MapContextId);
            mission.Transition(BackOnTheCrucible);
            mission.EnterArea(BackOnTheCrucible, CrucibleArea);
            mission.Action(BackOnTheCrucible, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionconversation/1525",
                "Texts 13601-13605 (log 'Report to Information Analyist Ruben.'); no missionobjective, objectiveconversation or " +
                "indicator rows for 1525. Names 9725 Cipher Specialist Brian, 9724 Information Analyst Ruben.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Information_is_Key",
                "Giver Brian, reward giver Information Analyst Ruben (Outpost Intrepid); requires Factory Fortification; 42500XP, " +
                "6000 credits; Class VIII Standard Med Pack (3), Incendiary Grenade (3), Advanced Med Pack (2), EMP Grenade (3); no " +
                "objective. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleInformationIsKeyV1",
                "One hidden objective, not required (the server needs a row; the client has none): completeable at the accept, " +
                "handed in at Ruben 770058 on the Crucible; the objective completes on the Crucible (map area) and holds the reward. " +
                "Items 45064x3, 111039x3, 45065x2, 111029x3, one to choose.", 0.3);

            // The scene holds the hidden objective; its map is the Crucible's (the receiver's): taken in Chaukas, handed in there.
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                HiddenObjectiveIds = new() { BackOnTheCrucible }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
