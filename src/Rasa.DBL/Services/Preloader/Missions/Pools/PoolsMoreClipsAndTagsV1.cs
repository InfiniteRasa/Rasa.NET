using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// More Clips and Tags (native mission 1321), Valverde Pools: Supply Chief Riggins's repeatable Death Patrol.
    ///
    /// Native: texts 10721-10725 (missionconversation): offer "This job never ends. The more soldiers we lose, the more we
    /// need to collect their tags and most importantly their empty clips", turn-in "Now get back to the grind."; objectives
    /// 1 "Collect Dog Tags" 10726/10727, 2 "Collect Empty Ammunition Clips" 10728/10729, no counters (missionobjective).
    /// The same items as Death Patrol (classes 25572, 25573).
    ///
    /// Sourced: TaRapedia More Clips and Tags: giver and receiver Supply Chief Riggins; 8 dog tags and 16 clips; credits
    /// "34000", no XP, no item; no requirement; twelve bodies, ten of them Death Patrol's and Corporal Schulte (-667, 777,
    /// 39) and Sergeant Steinfeldt (-427, 885, 657). PlanetTR (Dec 2007): "after you finish [Death Patrol] the first time,
    /// it becomes an infinitely repeatable mission ('This job never ends.')".
    ///
    /// Reconstructed (ESTIMATE): requires Death Patrol and repeats at once (PlanetTR); its bodies are Death Patrol's 22
    /// and TaRapedia's two more, the player's own for each run, as Death Patrol's: one clip a body, a tag by chance until
    /// 8 (PoolsFallenSoldiersScene), any 16 of the 24. Reward: 3,400 credits (TaRapedia's
    /// "34000" read as a typo: ten times Medicinal Measures' 3,400 and more than any Pools mission's) and 27,500 XP (no
    /// source: Medicinal Measures', a short Snake Pit job of this level); no item. A marker on each body for 1 and 2.
    /// </summary>
    public static class PoolsMoreClipsAndTagsV1
    {
        public const uint MissionId = 1321;
        public const uint Level = 33;
        public const uint Riggins = PoolsSnakePitNpcsV1.Riggins;
        public const uint TagCount = 8, ClipCount = 16;
        public const string TagKey = "more-dog-tags", ClipKey = "more-empty-ammo-clips";

        /// <summary>Death Patrol's 22 and TaRapedia's two more (heights from the navmesh).</summary>
        public static readonly (string Name, ScenePosition At)[] Bodies = PoolsDeathPatrolV1.Bodies.Concat(new (string, ScenePosition)[]
        {
            ("Corporal Schulte", new(-667, 777.4f, 39)),
            ("Sergeant Steinfeldt", new(-427, 886.2f, 657)),
        }).ToArray();

        public const uint Experience = 27500;
        public const uint Credits = 3400;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "More Clips and Tags", 10721, Riggins, Riggins, Level);
            mission.Reward(Experience, Credits, new uint[0]);
            mission.Prerequisite(PoolsDeathPatrolV1.MissionId);
            mission.RepeatImmediately();
            PoolsDeathPatrolV1.Collection(mission, 10726, 10727, 10728, 10729, TagCount, ClipCount, (uint)Bodies.Length);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1321",
                "Native1 'Collect Dog Tags' 10726/10727, 2 'Collect Empty Ammunition Clips' 10728/10729, no counters. Texts " +
                "10721-10725. Items as 1315: class25572/template97508, 25573/97509.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/More_Clips_and_Tags",
                "Giver/receiver Supply Chief Riggins; 8 dog tags, 16 clips; credits '34000', no XP, no item; 12 bodies (Schulte " +
                "-667,777,39 and Steinfeldt -427,885,657 new). PlanetTR Dec 2007: repeatable after Death Patrol.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMoreClipsAndTagsV1",
                "Requires 1315, repeats at once. 24 personal bodies (1315's 22 + Schulte, Steinfeldt), one clip each, a tag by " +
                "chance until 8; any 16 of 24. " +
                "3400 credits ('34000' a typo) and 27500XP (no source), no item. A marker on each body for 1 and 2.", 0.3);

            PoolsDeathPatrolV1.Markers(mission, Bodies);

            mission.Enable(PoolsDeathPatrolV1.Scene(MissionId, PoolsDeathPatrolV1.Clips, TagKey, ClipKey, TagCount, ClipCount, Bodies));
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
