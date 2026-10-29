using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Monkey Wrench (native mission 1188), Ustor Yard (squad instance 1502): Field Lieutenant Rapp sends the
    /// player to the main facility computer in the centre of the yard to shut down the supply line conveyor;
    /// Headquarters takes the mission back by radio.
    ///
    /// Native: texts 9132-9136 (missionconversation): "infiltrate the main facility of the yard and gain access
    /// to the main computer system. From there you can turn off the supply line conveyor belt", turn-in "the
    /// conveyor belt has a large amount of drill parts on it"; objectives (missionobjective) 1 "Gain Access to
    /// Main Facility Computer From the Bane Security Officer." 9147/9148, 2 "Use Bane Computer Keyboard to Shut
    /// Down Facility Supply Line." 9151/9152, 3 "Enter Access Key to Gain Access to Facility Computer."
    /// 9149/9150; no counters, no objective conversation. Item: Ustor Facility Key (class 24888, template
    /// 50349, MisPlateauUstorItemFaciltyKey). The client's map: Yard Regulation and Control (map_region 4,
    /// round (75, 179, -12)), a console room (61-67, 179, -9 to -16) with a command chair, beside the foundry.
    ///
    /// Sourced: TaRapedia Monkey Wrench: giver Field Lieutenant Rapp, reward from Headquarters (Radio);
    /// requirement Interception!; 50,000 XP, 6,600 credits; one of Modification: Steal Armor / Regen Bonus /
    /// Debuff to Electric Resist / Electric Resistance; "Kill the guards, use the computer." TR MMO DB: level
    /// 32. A patch note: "Fixed an issue with Ustor mission monkey wrench where it couldn't be completed."
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - 1: the officer is a boss with three guards in the console room (PlateauUstorYardV1.Officer, pool
    ///    750067); his corpse gives the key, a personal 100% drop while 1 is open, picked up.
    ///  - 3: the key goes into a personal UsableTwoStateBaneConsoleKeycardSlot (11309) in the room; it opens 2.
    ///  - 2: a personal UsableStatelessBaneConsoleKeyboard (20631) beside it; then the radio turn-in.
    ///  - "Modification" read as grade [4] modules, as The Book Of Velonar's; "Electric Resistance" as the armor
    ///    module.
    ///  - Indicators (in the instance too): 1 at the officer's pool (he and his guards), 3 at the key slot, 2 at the
    ///    keyboard.
    /// </summary>
    public static class PlateauMonkeyWrenchV1
    {
        public const uint MissionId = 1188;
        public const uint Level = 32;
        public const uint Rapp = PlateauUstorYardV1.Rapp;
        public const uint Access = 1, Keyboard = 2, EnterKey = 3;
        public const uint Results = 1, KeyEntered = 1, LineDown = 2;

        public const uint Key = 50349, KeyClass = 24888;
        public const string KeyItemKey = "ustor-facility-key";
        public const uint SlotClass = 11309;            // UsableTwoStateBaneConsoleKeycardSlot
        public const uint SlotReadyState = 55, SlotSetState = 56;
        public const uint KeyboardClass = PlateauSilentButDeadlyV1.ComputerClass;
        public const uint KeyboardState = PlateauSilentButDeadlyV1.ComputerState, UseArgument = 1;
        public const string SlotRole = "keycard-slot", KeyboardRole = "facility-keyboard";

        public const uint Experience = 50000;
        public const uint Credits = 6600;
        public const uint StealArmor = 123266;          // Weapon Module: Steal Armor [4]
        public const uint RegenBonus = 122986;          // Armor Module: Regen Bonus [4]
        public const uint DebuffElectric = 123166;      // Weapon Module: Debuff Electric Resist [4]
        public const uint ResistElectric = 123016;      // Armor Module: Resist Electric [4]

        public static readonly ScenePosition SlotAt = new(68.4f, 179, -14.8f);
        public static readonly ScenePosition KeyboardAt = new(72, 178.8f, -8);
        public const double OfficerRadius = 15, ObjectRadius = 8;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Monkey Wrench", 9132, Rapp, null, Level);
            mission.Reward(Experience, Credits, new[] { StealArmor, RegenBonus, DebuffElectric, ResistElectric });
            mission.RadioCompletion();
            mission.Prerequisite(PlateauInterceptionV1.MissionId);

            mission.Objective(Access, 9147, 9148, 0, MissionObjectiveState.Incomplete);
            mission.Objective(EnterKey, 9149, 9150, 1, MissionObjectiveState.Inactive);
            mission.Objective(Keyboard, 9151, 9152, 2, MissionObjectiveState.Inactive);
            mission.Scenario(Results, "The key entered, the supply line shut down", 2);
            var officer = PlateauUstorYardV1.OfficerAt;
            mission.Indicator(Access, 1, officer.X, officer.Y, officer.Z, OfficerRadius);
            mission.Indicator(Keyboard, 2, KeyboardAt.X, KeyboardAt.Y, KeyboardAt.Z, ObjectRadius);
            mission.Indicator(EnterKey, 3, SlotAt.X, SlotAt.Y, SlotAt.Z, ObjectRadius);

            mission.Transition(Access);
            mission.Progress(Access, MissionProgressEventKind.ItemAcquired, KeyClass, 1);
            PlateauSilentButDeadlyV1.Open(mission, Access, 1, EnterKey);

            mission.Transition(EnterKey);
            mission.Progress(EnterKey, MissionProgressEventKind.ScenarioEvent, KeyEntered, null, counter: Results);
            PlateauSilentButDeadlyV1.Open(mission, EnterKey, 1, Keyboard);

            mission.Transition(Keyboard);
            mission.Progress(Keyboard, MissionProgressEventKind.ScenarioEvent, LineDown, null, counter: Results);
            mission.Action(Keyboard, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = PlateauUstorYardV1.MapContextId,   // played in the squad's Ustor Yard, wherever it is taken
                MapContextId = PlateauUstorYardV1.MapContextId,
                ExistingFactObjectiveIds = new() { Access },
                Items = new()
                {
                    new MissionItemBinding(KeyItemKey, Key, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        MissionItemCleanupDisposition.Retain,
                        Drop: new MissionItemDropDefinition(new[] { PlateauUstorYardV1.Officer }, Access, PlateauUstorYardV1.MapContextId, 100, 1))
                }
            };
            scene.Actors[SlotRole] = new SceneActorDefinition(SlotRole, SceneActorKind.Object, SlotClass, SlotAt,
                InitialObjectState: SlotReadyState, UseAction: new SceneObjectAction(MissionId, EnterKey, KeyEntered, UseArgument));
            scene.Actors[KeyboardRole] = new SceneActorDefinition(KeyboardRole, SceneActorKind.Object, KeyboardClass, KeyboardAt,
                Orientation: 3.1416, InitialObjectState: KeyboardState, UseAction: new SceneObjectAction(MissionId, Keyboard, LineDown, UseArgument));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("create-slot", SlotRole), new EnsureActorIntent("create-keyboard", KeyboardRole) }
            };
            scene.Sequences[KeyEntered] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("key-in", SlotRole, SlotSetState),
                    new SetInteractionIntent("slot-done", SlotRole, false)
                },
                Character = { new ConsumeMissionItemIntent("key-in-slot", MissionId, KeyItemKey, 1, MissionItemScope.CharacterOwned) },
                Signals = { new SceneMissionSignal(MissionId, Results, KeyEntered) }
            };
            scene.Sequences[LineDown] = new SceneSequenceDefinition
            {
                World = { new SetInteractionIntent("keyboard-done", KeyboardRole, false) },
                Signals = { new SceneMissionSignal(MissionId, Results, LineDown) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1188",
                "Native1 officer 9147/9148, 2 keyboard 9151/9152, 3 key 9149/9150; no counters, no objective conversation. Texts " +
                "9132-9136. Facility Key class24888/template50349. Yard Regulation and Control (region4), console room (61-67,179,-12).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Monkey_Wrench",
                "Giver Field Lieutenant Rapp, reward from Headquarters (Radio); requirement Interception!; 50000XP, 6600 credits; " +
                "Modification: Steal Armor/Regen Bonus/Debuff to Electric Resist/Electric Resistance; kill the guards, use the " +
                "computer. TR MMO DB level32.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMonkeyWrenchV1",
                "Level32. Officer650063 a boss (level31, 3x HP) with three guards650062 (pool650067); the key a personal 100% drop " +
                "while 1 is open; key slot11309 (3, takes the key), keyboard20631 (2); radio turn-in (no receiver). Grade [4] " +
                "modules 123266, 122986, 123166, 123016. Places are estimates. Indicators at the officer, the slot and the keyboard.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
