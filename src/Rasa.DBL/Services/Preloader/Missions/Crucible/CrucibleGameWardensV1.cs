using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Game Wardens (native mission 1147), Ligo Crucible: Viddia Grupa Donal wants Warden Bot parts for the Larai arena's
    /// reprogrammed bots: ten power supplies, ten neural processors and fifteen servo-motors, ripped from the hostile Warden
    /// Bots near the Warden Bot Facility.
    ///
    /// Native: texts 8399-8402, 8403 (missionconversation): log "Grupa Donal needs you to retrieve various important Warden
    /// Bot parts from hostile bots patrolling the area near the Warden Bot Facility. He needs: 15 Servo-Motors, 10 Power
    /// Supplies and 10 Neural Processors.", offer "Rip them from the inner workings of Warden Bots", turn-in "Take with you
    /// this token of the Viddia syndicate"; objectives (missionobjective, no counters) 1 "Get 10 Power Supplies" 8960/8961, 2
    /// "Get 10 Neural Processors" 8962/8963 (its body says "qo": the client's text), 3 "Get 15 Servo-Motors." 8964/8965; no
    /// objective conversation. Item classes 24603 Warden Bot Power Supply (template 50222), 24601 Warden Bot Neural
    /// Processor (50220), 24602 Warden Bot Servomotor (50221); 24750 Viddia Syndicate Token (50315).
    ///
    /// Sourced: TaRapedia Game Wardens: giver Viddia Grupa Donal (Staal Infirmary); requires The Boss Of All Bosses;
    /// follow-ups Architects Of Destruction (1202) and Sadistic Sabotage (1445); 75,000 XP, 0 credits; the Viddia Syndicate
    /// Token plus Voltrox Bio Armor Boots (icon 3) or Vitalius Stealth Armor Helmet (icon 2); objectives x/10, x/10, x/15. TR
    /// MMO DB quest list: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Donal (the turn-in text is his). The "Warden Bot Facility" is read as the Chaukas Robotics Facility's door
    ///    on the Crucible (no source names a facility of that name on Arieki's Ligo; the Chaukas Robotics Facility makes the
    ///    Warden Bots). The bots there are the new Warden Bot Screws (CrucibleWardensAndLashersNpcsV1).
    ///  - Each part a personal drop from any of them on the Crucible while its objective is open: a power supply at 50%, a
    ///    neural processor at 50%, a servo-motor at 75% (no source gives a rate; about twenty kills in all). Donal takes them
    ///    at the turn-in; an abandon keeps them (as the Marshes' drops).
    ///  - The token a fixed reward; the items the lowest Uncommon templates of the kinds and icon numbers in the band of
    ///    level 35: Bio V03 Boots 19395, Stealth V02 Helmet 39061; one to choose. One marker per objective round the three
    ///    pools.
    /// </summary>
    public static class CrucibleGameWardensV1
    {
        public const uint MissionId = 1147;
        public const uint Level = 35;
        public const uint Donal = CrucibleOutpostIntrepidNpcsV1.Donal;
        public const uint PowerSupplies = 1, NeuralProcessors = 2, ServoMotors = 3;
        public const uint WardenBot = CrucibleWardensAndLashersNpcsV1.WardenBot;

        /// <summary>The parts: objective, name, body, count, template, class, key, drop chance.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Count, uint Template, uint Class, string Key, int Chance)[] Parts =
        {
            (PowerSupplies, 8960, 8961, 10, 50222, 24603, "warden-bot-power-supplies", 50),
            (NeuralProcessors, 8962, 8963, 10, 50220, 24601, "warden-bot-neural-processors", 50),
            (ServoMotors, 8964, 8965, 15, 50221, 24602, "warden-bot-servomotors", 75)
        };

        /// <summary>The marker round the three Warden Bot pools: their middle and a radius that takes them in.</summary>
        public static readonly (double X, double Y, double Z) BotsAt = (-533.6, 249.7, 563.3);
        public const double BotsRadius = 55;

        public const uint Experience = 75000;
        public const uint Credits = 0;
        public const uint ViddiaToken = 50315, ViddiaTokenClass = 24750;
        public const uint BioBoots = 19395;             // Armor_T3_Bio_V03_UNC_Boots_35_to_39
        public const uint StealthHelmet = 39061;        // Armor_T3_Stealth_V02_UNC_Helmet_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Game Wardens", 8399, Donal, Donal, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, StealthHelmet }, new[] { ViddiaToken });
            mission.Prerequisite(CrucibleBossOfAllBossesV1.MissionId);

            foreach (var (objective, name, body, count, _, itemClass, _, _) in Parts)
            {
                mission.Objective(objective, name, body, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ItemAcquired, itemClass, count);
                mission.Indicator(objective, objective, BotsAt.X, BotsAt.Y, BotsAt.Z, BotsRadius);
            }
            mission.Action(PowerSupplies, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1147",
                "Native1 'Get 10 Power Supplies' 8960/8961, 2 'Get 10 Neural Processors' 8962/8963, 3 'Get 15 Servo-Motors.' " +
                "8964/8965, no counters; no objectiveconversation. Texts 8399-8403 (bots near the Warden Bot Facility). Parts " +
                "class24603 (50222), 24601 (50220), 24602 (50221); token 24750 (50315). Name 8045 Warden Bot Screw.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Game_Wardens",
                "Giver Viddia Grupa Donal (Staal Infirmary); requires The Boss Of All Bosses; follow-ups 1202, 1445; 75000XP, 0 " +
                "credits; Viddia Syndicate Token, then Voltrox Bio Armor Boots 3 or Vitalius Stealth Armor Helmet 2; x/10, x/10, " +
                "x/15. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleGameWardensV1",
                "Receiver Donal. The facility read as the Chaukas Robotics Facility's door; new Warden Bot Screws 770007 (copy of " +
                "Jailor 531044, level35) in pools 770009-770011. Parts personal drops 50/50/75%, taken at the turn-in, kept at " +
                "an abandon. Token fixed; items 19395, 39061, one to choose. One marker per objective round the pools.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { PowerSupplies, NeuralProcessors, ServoMotors },
                Items = new()
            };
            foreach (var (objective, _, _, count, template, _, key, chance) in Parts)
                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.CharacterOwned, count,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: count,
                    Drop: new MissionItemDropDefinition(new[] { WardenBot }, objective, CrucibleMissionDataV1.MapContextId, chance, 1)));
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
