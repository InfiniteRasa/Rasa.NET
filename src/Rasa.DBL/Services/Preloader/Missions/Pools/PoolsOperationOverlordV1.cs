using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Operation Overlord (native mission 898), Lamna Armory (squad instance 1465), Valverde Pools: Scout Bromfield, just
    /// inside the Armory's entrance, wants the Armory's Overlord dead; three underbosses hold the keys to his chamber.
    ///
    /// Native: texts 4743-4746, 7333 (missionconversation): log "Find and kill Overlord of the Lamna Armory and return to
    /// Commander Scout Bromfield.", offer "To get to him, you're going to have to find the Quartermaster's key, the
    /// Security Key, and the Weaponsmith's key. The underbosses should have those. When you've got them all you can open
    /// the door that leads to the Overlord's chamber. Careful, he's got a pet Kael in there.", turn-in "I'm impressed.";
    /// objectives 2 "Acquire Security Key" 6049/7334, 3 "Acquire Quartermaster Key" 6050/7335, 4 "Acquire Weaponsmith
    /// Key" 6051/7336, 5 "Kill the Overlord" 6052/7337 (missionobjective); no objective conversation. Items: 45122
    /// MisLamnaArmorySecurityKey (class 22613), 45123 Quartermaster Key (22614), 45124 Weaponsmith Key (22615). The
    /// client's map of the Armory places no key station, switch or laser door.
    ///
    /// Sourced: TaRapedia Operation Overlord: giver and receiver Scout Bromfield (Lamna Armory); no requirement; 60,000 XP,
    /// 5,250 credits; one of Eclipse Cryogenic Injector Gun lvl 32, Teleract Pulse Chaingun lvl 33, Vitalius Electric
    /// Rifle lvl 36, Animatics Rocket Launcher lvl 34; the three key bosses (PoolsLamnaArmoryNpcsV1) "respawn rather
    /// rapidly"; "The key cards will have to be inserted into 3 stations on the south side of the Testing Arena. Once all
    /// are placed, the fourth station with them can be used to turn off the laser door to the next chambers." Ten Ton
    /// Hammer Lamna Armory guide: Gaite gives the Security Chief's Key, Kewl the Weaponsmith's, Fearce the
    /// Quartermaster's; "use the three consoles (184, -85) to insert the three keys. Then turn around to activate the
    /// switch"; the Overlord Commander Vanduahl with a Kael pet; after him the Distance logos. TaRapedia Distance (logos):
    /// "need the keys from the three sub bosses, insert in the slots on the left of the door and press the button to
    /// unlock". TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: Gaite, Kwel, Fearce with keys, 3 key consoles and a
    /// switch-door, Vanduahl with a Kael pet):
    ///  - 2-4: each key a personal 100% drop of its boss while its objective is open, picked up from the corpse (as Monkey
    ///    Wrench's key); a key already held counts at the accept.
    ///  - Three personal keycard slots (class 11309 UsableTwoStateBaneConsoleKeycardSlot) on the floor of the Testing
    ///    Arena by Ten Ton Hammer's point, and a personal Bane keyboard (20631) as the switch, 18 m north of them. Server-
    ///    only hidden objectives: 8 an aggregate of 2-4; 9 on 8 opens 6 (the slots: each takes its key); 6 on its three
    ///    keys opens 7 (the switch).
    ///  - The switch stands in for the laser door (the server has no door object there and does not block a player's
    ///    walk): it sets Vanduahl and his Kael down in the hall before the Distance logos room and opens 5. Both are
    ///    personal, hostile, no loot; Vanduahl's death completes 5 and pays. The Kael need not die.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels ("Chaingun" the Machine Gun, "Pulse"
    ///    EMP as Death Patrol's Pulse Staff, "Cryogenic Injector" the Ice Injection Gun), one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsOperationOverlordV1
    {
        public const uint MissionId = 898;
        public const uint Level = 35;
        public const uint MapContextId = PoolsLamnaArmoryNpcsV1.MapContextId;
        public const uint Bromfield = PoolsLamnaArmoryNpcsV1.Bromfield;
        public const uint KillOverlord = 5, KeysIn = 6, Switch = 7, AllKeys = 8, Gate = 9;
        public const uint KeysScenario = 1, SwitchScenario = 2, OverlordScenario = 3;
        public const uint SwitchSequence = 4, OverlordDown = 5;

        public const uint SlotClass = Plateau.PlateauMonkeyWrenchV1.SlotClass;
        public const uint SlotReadyState = Plateau.PlateauMonkeyWrenchV1.SlotReadyState, SlotSetState = Plateau.PlateauMonkeyWrenchV1.SlotSetState;
        public const uint SwitchClass = Plateau.PlateauMonkeyWrenchV1.KeyboardClass, SwitchState = Plateau.PlateauMonkeyWrenchV1.KeyboardState;
        public const uint UseArgument = Plateau.PlateauMonkeyWrenchV1.UseArgument;
        public const string SwitchRole = "laser-door-switch", OverlordRole = "overlord", KaelRole = "overlord-kael";

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint CryogenicInjectorGun = 48104; // Weapon_Avatar_InjectionGun_Ice_UNC_32_to_36
        public const uint PulseChaingun = 15150;        // Weapon_Avatar_MachineGun_EMP_UNC_33_to_37
        public const uint ElectricRifle = 14738;        // Weapon_Avatar_Rifle_Electric_UNC_36_to_40
        public const uint RocketLauncher = 49136;       // Weapon_Avatar_RocketLauncher_Physical_UNC_34_to_38

        /// <summary>The keys: objective, texts, item key, template, class, the boss who drops it, its slot on the arena floor.</summary>
        public static readonly (uint Objective, uint Name, uint Body, string Key, uint Template, uint Class, uint Boss, ScenePosition Slot)[] Keys =
        {
            (2, 6049, 7334, "lamna-security-key", 45122, 22613, PoolsLamnaArmoryNpcsV1.Gaite, new(176.9f, -23.99f, -90f)),
            (3, 6050, 7335, "lamna-quartermaster-key", 45123, 22614, PoolsLamnaArmoryNpcsV1.Fearce, new(184f, -24.06f, -96f)),
            (4, 6051, 7336, "lamna-weaponsmith-key", 45124, 22615, PoolsLamnaArmoryNpcsV1.Kwel, new(190.6f, -23.99f, -89.9f)),
        };

        public static readonly ScenePosition SwitchAt = new(184f, -13.9f, -72f);
        public static readonly ScenePosition OverlordAt = new(72f, 8.01f, -16f);
        public static readonly ScenePosition KaelAt = new(64f, 8.06f, -16f);
        public const double BossRadius = 15;

        /// <summary>Each key's boss (PoolsLamnaArmoryNpcsV1), the markers of 2-4.</summary>
        public static readonly (double X, double Y, double Z)[] BossesAt =
            { PoolsLamnaArmoryNpcsV1.GaiteAt, PoolsLamnaArmoryNpcsV1.FearceAt, PoolsLamnaArmoryNpcsV1.KwelAt };

        public static string SlotRole(int index) => $"key-slot-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Operation Overlord", 4743, Bromfield, Bromfield, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicInjectorGun, PulseChaingun, ElectricRifle, RocketLauncher });

            var ordinal = 0U;
            foreach (var key in Keys)
                mission.Objective(key.Objective, key.Name, key.Body, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(KillOverlord, 6052, 7337, ordinal++, MissionObjectiveState.Inactive);
            mission.Objective(KeysIn, 6052, 7337, ordinal++, MissionObjectiveState.Inactive);
            mission.Objective(Switch, 6052, 7337, ordinal++, MissionObjectiveState.Inactive);
            mission.Objective(AllKeys, 6052, 7337, ordinal++, MissionObjectiveState.Incomplete);
            mission.Objective(Gate, 6052, 7337, ordinal, MissionObjectiveState.Incomplete);
            mission.Scenario(KeysScenario, "The three keys in their slots", (uint)Keys.Length);
            mission.Scenario(SwitchScenario, "The laser door's switch", 1);
            mission.Scenario(OverlordScenario, "The Overlord killed", 1);

            // 2-4: each key picked up from its boss.
            foreach (var key in Keys)
            {
                mission.Transition(key.Objective);
                mission.Progress(key.Objective, MissionProgressEventKind.ItemAcquired, key.Class, 1);
            }

            // 9: once 8 has the three keys, the slots take them.
            mission.Transition(Gate);
            mission.ObjectiveState(Gate, AllKeys, MissionObjectiveState.Completed);
            Open(mission, Gate, 1, KeysIn);

            // 6: the three keys in, the switch.
            var keys = (uint)Keys.Length;
            mission.Transition(KeysIn);
            for (var id = 1U; id <= keys; id++)
                mission.Progress(KeysIn, MissionProgressEventKind.ScenarioEvent, id, keys, counter: KeysScenario, trigger: id);
            Open(mission, KeysIn, 1, Switch);

            // 7: the switch, the Overlord.
            mission.Transition(Switch);
            mission.Progress(Switch, MissionProgressEventKind.ScenarioEvent, 1, null, counter: SwitchScenario);
            Open(mission, Switch, 1, KillOverlord);

            // 5: the Overlord's death.
            mission.Transition(KillOverlord);
            mission.Progress(KillOverlord, MissionProgressEventKind.ScenarioEvent, 1, null, counter: OverlordScenario);
            mission.Action(KillOverlord, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,   // taken and played in the Armory
                ExistingFactObjectiveIds = new() { Keys[0].Objective, Keys[1].Objective, Keys[2].Objective },
                HiddenObjectiveIds = new() { KeysIn, Switch, AllKeys, Gate },
                ObjectiveAggregations = new()
                {
                    [AllKeys] = new MissionObjectiveAggregation(new[] { Keys[0].Objective, Keys[1].Objective, Keys[2].Objective }, keys)
                },
                DefeatSequences = new() { [OverlordRole] = OverlordDown },
                Items = new()
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Keys.Length; index++)
            {
                var key = Keys[index];
                var role = SlotRole(index);
                var sequence = (uint)index + 1;
                scene.Items.Add(new MissionItemBinding(key.Key, key.Template, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    Drop: new MissionItemDropDefinition(new[] { key.Boss }, key.Objective, MapContextId, 100, 1)));
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SlotClass, key.Slot,
                    InitialObjectState: SlotReadyState, UseAction: new SceneObjectAction(MissionId, KeysIn, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"key-in-{role}", role, SlotSetState),
                        new SetInteractionIntent($"done-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{key.Key}-in-{role}", MissionId, key.Key, 1, MissionItemScope.CharacterOwned) },
                    Signals = { new SceneMissionSignal(MissionId, KeysScenario, sequence) }
                };
            }

            scene.Actors[SwitchRole] = new SceneActorDefinition(SwitchRole, SceneActorKind.Object, SwitchClass, SwitchAt,
                Orientation: 3.1416, InitialObjectState: SwitchState, UseAction: new SceneObjectAction(MissionId, Switch, SwitchSequence, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-switch", SwitchRole));
            scene.Actors[OverlordRole] = new SceneActorDefinition(OverlordRole, SceneActorKind.Creature, PoolsLamnaArmoryNpcsV1.Vanduahl,
                OverlordAt, Orientation: 1.5708, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
            scene.Actors[KaelRole] = new SceneActorDefinition(KaelRole, SceneActorKind.Creature, PoolsLamnaArmoryNpcsV1.Kael,
                KaelAt, Orientation: 1.5708, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
            scene.Sequences[SwitchSequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new SetInteractionIntent("switch-done", SwitchRole, false),
                    new EnsureActorIntent("create-overlord", OverlordRole),
                    new EnsureActorIntent("create-kael", KaelRole)
                },
                Signals = { new SceneMissionSignal(MissionId, SwitchScenario, 1) }
            };
            scene.Sequences[OverlordDown] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, OverlordScenario, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/898",
                "Native2 'Acquire Security Key' 6049/7334, 3 Quartermaster 6050/7335, 4 Weaponsmith 6051/7336, 5 'Kill the " +
                "Overlord' 6052/7337; no objectiveconversation. Texts 4743-4746, 7333. Keys 45122/22613, 45123/22614, " +
                "45124/22615. Names Gaite 8888, Kwel 8885, Fearce 8886, Commander Vanduahl 8571. No station on the Armory's map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Operation_Overlord",
                "Giver/receiver Scout Bromfield (Lamna Armory); no requirement; 60000XP, 5250 credits; one of Eclipse Cryogenic " +
                "Injector Gun32, Teleract Pulse Chaingun33, Vitalius Electric Rifle36, Animatics Rocket Launcher34; keys from a " +
                "grenadier, an infantry Thrax and a Lightbender; 3 stations south of the Testing Arena and a fourth that turns off " +
                "the laser door. Ten Ton Hammer: consoles (184,-85), the switch behind, Vanduahl with a Kael. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsOperationOverlordV1",
                "Keys personal 100% drops of the public bosses 760216-760218 (user); three keycard slots (11309) take them, a " +
                "keyboard (20631) as the switch sets personal Vanduahl 760219 and Kael 760220 down (no door: the server does not " +
                "block); hidden 6-9 chain them. Items 48104, 15150, 14738, 49136, one to choose. Places are estimates. Indicators in the instance too.", 0.3);

            for (var index = 0; index < Keys.Length; index++)
                mission.Indicator(Keys[index].Objective, 1 + (uint)index, BossesAt[index].X, BossesAt[index].Y, BossesAt[index].Z, BossRadius);
            mission.Indicator(KillOverlord, 1 + (uint)Keys.Length, OverlordAt.X, OverlordAt.Y, OverlordAt.Z, BossRadius);

            mission.Enable(scene);
        }

        /// <summary>Reveals and opens <paramref name="target"/> when <paramref name="objective"/> completes.</summary>
        private static void Open(PoolsMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
