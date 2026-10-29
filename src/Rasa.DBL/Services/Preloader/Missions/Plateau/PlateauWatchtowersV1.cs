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
    /// All Along The Watchtowers (native mission 1825), Plateau: Colonel Thibodeau at Wedge Rock Outpost
    /// wants the Remote Scanning Terminals in the three watchtowers over the eastern end of Valverde Chasm
    /// rebooted by hand.
    ///
    /// Native: name/log/offer/turn-in texts 18540-18543, reminder 18544 "none" (missionconversation);
    /// objectives 1-3 "Reboot Terminal #n" 18545/18546, 18548/18549, 18551/18552, no counters
    /// (missionobjective); each terminal's "REBOOT IN PROGRESS ... REBOOT COMPLETE" on packages 2309,
    /// 2310, 2311 (18547, 18550, 18553) (objectiveconversation, flag 1). The terminal is class 28580
    /// UsableMisPlateauRemoteSensorTerminal "Remote Sensor Terminal", an NPC augmentation (52), as Twin
    /// Pillars' blood analyzer (7123). Thibodeau is creature 510177.
    ///
    /// Sourced (TaRapedia All Along The Watchtowers): giver and receiver Thibodeau, level "32 (possibly
    /// less)", no requirement, 45,000 XP, 4,800 credits, "Equipment"; "reboot the computers on the upper
    /// level of the 3 watchtowers".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The towers: three of the four intact ArchHumOutpostTowerV02 (21056) on the client's map along
    ///    the chasm's east part, the three nearest the chasm. Each terminal stands on the tower's ground
    ///    floor, at its centre. The sources put them on the upper level, 14.4 m up, but the client's tower
    ///    has no stairs or ramp there and the server places no lift: a player could not walk up (real
    ///    client 2026-10-28, Plateau to-fix #40).
    ///  - One personal terminal per objective, there only with the mission; its talk completes it.
    ///  - "Equipment" read as the four Uncommon V04 helmets of the 30-34 band, one of each armor type.
    /// </summary>
    public static class PlateauWatchtowersV1
    {
        public const uint MissionId = 1825;
        public const uint Level = 32;
        public const uint Thibodeau = 510177;
        public const uint TerminalClass = 28580;        // UsableMisPlateauRemoteSensorTerminal

        /// <summary>(objective, name, body, package, terminal on the ground floor, on the navmesh): towers at (177.7, 133.36), (242, 275.5), (400.23, 88.43).</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Package, ScenePosition At)[] Terminals =
        {
            (1, 18545, 18546, 2309, new ScenePosition(177.6f, 349.48f, 133.6f)),
            (2, 18548, 18549, 2310, new ScenePosition(241.6f, 349.08f, 275.6f)),
            (3, 18551, 18552, 2311, new ScenePosition(400.34f, 331.68f, 88.82f)),
        };

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioHelmet = 46019;            // Armor_T3_Bio_V04_UNC_Helmet_30_to_34
        public const uint GravitonHelmet = 25079;       // Armor_T3_Graviton_V04_UNC_Helmet_30_to_34
        public const uint MechHelmet = 34205;           // Armor_T3_Mech_V04_UNC_Helmet_30_to_34
        public const uint StealthHelmet = 40333;        // Armor_T3_Stealth_V04_UNC_Helmet_30_to_34

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "All Along The Watchtowers", 18540, Thibodeau, Thibodeau, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, GravitonHelmet, MechHelmet, StealthHelmet });

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Dialogue = new List<MissionDialogueTopicDefinition>()
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var terminal in Terminals)
            {
                var role = $"terminal-{terminal.Objective}";
                mission.Objective(terminal.Objective, terminal.Name, terminal.Body, terminal.Objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(terminal.Objective);
                mission.Conversation(terminal.Objective, terminal.Package);
                mission.Indicator(terminal.Objective, terminal.Objective, terminal.At.X, terminal.At.Y, terminal.At.Z, 10);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, TerminalClass, terminal.At,
                    Conversation: new SceneObjectConversation(MissionId, terminal.Objective, terminal.Package, terminal.Objective));
                scene.Dialogue.Add(new MissionDialogueTopicDefinition(terminal.Objective, terminal.Package, 1, transitionId: 1));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"ensure-{role}", role));
            }
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1825",
                "Native1-3 'Reboot Terminal #n' 18545/18546, 18548/18549, 18551/18552, no counters; objectiveconversation 1825,n " +
                "packages 2309-2311 texts 18547/18550/18553. Texts 18540-18544. Terminal class28580 (augmentation52).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/All_Along_The_Watchtowers",
                "Giver/receiver Colonel Thibodeau, Wedge Rock Outpost; level '32 (possibly less)'; no requirement; 45000XP, 4800 " +
                "credits, 'Equipment'; the computers on the upper level of the 3 watchtowers.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauWatchtowersV1",
                "Towers: the three ArchHumOutpostTowerV02 nearest the chasm (177.7,133.36), (242,275.5), (400.23,88.43); terminals " +
                "on the ground floor (349.48, 349.08, 331.68): the upper level has no way up. Personal terminals, talk completes. Equipment read " +
                "as Uncommon V04 helmets 30-34: 46019, 25079, 34205, 40333.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
