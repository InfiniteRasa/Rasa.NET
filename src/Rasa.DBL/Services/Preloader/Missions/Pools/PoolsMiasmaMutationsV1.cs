using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Miasma Mutations (native mission 1131), Retread Caves (squad instance 1694), Valverde Pools: Dr. Donius wants ten
    /// skin samples of the miasmas his Mox poison mutated.
    ///
    /// Native: texts 8006-8010 (missionconversation): log "Bring 10 skin samples from mutated miasmas back to Dr. Donius
    /// in the Retread Caves.", offer "some of the poison that we used to kill the Mox has seeped through the cave system
    /// and into another cavern ... they seem to have, well, mutated", turn-in "Yes, I'll have to do this quickly.";
    /// objective 1 "Collect Mutation Samples" 8011/8012 (missionobjective); no objective conversation. Item: 49707
    /// MisRetreadMutatedMiasmaSkinSamples (class 24064).
    ///
    /// Sourced: TaRapedia Miasma Mutations: giver and receiver Dr. Donius (Retread Caves); requires Deadly Moxins,
    /// er-Toxins; 45,000 XP, 4,800 credits; one of Teleract Pulse RPG Launcher lvl 30, AccuMax Virulent Leech Gun lvl 29,
    /// Shinobi Incendiary Net Gun lvl 31, ChiTech Photonic Shotgun lvl 32. Ten Ton Hammer Retread Caves guide: "After you
    /// accept the mission, two mutated miasma will spring forth from the ground" (Dr. Donius boxes them); "Follow the
    /// mission marker from the Miasma Mutations mission into the Mox Lair and kill ten mutated miasmas."
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Each kill of a Mutated Miasma (PoolsRetreadCavesNpcsV1) drops one sample for the killer while 1 is open (100%,
    ///    as Monkey Wrench's key); samples already held count at the accept. Donius takes the ten at the turn-in.
    ///  - Two personal Mutated Miasmas spring up by Donius at the accept and attack the player; each drops a sample too.
    ///    The other eight or more come from the twelve public ones in the Mox Lair.
    ///  - Level 35, as the chain's. Rewards the lowest Uncommon templates of the bands that hold those levels ("Pulse RPG
    ///    Launcher" the EMP Grenade Launcher, "Photonic" Laser), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsMiasmaMutationsV1
    {
        public const uint MissionId = 1131;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Donius = PoolsRetreadCavesNpcsV1.Donius;
        public const uint Collect = 1;
        public const double PoolRadius = 15;

        public const uint SampleTemplate = 49707, SampleClass = 24064, Samples = 10;   // MisRetreadMutatedMiasmaSkinSamples
        public const string SampleKey = "mutated-miasma-sample";

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint PulseRpgLauncher = 16643;     // Weapon_Avatar_GrenadeLauncher_EMP_UNC_30_to_34
        public const uint VirulentLeechGun = 16597;     // Weapon_Avatar_LeechGun_Virulent_UNC_29_to_33
        public const uint IncendiaryNetGun = 48360;     // Weapon_Avatar_NetGun_Fire_UNC_31_to_35
        public const uint PhotonicShotgun = 14975;      // Weapon_Avatar_Shotgun_v1_Laser_UNC_32_to_36

        /// <summary>The two that spring up by Donius.</summary>
        public static readonly ScenePosition[] SpringAt = { new(-56f, 63.05f, -208f), new(-32f, 63.27f, -192f) };

        public static string SpringRole(int index) => $"mutated-miasma-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Miasma Mutations", 8006, Donius, Donius, Level);
            mission.Reward(Experience, Credits, new[] { PulseRpgLauncher, VirulentLeechGun, IncendiaryNetGun, PhotonicShotgun });
            mission.Prerequisite(PoolsDeadlyMoxinsV1.MissionId);

            mission.Objective(Collect, 8011, 8012, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Collect);
            mission.Progress(Collect, MissionProgressEventKind.ItemAcquired, SampleClass, Samples);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                ExistingFactObjectiveIds = new() { Collect },
                Items = new()
                {
                    new MissionItemBinding(SampleKey, SampleTemplate, MissionItemScope.CharacterOwned, Samples,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Samples,
                        Drop: new MissionItemDropDefinition(new[] { PoolsRetreadCavesNpcsV1.MutatedMiasma }, Collect, MapContextId, 100, 1))
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < SpringAt.Length; index++)
            {
                var role = SpringRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PoolsRetreadCavesNpcsV1.MutatedMiasma,
                    SpringAt[index], GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
                scene.Sequences[0].World.Add(new EnsureActorIntent($"{role}-springs", role));
                scene.Sequences[0].World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1131",
                "Native1 'Collect Mutation Samples' 8011/8012; no objectiveconversation. Texts 8006-8010 (10 skin samples). Item " +
                "49707 MisRetreadMutatedMiasmaSkinSamples class24064. Name 9037 Mutated Miasma.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Miasma_Mutations",
                "Giver/receiver Dr. Donius (Retread Caves); requires Deadly Moxins, er-Toxins; 45000XP, 4800 credits; one of " +
                "Teleract Pulse RPG Launcher30, AccuMax Virulent Leech Gun29, Shinobi Incendiary Net Gun31, ChiTech Photonic " +
                "Shotgun32. Ten Ton Hammer: two spring up at the accept; ten mutated miasmas in the Mox Lair.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMiasmaMutationsV1",
                "A sample a kill (100%, killer only, while open) of the Mutated Miasma 760226: two personal ones by Donius at " +
                "accept, twelve public ones in the Lair (pools 760235-760238). Level35. Items 16643, 16597, 48360, 14975, one to choose. Indicators in the instance too.", 0.3);

            var pools = PoolsRetreadCavesNpcsV1.MutatedPools;
            for (var index = 0; index < pools.Length; index++)
                mission.Indicator(Collect, 1 + (uint)index, pools[index].X, pools[index].Y, pools[index].Z, PoolRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
