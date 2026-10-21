using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Scavenger Hunt (native mission 1015), Plateau: Private "Scrounger" Scarpelo at Camp Resistance
    /// trades a carton of Hillsboro Lights - for Comic Relief (1251) - for five Thrax Insignia Patches
    /// and a Thrax Ceremonial Dagger.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 5555-5558 and 6440 (missionconversation);
    /// objectives 3 "Collect 5 Insignia Patches." 5563/6445 and 4 "Get a Thrax Ceremonial Dagger."
    /// 5564/6446, no counters, no objectives 1 and 2 (missionobjective). Patch: item class 21887
    /// (template 44881), dagger 21888 (44882), carton 21886 (44880). Unreferenced client texts 10026
    /// and 6448 name a Thrax "Luger" pistol instead of the dagger: an older version, not used.
    ///
    /// Sourced (TaRapedia Scavenger Hunt): giver and receiver Scarpelo, level "32 (possibly less)"
    /// (TR MMO DB: 25), 50,000 XP, no credits; requirement "Comic Relief (one objective)"; follow-up
    /// The Stalk Market; "These items randomly drop from any type of Thrax in Plateau."
    ///
    /// Reconstructed (ESTIMATE): level 31, as Comic Relief; offered once the player talked to the
    /// Scrounger in Comic Relief (flag <see cref="PlateauComicReliefV1.ScroungerFlag"/>), the "one
    /// objective"; personal drops from every Thrax kind the server has on the Plateau (Thrax Soldier,
    /// Grenadier, Technician, Commander, Preceptor Vorvaak; not the Maligo Elite Guards, Thrax in
    /// look only), a patch at 30% and the dagger at 10% a kill, picked up from the corpse; the
    /// Scrounger keeps them at turn-in; the reward is the carton itself, no item to choose. No
    /// indicator: any Thrax counts.
    /// </summary>
    public static class PlateauScavengerHuntV1
    {
        public const uint MissionId = 1015;
        public const uint Level = 31;
        public const uint Scarpelo = PlateauScroungerNpcsV1.Scarpelo;

        public const uint PatchTemplate = 44881, PatchClass = 21887, Patches = 5;
        public const uint DaggerTemplate = 44882, DaggerClass = 21888;
        public const string PatchKey = "thrax-insignia-patch", DaggerKey = "thrax-ceremonial-dagger";
        public const int PatchChance = 30, DaggerChance = 10;

        /// <summary>The Plateau's Thrax: Preceptor Vorvaak, Soldier, Grenadier, Technician, Commander.</summary>
        public static readonly uint[] Thrax = { 520054, 531082, 531083, 531084, 531086 };

        public const uint Experience = 50000;
        public const uint Credits = 0;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Scavenger Hunt", 5555, Scarpelo, Scarpelo, Level);
            mission.Reward(Experience, Credits, new uint[0], fixedTemplates: new[] { PlateauComicReliefV1.CartonTemplate });
            mission.FlagPrerequisite(PlateauComicReliefV1.ScroungerFlag, 1,
                "Requires the Scrounger's talk in Comic Relief (objective 10)");

            mission.Objective(3, 5563, 6445, 0, MissionObjectiveState.Incomplete);
            mission.Objective(4, 5564, 6446, 1, MissionObjectiveState.Incomplete);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, PatchClass, Patches);
            mission.Action(3, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(4);
            mission.Progress(4, MissionProgressEventKind.ItemAcquired, DaggerClass, 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1015",
                "Native3 'Collect 5 Insignia Patches' 5563/6445, 4 'Get a Thrax Ceremonial Dagger' 5564/6446, no counters, no " +
                "objectives1-2. Texts 5555-5558, 6440. Patch class21887/template44881, dagger 21888/44882, carton 21886/44880. " +
                "Orphan texts 10026/6448 (Thrax 'Luger') not used.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Scavenger_Hunt",
                "Giver/receiver Private 'Scrounger' Scarpelo, Camp Resistance; level '32 (possibly less)'; 50000XP, no credits; " +
                "requirement 'Comic Relief (one objective)'; follow-up The Stalk Market; drops from any type of Thrax in Plateau. " +
                "Stub page.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauScavengerHuntV1",
                "Level31. Offered after Comic Relief objective10 (flag1251001). Personal drops from Thrax 520054, 531082-531084, " +
                "531086 on map1497: patch 30%, dagger 10% (rates estimated). Turn-in takes 5 patches and the dagger; reward the " +
                "carton44880 (fixed), no choice.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 3, 4 },
                Items = new()
                {
                    new MissionItemBinding(PatchKey, PatchTemplate, MissionItemScope.CharacterOwned, Patches,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: Patches,
                        Drop: new MissionItemDropDefinition(Thrax, 3, PlateauMissionDataV1.MapContextId, PatchChance, 1)),
                    new MissionItemBinding(DaggerKey, DaggerTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(Thrax, 4, PlateauMissionDataV1.MapContextId, DaggerChance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
