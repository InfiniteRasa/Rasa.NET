using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Gas Shortages (native mission 1356), Ligo Ashen Desert: Lieutenant Dania Fontecilla at White Oasis Post sends the player
    /// to blow up the three fuel pumps of the Bane refuelling station on the Sheitan Embankment, east of the post.
    ///
    /// Native: texts 11149-11152, 6 = 11153 (missionconversation): log "Travel to the Shietan Embankment, east of White Oasis
    /// Post, and destroy the 3 Fuel Pumps that the Bane are using to refuel their vehicles. Once you have destroyed this
    /// position, report back to Lieutenant Dania Fontecilla at White Oasis Post.", offer "There's a Bane refuelling station just
    /// east of here. If you destroy those fuel pumps, it should slow their assault. ...", turn-in "Damn! Those gas pumps go up
    /// in all kinds of pretty colors. ...", reminder "I haven't seen any explosions. ..."; objectives (missionobjective) 1-3
    /// "Destroy Fuel Station 1" .. "3" 11342/11343, 11344/11345, 11346/11347, no counters. No objective conversation, no
    /// marker names. Item class 26442 Consc_Fuel_Bomb "Shaped Charge" (template 111185, stack 6). Pump class 10190
    /// UsableInertDestBaneRawFuelPumpHouseV01 "Raw Fuel Pump" (target flag 1; the model 9.8 by 5.9 m, 12 m tall); the client map
    /// places none. The embankment: three Bane landing pads ArchBaneObjLandingpadv02 (-50.5, 128.1), (-47.9, 172.9), (-102,
    /// 117.9) (statics) round the Embankment pool 531032 (World DB, Thrax garrison, radius 30).
    ///
    /// Sourced: no TaRapedia mission page. TaRapedia updates 2007-11-29 / 2007-12-11 and news 2007-11-22 / 2007-11-29: the
    /// mission "cannot be completed due to faulty bomb receptacles on gas pumps if you are disconnected" (known issue, fixed
    /// 2007-12-11): bomb receptacles on the pumps. TaRapedia Himbri: "Sheitan Embankment, a small Bane base East of the White
    /// Oasis Post CP".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Giver and receiver Fontecilla (the client's log; upstream 510012, moved off the control point's console by
    ///    AshenWhiteOasisNpcsV1). No prerequisite (no source). Level 41 (D5, the White Oasis missions).
    ///  - Three Shaped Charges (a mission item, AssignmentIssued, 3; "Fuel" in the class name) at the accept, taken back at an
    ///    abandon. Three personal Raw Fuel Pumps (10190, not the 13 by 18 m 12998) among the pads, their models 6.1 m and their devices 5.6 m
    ///    and more off each pad's model as the client places it (its own rotation; the dropship of pool 531032 lands on the pads), each on flat navmesh ground (0.22 m and less across its
    ///    model) and a client floor, seen from 16 of 16 places 2 m round it; beside each (8.1 m off its point, 3.2 m and more
    ///    outside its model) a personal Detonation Device (7334, use argument 1, 119 to 121, the "bomb receptacle") that takes
    ///    one charge and blows its pump (110 to 2, a destruction no hit reaches, as Turning Out the Lights' generators: the
    ///    server drops a hit on a disabled object), completing that pump's objective once. Any order.
    ///  - The walk from the White Oasis Post waypoint and back is on the client's floor and 30 m or more outside every hostile
    ///    pool's circle but the Embankment's own garrison 531032 (the station the player is sent into).
    ///  - A marker r10 on each pump (it takes in the device, 8.1 m off).
    ///  - Rewards (D6: no source): 95,000 XP and 6,300 credits (the zone median) and the stacks of the zone's talks (Formal
    ///    Complaints'), one to choose.
    /// </summary>
    public static class AshenGasShortagesV1
    {
        public const uint MissionId = 1356;
        public const uint Level = 41;
        public const uint Fontecilla = AshenWhiteOasisNpcsV1.Fontecilla;
        public const uint Pumps = 3;
        public const uint Blown = 1;                    // the scenario of the pumps: one scene result per pump
        public const double MarkerRadius = 10;   // takes in the pump and its device (8.1 m off)
        /// <summary>The Embankment's Thrax garrison (World DB): the station the mission sends the player into.</summary>
        public const uint EmbankmentPool = 531032;

        public const uint ChargeTemplate = 111185, ChargeClass = 26442;
        public const string ChargeKey = "shaped-charge";
        public const uint PumpClass = 10190;            // UsableInertDestBaneRawFuelPumpHouseV01 "Raw Fuel Pump"
        public const uint IntactState = AshenDeathFromAboveV1.IntactState, DestroyedState = AshenDeathFromAboveV1.DestroyedState;
        public const uint PumpHitPoints = AshenDeathFromAboveV1.LauncherHitPoints;
        public const uint DeviceClass = AshenDeathFromAboveV1.DeviceClass;
        public const uint DeviceReadyState = AshenDeathFromAboveV1.DeviceReadyState, DevicePlantedState = AshenDeathFromAboveV1.DevicePlantedState;
        public const uint UseArgument = AshenDeathFromAboveV1.UseArgument;

        /// <summary>Fuel Stations 1-3 (facing 0: the model 4.9 m across x, 2.95 m across z from its point).</summary>
        public static readonly ScenePosition[] PumpsAt =
        {
            new(-76f, 282.67f, 125f), new(-76f, 282.75f, 150f), new(-95f, 282.8f, 145f)
        };

        /// <summary>Each pump's Detonation Device, 8.1 m off it (probe 2026-11-10: navmesh ground on the client floor, seen from 16 of 16).</summary>
        public static readonly ScenePosition[] DevicesAt =
        {
            new(-76f, 282.69f, 133.1f), new(-67.9f, 282.65f, 150f), new(-95f, 282.74f, 136.9f)
        };

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint EmpGrenade = AshenFormalComplaintsV1.EmpGrenade, EmpGrenades = AshenFormalComplaintsV1.EmpGrenades;
        public const uint ResTraumaKit = AshenFormalComplaintsV1.ResTraumaKit, ResTraumaKits = AshenFormalComplaintsV1.ResTraumaKits;
        public const uint ConcussionGrenade = AshenFormalComplaintsV1.ConcussionGrenade, ConcussionGrenades = AshenFormalComplaintsV1.ConcussionGrenades;
        public const uint BasicMedPack = AshenFormalComplaintsV1.BasicMedPack, BasicMedPacks = AshenFormalComplaintsV1.BasicMedPacks;

        /// <summary>The objective of Fuel Station <paramref name="number"/> (1-3): the client's objective of the same number.</summary>
        public static uint Destroy(int number) => (uint)number;

        public static string PumpRole(int number) => $"sheitan-fuel-pump-{number}";
        public static string DeviceRole(int number) => $"sheitan-fuel-pump-{number}-detonation-device";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Gas Shortages", 11149, Fontecilla, Fontecilla, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);
            mission.Scenario(Blown, "A fuel pump blown up", Pumps);

            for (var number = 1; number <= Pumps; number++)
            {
                var objective = Destroy(number);
                mission.Objective(objective, 11340 + 2 * (uint)number, 11341 + 2 * (uint)number, objective - 1, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: Blown);
                mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = PumpsAt[number - 1];
                mission.Indicator(objective, objective, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1356",
                "Native1-3 'Destroy Fuel Station 1..3' 11342/11343 .. 11346/11347, no counters; no objectiveconversation. Texts " +
                "11149-11153 (the Shietan Embankment east of White Oasis Post, 3 fuel pumps; report to Lieutenant Dania " +
                "Fontecilla). Item class26442 Shaped Charge (template111185). Pump class10190 Raw Fuel Pump. Pads (-50.5, 128.1), " +
                "(-47.9, 172.9), (-102, 117.9).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Updates/2007-12-11",
                "No mission page. Updates 2007-11-29 / 2007-12-11, news 2007-11-22 / 2007-11-29: Gas Shortages not completable " +
                "after a disconnect, faulty bomb receptacles on the gas pumps (fixed). Himbri: Sheitan Embankment, a small Bane " +
                "base east of the White Oasis Post CP.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenGasShortagesV1",
                "Giver and receiver Fontecilla 510012 (moved off CP 17); no prerequisite; level41 estimated. Three charges 111185 " +
                "at the accept; three personal pumps 10190 among the pads (off the pads), a Detonation Device 7334 (arg1, " +
                "119->121) beside each takes one and blows it (110->2). Markers r10. 95000XP, 6300 credits (zone median); stacks " +
                "111029x3, 47600x1, 111019x3, 45063x6, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The three charges issued at the accept and the player's own three pumps, each with its Detonation Device.</summary>
        public static MissionSceneDefinition Scene() => Scene(PumpClass);

        /// <summary>The scene with the pumps of class <paramref name="pumpClass"/> (fix F: the V02 pump, AshenFixFV1).</summary>
        public static MissionSceneDefinition Scene(uint pumpClass)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ChargeKey, ChargeTemplate, MissionItemScope.AssignmentIssued, Pumps,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-shaped-charges", MissionId, ChargeKey, ChargeTemplate, Pumps) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var number = 1; number <= Pumps; number++)
            {
                var pump = PumpRole(number);
                var device = DeviceRole(number);
                var sequence = (uint)number;
                var objective = Destroy(number);
                scene.Actors[pump] = new SceneActorDefinition(pump, SceneActorKind.Object, pumpClass, PumpsAt[number - 1],
                    InitiallyInteractable: false, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, objective, sequence, PumpHitPoints, DestroyedState));
                scene.Actors[device] = new SceneActorDefinition(device, SceneActorKind.Object, DeviceClass, DevicesAt[number - 1],
                    InitialObjectState: DeviceReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{pump}", pump));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{device}", device));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{device}", device, DevicePlantedState),
                        new SetInteractionIntent($"close-{device}", device, false),
                        new TransitionObjectStateIntent($"blow-{pump}", pump, DestroyedState)
                    },
                    Character = { new ConsumeMissionItemIntent($"{device}-charge", MissionId, ChargeKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, Blown, sequence) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
