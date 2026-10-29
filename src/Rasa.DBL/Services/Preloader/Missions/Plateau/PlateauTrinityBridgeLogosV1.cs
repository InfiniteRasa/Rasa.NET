using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The five Trinity Bridge Logos that lie in instanced maps (native missions 1047, 1048, 1049, 1051,
    /// 1057), Plateau: after Trinity Bridge Logos: Past (1044), Vogren's hologram sends the player for the
    /// Logos "Future", "Disperse", "Logos", "Far" and "Distance", each in the squad instance of another
    /// zone, and takes them back at Vogren's Tomb. Each completes as 1044 does, on the Logos.
    ///
    /// Native: per mission the name/log/offer/turn-in/reminder texts (missionconversation) and objective 1
    /// "Acquire ..." with no counter (missionobjective); no objective conversation. The Logos are the
    /// server's shrines (table logos), already in their maps; a squad instance copies them with the map
    /// (DynamicObjectManager.CloneTemplateMap):
    ///  - 1047 Future: logos 58 (class 21222) in Guardian Prominence (1416) at 351.7, 333.7, 255.9;
    ///  - 1048 Disperse: logos 30 (class 12678) in P'reo Das (1743) at -33, 65.5, 390;
    ///  - 1049 Logos: logos 65 (class 21269) in the Logos Research Facility (1700) at 608, 258, -30;
    ///  - 1051 Far: logos 64 (class 21210) in Turpis Refinery (1429) at 40, 111.8, -224;
    ///  - 1057 Distance: logos 63 (class 21181) in Lamnas Armory (1465) at 36.7, 12.4, -16.
    ///
    /// Sourced: TaRapedia Trinity Bridge Logos pages: giver and receiver the Holographic Projection Device,
    /// Vogren's Tomb; requirement Trinity Bridge Logos: Past; level "33 (possibly less)" (not for 1048);
    /// credits 5,700 (1047) and 5,400 (the others); 65,000 XP for 1051 and 1057; the Logos as the reward;
    /// for 1057 four level 35 vests to choose from (named on the page by armor line only).
    ///
    /// Reconstructed (ESTIMATE): 65,000 XP for 1047, 1048 and 1049 too; level 33 for 1048; no item for
    /// 1057 (the vests are not named). A Logos the player already has counts. Indicator 1 on objective 1 at
    /// the Logos shrine (its logos row's place, radius <see cref="ShrineRadius"/>), in the instance too: the
    /// server draws a marker on every map for now (fixed later).
    /// The way from the door to Future (a 4.5 m step in a cave turn) and to Far (a gap of about 20 m in
    /// the navmesh of a raw Bane chunnel) is not checked for a player (playtest).
    /// </summary>
    public static class PlateauTrinityBridgeLogosV1
    {
        public const uint Hologram = PlateauVogrenNpcsV1.Hologram;
        public const uint Level = 33;
        public const uint Experience = 65000;
        public const double ShrineRadius = 8;

        /// <summary>One of the five: mission, Logos, credits and texts.</summary>
        public sealed record Logos(uint MissionId, string Name, uint LogosId, uint MapContextId, uint Credits,
            uint NameText, uint ObjectiveName, uint ObjectiveBody, uint Reminder, string Page, string Where,
            (double X, double Y, double Z) At);

        public static readonly Logos Future = new(1047, "Trinity Bridge Logos: Future", 58, 1416, 5700, 5885, 5889, 6378, 6377,
            "https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos:_Future", "Guardian Prominence 1416, 351.7,333.7,255.9",
            (351.7, 333.7, 255.9));
        public static readonly Logos Disperse = new(1048, "Trinity Bridge Logos: Disperse", 30, 1743, 5400, 5890, 5894, 6372, 6371,
            "https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos:_Disperse", "P'reo Das 1743, -33,65.5,390", (-33, 65.5, 390));
        public static readonly Logos LogosElement = new(1049, "Trinity Bridge Logos: Logos", 65, 1700, 5400, 5895, 5899, 6382, 6381,
            "https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos:_Logos", "Logos Research Facility 1700, 608,258,-30", (608, 258, -30));
        public static readonly Logos Far = new(1051, "Trinity Bridge Logos: Far", 64, 1429, 5400, 5905, 5909, 6376, 6375,
            "https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos:_Far", "Turpis Refinery 1429, 40,111.8,-224", (40, 111.8, -224));
        public static readonly Logos Distance = new(1057, "Trinity Bridge Logos: Distance", 63, 1465, 5400, 5935, 5939, 6374, 6373,
            "https://tabularasa.fandom.com/wiki/Trinity_Bridge_Logos:_Distance", "Lamnas Armory 1465, 36.7,12.4,-16", (36.7, 12.4, -16));

        public static readonly Logos[] All = { Future, Disperse, LogosElement, Far, Distance };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var logos in All)
                Up(migration, logos);
        }

        private static void Up(MigrationBuilder migration, Logos logos)
        {
            var mission = new PlateauMissionDataV1(migration, logos.MissionId, logos.Name, logos.NameText, Hologram, Hologram, Level);
            mission.Reward(Experience, logos.Credits, new uint[0]);
            mission.Prerequisite(PlateauTrinityLogosPastV1.MissionId);

            mission.Objective(1, logos.ObjectiveName, logos.ObjectiveBody, 0, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.LogosAcquired, logos.LogosId, null);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(1, 1, logos.At.X, logos.At.Y, logos.At.Z, ShrineRadius);

            var sourced = logos.MissionId is 1051 or 1057;
            mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{logos.MissionId}",
                $"Native1 {logos.ObjectiveName}/{logos.ObjectiveBody}, no counter; texts {logos.NameText}-{logos.NameText + 3}, " +
                $"{logos.Reminder}; no objective conversation. Logos{logos.LogosId} '{logos.Name.Substring(22)}' in {logos.Where} (logos table).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, logos.Page,
                $"Giver/receiver the Holographic Projection Device, Vogren's Tomb; requirement Trinity Bridge Logos: Past; " +
                (logos.MissionId == 1048 ? "no level; " : "level '33 (possibly less)'; ") +
                (sourced ? "65000XP, " : "no XP; ") + $"{logos.Credits} credits; reward the Logos" +
                (logos.MissionId == 1057 ? "; four level 35 vests (armor lines only)." : "."), 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauTrinityBridgeLogosV1",
                $"Level33{(logos.MissionId == 1048 ? " (no source)" : "")}" + (sourced ? "" : ", 65000XP as 1051/1057") +
                ", no item. Completes on the Logos; one already owned counts. Indicator at the shrine. The Logos is in a squad " +
                "instance, copied with its map.", 0.3);

            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { 1 } });
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var logos in All)
                PlateauMissionDataV1.Remove(migration, logos.MissionId);
        }
    }
}
