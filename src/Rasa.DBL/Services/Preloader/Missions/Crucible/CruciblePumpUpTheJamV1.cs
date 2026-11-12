using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Pump Up The Jam (native mission 1224), Ligo Crucible: Captain Tiersky at Prometheus Outpost sends the player to destroy
    /// the Bane jamming towers on the perimeter of Khulago Base.
    ///
    /// Native: texts 9641-9645 (missionconversation): log "Destroy the eight Bane Jamming Towers at Kahlago Base for Captain
    /// Tiersky at Prometheus Outpost.", offer "Khalago Base has Jamming Towers all along the perimeter. They're really fouling
    /// up our comm lines! ... We're getting power signatures that show there are eight of them. Take them down, soldier!",
    /// turn-in "Comm lines are clear again. Good going! I'll see if I can't drum up some air support.", reminder "Our Comm
    /// Lines are out! I need all eight Jamming Towers taken out!"; objectives 1-6 "Destroy Tower #1" .. "Destroy Towwer #6"
    /// 9646/9647, 9648/9649, 9650/9651, 9652/9653, 9654/9655, 9656/9657 ("Destroy the first .. sixth Bane Jamming Tower."),
    /// no counters (missionobjective). No objective conversation. The client has no jamming tower class on the Crucible and
    /// its map places none (NOTES-crucible-research-client).
    ///
    /// Sourced: TaRapedia Pump Up The Jam (its page "bug noted"): giver and reward giver Captain Tiersky (Prometheus Outpost);
    /// requirement "Unknown"; follow-up Strike Zone; 85,000 XP, 6,000 credits; one of Vextronics Electric Staff, Teleract
    /// Rocket Launcher, Wellcare Direct Repair Tool, Vextronics Sonic Pistol; six tower objectives. TR MMO DB: level 36.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Six towers (the client's six objectives, not the texts' eight: triage decision 2), one objective each, all open from
    ///    the accept, any order. No prerequisite (TaRapedia "Unknown"; none found).
    ///  - Each tower a personal destructible scene object (as Bane Jamming 938's machinery, PoolsBaneJammingV1) of
    ///    UsableInertDestBaneSonicTowerV01 10282 (a Bane tower, augmentation 41 InertDestroyable, targetable; client name
    ///    "Sonic Tower"), shown with the client's usable name override 4 "Biomechanical Jamming Tower" (its key
    ///    GUARDPROM_JAMMINGTOWER is another zone's), 1500 hit points (PlacedDestructibles' Large size, the size the server
    ///    gives this class); its destruction is a scene result that completes its objective.
    ///  - The six places on the rim of Khulago Base (client region 383, 174, -188, r200), clockwise from the north: (433,
    ///    173.17, 2), (523, 185.34, -68), (523, 177.01, -208), (363, 164.68, -368), (323, 174.5, -258), (233, 178.95, -158).
    ///    Each on the navmesh and a client floor, 5 m or more from a wall, seen from 13 or more of 16 places 2 m round it, no
    ///    navmesh lava within 40 m, 22 m or more outside every hostile pool's circle, 500 m and more from every map link,
    ///    walked to from Confirmed Kills' north trench marker with no lava on the walk (probe 2026-11-07). A marker (r10) on
    ///    each.
    ///  - The items: the lowest template id of each kind whose Uncommon band holds level 36 (Staff_Electric 15181,
    ///    RocketLauncher_Physical 49136: the name gives no damage kind, as 1215's RPG; Tool_Avatar_Field_Repair_Direct 18260,
    ///    Pistol_Sonic 14444), one to choose.
    /// </summary>
    public static class CruciblePumpUpTheJamV1
    {
        public const uint MissionId = 1224;
        public const uint Level = 36;
        public const uint Tiersky = CrucibleKhulagoStrikesNpcsV1.Tiersky;
        public const uint Towers = 6;
        public const uint DestroyedScenario = 1;        // one scene result per tower

        public const uint TowerClass = 10282;           // UsableInertDestBaneSonicTowerV01 "Sonic Tower"
        public const uint TowerName = 4;                // usablenameoverride GUARDPROM_JAMMINGTOWER "Biomechanical Jamming Tower"
        public const uint TowerHitPoints = 1500;        // PlacedDestructibles.LargeHitPoints (10282 is Size.Large there)
        public const uint IntactState = 110, DestroyedState = 2;
        public const double MarkerRadius = 10;

        public const uint Experience = 85000;
        public const uint Credits = 6000;
        public const uint ElectricStaff = 15181;        // Weapon_Avatar_Staff_Electric_UNC_35_to_39
        public const uint RocketLauncher = 49136;       // Weapon_Avatar_RocketLauncher_Physical_UNC_34_to_38
        public const uint DirectRepairTool = 18260;     // Tool_Avatar_Field_Repair_Direct_UNC_35_to_39
        public const uint SonicPistol = 14444;          // Weapon_Avatar_Pistol_Sonic_UNC_35_to_39

        /// <summary>Towers #1-#6 on the rim of Khulago Base, clockwise from the north, on the navmesh.</summary>
        public static readonly ScenePosition[] TowersAt =
        {
            new(433f, 173.17f, 2f),
            new(523f, 185.34f, -68f),
            new(523f, 177.01f, -208f),
            new(363f, 164.68f, -368f),
            new(323f, 174.5f, -258f),
            new(233f, 178.95f, -158f),
        };

        /// <summary>Objective of tower #<paramref name="number"/> (1-6): the client's objective <paramref name="number"/>.</summary>
        public static uint Destroy(int number) => (uint)number;

        /// <summary>Name and body texts of tower #<paramref name="number"/>: 9646/9647 .. 9656/9657.</summary>
        public static (uint Name, uint Body) TextsOf(int number) => (9644 + 2 * (uint)number, 9645 + 2 * (uint)number);

        public static string TowerRole(int number) => $"jamming-tower-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Pump Up The Jam", 9641, Tiersky, Tiersky, Level);
            mission.Reward(Experience, Credits, new[] { ElectricStaff, RocketLauncher, DirectRepairTool, SonicPistol });
            mission.Scenario(DestroyedScenario, "A jamming tower destroyed", Towers);

            for (var number = 1; number <= Towers; number++)
            {
                var (name, body) = TextsOf(number);
                mission.Objective(Destroy(number), name, body, (uint)number - 1, MissionObjectiveState.Incomplete);
                mission.Transition(Destroy(number));
                mission.Progress(Destroy(number), MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: DestroyedScenario);
                mission.Action(Destroy(number), 1, MissionActionKind.GrantReward, reward: 1);
                var at = TowersAt[number - 1];
                mission.Indicator(Destroy(number), (uint)number, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1224",
                "Native1-6 'Destroy Tower #1'..'Destroy Towwer #6' 9646/9647..9656/9657 ('Destroy the first..sixth Bane Jamming " +
                "Tower.'), no counters; no objectiveconversation. Texts 9641-9645 (eight towers in the log and offer). No jamming " +
                "tower class on the Crucible; usablenameoverride 4 GUARDPROM_JAMMINGTOWER 'Biomechanical Jamming Tower'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Pump_Up_The_Jam",
                "Giver/reward giver Captain Tiersky (Prometheus Outpost); requirement Unknown; follow-up Strike Zone; 85000XP, 6000 " +
                "credits; one of Vextronics Electric Staff, Teleract Rocket Launcher, Wellcare Direct Repair Tool, Vextronics Sonic " +
                "Pistol; six towers. TR MMO DB level36.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CruciblePumpUpTheJamV1",
                "Six towers (the client's objectives), all open, any order; each a personal destructible 10282 Sonic Tower named " +
                "4, 1500HP, on the rim of Khulago Base (no navmesh lava within 40 m), a marker r10 on each; no prerequisite. " +
                "Items 15181, 49136, 18260, 14444 (lowest template of the kind with an Uncommon band at 36), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own six jamming towers; each one's destruction completes its objective.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = CrucibleMissionDataV1.MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= Towers; number++)
            {
                var role = TowerRole(number);
                var sequence = (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, TowerClass, TowersAt[number - 1],
                    Orientation: number * 1.0471975512, InitialObjectState: IntactState, NameOverrideId: TowerName,
                    Destruction: new SceneObjectDestruction(MissionId, Destroy(number), sequence, TowerHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, DestroyedScenario, (uint)number) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
