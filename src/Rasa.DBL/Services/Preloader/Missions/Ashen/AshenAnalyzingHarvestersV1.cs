using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Analyzing the Harvesters (native mission 1295), Ligo Ashen Desert: Captain Daris Linier at White Oasis Post sends the
    /// player with Doctor Meeks' diagnostic tool to the four Brann Water Harvesters of Maji Field; Meeks reads the results.
    ///
    /// Native: texts 10442-10446 (missionconversation): log "Find the Harvester Processor at each of the four Water Harvesters
    /// in Maji Field. Run the diagnostic program at each harvester, then return to Dr. Harold Meeks at White Oasis Outpost with
    /// the results.", offer "The main objective of my Ops here in Maji Field are to repair and secure those big Water
    /// Harvesters out there. ... Dr. Meeks is here to take care of all the technical details. In fact, the Doctor has put
    /// together a special tool ...", turn-in "Thank you soldier. Let me look at those results. ... the processing and filtration
    /// systems have worn out. ... I'm going to need your help though, so don't go away."; objectives (missionobjective) 1-4
    /// "Analyze Harvester 1".."4" 11854/11855, 11856/11857, 11858/11859, 11860/11861, no counters. No objective conversation, no
    /// marker names. Item class 25476 Mis_Ligo_Ashen_Maji_Diagnostic "Harvester Diagnostic Tool" (template 97477, stack 1).
    /// Usable name 69 LIGO_ASHEN_MAJI_DIAGNOSTIC "Harvester Control Panel" (usablenameoverride). The client map: four
    /// ArchBrannHarvesterV01 in Maji Field (-296, 287, 175), (-269, 292, 306), (-386, 299, 361), (-385, 299, 482) (statics).
    ///
    /// Sourced: TaRapedia Analyzing the Harvesters: giver Captain Daris Linier, reward giver Dr. Harold Meeks (White Oasis Post);
    /// follow-up Murky Waters; 6,450 credits, XP "?"; one of Vextronics Flame Thrower, Incendiary Net Gun, Cryogenic Leech Gun,
    /// Pulse Rifle; the tool made "using the information given to us by the Viddia Syndicate". TaRapedia Poisoning the
    /// Wellwater (1302): "Harvester Control Panel 1 at -305.7, 198.1, 2 at -260.2, 340.6, 3 at -375.5, 394.1, 4 at -363.9,
    /// 510.7" (the same usable name 69, so the same panels).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 41 (triage D5: after You Can't Drink Ash, 40). XP 100,000 (TaRapedia "?": the 6,450-credit missions of the zone
    ///    pay 100,000, as Turning Out the Lights). No prerequisite (TaRapedia names none; You Can't Drink Ash is a lead-in).
    ///  - The tool (a mission item, AssignmentIssued, 1) at the accept, gone at the turn-in or an abandon. The panels do not
    ///    check it (the panels are the holder's own and a mission item cannot be destroyed, so the holder always has it).
    ///  - A personal Harvester Control Panel at each harvester (the client's human monitor UsableTwoStateHumComputerV01V06 9478,
    ///    off 55, on 56, usabledata 1, as set 9's radar consoles; named 69; facing 0): panels 1 and 3 at TaRapedia's spots, 2 on
    ///    the harvester's west side (-292, 305), 23 m off it (TaRapedia's spot is 25 m from Fithik pool 540075's circle and 26 m
    ///    from Overseer Ulovik 520006), 4 4 m off TaRapedia's spot (seen from 15 of 16 ring places there). Heights from the
    ///    navmesh. Each on a client floor, seen from 16 of 16 places 2 m round it, walked to on the client's floor from the White
    ///    Oasis Post waypoint and back, the walk 30 m or more outside every hostile pool's circle (probe of 2026-11-10). Its use
    ///    completes its objective (a scene result) and turns it on. Any order.
    ///  - A marker r8 on each panel.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 41 (client entityclass names; "Flame
    ///    Thrower" the fire propellant gun, as set 3; "Pulse" EMP): Propellant Gun Fire 48752 (37-41), Net Gun Fire 12125
    ///    (41-45), Leech Gun Ice 47772 (39-43), Rifle EMP 11957 (41-45); one to choose.
    /// </summary>
    public static class AshenAnalyzingHarvestersV1
    {
        public const uint MissionId = 1295;
        public const uint Level = 41;
        public const uint DarisLinier = AshenWhiteOasisNpcsV1.DarisLinier, Meeks = AshenWhiteOasisNpcsV1.Meeks;
        public const uint Analyzed = 1;                 // the scenario of the panels' uses: step K for harvester K
        public const double MarkerRadius = 8;

        public const uint ToolTemplate = 97477, ToolClass = 25476;
        public const string ToolKey = "harvester-diagnostic-tool";
        public const uint PanelClass = AshenOnTheSameFrequencyV1.ConsoleClass;   // 9478 UsableTwoStateHumComputerV01V06
        public const uint PanelName = 69;               // usablenameoverride LIGO_ASHEN_MAJI_DIAGNOSTIC "Harvester Control Panel"
        public const uint UseArgument = AshenOnTheSameFrequencyV1.UseArgument;
        public const uint OffState = AshenOnTheSameFrequencyV1.OffState, OnState = AshenOnTheSameFrequencyV1.OnState;

        /// <summary>Harvester Control Panels 1-4, in the client's objective order (TaRapedia's numbering).</summary>
        public static readonly ScenePosition[] PanelsAt =
        {
            new(-305.7f, 284.0f, 198.1f), new(-292f, 290.71f, 305f), new(-375.5f, 297.29f, 394.1f), new(-367f, 298.49f, 514f)
        };

        public const uint Experience = 100000;
        public const uint Credits = 6450;
        public const uint FlameThrower = 48752;         // Weapon_Avatar_PropellantGun_Fire_UNC_37_to_41
        public const uint IncendiaryNetGun = 12125;     // Weapon_Avatar_NetGun_Fire_UNC_41_to_45
        public const uint CryogenicLeechGun = 47772;    // Weapon_Avatar_LeechGun_Ice_UNC_39_to_43
        public const uint PulseRifle = 11957;           // Weapon_Avatar_Rifle_EMP_UNC_41_to_45

        public static string PanelRole(int number) => $"maji-harvester-panel-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Analyzing the Harvesters", 10442, DarisLinier, Meeks, Level);
            mission.Reward(Experience, Credits, new[] { FlameThrower, IncendiaryNetGun, CryogenicLeechGun, PulseRifle });
            mission.Scenario(Analyzed, "A Harvester Control Panel analyzed", (uint)PanelsAt.Length);

            for (var number = 1; number <= PanelsAt.Length; number++)
            {
                var objective = (uint)number;
                mission.Objective(objective, 11852 + 2 * objective, 11853 + 2 * objective, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, objective, null, counter: Analyzed);
                var at = PanelsAt[number - 1];
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
            }
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1295",
                "Native1-4 'Analyze Harvester 1'..'4' 11854/11855..11860/11861, no counters; no objectiveconversation. Texts " +
                "10442-10446 (four Water Harvesters in Maji Field; return to Dr. Harold Meeks). Item class25476 Harvester Diagnostic " +
                "Tool (template97477). Usable name 69 'Harvester Control Panel'. Harvesters (-296, 175), (-269, 306), (-386, 361), " +
                "(-385, 482).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Analyzing_the_Harvesters",
                "Giver Captain Daris Linier, reward giver Dr. Harold Meeks (White Oasis Post); follow-up Murky Waters; 6450 credits, " +
                "XP '?'; Vextronics Flame Thrower, Incendiary Net Gun, Cryogenic Leech Gun, Pulse Rifle. Poisoning the Wellwater: " +
                "Harvester Control Panels at -305.7, 198.1 / -260.2, 340.6 / -375.5, 394.1 / -363.9, 510.7.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenAnalyzingHarvestersV1",
                "Level41 and 100000XP estimated; no prerequisite. The tool 97477 at the accept, gone at the turn-in. A personal " +
                "panel 9478 (name69; arg1; 55->56) per harvester: 1, 3 at TaRapedia's spots, 2 at (-292, 305) (off a Fithik pool and " +
                "Ulovik), 4 4 m off; its use completes its objective. Markers r8. Items 48752, 12125, 47772, 11957 (lowest Uncommon, " +
                "band of level 41), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The diagnostic tool issued at the accept and the player's own four panels.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ToolKey, ToolTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-harvester-diagnostic-tool", MissionId, ToolKey, ToolTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= PanelsAt.Length; number++)
            {
                var role = PanelRole(number);
                var objective = (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, PanelClass, PanelsAt[number - 1],
                    InitialObjectState: OffState, NameOverrideId: PanelName,
                    UseAction: new SceneObjectAction(MissionId, objective, objective, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[objective] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-analyzed", role, OnState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, Analyzed, objective) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
