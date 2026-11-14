using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Into the Lion's Den (native mission 1499), the Bane Conscription Facility (squad instance 1988): the Enslaver is on the
    /// move against the prisoners of the east wing's Atta Observatory; the player races there and saves either Lt. Humphries in
    /// the Atta pit or six Brann prisoners on the ledge above it ("I doubt you'll have time to save both").
    ///
    /// Native: texts 13389-13393 (missionconversation; 13393 "none"); objectives (missionobjective) 1 "Get To Arena Before the
    /// Enslaver Kills the Prisoners" 14300/14301, 2 "Hidden - Master Objective" 14302/14303, 3 "Help Lt. Humpries in the Atta
    /// Pit" 14304/14305, 4 "Help 6 Brann Prisoners" 14309/14310, 5 "Speak with Lt. Humphries" 14306/14307, 6 "Speak with Lukaz"
    /// 14311/14312; objective conversations (1499,5) package 1936 Humphries variant 1, 14308 ("I nicked a key from the guard
    /// ... the room connected to the ramp above the pit"), (1499,6) package 1934 Lukaz variant 1, 14313 ("I have 3 men that can
    /// help you"). Names 9746 Lt. Humphries, 9755 Lukaz, 9483 Brann Prisoner. Use name 140 "Cell Door Control"
    /// (usablenameoverridelanguage).
    ///
    /// Sourced: TaRapedia Into the Lion's Den (0.5): requires Blast the Biolab, follow-up Save the Queen; Headquarters rewards;
    /// 100,000 XP, 8,600 credits; Vextronics Sonic Disperser, Virulent Injector Gun, Vitalius Exothermic Polarity Gun, Vextronics
    /// Cryogenic Net Gun; the officer's side "three Atta bosses and a swarm of normal Atta", the Brann side "about four normal
    /// Atta and two Nitroglazers", "walk north along the ledge, kill the Nitroglazers and Atta" (the walkthrough, cache).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43; offered by radio on the 1498 reward and on arrival in the facility, turned in by radio (as 1498's);
    ///    requires 1498; bound to the squad's copy.
    ///  - 1 the Atta Observatory (the arena, triage M7) within ten minutes of the accept (no source gives the time), else it
    ///    fails, and the mission with it (taken again from the radio on the next arrival). Its completion opens 3 and 4 and
    ///    brings both fights: the pit's three Atta Imperial Guards (520002, the bosses) and four Atta Soldiers (531015) round
    ///    Humphries, the ledge's two Nitroglazers (790240) and four Atta Soldiers before the Brann cells. Each member his own
    ///    (the scene's, hostile, as Petrovich's Run's waves).
    ///  - Hidden 9 (ours, Inactive until 1) the pit's seven dead (a tally of the scene's defeats; a counter rule is refused on 3,
    ///    which has no client counter text), and 3 follows it. Hidden 7 (ours, Inactive until 1) the ledge's six dead: the
    ///    member's own Cell Door Control (UsableTwoStateBaneConsoleSwitchV01 7468, dark 55, on 56; argument 1) usable; 4 its
    ///    use (the cells open). 3 and 4 shut each other out: the first done fails the other (a Failed transition on the other's
    ///    state), and the losing side's NPCs and switch go (the Enslaver's work); every fighter still standing, either side,
    ///    falls (killed by nobody: no XP, loot or credit; no body is removed, so the loot of every kill stays). 7 also fails when 3 or 4 is done
    ///    first (a squad's shared credit), so a late tally lights no switch (its step IfPresent too).
    ///  - 2 (the client's hidden master) an aggregate of 3 and 4, one of two. 5 Humphries' talk opens after 3, 6 Lukaz' after
    ///    4; hidden 8 (ours) an aggregate of 5 and 6, one of two: either talk gives the reward.
    ///  - Humphries in the pit, Lukaz and six Brann Prisoners on the ledge before the cells (AshenConscriptionQueenNpcsV1),
    ///    each the member's own (OwnerOnly), invulnerable, from the arrival.
    ///  - Not built: the key room Humphries names (14308), Lukaz' three men (14313) (deviations, docs/ashen-playtest.md).
    ///  - Markers (unnamed): 1 the arena, on the ledge (the area's middle), r30; 3 and 5 Humphries; 4 and 6 Lukaz. 2, 7, 8, 9 hidden, none.
    ///  - The items by TaRapedia's names, the lowest Uncommon template of each in the band (client entityclass names):
    ///    PropellantGun_Sonic 12071 (42-46, as 1447's), InjectionGun_Virulent 12155 (42-46), PolarityGun_Fire 12135 (43-47),
    ///    NetGun_Ice 12129 (41-45); one to choose.
    /// </summary>
    public static class AshenIntoTheLionsDenV1
    {
        public const uint MissionId = 1499;
        public const uint Level = 43;
        public const uint Facility = AshenConscriptionQueenNpcsV1.Facility;
        public const uint Humphries = AshenConscriptionQueenNpcsV1.Humphries, HumphriesPackage = AshenConscriptionQueenNpcsV1.HumphriesPackage;
        public const uint Lukaz = AshenConscriptionQueenNpcsV1.Lukaz, LukazPackage = AshenConscriptionQueenNpcsV1.LukazPackage;
        public const uint Prisoner = AshenConscriptionQueenNpcsV1.Prisoner;
        public const uint Variant = 1;

        public const uint ToTheArena = 1, Master = 2, HelpHumphries = 3, HelpThePrisoners = 4, SpeakHumphries = 5, SpeakLukaz = 6,
            LedgeCleared = 7, Spoken = 8, PitCleared = 9;    // 7-9: server only, hidden
        public const uint ArenaSeconds = 600;           // ESTIMATE (M7)
        public const uint ArenaArea = 1;

        public const uint ImperialGuard = AshenConscriptionQueenNpcsV1.ImperialGuard, AttaSoldier = AshenConscriptionQueenNpcsV1.AttaSoldier,
            Nitroglazer = 790240;

        public const uint SwitchClass = 7468;           // UsableTwoStateBaneConsoleSwitchV01
        public const uint SwitchOff = 55, SwitchOn = 56;
        public const uint SwitchName = 140;             // usablenameoverridelanguage "Cell Door Control"
        public const uint UseArgument = 1;              // usabledata lookup[7468]: 1
        public const string HumphriesRole = "humphries", LukazRole = "lukaz", SwitchRole = "bcf-cell-door-control";
        public static string PrisonerRole(int index) => $"brann-prisoner-{index}";

        /// <summary>Scenarios: the fights (1 done), the pit's and the ledge's dead counted, the switch used (4's event), the switch
        /// usable (7 done), the pit side won (3 done), the cells open (4 done).</summary>
        public const uint FightsBegin = 1, PitCounted = 2, LedgeCounted = 3, SwitchUsed = 4, SwitchReady = 5, PitSaved = 6, CellsOpen = 7;
        public const uint SwitchSequence = 11, PitDownFrom = 100, LedgeDownFrom = 200;

        /// <summary>The arena: the pit and the ledge over it (the Atta Observatory), a cylinder.</summary>
        // On the ledge (the way in from the east wing's pad 926 and the only way down into the pit, by the ramp from it): a
        // move ends inside it on the real way (MissionAreaService takes an entry only from a move's end point). 30 m round the
        // ledge's middle, y 72-104: the ledge (96.02), the corridor to the cells (97.61) and the pit floor under it (80.97, the
        // probe's grid, 2026-11-12); pad 926 74 m off, Lukaz 46 m off (outside). ESTIMATE of TaRapedia's "arena".
        public static readonly (double X, double Y, double Z) ArenaAt = (240, 88, -100);
        public const double ArenaRadius = 30, ArenaHalfHeight = 16;
        public static readonly ScenePosition ArenaMarkerAt = new(240f, 96.02f, -100f);
        public const double NpcMarkerRadius = 8, FightMarkerRadius = 15;     // ESTIMATE

        public static readonly ScenePosition HumphriesAt = new(238f, 80.97f, -150f);
        public static readonly ScenePosition LukazAt = new(240f, 97.61f, -54f);
        public static readonly ScenePosition SwitchAt = new(243f, 97.61f, -60f);
        public static readonly ScenePosition[] PrisonersAt =
        {
            new(241f, 97.61f, -49f), new(243f, 97.61f, -52f), new(243f, 97.61f, -56f),
            new(241f, 97.61f, -58f), new(237f, 97.61f, -55f), new(236.5f, 97.61f, -48f)
        };

        /// <summary>The pit's fighters round Humphries: the three bosses, then four Atta Soldiers.</summary>
        public static readonly (string Role, uint Creature, ScenePosition At)[] PitFighters =
        {
            ("pit-guard-1", ImperialGuard, new(232f, 80.97f, -142f)), ("pit-guard-2", ImperialGuard, new(244f, 80.97f, -142f)),
            ("pit-guard-3", ImperialGuard, new(238f, 80.97f, -160f)),
            ("pit-atta-1", AttaSoldier, new(231f, 80.97f, -153f)), ("pit-atta-2", AttaSoldier, new(245f, 80.97f, -153f)),
            ("pit-atta-3", AttaSoldier, new(234f, 80.97f, -136f)), ("pit-atta-4", AttaSoldier, new(242f, 80.97f, -136f))
        };

        /// <summary>The ledge's fighters before the cells: two Nitroglazers, four Atta Soldiers.</summary>
        public static readonly (string Role, uint Creature, ScenePosition At)[] LedgeFighters =
        {
            ("ledge-nitroglazer-1", Nitroglazer, new(240f, 96.02f, -95f)), ("ledge-nitroglazer-2", Nitroglazer, new(239f, 97.61f, -78f)),
            ("ledge-atta-1", AttaSoldier, new(235f, 96.02f, -97f)), ("ledge-atta-2", AttaSoldier, new(245f, 96.02f, -97f)),
            ("ledge-atta-3", AttaSoldier, new(241f, 97.61f, -83f)), ("ledge-atta-4", AttaSoldier, new(237f, 97.61f, -72f))
        };

        public const uint Experience = 100000;
        public const uint Credits = 8600;
        public const uint SonicDisperser = 12071;       // Weapon_Avatar_PropellantGun_Sonic_UNC_42_to_46
        public const uint VirulentInjector = 12155;     // Weapon_Avatar_InjectionGun_Virulent_UNC_42_to_46
        public const uint PolarityGun = 12135;          // Weapon_Avatar_PolarityGun_Fire_UNC_43_to_47
        public const uint CryogenicNetGun = 12129;      // Weapon_Avatar_NetGun_Ice_UNC_41_to_45

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Into the Lion's Den", 13389, null, null, Level);
            mission.RadioOfferAndCompletion(MissionOfferSourceDefinition.MissionRewarded(AshenBlastTheBiolabV1.MissionId),
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, Facility));
            mission.Reward(Experience, Credits, new[] { SonicDisperser, VirulentInjector, PolarityGun, CryogenicNetGun });
            mission.Prerequisite(AshenBlastTheBiolabV1.MissionId);
            mission.Scenario(FightsBegin, "At the arena: the pit's and the ledge's fights (1 done)", 0);
            mission.Scenario(PitCounted, "The pit's fighters killed", (uint)PitFighters.Length);
            mission.Scenario(LedgeCounted, "The ledge's fighters killed", (uint)LedgeFighters.Length);
            mission.Scenario(SwitchUsed, "The Cell Door Control used", 1);
            mission.Scenario(SwitchReady, "The Cell Door Control usable (7 done)", 0);
            // The names fit MySQL's varchar(64) (fix W renames the rows of a database that ran this with the longer ones).
            mission.Scenario(PitSaved, "Humphries saved: ledge NPCs, switch go, the rest fall (3 done)", 0);
            mission.Scenario(CellsOpen, "The cells open: Humphries goes, the rest fall (4 done)", 0);

            mission.Objective(ToTheArena, 14300, 14301, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Master, 14302, 14303, 1, MissionObjectiveState.Incomplete);                               // hidden
            mission.Objective(HelpHumphries, 14304, 14305, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(HelpThePrisoners, 14309, 14310, 3, MissionObjectiveState.Inactive, required: false);
            mission.Objective(SpeakHumphries, 14306, 14307, 4, MissionObjectiveState.Inactive, required: false);
            mission.Objective(SpeakLukaz, 14311, 14312, 5, MissionObjectiveState.Inactive, required: false);
            mission.Objective(LedgeCleared, 14309, 14310, 6, MissionObjectiveState.Inactive, required: false);           // hidden
            mission.Objective(Spoken, 14302, 14303, 7, MissionObjectiveState.Incomplete);                               // hidden
            mission.Objective(PitCleared, 14304, 14305, 8, MissionObjectiveState.Inactive, required: false);            // hidden

            // 1: the arena in time: 3, 4 and the hidden 7 open, the fights come; past the deadline 1 fails, and the mission.
            mission.Area(ArenaArea, Facility, ArenaAt.X, ArenaAt.Y, ArenaAt.Z, ArenaRadius, ArenaHalfHeight);
            mission.Transition(ToTheArena);
            mission.EnterArea(ToTheArena, ArenaArea);
            AshenRaidTheConscriptionFacilityV1.Open(mission, ToTheArena, 1, HelpHumphries);
            AshenRaidTheConscriptionFacilityV1.Open(mission, ToTheArena, 3, HelpThePrisoners);
            AshenRaidTheConscriptionFacilityV1.Open(mission, ToTheArena, 5, LedgeCleared);
            AshenRaidTheConscriptionFacilityV1.Open(mission, ToTheArena, 7, PitCleared);
            mission.Action(ToTheArena, 9, MissionActionKind.StartScenario, scenario: FightsBegin);
            mission.FailOnDeadline(ToTheArena, ArenaSeconds, transition: 2);

            // 9, hidden: the pit's seven dead (a counter rule only on an objective the client shows no counter of: 3 has none).
            mission.Transition(PitCleared);
            for (var fighter = 1U; fighter <= PitFighters.Length; fighter++)
                mission.Progress(PitCleared, MissionProgressEventKind.ScenarioEvent, fighter, (uint)PitFighters.Length, counter: PitCounted, trigger: fighter);

            // 3: 9 done: 5 opens, the ledge's NPCs and switch go, the fighters still standing fall. 4 done first fails it.
            mission.Transition(HelpHumphries);
            mission.ObjectiveState(HelpHumphries, PitCleared, MissionObjectiveState.Completed);
            AshenRaidTheConscriptionFacilityV1.Open(mission, HelpHumphries, 1, SpeakHumphries);
            mission.Action(HelpHumphries, 3, MissionActionKind.StartScenario, scenario: PitSaved);
            mission.Transition(HelpHumphries, 2, 2, MissionObjectiveState.Failed);
            mission.ObjectiveState(HelpHumphries, 2, HelpThePrisoners, MissionObjectiveState.Completed);

            // 7, hidden: the ledge's six dead: the switch usable.
            mission.Transition(LedgeCleared);
            for (var fighter = 1U; fighter <= LedgeFighters.Length; fighter++)
                mission.Progress(LedgeCleared, MissionProgressEventKind.ScenarioEvent, fighter, (uint)LedgeFighters.Length, counter: LedgeCounted, trigger: fighter);
            mission.Action(LedgeCleared, 1, MissionActionKind.StartScenario, scenario: SwitchReady);
            // 7 fails when a side is decided first (3 or 4 done, also by a mate's shared credit): no late switch step.
            mission.Transition(LedgeCleared, 2, 2, MissionObjectiveState.Failed);
            mission.ObjectiveState(LedgeCleared, 2, HelpHumphries, MissionObjectiveState.Completed);
            mission.Transition(LedgeCleared, 3, 3, MissionObjectiveState.Failed);
            mission.ObjectiveState(LedgeCleared, 3, HelpThePrisoners, MissionObjectiveState.Completed);

            // 4: the switch used: the cells open, 6 opens, the pit's side goes. 3 done first fails it.
            mission.Transition(HelpThePrisoners);
            mission.Progress(HelpThePrisoners, MissionProgressEventKind.ScenarioEvent, 1, null, counter: SwitchUsed);
            AshenRaidTheConscriptionFacilityV1.Open(mission, HelpThePrisoners, 1, SpeakLukaz);
            mission.Action(HelpThePrisoners, 3, MissionActionKind.StartScenario, scenario: CellsOpen);
            mission.Transition(HelpThePrisoners, 2, 2, MissionObjectiveState.Failed);
            mission.ObjectiveState(HelpThePrisoners, 2, HelpHumphries, MissionObjectiveState.Completed);

            // 5, 6: the talk of the side saved; the reward (the turn-in by radio).
            mission.Transition(SpeakHumphries);
            mission.Conversation(SpeakHumphries, HumphriesPackage, Variant);
            mission.Action(SpeakHumphries, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(SpeakLukaz);
            mission.Conversation(SpeakLukaz, LukazPackage, Variant);
            mission.Action(SpeakLukaz, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(ToTheArena, 1, ArenaMarkerAt.X, ArenaMarkerAt.Y, ArenaMarkerAt.Z, ArenaRadius, Facility);
            mission.Indicator(HelpHumphries, 2, HumphriesAt.X, HumphriesAt.Y, HumphriesAt.Z, FightMarkerRadius, Facility);
            mission.Indicator(HelpThePrisoners, 3, LukazAt.X, LukazAt.Y, LukazAt.Z, FightMarkerRadius, Facility);
            mission.Indicator(SpeakHumphries, 4, HumphriesAt.X, HumphriesAt.Y, HumphriesAt.Z, NpcMarkerRadius, Facility);
            mission.Indicator(SpeakLukaz, 5, LukazAt.X, LukazAt.Y, LukazAt.Z, NpcMarkerRadius, Facility);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1499",
                "Native1 'Get To Arena Before the Enslaver Kills the Prisoners' 14300/14301, 2 'Hidden - Master Objective' 14302/14303, " +
                "3 'Help Lt. Humpries in the Atta Pit' 14304/14305, 4 'Help 6 Brann Prisoners' 14309/14310, 5 'Speak with Lt. Humphries' " +
                "14306/14307, 6 'Speak with Lukaz' 14311/14312; objectiveconversation 1499,5 pkg1936 14308, 1499,6 pkg1934 14313. Texts " +
                "13389-13393. Names 9746, 9755, 9483. Use name 140 Cell Door Control.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Into_the_Lion%27s_Den",
                "Requires Blast the Biolab; follow-up Save the Queen; Headquarters rewards; 100000XP, 8600 credits; Vextronics Sonic " +
                "Disperser, Virulent Injector Gun, Vitalius Exothermic Polarity Gun, Vextronics Cryogenic Net Gun; officer side three " +
                "Atta bosses and a swarm of Atta, Brann side four Atta and two Nitroglazers.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenIntoTheLionsDenV1",
                "Level43; radio offer (1498 rewarded, arrival in 1988) and radio completion; bound to the squad's copy. 1 the arena " +
                "(240,88,-100) r30 (the ledge) within 600 s, else failed; it brings the member's own fights: the pit's 3x520002 and 4x531015 round " +
                "Humphries 790668, the ledge's 2x790240 and 4x531015 before Lukaz 790669 and six prisoners 790670 (all his own, " +
                "invulnerable). Hidden 9 the pit's seven dead, 3 follows it; hidden 7 the ledge's six dead make the Cell Door Control 7468 (name 140) usable, " +
                "4 its use; the first of 3 and 4 fails the other and 7, the fighters still standing fall; 2 aggregates 3/4, hidden 8 aggregates 5/6; 5 Humphries' talk " +
                "(pkg1936), 6 Lukaz' (pkg1934), either rewards. No key room, no Lukaz men. Items 12071, 12155, 12135, 12129.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The member's own Humphries, Lukaz, prisoners, switch and the two fights, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Facility,
                MapContextId = Facility,
                HiddenObjectiveIds = new() { Master, LedgeCleared, Spoken, PitCleared },
                ObjectiveAggregations = new()
                {
                    [Master] = new MissionObjectiveAggregation(new[] { HelpHumphries, HelpThePrisoners }, 1),
                    [Spoken] = new MissionObjectiveAggregation(new[] { SpeakHumphries, SpeakLukaz }, 1)
                },
                DefeatSequences = new()
            };
            var unhurt = new ActorGameplayPolicy { Invulnerable = true };
            scene.Actors[HumphriesRole] = new SceneActorDefinition(HumphriesRole, SceneActorKind.Creature, Humphries, HumphriesAt,
                GameplayPolicy: unhurt, OwnerOnly: true);
            scene.Actors[LukazRole] = new SceneActorDefinition(LukazRole, SceneActorKind.Creature, Lukaz, LukazAt,
                GameplayPolicy: unhurt, OwnerOnly: true);
            for (var index = 0; index < PrisonersAt.Length; index++)
                scene.Actors[PrisonerRole(index + 1)] = new SceneActorDefinition(PrisonerRole(index + 1), SceneActorKind.Creature, Prisoner,
                    PrisonersAt[index], GameplayPolicy: unhurt, OwnerOnly: true);
            scene.Actors[SwitchRole] = new SceneActorDefinition(SwitchRole, SceneActorKind.Object, SwitchClass, SwitchAt,
                InitiallyInteractable: false, InitialObjectState: SwitchOff, NameOverrideId: SwitchName,
                UseAction: new SceneObjectAction(MissionId, HelpThePrisoners, SwitchSequence, UseArgument));

            var arrival = new SceneSequenceDefinition();
            foreach (var role in new[] { HumphriesRole, LukazRole, SwitchRole }.Concat(Enumerable.Range(1, PrisonersAt.Length).Select(PrisonerRole)))
                arrival.World.Add(new EnsureActorIntent($"create-{role}", role));
            scene.Sequences[0] = arrival;

            var fights = new SceneSequenceDefinition();
            void Fighters((string Role, uint Creature, ScenePosition At)[] fighters, uint counted, uint downFrom)
            {
                for (var index = 0; index < fighters.Length; index++)
                {
                    var (role, creature, at) = fighters[index];
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at,
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile());
                    fights.World.Add(new EnsureActorIntent($"{role}-comes", role));
                    var down = downFrom + 1 + (uint)index;
                    scene.DefeatSequences[role] = down;
                    scene.Sequences[down] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, counted, (uint)index + 1) } };
                }
            }
            Fighters(PitFighters, PitCounted, PitDownFrom);
            Fighters(LedgeFighters, LedgeCounted, LedgeDownFrom);
            scene.Sequences[FightsBegin] = fights;

            // The switch's use: the event of 4 only (the world changes after 4's own steps, the set 15 door's order).
            scene.Sequences[SwitchSequence] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, SwitchUsed, 1) } };
            // IfPresent: a late 7 (a mate's kills after this member's side was decided) finds no switch to light; 7 also fails
            // when 3 or 4 completes first, so it does not come then.
            scene.Sequences[SwitchReady] = new SceneSequenceDefinition { World = { new SetInteractionIntent("cell-door-control-usable", SwitchRole, true, IfPresent: true) } };

            // A side decided: the member's own fighters of both sides still standing fall (a mate's kills or use completed his 3
            // or 4 by the shared credit, GroupCreditService), killed by nobody (no kill, XP, loot or credit); no fighter is
            // removed, so the loot of every kill stays (the ones he killed himself, either side). The NPCs and the switch of
            // the side lost go.
            var pitSaved = new SceneSequenceDefinition();
            foreach (var role in Enumerable.Range(1, PrisonersAt.Length).Select(PrisonerRole).Append(LukazRole).Append(SwitchRole))
                pitSaved.World.Add(new RemoveActorIntent($"{role}-goes", role));
            foreach (var role in PitFighters.Concat(LedgeFighters).Select(fighter => fighter.Role))
                pitSaved.World.Add(new KillActorIntent($"{role}-falls-pit-saved", role));
            scene.Sequences[PitSaved] = pitSaved;

            var cellsOpen = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("cell-door-control-on", SwitchRole, SwitchOn),
                    new SetInteractionIntent("cell-door-control-done", SwitchRole, false)
                }
            };
            cellsOpen.World.Add(new RemoveActorIntent($"{HumphriesRole}-goes", HumphriesRole));
            foreach (var role in PitFighters.Concat(LedgeFighters).Select(fighter => fighter.Role))
                cellsOpen.World.Add(new KillActorIntent($"{role}-falls-cells-open", role));
            scene.Sequences[CellsOpen] = cellsOpen;
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
