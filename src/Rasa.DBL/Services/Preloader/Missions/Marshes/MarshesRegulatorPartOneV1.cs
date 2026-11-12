using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Regulator: Part I (native mission 925), Valverde Marshes: Retread McCormick in Retread City offers to build the
    /// player a nanoviral regulator against the Bane's pipeline fields, and sends them to Retread Wilson for scrap metal.
    ///
    /// Native: texts 4943-4946 and 7330 (missionconversation): log "Report to Retread Wilson outside Academy Point. He'll
    /// provide scrap metal for the Nanoviral Regulator as well as instructions for further actions.", offer "you're going to
    /// want one of our top-of-the-line, class "A" nanoviral regulators... Wilson over by the Logos Research Control will set
    /// you up with some scrap metal... Bring everything back to me", turn-in "So Wilson's got you running errands now, eh?";
    /// objective 1 "Get Scrap Metal from Retread Wilson at the Logos Control Station" 4947/7331, no counter
    /// (missionobjective); objectiveconversation (925,1) package 969 text 4948 "Scrap? ... Here - take this.". Item Retread
    /// Scrap Metal class 21088 MisMarshesItemScrapMetal (World DB template 42432, stack 10). Names 8578 Retread McCormick,
    /// 8579 Retread Wilson.
    ///
    /// Sourced: TaRapedia The Regulator: Part 1: giver Retread McCormick (Retread City), receiver Retread Wilson; follow-up
    /// Part 2; 35,000 XP, 3,700 credits (TaRapedia history: 1,800, then 0, then 3,700 from 2008-03-17); one of Class VII
    /// Advanced Med Pack (2), Class VII Power Charger (2), Class VII Fragmentation Grenade (4), Class VII Cryogenic Grenade
    /// (4). TR MMO DB: level 35, 35,000 XP, 3,700 credits. Ten Ton Hammer Retread City: 1,850 credits and a Class VII EMP
    /// Bomb (an older version; TaRapedia's later list wins, the triage's conflict 21). World DB: McCormick 510201 (package 968).
    ///
    /// Reconstructed (ESTIMATE): Wilson's talk completes 1 and hands over one piece of scrap (the client gives no count); the
    /// turn-in is at Wilson, who keeps it (a deviation, docs/marshes-playtest.md): the server binds a mission's issued items
    /// (AssignmentIssued: scrap, nanovirus) to that mission, so a later mission cannot take them. The core (a corpse drop,
    /// untagged) could be carried to McCormick (DevilsDenContent.cs:830-842 precedent), but a lost core would then block
    /// Part IV (1030 has no drop of its own), so each part goes in at its own turn-in. The finishing text "So Wilson's got
    /// you running errands now, eh?" reads in McCormick's voice although TaRapedia makes Wilson the receiver; the client has
    /// no receiver data (kept as TaRapedia). The stacks the client's Class VII templates (the classes' only Common ones,
    /// World DB). A marker at Wilson.
    /// </summary>
    public static class MarshesRegulatorPartOneV1
    {
        public const uint MissionId = 925;
        public const uint Level = 35;
        public const uint McCormick = MarshesRegulatorNpcsV1.McCormick, Wilson = MarshesRegulatorNpcsV1.Wilson;
        public const uint WilsonPackage = MarshesRegulatorNpcsV1.WilsonPackage;
        public const uint Scrap = 1;

        public const uint ScrapTemplate = 42432, ScrapClass = 21088, ScrapCount = 1;
        public const string ScrapKey = "retread-scrap-metal";

        public const uint Experience = 35000;
        public const uint Credits = 3700;
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 2;      // Class VII Advanced Med Pack (class 22545)
        public const uint PowerCharger = 118907, PowerChargers = 2;          // Class VII Power Charger (class 28498)
        public const uint FragGrenade = 45446, FragGrenades = 4;             // Class VII Fragmentation Grenade (class 22963)
        public const uint CryoGrenade = 111048, CryoGrenades = 4;            // Class VII Cryogenic Grenade (class 26298)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "The Regulator: Part I", 4943, McCormick, Wilson, Level);
            mission.Reward(Experience, Credits,
                new[] { (AdvancedMedPack, AdvancedMedPacks), (PowerCharger, PowerChargers), (FragGrenade, FragGrenades), (CryoGrenade, CryoGrenades) },
                new (uint, uint)[0]);

            mission.Objective(Scrap, 4947, 7331, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Scrap);
            mission.Conversation(Scrap, WilsonPackage);
            mission.Action(Scrap, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("wilson-hands-scrap", MissionId, ScrapKey, ScrapTemplate, ScrapCount));
            mission.Action(Scrap, 2, MissionActionKind.GrantReward, reward: 1);
            var wilson = MarshesRegulatorNpcsV1.WilsonAt;
            mission.Indicator(Scrap, 1, wilson.X, wilson.Y, wilson.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/925",
                "Native1 'Get Scrap Metal from Retread Wilson at the Logos Control Station' 4947/7331, no counter; " +
                "objectiveconversation 925,1 package969 text4948. Texts 4943-4946, 7330 (log: Wilson outside Academy Point). Item " +
                "class21088 Retread Scrap Metal (template42432). Names 8578 Retread McCormick, 8579 Retread Wilson.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Regulator%3A_Part_1",
                "Giver Retread McCormick (Retread City), receiver Retread Wilson; follow-up Part 2; 35000XP, 3700 credits; one of " +
                "Class VII Advanced Med Pack (2), Power Charger (2), Fragmentation Grenade (4), Cryogenic Grenade (4). TR MMO DB " +
                "level35. Ten Ton Hammer: Wilson northeast around -138,-468; 1850 credits and an EMP Bomb (older).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesRegulatorPartOneV1",
                "Wilson 760313 (McCormick's row and look, package969) 4 m from Ten Ton Hammer's point on the navmesh. His talk " +
                "completes 1 and issues 1 scrap (42432), taken at the turn-in at Wilson (no cross-mission hand-off to McCormick). " +
                "Stacks 45062 x2, 118907 x2, 45446 x4, 111048 x4, one to choose. A marker at Wilson.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Items = new()
                {
                    new MissionItemBinding(ScrapKey, ScrapTemplate, MissionItemScope.AssignmentIssued, ScrapCount,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove, TurnInQuantity: ScrapCount)
                }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
