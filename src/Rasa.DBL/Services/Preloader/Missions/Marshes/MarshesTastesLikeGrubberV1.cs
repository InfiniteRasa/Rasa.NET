using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Tastes Like Grubber (native mission 959), Valverde Marshes: Research Chemist Horsh at Falcon Hold suspects the Bane's
    /// pollutants have reached the Swamp Grubbers' diet and wants meat samples of them.
    ///
    /// Native: texts 5211-5214 and 7001 (missionconversation): log "Bring 10 Swamp Grubber meat samples back to Research
    /// Chemist Horsch in Falcon's Hold. Grubbers can be found throughout the western marshes.", offer "bringing me back samples
    /// of the beasts - flesh, meat, internal organs - for analysis", turn-in "There's definitely been at least a low level of
    /// contamination"; objective 1 "Gather meat from the Swamp Grubbers" 5215/7002, no counter (missionobjective); no
    /// objective conversation. Item Grubber Steak class 21777 (World DB itemtemplate_itemclass: templates 44694, 44822; stack
    /// 10). Name 3142 Research Chemist Horsh.
    ///
    /// Sourced: TaRapedia Tastes Like Grubber: giver and receiver Research Chemist Horsh (Falcon Hold); follow-up Fixing the
    /// Food Chain; 65,000 XP, 5,400 credits; one of Modification: Electric / Fire / Ice / Virulent Resistance lvl 30. TR MMO
    /// DB: level 33, 5,400 credits. Ten Ton Hammer Falcon Hold: grubbers "in the swamp outside the base", 5,400 credits, an
    /// "Armor Modification Recipe". World DB: Swamp Grubber 540014, two pools on map 1454, both in the east.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Ten steaks (the log's count), personal drops picked up from the corpse, from any Swamp Grubber 540014 on the map at
    ///    60% (no source gives a rate); Horsh keeps them at the turn-in. The template the class's lowest (44694).
    ///  - Two mission-only Grubber pools west of Falcon Hold (MarshesFalconHoldNpcsV1.SteakPools), the log's "western
    ///    marshes"; a marker on each.
    ///  - Level TR MMO DB's (TaRapedia gives none). "Modification" read as the grade [4] Armor Modules (Ten Ton Hammer's
    ///    "Armor Modification", the Pools' reading of a "Modification"), Resist Electric, Fire, Ice, Virulent, one to choose.
    /// </summary>
    public static class MarshesTastesLikeGrubberV1
    {
        public const uint MissionId = 959;
        public const uint Level = 33;
        public const uint Horsh = MarshesFalconHoldNpcsV1.Horsh;
        public const uint Gather = 1;

        public const uint SteakTemplate = 44694, SteakClass = 21777, SteakCount = 10;
        public const string SteakKey = "grubber-steak";
        public const int SteakChance = 60;
        public const uint SwampGrubber = MarshesFalconHoldNpcsV1.SwampGrubber;
        public const double PoolMarkerRadius = 20;

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint ResistElectric = 123016;      // Armor Module: Resist Electric [4]
        public const uint ResistFire = 123026;          // Armor Module: Resist Fire [4]
        public const uint ResistIce = 123031;           // Armor Module: Resist Ice [4]
        public const uint ResistVirulent = 123071;      // Armor Module: Resist Virulent [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Tastes Like Grubber", 5211, Horsh, Horsh, Level);
            mission.Reward(Experience, Credits, new[] { ResistElectric, ResistFire, ResistIce, ResistVirulent });

            mission.Objective(Gather, 5215, 7002, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Gather);
            mission.Progress(Gather, MissionProgressEventKind.ItemAcquired, SteakClass, SteakCount);
            mission.Action(Gather, 1, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            foreach (var (_, x, y, z) in MarshesFalconHoldNpcsV1.SteakPools)
                mission.Indicator(Gather, marker++, x, y, z, PoolMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/959",
                "Native1 'Gather meat from the Swamp Grubbers' 5215/7002, no counter; no objectiveconversation. Texts 5211-5214, " +
                "7001 (log: 10 meat samples, western marshes, Horsch). Item class21777 Grubber Steak (templates 44694, 44822). " +
                "Name 3142 Research Chemist Horsh.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Tastes_Like_Grubber",
                "Giver/receiver Research Chemist Horsh (Falcon Hold); follow-up Fixing the Food Chain; 65000XP, 5400 credits; one of " +
                "Modification: Electric/Fire/Ice/Virulent Resistance lvl30. TR MMO DB level33. Ten Ton Hammer: grubbers in the " +
                "swamp outside the base, an Armor Modification Recipe.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesTastesLikeGrubberV1",
                "10 steaks (template44694), personal drops at 60% from Swamp Grubber 540014 on map1454 (rate estimated); Horsh " +
                "keeps them. Horsh 760309 a copy of Vetter's row at TaRapedia's place. Two mission pools 760310-760311 west of " +
                "Falcon Hold, a marker on each. Level TR MMO DB's. Armor Modules [4] 123016, 123026, 123031, 123071, one to choose.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Gather },
                Items = new()
                {
                    new MissionItemBinding(SteakKey, SteakTemplate, MissionItemScope.CharacterOwned, SteakCount,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: SteakCount,
                        Drop: new MissionItemDropDefinition(new[] { SwampGrubber }, Gather, MarshesMissionDataV1.MapContextId, SteakChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
