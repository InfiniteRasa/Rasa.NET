using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Dirty Secrets (native mission 1077), the Village Ruins (squad instance 1743): Forean Elder Qunle at P'reo Das gives the
    /// player a key and sends him into Central Operations for the Logos element that powers the Bane's toxin pumps; the
    /// player destroys the Bane equipment there, takes a toxin sample and brings it to Elder Toida in Paludos. The Forean way
    /// to The Choices We Make's (1078) last three objectives.
    ///
    /// Native: texts 6073-6076 and 7287 (missionconversation): title "Dirty Secrets", log "Proceed to Central Operations and
    /// retrieve the Logos element powering the toxin pumps.", offer "Their base is powered by an element of the Eloh ... Take
    /// this key, and put an end to this madness once and for all.", turn-in (Toida) "so this is the cause of the sickness in
    /// P'reo Das"; objectives (missionobjective) 1 "Proceed to Central Operations and retrieve the Logos element powering the
    /// toxin pumps." 6135/7289, 2 "Destroy Bane Equipment and Gather Toxin Sample" 17185/17186, 3 "Return Toxin Sample to
    /// Elder Toida in Palados" 17187/17188; no counters; objectiveconversation 1077,3 package 288 text 17189. Items as 1078's.
    ///
    /// Sourced: TaRapedia Dirty Secrets: giver Forean Elder Qunle (P'reo Das), receiver "Elder Toids" (P'reo Das; the client's
    /// objective names Paludos: the client wins, the triage's conflict 6); 1,750 credits, no XP and no items given; bug note:
    /// "Qunle does NOT give you the key" (the client's offer does: the triage's conflict 7). TaRapedia P'reo Das: Disperse in
    /// the control room. TR MMO DB level 35.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Bound to the squad's Village Ruins. The key (Level 1 Security Clearance Pass) at the accept; it opens nothing (no
    ///    locked door here). 1: the Disperse Logos, one already owned counting. 2: 1078's lab, the player's own: three pieces
    ///    of equipment and a crate with the sample, at other places in the control room than 1078's (a player with both
    ///    missions sees both sets). 1 and 2 are open from the accept, in any order (an owned Logos completes 1 in the accept,
    ///    where the engine cannot yet set the lab down: the lab stands from the start); 2 opens 3; the mission is done with 1
    ///    and 3. Toida's talk takes the sample and completes 3; she pays.
    ///  - XP 35,000 (TaRapedia none); no item. Markers at the Logos, the client's "Bane Equipment" (72) and Toida.
    /// </summary>
    public static class MarshesDirtySecretsV1
    {
        public const uint MissionId = 1077;
        public const uint Level = 35;
        public const uint Qunle = MarshesPreoDasNpcsV1.Qunle, Toida = MarshesPreoDasNpcsV1.Toida;
        public const uint Ruins = MarshesPreoDasNpcsV1.MapContextId;
        public const uint Element = 1, Equipment = 2, Return = 3, LabTally = 4;

        public static readonly ScenePosition[] PiecesAt = { new(-22f, 63.86f, 386f), new(-40f, 63.86f, 386f), new(-34f, 63.98f, 412f) };
        public static readonly ScenePosition CrateAt = new(-28f, 63.99f, 412f);

        public const uint Experience = 35000;
        public const uint Credits = 1750;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Dirty Secrets", 6073, Qunle, Toida, Level);
            mission.Reward(Experience, Credits, new uint[0]);

            mission.Objective(Element, 6135, 7289, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Equipment, 17185, 17186, 1, MissionObjectiveState.Incomplete);
            mission.Objective(Return, 17187, 17188, 2, MissionObjectiveState.Inactive);

            MarshesTheChoicesWeMakeV1.Lab(mission, MissionId, Element, Equipment, LabTally, Return, MarshesTheChoicesWeMakeV1.DisperseLogos,
                17185, 17186, 3, gated: false);

            mission.Transition(Return);
            mission.Conversation(Return, MarshesTheChoicesWeMakeV1.ToidaPackage);
            mission.Action(Return, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("toida-takes-the-sample", MissionId, MarshesTheChoicesWeMakeV1.SampleKey, 1,
                    MissionItemScope.AssignmentIssued));
            mission.Action(Return, 2, MissionActionKind.GrantReward, reward: 1);

            var logos = MarshesTheChoicesWeMakeV1.DisperseAt;
            var room = MarshesTheChoicesWeMakeV1.ControlRoomAt;
            var toida = MarshesTheChoicesWeMakeV1.ToidaAt;
            mission.Indicator(Element, 1, logos.X, logos.Y, logos.Z, MarshesTheChoicesWeMakeV1.MarkerRadius, Ruins);
            mission.NamedIndicator(Equipment, MarshesTheChoicesWeMakeV1.EquipmentMarker, room.X, room.Y, room.Z,
                MarshesTheChoicesWeMakeV1.ControlRoomRadius, Ruins);
            mission.Indicator(Return, 2, toida.X, toida.Y, toida.Z, MarshesTheChoicesWeMakeV1.MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1077",
                "Native1 'Proceed to Central Operations and retrieve the Logos element' 6135/7289, 2 'Destroy Bane Equipment and " +
                "Gather Toxin Sample' 17185/17186, 3 'Return Toxin Sample to Elder Toida in Palados' 17187/17188; no counters; " +
                "objectiveconversation 1077,3 pkg288 17189. Texts 6073-6076, 7287 (offer: 'Take this key'). Name 8558 Qunle. " +
                "Items 22979/45453 key pass, 26696/117028 toxin sample. IND 72 'Bane Equipment' (by name).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Dirty_Secrets",
                "Giver Forean Elder Qunle (P'reo Das), receiver 'Elder Toids' (P'reo Das; client: Toida in Paludos); 1750 credits; " +
                "no XP, no items; bug note: Qunle does not give the key (client: he does). P'reo Das page: Disperse in the control " +
                "room. TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesDirtySecretsV1",
                "Bound to 1743; Qunle 760373 at P'reo Das (95,58.12,-230); the key at accept (opens nothing). 1 Logos 30, owned " +
                "counts; 2 1078's lab at other places in the control room, standing from the accept (1 and 2 in any order); Toida's talk " +
                "takes the sample. XP 35000, no item.", 0.3);

            var scene = MarshesTheChoicesWeMakeV1.LabScene(MissionId, Element, LabTally, PiecesAt, CrateAt, gated: false);
            scene.Items.Add(new MissionItemBinding(MarshesTheChoicesWeMakeV1.KeyKey, MarshesTheChoicesWeMakeV1.KeyTemplate,
                MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
            scene.AcceptanceItems = new()
            {
                new IssueMissionItemIntent("qunles-key", MissionId, MarshesTheChoicesWeMakeV1.KeyKey, MarshesTheChoicesWeMakeV1.KeyTemplate, 1)
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
