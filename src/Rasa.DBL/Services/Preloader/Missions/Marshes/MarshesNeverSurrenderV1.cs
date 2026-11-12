using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Never Surrender (native mission 891), the Village Ruins (squad instance 1743): Forean Warrior Tola, pinned down by the
    /// arrival, asks the player to push the Bane back from his village, P'reo Das; Elder Kirii there thanks him.
    ///
    /// Native: texts 4676-4679 and 7283 (missionconversation): title "Never Surrender", log "Hold back the Bane attack on Tola's
    /// Village", offer "We are under attack by the Bane and can barely hold them back. My village, up ahead, was overrun, so my
    /// warriors and I were trying get help when we were pinned.", turn-in "The few us of that do remain, owe you a debt of
    /// gratitude."; objective (missionobjective) 1 "Hold back the Bane attack on P'reo Das." 4694/7284, no counter; no
    /// objective conversation.
    ///
    /// Sourced: TaRapedia Never Surrender: giver Forean Warrior Tola, receiver Forean Elder Kirii (P'reo Das); 130,000 XP,
    /// 7,000 credits; one of 3 Class VII Standard Med Pack, 2 Class VII Advanced Med Pack, 3 Class VII Incendiary Grenade, 3
    /// Class VII EMP Grenade; follow-up Cleaning House; walkthrough "Proceed north and repel the Bane invasion." TR MMO DB:
    /// level 35. Items (the client's classes 22544, 22545, 26288, 26278): templates 45061, 45062, 111038, 111028. World DB: the
    /// Marshes Thrax Soldier 531045 and Grenadier 531046 (the Village Ruins' own pools' rows).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Taken at Tola in the squad's Village Ruins. Walking into P'reo Das (30 m round the region's centre; hidden 2) brings
    ///    the attack: three waves of the run's own Thrax, 4, 5 and 6 (the triage's sizes), at once, after 40 s and after 80 s,
    ///    from the north edge of the village (the walkthrough's "north"), each attacking the player. Hidden 3 counts the 15
    ///    kills; 3 done completes 1. No fail: a player who dies comes back to the waves still standing.
    ///  - Kirii takes the turn-in. Markers at P'reo Das (the attack) and at Kirii.
    /// </summary>
    public static class MarshesNeverSurrenderV1
    {
        public const uint MissionId = 891;
        public const uint Level = 35;
        public const uint Tola = MarshesPreoDasVillageNpcsV1.Tola, Kirii = MarshesPreoDasVillageNpcsV1.Kirii;
        public const uint Ruins = MarshesPreoDasVillageNpcsV1.MapContextId;
        public const uint HoldBack = 1, Arrive = 2, KillTally = 3;
        public const uint VillageArea = 1;

        /// <summary>Scenarios: the attack (its sequence), the kills.</summary>
        public const uint AttackComes = 1, Kills = 2;
        public const uint WaveSequence = 10, DefeatSequence = 100;

        public const uint Soldier = 531045, Grenadier = 531046;
        public static readonly (double X, double Y, double Z) VillageAt = (100.8, 57.8, -236.8);
        public const double ArriveRadius = 30, MarkerRadius = 40;

        /// <summary>The waves: when (ms after the attack comes) and who comes from where (the village's north edge).</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (0, new[] { (Soldier, new ScenePosition(100f, 56.91f, -185f)), (Soldier, new ScenePosition(120f, 55.78f, -190f)),
                (Grenadier, new ScenePosition(140f, 62.69f, -206.8f)), (Grenadier, new ScenePosition(56.4f, 57.97f, -207.2f)) }),
            (40000, new[] { (Soldier, new ScenePosition(102f, 56.91f, -185f)), (Soldier, new ScenePosition(122f, 55.78f, -190f)),
                (Soldier, new ScenePosition(56.4f, 57.97f, -205.2f)), (Grenadier, new ScenePosition(98f, 56.91f, -185f)),
                (Grenadier, new ScenePosition(140f, 62.69f, -208.8f)) }),
            (80000, new[] { (Soldier, new ScenePosition(100f, 56.91f, -187f)), (Soldier, new ScenePosition(118f, 55.78f, -190f)),
                (Soldier, new ScenePosition(138f, 62.69f, -206.8f)), (Soldier, new ScenePosition(58.4f, 57.97f, -207.2f)),
                (Grenadier, new ScenePosition(120f, 55.78f, -192f)), (Grenadier, new ScenePosition(56.4f, 57.97f, -209.2f)) })
        };
        public const uint Attackers = 15;

        public const uint Experience = 130000;
        public const uint Credits = 7000;
        public const uint StandardMedPack = 45061, StandardMedPacks = 3;        // Class VII Standard Med Pack
        public const uint AdvancedMedPack = 45062, AdvancedMedPacks = 2;        // Class VII Advanced Med Pack
        public const uint IncendiaryGrenade = 111038, IncendiaryGrenades = 3;   // Class VII Incendiary Grenade
        public const uint EmpGrenade = 111028, EmpGrenades = 3;                 // Class VII EMP Grenade

        public static string WaveRole(int wave, int member) => $"wave-{wave + 1}-{member + 1}";
        public static string WaveTimer(int wave) => $"wave-{wave + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Never Surrender", 4676, Tola, Kirii, Level);
            mission.Reward(Experience, Credits,
                new[] { (StandardMedPack, StandardMedPacks), (AdvancedMedPack, AdvancedMedPacks), (IncendiaryGrenade, IncendiaryGrenades),
                    (EmpGrenade, EmpGrenades) },
                new (uint, uint)[0]);
            mission.Scenario(AttackComes, "In P'reo Das: the Bane attack", 0);
            mission.Scenario(Kills, "The attacking Thrax killed", Attackers);
            mission.Area(VillageArea, Ruins, VillageAt.X, VillageAt.Y, VillageAt.Z, ArriveRadius, 20);

            mission.Objective(HoldBack, 4694, 7284, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Arrive, 4694, 7284, 1, MissionObjectiveState.Incomplete, required: false);       // hidden
            mission.Objective(KillTally, 4694, 7284, 2, MissionObjectiveState.Incomplete, required: false);    // hidden

            mission.Transition(Arrive);
            mission.EnterArea(Arrive, VillageArea);
            mission.Action(Arrive, 1, MissionActionKind.StartScenario, scenario: AttackComes);

            mission.Transition(KillTally);
            for (var kill = 1U; kill <= Attackers; kill++)
                mission.Progress(KillTally, MissionProgressEventKind.ScenarioEvent, kill, Attackers, counter: Kills, trigger: kill);
            mission.Transition(HoldBack);
            mission.ObjectiveState(HoldBack, KillTally, MissionObjectiveState.Completed);
            mission.Action(HoldBack, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(HoldBack, 1, VillageAt.X, VillageAt.Y, VillageAt.Z, MarkerRadius, Ruins);
            var kirii = MarshesPreoDasVillageNpcsV1.KiriiAt;
            mission.Indicator(HoldBack, 2, kirii.X, kirii.Y, kirii.Z, 8, Ruins);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/891",
                "Native1 'Hold back the Bane attack on P'reo Das.' 4694/7284, no counter, no objectiveconversation. Texts 4676-4679, " +
                "7283 (log 'Tola's Village'; offer: pinned on the way to get help, 'My village, up ahead'). Names 8560 Tola, 8557 " +
                "Kirii. Items classes 22544/22545/26288/26278 (templates 45061, 45062, 111038, 111028).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Never_Surrender",
                "Giver Forean Warrior Tola, receiver Forean Elder Kirii (P'reo Das); 130000XP, 7000 credits; one of 3 Class VII " +
                "Standard Med Pack, 2 Advanced Med Pack, 3 Incendiary Grenade, 3 EMP Grenade; follow-up Cleaning House; walkthrough " +
                "'Proceed north and repel the Bane invasion'. TR MMO DB level35.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesNeverSurrenderV1",
                "Bound to 1743; Tola 760376 by the arrival, Kirii 760377 at P'reo Das. Hidden 2: an area r30 at P'reo Das brings three " +
                "waves (4/5/6 Thrax 531045/531046, at 0/40/80 s) from the village's north edge, attacking the player; hidden 3 counts " +
                "the 15 kills and completes 1. No fail. Sizes, times and places are estimates.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The run's own attackers, on the squad's copy of the Village Ruins.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Ruins,
                MapContextId = Ruins,
                DefeatSequences = new(),
                HiddenObjectiveIds = new() { Arrive, KillTally }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            var attack = new SceneSequenceDefinition();
            var kill = 0U;
            for (var wave = 0; wave < Waves.Length; wave++)
            {
                var comes = new SceneSequenceDefinition();
                for (var member = 0; member < Waves[wave].Members.Length; member++)
                {
                    var role = WaveRole(wave, member);
                    var (creature, at) = Waves[wave].Members[member];
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at, Orientation: 3.1416,
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    comes.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    comes.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                    kill++;
                    scene.DefeatSequences[role] = DefeatSequence + kill;
                    scene.Sequences[DefeatSequence + kill] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, Kills, kill) } };
                }
                if (Waves[wave].After == 0)
                {
                    attack.World.AddRange(comes.World);
                    continue;
                }
                attack.Timers.Add(new SequenceTimer(WaveTimer(wave), Waves[wave].After, WaveSequence + (uint)wave));
                scene.Sequences[WaveSequence + (uint)wave] = comes;
            }
            scene.Sequences[AttackComes] = attack;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
