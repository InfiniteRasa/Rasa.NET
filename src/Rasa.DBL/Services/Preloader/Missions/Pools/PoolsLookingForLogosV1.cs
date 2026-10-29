using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Looking For Logos? (native mission 1308), Valverde Pools: Spymaster Timlin at the Snake Pit sends the player to
    /// Scientist Suresh at the South Listening Post; she wants to see the "Far" Logos, in Turpis Refinery.
    ///
    /// Native: texts 10610-10614 (missionconversation): offer "I have a scientist that helps man our South Listening Post
    /// that has done quite a bit of research on the subject", turn-in "Wow! You truely found the "Far" logos inside
    /// Turpis?!"; objectives 1 "Speak with Suresh" 10627/10628, 2 "Show Scientist Suresh the Far Logos" 10630/10631, 3
    /// "Find the "Far" Logos and return to Scientist Suresh" 10633/10634, 4 "Objective Tracker" 10635/10636, no counters
    /// (missionobjective); Suresh's lines on package 1495 for objective 1 (10629, "Timlin sent word that you would be
    /// coming") and 2 (10632, "You have the "Far" Logos in your Tablet already") (objectiveconversation, flag 1). The Far
    /// Logos is the server's shrine 64 (class 21210) in Turpis Refinery (1429) at 40, 111.8, -224, as Trinity Bridge
    /// Logos: Far (PlateauTrinityBridgeLogosV1) uses it.
    ///
    /// Sourced: TaRapedia Looking for Logos?: giver Spymaster Timlin, reward Scientist Suresh, level 33, 60,000 XP, 4,950
    /// credits, no requirement, no follow-up; one of Olympia Bio Armor Gloves, Wellcare Graviton Armor Helmet, Titan Mech
    /// Armor Gloves, Astra Stealth Armor Gloves, level 30 (the current page; older revisions list four weapons). Ten Ton
    /// Hammer "Turpis Refinery": Suresh "will change the Looking for Logos objective into finding the logos Far and
    /// returning to her"; "You'll want to show Suresh the logos to complete the Looking for Logos mission." Suresh is
    /// creature 510202 (pool 510202, package 1495) on the server already. The TR MMO DB Pools quest list gives level 35;
    /// TaRapedia's 33 is used.
    ///
    /// Reconstructed (ESTIMATE): objectives 1, then 3, then 2 (user, 2026-10-25); 3 completes on the Logos, and one the
    /// player has already counts (as 1051); 4, with no text, is left out. The four armor pieces as the lowest Uncommon
    /// V04 templates of the 30-34 band (the manufacturer names are not in the client tables), one to choose. Objective
    /// 3's marker on Turpis Refinery's door on the Pools.
    /// </summary>
    public static class PoolsLookingForLogosV1
    {
        public const uint MissionId = 1308;
        public const uint Level = 33;
        public const uint Timlin = PoolsSnakePitContactsV1.Timlin;
        public const uint Suresh = 510202, SureshPackage = 1495;
        public static readonly (double X, double Y, double Z) SureshAt = (244.2, 664.6, -588.1);
        public const uint Speak = 1, Show = 2, Find = 3;

        public const uint FarLogos = 64, TurpisRefinery = 1429;
        public static readonly (double X, double Y, double Z) RefineryDoor = (148, 673, -607);

        public const uint Experience = 60000;
        public const uint Credits = 4950;
        public const uint BioGloves = 46011;            // Armor_T3_Bio_V04_UNC_Gloves_30_to_34
        public const uint GravitonHelmet = 25079;       // Armor_T3_Graviton_V04_UNC_Helmet_30_to_34
        public const uint MechGloves = 34076;           // Armor_T3_Mech_V04_UNC_Gloves_30_to_34
        public const uint StealthGloves = 40203;        // Armor_T3_Stealth_V04_UNC_Gloves_30_to_34

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Looking For Logos?", 10610, Timlin, Suresh, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonHelmet, MechGloves, StealthGloves });

            mission.Objective(Speak, 10627, 10628, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Find, 10633, 10634, 1, MissionObjectiveState.Inactive);
            mission.Objective(Show, 10630, 10631, 2, MissionObjectiveState.Inactive);

            // 1: Suresh hears about the Bane chatter's Logos shrine.
            mission.Transition(Speak);
            mission.Conversation(Speak, SureshPackage);
            Open(mission, Speak, 1, Find);

            // 3: the Far Logos, in Turpis Refinery.
            mission.Transition(Find);
            mission.Progress(Find, MissionProgressEventKind.LogosAcquired, FarLogos, null);
            Open(mission, Find, 1, Show);

            // 2: Suresh sees it in the Tabula.
            mission.Transition(Show);
            mission.Conversation(Show, SureshPackage);
            mission.Action(Show, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Speak, 1, SureshAt.X, SureshAt.Y, SureshAt.Z);
            mission.Indicator(Find, 2, RefineryDoor.X, RefineryDoor.Y, RefineryDoor.Z, 10);
            mission.Indicator(Show, 3, SureshAt.X, SureshAt.Y, SureshAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1308",
                "Native1 'Speak with Suresh' 10627/10628, 2 'Show Scientist Suresh the Far Logos' 10630/10631, 3 'Find the Far " +
                "Logos and return to Scientist Suresh' 10633/10634, 4 'Objective Tracker' 10635/10636, no counters; " +
                "objectiveconversation 1308,1 and 1308,2 package1495 texts10629/10632. Texts 10610-10614. Far Logos 64 (class21210) " +
                "in Turpis Refinery 1429.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Looking_for_Logos%3F",
                "Giver Spymaster Timlin, reward Scientist Suresh; level33; 60000XP, 4950 credits; no requirement, no follow-up; " +
                "one of Olympia Bio Armor Gloves, Wellcare Graviton Armor Helmet, Titan Mech Armor Gloves, Astra Stealth Armor " +
                "Gloves (level30, current page); TR MMO DB list level35, TaRapedia's 33 used. Ten Ton Hammer Turpis Refinery: Suresh turns it into finding Far; show her the logos.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsLookingForLogosV1",
                "Order 1,3,2 (user); 3 on Logos64, one already owned counts; 4 left out. Items 46011, 25079, 34076, 40203 (V04 " +
                "UNC 30-34), one to choose. Marker 3 on the Refinery door (148,673,-607). Timlin 760202 new.", 0.3);

            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { Find } });
        }

        /// <summary>Reveals and activates <paramref name="target"/>, actions <paramref name="first"/> and the next.</summary>
        private static void Open(PoolsMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
