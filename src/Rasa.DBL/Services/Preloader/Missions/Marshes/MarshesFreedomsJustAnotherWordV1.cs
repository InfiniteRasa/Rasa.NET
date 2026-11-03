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
    /// Freedom's Just Another Word (native mission 548), the Logos Research Facility (squad instance 1700): Specialist Hax
    /// sends the player for the security level 3 keypass and into the level 2 cell block to free the AFS prisoners, Captain
    /// Sakai among them.
    ///
    /// Native: texts 1482-1485 and 7298 (missionconversation): log "Obtain a security clearance pass which will open the
    /// Logos Research level 2 cell block.", offer "a high ranking AFS officer ... rescue them from the level 2 cell block",
    /// turn-in (Sakai) "It's good to see a friendly face, soldier"; objectives (missionobjective, no counters) 1 "Locate the
    /// Security Level 3 Keypass" 3422/7299, 2 "Release the Prisoners from Cell block A." 3431/7300, 4 "... Cell block B."
    /// 4710/7301, 5 "... Cell block C." 4711/7302; no objective conversation. Indicator names 345-347 "Cellblock A/B/C"
    /// (missionobjectiveindicatorlanguage). Item Logos Research Facility Keycard Level 3 (class 21076, template 42430). The
    /// LRF map: the prison chunnel (ArchBaneIndustrialChunnelStraight02Prison32mV02 728, 288, -216; the prison tee 728, 288,
    /// -264) with its dead (PropForean/PropHum corpses 709-744, -220 to -284), a console pair at (746.4, 289.6, -263.4),
    /// Specimen Control above it (map_region 20057, the suspended command chair 728.1, 297.2, -181.9).
    ///
    /// Sourced: TaRapedia Freedom's Just Another Word: giver Specialist Hax, receiver Captain Sakai (both Logos Research
    /// Facility); requires A Taste of Their Own Medicine; 75,000 XP, 5,700 credits; one of Vextronics Virulent Disperser,
    /// Vextronics Pulse Staff, Teleract Cryogenic Net Gun, ChiTech Leech Gun; follow-up Leggo My Logos. TR MMO DB: level 35;
    /// tip: the key drops from a boss in the control room, the cells open from terminals. The client's item block
    /// 96268-96271 (Virulent Propellant Gun 37-41, EMP Staff 35-39, Ice Net Gun 36-40, Physical Leech Gun 34-38: TaRapedia's
    /// four in order, the client's only such block).
    ///
    /// Reconstructed (ESTIMATE, the triage's choices of 2026-11-02):
    ///  - The scene (bound to the squad's facility) puts in the run's own Thrax Commander with two Thrax guards in Specimen
    ///    Control, two Prisoners in each block of the cell chunnel (A at its north end, B in the middle, C at the tee) and
    ///    Sakai (the player's own, OwnerOnly, invulnerable; the public one by the hospital is hidden from the player while
    ///    he stands) in block C.
    ///  - 1: the Commander's kill gives the keypass (issued at his death, not a corpse drop) and completes 1, which opens 2, 4
    ///    and 5 together and sets up a cell terminal (UsableStatelessBaneConsoleKeyboard) at each block.
    ///  - 2, 4, 5: each block's terminal used, its prisoners go free (they leave); in C Sakai stays, the receiver. The turn-in
    ///    at him ends the scene; the public Sakai is back by the hospital and gives Leggo My Logos.
    ///  - Markers: 1 at the Commander, 2/4/5 the client's "Cellblock A/B/C" at the blocks.
    /// </summary>
    public static class MarshesFreedomsJustAnotherWordV1
    {
        public const uint MissionId = 548;
        public const uint Level = 35;
        public const uint Hax = MarshesLogosResearchNpcsV1.Hax, Sakai = MarshesLogosResearchNpcsV1.Sakai;
        public const uint Facility = MarshesLogosResearchNpcsV1.MapContextId;
        public const uint Keypass = 1, BlockA = 2, BlockB = 4, BlockC = 5;

        /// <summary>Scenarios: the terminals come (its sequence); the results (1 the keypass, 2-4 the blocks A-C).</summary>
        public const uint TerminalsCome = 2, Results = 3;
        public const uint CommanderDown = 11;
        public const uint CellblockA = 345, CellblockB = 346, CellblockC = 347;   // missionobjectiveindicatorlanguage

        public const uint KeycardTemplate = 42430, KeycardClass = 21076;
        public const string KeycardKey = "lrf-keycard-level-3";
        public const uint TerminalClass = PlateauSilentButDeadlyV1.ComputerClass, TerminalState = PlateauSilentButDeadlyV1.ComputerState;
        public const uint UseArgument = 1;
        public const string CommanderRole = "thrax-commander", SakaiRole = "sakai";

        public static readonly ScenePosition CommanderAt = new(728.5f, 297.77f, -176f);
        public static readonly ScenePosition[] GuardsAt = { new(726f, 297.77f, -172f), new(731f, 297.77f, -172f) };
        public const uint Guard = 531045;

        /// <summary>The blocks: objective, client marker name, marker, terminal, prisoners.</summary>
        public static readonly (uint Objective, uint Name, ScenePosition Marker, ScenePosition Terminal, ScenePosition[] Prisoners)[] Blocks =
        {
            (BlockA, CellblockA, new(728f, 289.9f, -220f), new(728f, 289.9f, -222f),
                new ScenePosition[] { new(720f, 289.07f, -220f), new(736f, 289.04f, -220f) }),
            (BlockB, CellblockB, new(728f, 289.92f, -240f), new(728f, 289.92f, -246f),
                new ScenePosition[] { new(716f, 289.37f, -240f), new(740f, 289.37f, -240f) }),
            (BlockC, CellblockC, new(728f, 289.82f, -268f), new(744f, 289.77f, -262f),
                new ScenePosition[] { new(712f, 289.17f, -275f), new(744f, 289.17f, -275f) }),
        };
        public static readonly ScenePosition SakaiAt = new(716f, 290f, -264f);
        public const double BlockRadius = 12;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public static readonly uint[] Rewards = { 96268, 96269, 96270, 96271 };

        public static string GuardRole(int index) => $"commander-guard-{index + 1}";
        public static string TerminalRole(int block) => $"cell-terminal-{(char)('a' + block)}";
        public static string PrisonerRole(int block, int index) => $"prisoner-{(char)('a' + block)}-{index + 1}";
        public static uint TerminalSequence(int block) => 21 + (uint)block;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Freedom's Just Another Word", 1482, Hax, Sakai, Level);
            mission.Reward(Experience, Credits, Rewards);
            mission.Prerequisite(MarshesTasteOfTheirOwnMedicineV1.MissionId);
            mission.Scenario(TerminalsCome, "The keypass held: the cell terminals", 0);
            mission.Scenario(Results, "The keypass taken, cell blocks A, B and C released", 4);

            mission.Objective(Keypass, 3422, 7299, 0, MissionObjectiveState.Incomplete);
            mission.Objective(BlockA, 3431, 7300, 1, MissionObjectiveState.Inactive);
            mission.Objective(BlockB, 4710, 7301, 2, MissionObjectiveState.Inactive);
            mission.Objective(BlockC, 4711, 7302, 3, MissionObjectiveState.Inactive);

            mission.Transition(Keypass);
            mission.Progress(Keypass, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Results);
            var action = 1U;
            foreach (var block in Blocks)
            {
                MarshesTasteOfTheirOwnMedicineV1.Open(mission, Keypass, action, block.Objective);
                action += 2;
            }
            mission.Action(Keypass, action, MissionActionKind.StartScenario, scenario: TerminalsCome);
            mission.Action(Keypass, action + 1, MissionActionKind.GrantReward, reward: 1);   // the turn-in's reward (Sakai)
            for (var index = 0; index < Blocks.Length; index++)
            {
                mission.Transition(Blocks[index].Objective);
                mission.Progress(Blocks[index].Objective, MissionProgressEventKind.ScenarioEvent, 2 + (uint)index, null, counter: Results);
            }

            mission.Indicator(Keypass, 1, CommanderAt.X, CommanderAt.Y, CommanderAt.Z, 15, Facility);
            foreach (var block in Blocks)
                mission.NamedIndicator(block.Objective, block.Name, block.Marker.X, block.Marker.Y, block.Marker.Z, BlockRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/548",
                "Native1 'Locate the Security Level 3 Keypass' 3422/7299, 2 'Release the Prisoners from Cell block A.' 3431/7300, 4 " +
                "'... B.' 4710/7301, 5 '... C.' 4711/7302; no counters, no objectiveconversation. Texts 1482-1485, 7298. Indicator " +
                "names 345-347 'Cellblock A/B/C'. Keycard Level 3 class21076/template42430. Names 8563 Sakai, 10117 Prisoner, 10629 " +
                "Thrax Commander. Item block 96268-96271. LRF prison chunnel (728,288,-216..-264), Specimen Control (728.5,297.5,-176).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Freedom%27s_Just_Another_Word",
                "Giver Specialist Hax, receiver Captain Sakai (LRF); requires 546; 75000XP, 5700 credits; Vextronics Virulent " +
                "Disperser/Vextronics Pulse Staff/Teleract Cryogenic Net Gun/ChiTech Leech Gun. TR MMO DB level35; the key drops " +
                "from a boss in the control room, the cells open from terminals.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesFreedomsJustAnotherWordV1",
                "Bound to the squad's LRF. The run's own Thrax Commander 760365 (L38) and two Thrax 531045 in Specimen Control; his " +
                "kill issues the keypass and completes 1, opening 2/4/5 and three terminals 20631. Two Prisoners 760369 a block; a " +
                "terminal frees its block's. Sakai 760363 (OwnerOnly, invulnerable, the public one hidden) in block C, the " +
                "receiver. Places and counts are estimates.", 0.3);

            mission.Enable(Scene());
        }

        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                DefeatSequences = new() { [CommanderRole] = CommanderDown },
                Items = new()
                {
                    new MissionItemBinding(KeycardKey, KeycardTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            var start = new SceneSequenceDefinition();
            scene.Actors[CommanderRole] = new SceneActorDefinition(CommanderRole, SceneActorKind.Creature, MarshesLogosResearchNpcsV1.Commander,
                CommanderAt, Orientation: 3.1416, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
            start.World.Add(new EnsureActorIntent("commander-stands", CommanderRole));
            for (var index = 0; index < GuardsAt.Length; index++)
            {
                var role = GuardRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, Guard, GuardsAt[index], Orientation: 3.1416,
                    GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                start.World.Add(new EnsureActorIntent($"{role}-stands", role));
            }
            scene.Actors[SakaiRole] = new SceneActorDefinition(SakaiRole, SceneActorKind.Creature, Sakai, SakaiAt, Orientation: -1.5708,
                OwnerOnly: true, ReplacesPublicSpawnId: MarshesLogosResearchNpcsV1.Sakai, ReplaceWhilePresent: true,
                GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true });
            start.World.Add(new EnsureActorIntent("sakai-held", SakaiRole));

            var terminals = new SceneSequenceDefinition();
            for (var block = 0; block < Blocks.Length; block++)
            {
                var terminal = TerminalRole(block);
                scene.Actors[terminal] = new SceneActorDefinition(terminal, SceneActorKind.Object, TerminalClass, Blocks[block].Terminal,
                    InitialObjectState: TerminalState,
                    UseAction: new SceneObjectAction(MissionId, Blocks[block].Objective, TerminalSequence(block), UseArgument));
                terminals.World.Add(new EnsureActorIntent($"create-{terminal}", terminal));
                var released = new SceneSequenceDefinition
                {
                    World = { new SetInteractionIntent($"{terminal}-done", terminal, false) },
                    Signals = { new SceneMissionSignal(MissionId, Results, 2 + (uint)block) }
                };
                for (var index = 0; index < Blocks[block].Prisoners.Length; index++)
                {
                    var prisoner = PrisonerRole(block, index);
                    scene.Actors[prisoner] = new SceneActorDefinition(prisoner, SceneActorKind.Creature, MarshesLogosResearchNpcsV1.Prisoner,
                        Blocks[block].Prisoners[index], Orientation: index == 0 ? -1.5708 : 1.5708, InitiallyInteractable: false,
                        GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true });
                    start.World.Add(new EnsureActorIntent($"{prisoner}-held", prisoner));
                    released.World.Add(new RemoveActorIntent($"{prisoner}-free", prisoner));
                }
                scene.Sequences[TerminalSequence(block)] = released;
            }
            scene.Sequences[0] = start;
            scene.Sequences[TerminalsCome] = terminals;
            scene.Sequences[CommanderDown] = new SceneSequenceDefinition
            {
                Character = { new IssueMissionItemIntent("keypass-taken", MissionId, KeycardKey, KeycardTemplate, 1) },
                Signals = { new SceneMissionSignal(MissionId, Results, 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
