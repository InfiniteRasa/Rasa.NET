using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Bonds of Blood (native mission 1282), Ligo Ashen Desert: Researcher Endala Berish begs the player to find her brother
    /// Maiek, held by the Bane in their Conscription Base; the player finds him dead and tells her. One of the two follow-ups of
    /// Missing Scientists: God of Storms (1281, Odesha's) is the other.
    ///
    /// Native: texts 10324-10327, 10328 (missionconversation): log "Infiltrate the Bane Conscription Base and find where they are
    /// holding Researcher Maiek Berish. If you find Maiek, then you will lose the opportunity to reprogram the Lightning
    /// Resistors and damage the Bane Conscription Facility. Return to Endala Berish in Indra Caves once you have found him.",
    /// offer "I heard that you did not find my brother. Maiek, you must find him. ...", turn-in "I am sorry. I need to be alone.
    /// ..."; objectives (missionobjective) 1 "Find Researcher Maiek Barish" 10329/10330, 2 "Talk to Endala Berish" 10331/10332,
    /// 3 "Flag Reader" 11173/11174, no counters; objectiveconversation (1282,2) package 1475 Endala 10333 ("He was already
    /// dead?"). Name 9354 Endala. Maiek has no name and no usable name of his own in the client.
    ///
    /// Sourced: TaRapedia The Bonds of Blood: giver and reward giver Researcher Endala Berish (Indra Pass); requires Missing
    /// Scientists; 200,000 XP, 8,600 credits; one of Vitalius Leech Gun, Vextronics Electric Net Gun, ChiTech Pistol, Teleract
    /// Incendiary RPG Launcher; "The body of Researcher Maiek Barish is located in the Bane Conscription Base with the power
    /// generators. The body is very near Overseer Naxveik"; God of Storms: "Once you accept God of Storms you cannot see or
    /// accept her quest".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3; triage D4):
    ///  - Level 42 (the chain's band). Not offered and not taken while God of Storms is held or once it is done (offer and
    ///    accept only; the scene's requirement), the reverse of God of Storms'.
    ///  - Maiek's body a personal Brann corpse (UsableTreasureDispBrannCorpseMaleV01 7928, the client's own name "Rotting Brann
    ///    Corpse": no usable name of his; closed 200, opened 201; the client sends 1) by the Conscription Base's power generators
    ///    (Turning Out the Lights' three, set 5) at (344, -72), below their ledge: 36 m from the third generator, 64 m from Overseer Naxveik's point,
    ///    not "very near" him, as the place keeps 34 m and more outside every hostile pool's circle (Naxveik's own pool among
    ///    them) and is seen from every place round it (the ground by the generators slopes too much for the ring); on a client floor, one floor, under open sky, walked to on the client's floor from the Ashoka Settlement
    ///    waypoint and back, 30 m or more outside every hostile pool's circle the whole way (probe of 2026-11-10; the
    ///    Conscription Garrison's waypoint is the Bane's by default). Its search completes 1 (a scene result) and opens 2; the
    ///    hidden 3 ("Flag Reader") completes with 1 and closes an open God of Storms (a scene signal to 1281, whose open
    ///    objectives fail: the client log's "you will lose the opportunity to reprogram the Lightning Resistors"). The tenth
    ///    resistor of God of Storms closes an open The Bonds of Blood the same way.
    ///  - 2 completes in Endala's talk (her package's 10333), and she gives the reward.
    ///  - Markers r8 on the body and on Endala.
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 ("Leech Gun" of no kind read as physical,
    ///    "Pistol" physical, "Incendiary RPG Launcher" the fire grenade launcher): Leech Gun Physical 45763 (39-43), Net Gun
    ///    Electric 12131 (41-45), Pistol Physical 14409 (40-44), Grenade Launcher Fire 47916 (40-44); one to choose.
    /// </summary>
    public static class AshenBondsOfBloodV1
    {
        public const uint MissionId = 1282;
        public const uint Level = 42;
        public const uint Endala = AshenIndraPassNpcsV1.Endala;
        public const uint Find = 1, Talk = 2, FlagReader = 3;
        public const uint Found = 1;                    // the scenario of the body's search: one scene result
        public const double MarkerRadius = 8;

        public const uint CorpseClass = AshenMissingScientistsV1.CorpseClass;
        public const uint CorpseName = 0;               // the client's own name, "Rotting Brann Corpse"
        public const uint UseArgument = AshenMissingScientistsV1.UseArgument;
        public const uint ClosedState = AshenMissingScientistsV1.ClosedState, OpenedState = AshenMissingScientistsV1.OpenedState;
        public const string CorpseRole = "conscription-base-maiek";

        public static readonly ScenePosition MaiekAt = new(344f, 288.75f, -72f);

        public const uint Experience = 200000;
        public const uint Credits = 8600;
        public const uint LeechGun = 45763;             // Weapon_Avatar_LeechGun_Physical_UNC_39_to_43
        public const uint ElectricNetGun = 12131;       // Weapon_Avatar_NetGun_Electric_UNC_41_to_45
        public const uint Pistol = 14409;               // Weapon_Avatar_Pistol_Physical_UNC_40_to_44
        public const uint IncendiaryRpgLauncher = 47916; // Weapon_Avatar_GrenadeLauncher_Fire_UNC_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "The Bonds of Blood", 10324, Endala, Endala, Level);
            mission.Reward(Experience, Credits, new[] { LeechGun, ElectricNetGun, Pistol, IncendiaryRpgLauncher });
            mission.Prerequisite(AshenMissingScientistsV1.MissionId);
            mission.Scenario(Found, "Researcher Maiek Berish found", 1);

            mission.Objective(Find, 10329, 10330, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Talk, 10331, 10332, 1, MissionObjectiveState.Inactive);
            mission.Objective(FlagReader, 11173, 11174, 2, MissionObjectiveState.Incomplete);    // hidden

            // 1: the body found opens 2.
            mission.Transition(Find);
            mission.Progress(Find, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Found);
            mission.Action(Find, 1, MissionActionKind.RevealObjective, targetObjective: Talk);
            mission.Action(Find, 2, MissionActionKind.ActivateObjective, targetObjective: Talk, state: MissionObjectiveState.Incomplete);

            // 2: Endala hears it.
            mission.Transition(Talk);
            mission.Conversation(Talk, AshenIndraPassNpcsV1.EndalaPackage);
            mission.Action(Talk, 1, MissionActionKind.GrantReward, reward: 1);

            // 3 (hidden): completes with 1 and closes an open God of Storms; the tenth resistor closes this mission.
            mission.Transition(FlagReader);
            mission.ObjectiveState(FlagReader, Find, MissionObjectiveState.Completed);
            var scene = Scene();
            scene.Requirement = AshenGodOfStormsV1.NotWhileTheOther(AshenGodOfStormsV1.MissionId);
            AshenGodOfStormsV1.CloseTheOther(mission, scene, FlagReader, 1, AshenGodOfStormsV1.MissionId);
            AshenGodOfStormsV1.ClosedByTheOther(mission, "God of Storms", (Find, 2), (Talk, 2), (FlagReader, 2));

            var endala = AshenIndraPassNpcsV1.EndalaAt;
            mission.Indicator(Find, 1, MaiekAt.X, MaiekAt.Y, MaiekAt.Z, MarkerRadius);
            mission.Indicator(Talk, 2, endala.X, endala.Y, endala.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1282",
                "Native1 'Find Researcher Maiek Barish' 10329/10330, 2 'Talk to Endala Berish' 10331/10332, 3 'Flag Reader' " +
                "11173/11174, no counters; objectiveconversation 1282,2 pkg1475 10333. Texts 10324-10328 (Maiek in the Bane " +
                "Conscription Base; finding him loses the resistors). Name 9354 Endala.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Bonds_of_Blood",
                "Giver/reward giver Researcher Endala Berish (Indra Pass); requires Missing Scientists; 200000XP, 8600 credits; " +
                "Vitalius Leech Gun, Vextronics Electric Net Gun, ChiTech Pistol, Teleract Incendiary RPG Launcher; Maiek's body in the " +
                "Bane Conscription Base with the power generators, very near Overseer Naxveik.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenBondsOfBloodV1",
                "Level42 estimated. Not offered while 1281 is held or done. Maiek a personal Brann corpse 7928 (arg1; 200->201) at " +
                "(344, -72) by the generators, 64 m from Naxveik; its search completes 1, opens 2; hidden 3 completes with 1 and " +
                "closes an open 1281; the tenth resistor closes this one. Endala's talk 10333 completes 2. Markers r8. Items 45763, " +
                "12131, 14409, 47916 (lowest Uncommon, band of level 42), one to choose.", 0.3);

            mission.Enable(scene);
        }

        /// <summary>The player's own Maiek, searched once.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { FlagReader }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[CorpseRole] = new SceneActorDefinition(CorpseRole, SceneActorKind.Object, CorpseClass, MaiekAt,
                Orientation: 2.4, InitialObjectState: ClosedState, NameOverrideId: CorpseName,
                UseAction: new SceneObjectAction(MissionId, Find, 1, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{CorpseRole}", CorpseRole));
            scene.Sequences[1] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{CorpseRole}-searched", CorpseRole, OpenedState),
                    new SetInteractionIntent($"{CorpseRole}-done", CorpseRole, false)
                },
                Signals = { new SceneMissionSignal(MissionId, Found, 1) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
