using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Needle in a Bane Camp (native mission 893), the Village Ruins (squad instance 1743): Headquarters calls after The Lost
    /// Ones: a large Bane force is on its way to a hidden Forean village; the player makes contact with its encampment. Not
    /// both this and The Choices We Make (1078): one closes the other.
    ///
    /// Native: texts 4686-4689 (missionconversation): title "Needle in a Bane Camp", log "Rescue the hidden Forean village from
    /// Bane attack.", offer "a large Bane force is advancing to an area void of Bane signals, but with a few Forean signals
    /// remaining ... Intel predicts that you will not have enough time to both save this village and intercept the next Bane
    /// convoy", turn-in "In order to repay your kindness and bravery, we have some secrets we can share."; objective
    /// (missionobjective) 1 "Make contact with Forean encampment in Village Ruins." 4696/7286, no counter; objectiveconversation
    /// 893,1 package 958 text 4697 ("We knew we could not stay hidden for much longer. Thank you for your aid.").
    ///
    /// Sourced: TaRapedia Needle in a Bane Camp: giver Headquarters (radio), at P'reo Das; requires The Lost Ones; 3,600
    /// credits, no XP and no receiver given; one of 3 Class VII EMP Bomb, 3 Class VII Power Charger, 6 Class VII Fragmentation
    /// Grenade. TaRapedia P'reo Das (2008-04): 893 and 1078 "are mutually exclusive". TR MMO DB level 35.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; the triage's choices 12 and of 2026-11-02):
    ///  - Offered by radio at The Lost Ones' turn-in and again on arrival in the Village Ruins, not once 1078 is done. The
    ///    receiver the encampment's Forean Elder (package 958) at He'kal Das (the village ruins by the He'kal shrine); his talk
    ///    completes 1 and signals 1078, whose Myrquar objective fails if it is held and open (ClosedByTheOther: a signal to a
    ///    mission not held changes nothing); he pays. Myrquar's death in 1078 fails a held 893 the same way.
    ///  - The "Bane attack" of the log is not built: the client has only the contact objective. XP 30,000 (as 1076; TaRapedia
    ///    none). Markers at the Marshes door and the client's "Undefended Village" (10000003, by name only) at the elder.
    /// </summary>
    public static class MarshesNeedleInABaneCampV1
    {
        public const uint MissionId = 893;
        public const uint Level = 35;
        public const uint Elder = MarshesPreoDasNpcsV1.Elder;
        public const uint Ruins = MarshesPreoDasNpcsV1.MapContextId;
        public const uint Contact = 1;
        public const uint VillageMarker = 10000003;     // missionobjectiveindicatorlanguage "Undefended Village"
        public const double MarkerRadius = 10;

        public const uint Experience = 30000;
        public const uint Credits = 3600;
        public const uint EmpBomb = 111232, EmpBombs = 3;                       // Class VII EMP Bomb
        public const uint PowerCharger = 118907, PowerChargers = 3;             // Class VII Power Charger
        public const uint FragGrenade = 45446, FragGrenades = 6;                // Class VII Fragmentation Grenade

        /// <summary>Not offered, not taken, once the other path is done.</summary>
        public static MissionRequirement NotTheOtherPath(uint other) => new NotRequirement(new MissionStateRequirement(other));

        /// <summary>
        /// The fork between 893 and 1078, with no requirement on any talk (the client's flag-1 text stays the only topic): 893's
        /// talk starts scenario CloseOther, whose scene sequence signals 1078's scenario ClosedByOther; Myrquar's death signals
        /// 893's. There the listed objective, if Incomplete, fails by its own transition (and the mission with it). A signal to a mission the player does not hold changes nothing, so nothing throws when the other is not held
        /// (an unconditional FailRelatedMission throws "The related mission is not an active assignment").
        /// </summary>
        public const uint ClosedByOther = 9, CloseOther = 10;

        /// <summary>The other path's signal fails these objectives (each by its own transition) while they are Incomplete.</summary>
        internal static void ClosedByTheOther(MarshesMissionDataV1 mission, string other, params (uint Objective, uint Transition)[] objectives)
        {
            mission.Scenario(ClosedByOther, $"{other} done: this path closes", 1);
            foreach (var (objective, transition) in objectives)
            {
                mission.Transition(objective, transition, transition, MissionObjectiveState.Failed);
                mission.Progress(objective, transition, MissionProgressEventKind.ScenarioEvent, 1, null, counter: ClosedByOther);
            }
        }

        /// <summary>This path's last talk closes the other: scenario CloseOther, its sequence's signal to <paramref name="other"/>.</summary>
        internal static void CloseTheOther(MarshesMissionDataV1 mission, MissionSceneDefinition scene, uint objective, uint action, uint other)
        {
            mission.Scenario(CloseOther, $"Done: mission {other} closes", 0);
            mission.Action(objective, action, MissionActionKind.StartScenario, scenario: CloseOther);
            scene.Sequences[CloseOther] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(other, ClosedByOther, 1) } };
        }

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Needle in a Bane Camp", 4686, null, Elder, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpBomb, EmpBombs), (PowerCharger, PowerChargers), (FragGrenade, FragGrenades) }, new (uint, uint)[0]);
            mission.Prerequisite(MarshesTheLostOnesV1.MissionId);
            mission.RadioOffer(
                MissionOfferSourceDefinition.MissionRewarded(MarshesTheLostOnesV1.MissionId) with
                {
                    Requirement = NotTheOtherPath(MarshesTheChoicesWeMakeV1.MissionId)
                },
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Ruins,
                    Requirement: NotTheOtherPath(MarshesTheChoicesWeMakeV1.MissionId)));

            mission.Objective(Contact, 4696, 7286, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Contact);
            mission.Conversation(Contact, MarshesPreoDasNpcsV1.ElderPackage);
            mission.Action(Contact, 1, MissionActionKind.GrantReward, reward: 1);
            ClosedByTheOther(mission, "The Choices We Make", (Contact, 2));
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = Ruins,
                Requirement = NotTheOtherPath(MarshesTheChoicesWeMakeV1.MissionId)
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            CloseTheOther(mission, scene, Contact, 2, MarshesTheChoicesWeMakeV1.MissionId);

            var elder = MarshesPreoDasNpcsV1.ElderAt;
            mission.Indicator(Contact, 1, MarshesTheLostOnesV1.DoorAt.X, MarshesTheLostOnesV1.DoorAt.Y, MarshesTheLostOnesV1.DoorAt.Z, 10);
            mission.NamedIndicator(Contact, VillageMarker, elder.X, elder.Y, elder.Z, MarkerRadius, Ruins);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/893",
                "Native1 'Make contact with Forean encampment in Village Ruins.' 4696/7286, no counter; objectiveconversation 893,1 " +
                "pkg958 4697. Texts 4686-4689 (offer: not enough time to both save this village and intercept the next Bane " +
                "convoy). Name 9626 'Forean Elder'. IND 10000003 'Undefended Village' (by name).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Needle_in_a_Bane_Camp",
                "Giver Headquarters (radio), P'reo Das; requires The Lost Ones; 3600 credits, no XP, no receiver; one of 3 Class " +
                "VII EMP Bomb, 3 Power Charger, 6 Frag Grenade. P'reo Das page: 893 and 1078 mutually exclusive. TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesNeedleInABaneCampV1",
                "Radio offer at 1076's reward and on arrival in 1743, not once 1078 is done. Receiver the elder 760374 (pkg958) at " +
                "He'kal Das (130,65.97,-18); his talk signals 1078, which fails if held. No Bane attack built. XP 30000.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
