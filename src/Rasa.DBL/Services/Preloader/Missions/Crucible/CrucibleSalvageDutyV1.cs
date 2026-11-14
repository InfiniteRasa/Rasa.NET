using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Salvage Duty (native mission 1203) and More Salvage Duty (1204), Ligo Crucible: Labbna Sigal Haggus wants machine
    /// parts from the Warden and Reconstructor Bots: six Electrostatic Sequencers and eight Pressure Boosters, then the rarer
    /// six Process Controllers, ten Solenoid Valves and ten Vacuum Pumps.
    ///
    /// Native: 1203 texts 9434-9437, 9438 (missionconversation): log "Collect 6 Electrostatic Sequencers and 8 Pressure
    /// Boosters by hunting Reconstructor and Warden Bots.", offer "You will find Electrostatic Sequencers and Pressure Boosters
    /// in the inner workings of Warden and Reconstructor Bots"; objectives 1 "Get 6 Sequencers" 9439/9440, 2 "Get 8 Pressure
    /// Boosters" 9441/9442. 1204 texts 9453-9456, 9457: log "Collect 6 Process Controllers, 10 Solenoid Valves and 10 Vacuum
    /// Pumps by hunting Reconstructor and Warden Bots and return them to Sigal Haggus", offer "some rarer parts ... not so
    /// common"; objectives 3 "Get 6 Process Controllers." 9462/9463, 4 "Get 10 Solenoid Valves" 9464/9465, 5 "Get 10 Vacuum
    /// Pumps." 9466/9467 (missionobjective: no counters; 1204 has no objectives 1 and 2). No objective conversation. Item
    /// classes 25073 MisCrucibleSalvageElectrostaticSequencer (template 50431), 25069 ...PressureBooster (50429), 25072
    /// ...ProcessController (50430), 25068 ...SolenoidValve (50428), 25067 ...VacuumPump (50427), stacks of 10.
    ///
    /// Sourced: TaRapedia Salvage Duty: giver and reward giver Labbna Sigal Haggus (Labbna Quarter); requires Junkyard Dogs;
    /// follow-up More Salvage Duty; 80,000 XP, 5,850 credits; one of AccuMax Electric Blade, Vextronics Virulent Staff,
    /// Incendiary RPG Launcher, AccuMax Rifle. TaRapedia More Salvage Duty: Haggus; requires Salvage Duty; 160,000 XP, 7,800
    /// credits; one of Modification: Light Resistance, Regen Bonus, Regen Armor Bonus, Debuff to Fire Resist; its counters
    /// x/6, x/8, x/8 (0.3: the names say 10).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Levels 35 (no source; the level of the bots and of 1147, the Warden Bot mission).
    ///  - 1204's counts 6, 10, 10 from the client objective names (triage decision 2: the client has no counter text; TaRapedia's
    ///    counters x/8 are 0.3).
    ///  - Each part a personal drop from a Warden Bot Screw (770007, set 3) or a Reconstructor Bot (770015) on the Crucible
    ///    while its objective is open: 1203 a sequencer at 40%, a pressure booster at 50% ("the most common"); 1204 a
    ///    process controller at 25%, a solenoid valve and a vacuum pump at 30% ("rarer"); no source gives a rate. Haggus takes
    ///    them at the turn-in; an abandon keeps them (as 1147's).
    ///  - Items: 1203 the lowest Uncommon templates of the kinds in the band of level 35 ("RPG Launcher" read as the physical
    ///    rocket launcher, no fire one exists; the rifle of no kind read as physical): Electric Blade 15470, Virulent Staff
    ///    15247, Rocket Launcher 49136, Rifle 14610. 1204 "Modification" read as the grade [4] modules (as The Supply
    ///    Master's): Armor Module: Resist Photonic [4] 123041 (Light), Armor Module: Regen Bonus [4] 122986, Armor Module:
    ///    Armor Recharge [4] 122961 (Regen Armor), Weapon Module: Debuff Fire Resist [4] 123176. One to choose.
    ///  - For each objective a marker (radius 20) on each bot pool: the three Warden Bot pools and the two Reconstructor
    ///    pools.
    /// </summary>
    public static class CrucibleSalvageDutyV1
    {
        public const uint MissionId = 1203, MoreMissionId = 1204;
        public const uint Level = 35, MoreLevel = 35;
        public const uint Haggus = CrucibleGrupaChiefsNpcsV1.Haggus;
        public const uint WardenBot = CrucibleWardensAndLashersNpcsV1.WardenBot;
        public const uint Reconstructor = CrucibleMinefieldAndSalvageNpcsV1.Reconstructor;
        public const double MarkerRadius = 20;

        /// <summary>The parts: mission, objective, name, body, count, template, class, key, drop chance.</summary>
        public static readonly (uint Mission, uint Objective, uint Name, uint Body, uint Count, uint Template, uint Class, string Key, int Chance)[] Parts =
        {
            (MissionId, 1, 9439, 9440, 6, 50431, 25073, "salvage-electrostatic-sequencers", 40),
            (MissionId, 2, 9441, 9442, 8, 50429, 25069, "salvage-pressure-boosters", 50),
            (MoreMissionId, 3, 9462, 9463, 6, 50430, 25072, "salvage-process-controllers", 25),
            (MoreMissionId, 4, 9464, 9465, 10, 50428, 25068, "salvage-solenoid-valves", 30),
            (MoreMissionId, 5, 9466, 9467, 10, 50427, 25067, "salvage-vacuum-pumps", 30)
        };

        /// <summary>The bot pools the markers stand on: the Warden Bots' (set 3), then the Reconstructors'.</summary>
        public static IEnumerable<(uint Pool, (double X, double Y, double Z) At)> BotPools =>
            CrucibleWardensAndLashersNpcsV1.WardenPools.Concat(CrucibleMinefieldAndSalvageNpcsV1.ReconstructorPools);

        public const uint Experience = 80000, Credits = 5850;
        public const uint ElectricBlade = 15470;        // Weapon_Avatar_Blade_Electric_UNC_31_to_35
        public const uint VirulentStaff = 15247;        // Weapon_Avatar_Staff_Virulent_UNC_35_to_39
        public const uint RocketLauncher = 49136;       // Weapon_Avatar_RocketLauncher_Physical_UNC_34_to_38
        public const uint Rifle = 14610;                // Weapon_Avatar_Rifle_Physical_UNC_31_to_35

        public const uint MoreExperience = 160000, MoreCredits = 7800;
        public const uint ResistPhotonic = 123041;      // Armor Module: Resist Photonic [4]
        public const uint RegenBonus = 122986;          // Armor Module: Regen Bonus [4]
        public const uint ArmorRecharge = 122961;       // Armor Module: Armor Recharge [4]
        public const uint DebuffFire = 123176;          // Weapon Module: Debuff Fire Resist [4]

        public static void Up(MigrationBuilder migration)
        {
            var salvage = new CrucibleMissionDataV1(migration, MissionId, "Salvage Duty", 9434, Haggus, Haggus, Level);
            salvage.Reward(Experience, Credits, new[] { ElectricBlade, VirulentStaff, RocketLauncher, Rifle });
            salvage.Prerequisite(CrucibleJunkyardDogsV1.MissionId);
            Collect(salvage, MissionId);
            salvage.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1203",
                "Native1 'Get 6 Sequencers' 9439/9440, 2 'Get 8 Pressure Boosters' 9441/9442, no counters; no " +
                "objectiveconversation. Texts 9434-9438 (Reconstructor and Warden Bots). Parts class25073 (50431), 25069 (50429).");
            salvage.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Salvage_Duty",
                "Giver/reward giver Labbna Sigal Haggus (Labbna Quarter); requires Junkyard Dogs; follow-up More Salvage Duty; " +
                "80000XP, 5850 credits; one of AccuMax Electric Blade, Vextronics Virulent Staff, Incendiary RPG Launcher, AccuMax " +
                "Rifle; parts in Warden and Reconstructor Bots.");
            salvage.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleSalvageDutyV1",
                "Level35 (no source). Parts personal drops from Warden Bot Screws 770007 and Reconstructor Bots 770015, 40/50%, " +
                "taken at the turn-in, kept at an abandon. Items 15470, 15247, 49136, 14610, one to choose. A marker r20 per " +
                "objective on each bot pool.", 0.3);
            salvage.Enable(Scene(MissionId));

            var more = new CrucibleMissionDataV1(migration, MoreMissionId, "More Salvage Duty", 9453, Haggus, Haggus, MoreLevel);
            more.Reward(MoreExperience, MoreCredits, new[] { ResistPhotonic, RegenBonus, ArmorRecharge, DebuffFire });
            more.Prerequisite(MissionId);
            Collect(more, MoreMissionId);
            more.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1204",
                "Native3 'Get 6 Process Controllers.' 9462/9463, 4 'Get 10 Solenoid Valves' 9464/9465, 5 'Get 10 Vacuum Pumps.' " +
                "9466/9467, no counters; no objectiveconversation. Texts 9453-9457. Parts class25072 (50430), 25068 (50428), " +
                "25067 (50427).");
            more.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/More_Salvage_Duty",
                "Giver/reward giver Labbna Sigal Haggus; requires Salvage Duty; 160000XP, 7800 credits; one of Modification: Light " +
                "Resistance, Regen Bonus, Regen Armor Bonus, Debuff to Fire Resist; counters x/6, x/8, x/8.");
            more.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleSalvageDutyV1",
                "Level35 (no source). Counts 6/10/10 from the client names (triage decision 2). Parts personal drops from Warden Bot Screws and " +
                "Reconstructor Bots, 25/30/30%, taken at the turn-in, kept at an abandon. Modules [4] 123041, 122986, 122961, " +
                "123176, one to choose. A marker r20 per objective on each bot pool.", 0.3);
            more.Enable(Scene(MoreMissionId));
        }

        public static void Down(MigrationBuilder migration)
        {
            CrucibleMissionDataV1.Remove(migration, MoreMissionId);
            CrucibleMissionDataV1.Remove(migration, MissionId);
        }

        /// <summary>The mission's part objectives, open at once, each by its item held; the reward on the first; the markers.</summary>
        private static void Collect(CrucibleMissionDataV1 mission, uint missionId)
        {
            var parts = Parts.Where(part => part.Mission == missionId).ToArray();
            var marker = 0U;
            for (var ordinal = 0; ordinal < parts.Length; ordinal++)
            {
                var (_, objective, name, body, count, _, itemClass, _, _) = parts[ordinal];
                mission.Objective(objective, name, body, (uint)ordinal, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ItemAcquired, itemClass, count);
                foreach (var (_, at) in BotPools)
                    mission.Indicator(objective, ++marker, at.X, at.Y, at.Z, MarkerRadius);
            }
            mission.Action(parts[0].Objective, 1, MissionActionKind.GrantReward, reward: 1);
        }

        private static MissionSceneDefinition Scene(uint missionId)
        {
            var parts = Parts.Where(part => part.Mission == missionId).ToArray();
            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = parts.Select(part => part.Objective).ToList(),
                Items = new()
            };
            foreach (var (_, objective, _, _, count, template, _, key, chance) in parts)
                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.CharacterOwned, count,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: count,
                    Drop: new MissionItemDropDefinition(new[] { WardenBot, Reconstructor }, objective, CrucibleMissionDataV1.MapContextId, chance, 1)));
            return scene;
        }
    }
}
