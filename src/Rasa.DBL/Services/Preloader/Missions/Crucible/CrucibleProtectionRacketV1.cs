using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Protection Racket (native mission 1146), Ligo Crucible: Larai Grupa Vitto sends the player to take the Larai's
    /// protection money from the four merchants of the Staal Centrum. Vargo will not pay and sets his pet Flare Gasher,
    /// Roscoe, on the player; with Roscoe dead, he pays.
    ///
    /// Native: texts 8394-8397, 8398 (missionconversation): log "Grupa Vitto needs you to collect protection money from
    /// Merchant Bunko, Merchant Cugin, Merchant Vargo and Merchant Yeg", turn-in "Take this Token of the Larai syndicate and
    /// carry it with pride."; objectives (missionobjective, no counters) 1 "Collect Bunko's payment." 8917/8918, 2 "Collect
    /// Cugin's payment." 8920/8921, 3 "Collect Vargo's payment." 8923/8924, 4 "Collect Yeg's payment." 8931/8932 (its body
    /// names Bunko: the client's own text), 5 "Kill Vargo's pet Flare Gasher!" 8926/8927, 6 "Collect Vargo's money."
    /// 8928/8929; objectiveconversation (1146,1) package 1224 (Bunko) 8919, (1146,2) 1269 (Cugin) 8922, (1146,3) 1271 (Vargo)
    /// 8925 "I think I will let my "pet" convince you", (1146,4) 1272 (Yeg) 8933, (1146,6) 1271 8930 "You killed my Roscoe!
    /// ... Take your money". Item classes 24595 Bunko's, 24596 Cugin's, 24597 Vargo's, 24598 Yeg's Protection Money
    /// (templates 50215, 50216, 50223, 50217); 24749 Larai Syndicate Token (50314).
    ///
    /// Sourced: TaRapedia The Protection Racket: giver Larai Grupa Vitto (Staal Detention Center); requires The Boss Of All
    /// Bosses; 75,000 XP, 0 credits; the Larai Syndicate Token, then Vextronics Sonic Pistol or Vextronics Shotgun; "All
    /// merchants are located in the Staal Centrum. Only Vargo will refuse to pay and will order his pet Flare Gasher to attack
    /// you. This is an easy fight"; the pet is "Roscoe". TR MMO DB quest list: level 35, 75000, 0.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Vitto (the turn-in text is his). 1-4 open at once, any order; each talk gives that merchant's money. Vargo's
    ///    first talk (3) calls Roscoe: the player's own Flare Gasher (CrucibleGrupaChiefsNpcsV1.Roscoe) comes out 22 m west of him
    ///    and attacks, and 5 opens; his death opens 6, Vargo's second talk, which gives his money. Vitto takes the four at the
    ///    turn-in. "Help from the surrounding guards" is not built.
    ///  - The token a fixed reward (the turn-in text gives it); the two weapons the lowest Uncommon templates of the bands that
    ///    hold level 35: Sonic Pistol 14444 (35-39), Shotgun 14815 (Physical, 32-36); one to choose. The four Protection
    ///    Guaranty items (24864-24868) are not used. A marker at each merchant and one at Roscoe.
    /// </summary>
    public static class CrucibleProtectionRacketV1
    {
        public const uint MissionId = 1146;
        public const uint Level = 35;
        public const uint Vitto = CrucibleOutpostIntrepidNpcsV1.Vitto;
        public const uint Bunko = 1, Cugin = 2, Vargo = 3, Yeg = 4, Roscoe = 5, VargoPays = 6;
        public const uint RoscoeComes = 1, RoscoeDead = 2;          // scenarios (and their scene sequences)
        public const string RoscoeRole = "roscoe";
        public const double MarkerRadius = 8, RoscoeMarkerRadius = 15;

        /// <summary>The four payments: objective, name, body, the merchant's pool, its package, the money's key, template, class.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Npc, uint Package, string Key, uint Template, uint Class)[] Payments =
        {
            (Bunko, 8917, 8918, CrucibleGrupaChiefsNpcsV1.Bunko, CrucibleGrupaChiefsNpcsV1.BunkoPackage, "bunko-protection-money", 50215, 24595),
            (Cugin, 8920, 8921, CrucibleGrupaChiefsNpcsV1.Cugin, CrucibleGrupaChiefsNpcsV1.CuginPackage, "cugin-protection-money", 50216, 24596),
            (Vargo, 8923, 8924, CrucibleGrupaChiefsNpcsV1.Vargo, CrucibleGrupaChiefsNpcsV1.VargoPackage, "vargo-protection-money", 50223, 24597),
            (Yeg, 8931, 8932, CrucibleGrupaChiefsNpcsV1.Yeg, CrucibleGrupaChiefsNpcsV1.YegPackage, "yeg-protection-money", 50217, 24598)
        };

        public const uint Experience = 75000;
        public const uint Credits = 0;
        public const uint LaraiToken = 50314, LaraiTokenClass = 24749;
        public const uint SonicPistol = 14444;      // Weapon_Avatar_Pistol_Sonic_UNC_35_to_39
        public const uint Shotgun = 14815;          // Weapon_Avatar_Shotgun_v1_Physical_UNC_32_to_36

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Protection Racket", 8394, Vitto, Vitto, Level);
            mission.Reward(Experience, Credits, new[] { SonicPistol, Shotgun }, new[] { LaraiToken });
            mission.Prerequisite(CrucibleBossOfAllBossesV1.MissionId);
            mission.Scenario(RoscoeComes, "Vargo calls Roscoe", 0);
            mission.Scenario(RoscoeDead, "Roscoe is dead", 1);

            for (var index = 0; index < Payments.Length; index++)
            {
                var (objective, name, body, npc, _, _, _, _) = Payments[index];
                mission.Objective(objective, name, body, (uint)index, MissionObjectiveState.Incomplete);
                var at = CrucibleGrupaChiefsNpcsV1.At(npc);
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
            }
            mission.Objective(Roscoe, 8926, 8927, 4, MissionObjectiveState.Inactive);
            mission.Objective(VargoPays, 8928, 8929, 5, MissionObjectiveState.Inactive);

            foreach (var (objective, _, _, _, package, key, template, _) in Payments)
            {
                mission.Transition(objective);
                mission.Conversation(objective, package);
                if (objective == Vargo)
                {
                    // Vargo will not pay: he calls Roscoe.
                    mission.Action(objective, 1, MissionActionKind.StartScenario, scenario: RoscoeComes);
                    Open(mission, objective, Roscoe, 2);
                    continue;
                }
                mission.Action(objective, 1, MissionActionKind.IssueMissionItem,
                    itemIntent: new IssueMissionItemIntent($"{key}-paid", MissionId, key, template, 1));
            }

            mission.Transition(Roscoe);
            mission.Progress(Roscoe, MissionProgressEventKind.ScenarioEvent, 1, null, counter: RoscoeDead);
            Open(mission, Roscoe, VargoPays, 1);

            var vargoMoney = Payments[2];
            mission.Transition(VargoPays);
            mission.Conversation(VargoPays, CrucibleGrupaChiefsNpcsV1.VargoPackage);
            mission.Action(VargoPays, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent($"{vargoMoney.Key}-paid", MissionId, vargoMoney.Key, vargoMoney.Template, 1));
            mission.Action(VargoPays, 2, MissionActionKind.GrantReward, reward: 1);

            var roscoe = CrucibleGrupaChiefsNpcsV1.RoscoeAt;
            var vargo = CrucibleGrupaChiefsNpcsV1.VargoAt;
            mission.Indicator(Roscoe, 5, roscoe.X, roscoe.Y, roscoe.Z, RoscoeMarkerRadius);
            mission.Indicator(VargoPays, 6, vargo.X, vargo.Y, vargo.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1146",
                "Native1 Bunko 8917/8918, 2 Cugin 8920/8921, 3 Vargo 8923/8924, 4 Yeg 8931/8932, 5 'Kill Vargo's pet Flare Gasher!' " +
                "8926/8927, 6 'Collect Vargo's money.' 8928/8929, no counters; objectiveconversation 1146,1 package1224 8919, 1146,2 " +
                "1269 8922, 1146,3 1271 8925, 1146,4 1272 8933, 1146,6 1271 8930. Texts 8394-8398. Money classes 24595-24598 " +
                "(templates 50215, 50216, 50223, 50217); token 24749 (50314). Names 9070 Bunko, 9136 Yeg, 9140 Roscoe.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Protection_Racket",
                "Giver Larai Grupa Vitto (Staal); requires The Boss Of All Bosses; 75000XP, 0 credits; Larai Syndicate Token, then " +
                "Vextronics Sonic Pistol or Vextronics Shotgun; all merchants in the Staal Centrum; only Vargo refuses and sets his " +
                "pet Flare Gasher Roscoe on the player, an easy fight. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleProtectionRacketV1",
                "Receiver Vitto. 1-4 any order, each talk gives the money; Vargo's first talk calls the player's own Roscoe 770005 " +
                "(copy of Flaregasher 540030, level35, 3000HP), whose death opens 6; Vitto takes the money. Bunko 770003 and Yeg " +
                "770004 new, Cugin moved onto the Centrum floor. Token fixed; weapons 14444, 14815, one to choose. Markers at " +
                "each merchant and Roscoe.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The money, issued at the talks and taken at the turn-in, and Roscoe, the player's own, at Vargo's call.</summary>
        public static MissionSceneDefinition Scene()
        {
            var roscoe = CrucibleGrupaChiefsNpcsV1.RoscoeAt;
            var items = new List<MissionItemBinding>();
            foreach (var (_, _, _, _, _, key, template, _) in Payments)
                items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = items,
                Actors = new()
                {
                    [RoscoeRole] = new SceneActorDefinition(RoscoeRole, SceneActorKind.Creature, CrucibleGrupaChiefsNpcsV1.Roscoe,
                        new ScenePosition((float)roscoe.X, (float)roscoe.Y, (float)roscoe.Z), Orientation: 0,
                        GameplayPolicy: new ActorGameplayPolicy
                        {
                            RewardScenarioKills = true, TrackParticipation = true,
                            // The run's own creature on a public map: no ordinary loot table can bind it, so none.
                            Loot = new AuthoredLootProfile(System.Array.Empty<LootDrop>())
                        })
                },
                DefeatSequences = new() { [RoscoeRole] = RoscoeDead }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[RoscoeComes] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("roscoe-comes", RoscoeRole),
                    QuestSceneIntents.Attack("roscoe-attacks", RoscoeRole)
                }
            };
            scene.Sequences[RoscoeDead] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, RoscoeDead, 1) } };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);

        private static void Open(CrucibleMissionDataV1 mission, uint objective, uint next, uint firstAction)
        {
            mission.Action(objective, firstAction, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, firstAction + 1, MissionActionKind.ActivateObjective, targetObjective: next,
                state: MissionObjectiveState.Incomplete);
        }
    }
}
