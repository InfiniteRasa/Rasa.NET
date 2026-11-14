using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Rehabilitation at Gunpoint (native mission 1402), Ligo Crucible and the Incurables Ward (squad instance 2141): Larai
    /// Sigal Tempa sends the player into the Ward to thin out the Incurables and execute Lord Jaikev and Lady Parvatee.
    ///
    /// Native: texts 12067-12071 (missionconversation): log "Travel into the Incurables Ward inside of Staal Centrum. Thin out
    /// the population of Incurables by killing them, and execute Lord Jaikev and Lady Parvatee for the security of the Brann.
    /// Report back to Sigal Tempa inside of Staal ...", offer "Silence the Incurables and bring word of their deaths back to
    /// me. If you do this, Grupa Vitto will reward you accordingly. ...", turn-in "You have done what even my best Enforcers
    /// couldn't do. ... Grupa Vitto be giving his thanks. He wanted you to have these."; objectives (missionobjective) 1 "Kill
    /// Jaikev" 12651/12652 counter 12653, 2 "Kill Parvatee" 12654/12655 counter 12656, 3 "Execute 30 Incurables" 12657/12658
    /// counter 12659 "Incurables Killed"; no objective conversation.
    ///
    /// Sourced: TaRapedia Rehabilitation at Gunpoint: giver and reward giver Larai Sigal Tempa ("just outside the Larai
    /// Arena", -307, 180, 326); requirement "?"; 180,000 XP, 8,200 credits; one of Modification: Health Bonus, Physical
    /// Resistance, Light Resistance, Steal Power; the Ward's "Rehabilitation at Gunpoint (Given outside the instance)" (TRW-IW,
    /// 0.5). TR MMO DB: level 35, 180000 XP (its quest list's 1,800,000 a typo).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - No prerequisite (none known). The three objectives open at once, any order. 1 and 2 the kill of Jaikev 770049 and
    ///    of Parvatee 770050 (CreatureKilled, counter 0 to 1); 3 the kills of the Incurables' two classes, 23191 and 25611
    ///    (CreatureClassKilled on counter 0, one trigger per class, as Architects Of Destruction 1202), 30; the bosses' own
    ///    classes do not count. The turn-in at Tempa on the Crucible.
    ///  - The scene bound to the Ward (BoundMapContextId): taken at Tempa on the Crucible, it waits for its owner in the squad's
    ///    Ward. Jaikev and Parvatee named public-spawn actors: in the squad's copy they come back on their pools' own 90 s
    ///    (SpawnPoolManager.ReturnsOnOwnTime); the Incurables with the copy (36 in nine pools).
    ///  - Tempa 510028 moved to TaRapedia's place by the Larai Arena (CrucibleIncurablesWardNpcsV1).
    ///  - The items: "Modification" read as the grade [4] modules (as Salvage Duty 1204 and The Supply Master): Armor Module:
    ///    Health Bonus [4] 122971, Armor Module: Resist Physical [4] 123046, Armor Module: Resist Photonic [4] 123041 (Light),
    ///    Weapon Module: Steal Power [4] 123276 (client itemtemplatelanguage). One to choose.
    ///  - Markers: on Jaikev and on Parvatee (r10), on each Incurable pool (r15), and before the Ward's door on the Crucible
    ///    for each objective.
    /// </summary>
    public static class CrucibleRehabilitationAtGunpointV1
    {
        public const uint MissionId = 1402;
        public const uint Level = 35;
        public const uint Tempa = CrucibleIncurablesWardNpcsV1.Tempa;
        public const uint MapContextId = CrucibleIncurablesWardNpcsV1.Ward;
        public const uint KillJaikev = 1, KillParvatee = 2, Execute = 3;
        public const uint Incurables = 30;
        /// <summary>Redshirt_Brann_Incurable_Male and Redshirt_Brann_Incurable_Female (client entityclass).</summary>
        public static readonly uint[] IncurableClasses = { CrucibleIncurablesWardNpcsV1.BruteClass, CrucibleIncurablesWardNpcsV1.SlayerClass };
        public const double BossMarkerRadius = 10, PoolMarkerRadius = 15, DoorMarkerRadius = 6;
        public const string JaikevRole = "jaikev", ParvateeRole = "parvatee";

        public const uint Experience = 180000;
        public const uint Credits = 8200;
        public const uint HealthBonus = 122971;         // Armor Module: Health Bonus [4]
        public const uint ResistPhysical = 123046;      // Armor Module: Resist Physical [4]
        public const uint ResistPhotonic = 123041;      // Armor Module: Resist Photonic [4]
        public const uint StealPower = 123276;          // Weapon Module: Steal Power [4]

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Rehabilitation at Gunpoint", 12067, Tempa, Tempa, Level);
            mission.Reward(Experience, Credits, new[] { HealthBonus, ResistPhysical, ResistPhotonic, StealPower });

            mission.Objective(KillJaikev, 12651, 12652, 0, MissionObjectiveState.Incomplete, counterTextId: 12653);
            mission.Objective(KillParvatee, 12654, 12655, 1, MissionObjectiveState.Incomplete, counterTextId: 12656);
            mission.Objective(Execute, 12657, 12658, 2, MissionObjectiveState.Incomplete, counterTextId: 12659);

            mission.Transition(KillJaikev);
            mission.Progress(KillJaikev, MissionProgressEventKind.CreatureKilled, CrucibleIncurablesWardNpcsV1.Jaikev, 1, counter: 0);
            mission.Action(KillJaikev, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(KillParvatee);
            mission.Progress(KillParvatee, MissionProgressEventKind.CreatureKilled, CrucibleIncurablesWardNpcsV1.Parvatee, 1, counter: 0);
            mission.Transition(Execute);
            for (var i = 0; i < IncurableClasses.Length; i++)
                mission.Progress(Execute, MissionProgressEventKind.CreatureClassKilled, IncurableClasses[i], Incurables, counter: 0, trigger: (uint)i + 1);

            var door = CrucibleIncurablesWardNpcsV1.DoorWayAt;
            var jaikev = CrucibleIncurablesWardNpcsV1.JaikevAt;
            var parvatee = CrucibleIncurablesWardNpcsV1.ParvateeAt;
            var marker = 1U;
            mission.Indicator(KillJaikev, marker++, jaikev.X, jaikev.Y, jaikev.Z, BossMarkerRadius, MapContextId);
            mission.Indicator(KillParvatee, marker++, parvatee.X, parvatee.Y, parvatee.Z, BossMarkerRadius, MapContextId);
            foreach (var (_, at) in CrucibleIncurablesWardNpcsV1.IncurablePools)
                mission.Indicator(Execute, marker++, at.X, at.Y, at.Z, PoolMarkerRadius, MapContextId);
            foreach (var objective in new[] { KillJaikev, KillParvatee, Execute })
                mission.Indicator(objective, marker++, door.X, door.Y, door.Z, DoorMarkerRadius, CrucibleMissionDataV1.MapContextId);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1402",
                "Native1 'Kill Jaikev' 12651/12652 counter12653, 2 'Kill Parvatee' 12654/12655 counter12656, 3 'Execute 30 " +
                "Incurables' 12657/12658 counter12659 'Incurables Killed'; no objectiveconversation. Texts 12067-12071. Names 9067 " +
                "Larai Sigal Tempa, 9539 Jaikev, 9622 Parvatee; classes 23191/25611 Incurable Brann, 25547/25617 their bosses.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Rehabilitation_at_Gunpoint",
                "Giver/reward giver Larai Sigal Tempa (outside the Larai Arena, -307,180,326); requirement unknown; 180000XP, 8200 " +
                "credits; Modification: Health Bonus, Physical Resistance, Light Resistance, Steal Power; given outside the " +
                "instance (TRW-IW). TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleRehabilitationAtGunpointV1",
                "Bound to the squad's Ward 2141; no prerequisite. 1/2 CreatureKilled Jaikev 770049 / Parvatee 770050 (public-spawn " +
                "actors, 90s in the copy); 3 CreatureClassKilled 23191 and 25611, counter 0, 30 (36 in pools 770047-770055). Tempa " +
                "510028 moved to -307,180.79,326. Modules [4] 122971, 123046, 123041, 123276, one to choose. Markers: the bosses, " +
                "the pools, before the Ward's door.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Jaikev and Parvatee named in the squad's Ward, no sequence: back on their pools' own time.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = MapContextId,   // taken at Tempa, played in the squad's Ward
                MapContextId = MapContextId
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[JaikevRole] = new SceneActorDefinition(JaikevRole, SceneActorKind.PublicSpawn, CrucibleIncurablesWardNpcsV1.JaikevPool);
            scene.Actors[ParvateeRole] = new SceneActorDefinition(ParvateeRole, SceneActorKind.PublicSpawn, CrucibleIncurablesWardNpcsV1.ParvateePool);
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
