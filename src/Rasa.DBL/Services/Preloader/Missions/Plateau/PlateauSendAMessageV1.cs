using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Send a Message (native mission 1181), Temporal Chamber (squad instance 2029): after the two trials Veryin opens
    /// the Communications Chamber; the player uses the Communications Array to send a message to any Eloh left in the
    /// galaxy, and goes back to Veryin.
    ///
    /// Native: texts 8966-8970 (missionconversation): offer "I shall grant you access to the Communications Chamber. The
    /// doors are now open to you. Once within the chamber, you may then use the Communications Array", turn-in "You
    /// have sent the message. Perhaps one day, your message will be answered."; objective 2 "Use the Communications
    /// Array." 8976/8977 ("at the center of the Communications Room"), no counter, no objective conversation, no
    /// objective 1 (missionobjective). Client map 2029: ArchElohCorridorRoomMessengerMachineV01 at (0, 248, -64), in the
    /// chamber's second round room.
    ///
    /// Sourced: TaRapedia Send a Message: giver and reward from the Eloh Hologram (Temporal Chamber), requires Foreas
    /// Base Past, no follow-up; 140,000 XP, 9,250 credits. TR MMO DB: level 30.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-24): the Array is a personal Eloh hologram platform
    /// (Array Of Hope's conduit class, a known usable) in the second round room by the Messenger Machine; its use
    /// completes 2. The room is open already (the chamber's navmesh is one area): no door. Level 30. No indicator.
    /// </summary>
    public static class PlateauSendAMessageV1
    {
        public const uint MissionId = 1181;
        public const uint Level = 30;
        public const uint Veryin = PlateauTemporalChamberNpcsV1.Veryin;
        public const uint Use = 2;
        public const uint Sent = 1, Used = 1, UseSequence = 1;

        public const uint Experience = 140000;
        public const uint Credits = 9250;

        public const uint ArrayClass = PlateauArrayOfHopeV1.ConduitClass;
        public const uint ReadyState = PlateauArrayOfHopeV1.ReadyState, UsedState = PlateauArrayOfHopeV1.UsedState;
        public const uint UseArgument = PlateauArrayOfHopeV1.UseArgument;
        public const string ArrayRole = "communications-array";

        public static readonly ScenePosition ArrayAt = new(0, 248.25f, -84);

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Send a Message", 8966, Veryin, Veryin, Level);
            mission.Reward(Experience, Credits, Array.Empty<uint>());
            mission.Prerequisite(PlateauForeasBasePastV1.MissionId);

            mission.Objective(Use, 8976, 8977, 0, MissionObjectiveState.Incomplete);
            mission.Scenario(Sent, "The Communications Array used", 1);
            mission.Transition(Use);
            mission.Progress(Use, MissionProgressEventKind.ScenarioEvent, Used, null, counter: Sent);
            mission.Action(Use, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = PlateauTemporalChamberNpcsV1.MapContextId
            };
            scene.Actors[ArrayRole] = new SceneActorDefinition(ArrayRole, SceneActorKind.Object, ArrayClass, ArrayAt,
                InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, Use, UseSequence, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent("create-array", ArrayRole) } };
            scene.Sequences[UseSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("array-sends", ArrayRole, UsedState),
                    new SetInteractionIntent("array-done", ArrayRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, Sent, Used) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1181",
                "Native2 'Use the Communications Array.' 8976/8977, no counter, no objective conversation, no objective 1. Texts " +
                "8966-8970. Map 2029 ArchElohCorridorRoomMessengerMachineV01 at (0,248,-64).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Send_a_Message",
                "Giver and reward from the Eloh Hologram (Temporal Chamber); requires Foreas Base Past; no follow-up; 140000XP, " +
                "9250 credits. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauSendAMessageV1",
                "Level30. A personal Eloh hologram platform20823 (states 55/56) in the second round room by the Messenger Machine; " +
                "its use completes 2. No door, no indicator. Place is an estimate.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
