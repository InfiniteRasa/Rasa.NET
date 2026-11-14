using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Overly Sensitive (native mission 1473), Avernus Outpost (squad instance 2110): Captain Sera Karter sends the player into the
    /// overrun outpost for the Armor Sample and the Commander's Diary, to destroy the prototype ammunition and the data backup
    /// machine, to shut down the four servers in the HQ and to destroy the secret stash in the high security area, reached by a
    /// teleporter that needs a key card.
    ///
    /// Native: texts 13094-13098 (missionconversation): log "Head to Avernus Outpost and bring back the Armor Sample and the
    /// Commander's Diary. ... shut down the 4 servers, and destroy whatever secret thing the AFS have stashed in the high security
    /// area. You may need to find a key card to get the teleporter operational to get inside.", offer "... There's a prototype ammo
    /// crate there, probably near some other weapons, destroy it. Also, destroy the backup data recorder in the main HQ. Inside HQ
    /// you'll find a bank of 4 computers ...", turn-in "Good work, soldier. ...". Objectives (missionobjective) 1 "Armor Sample"
    /// 13915/13916, 2 "Destroy Prototype Ammo" 13917/13918 counter 13919 "Prototype Ammo Destroyed", 3 "Comander's Diary"
    /// 13926/13927, 4 "Shut Down the Servers" 13920/13921 counter 13922 "Servers Shutdown", 5 "Secret Stash Destroyed" 13928/13929
    /// counter 13930 "Secret Experiment Destroyed", 6 "Destroy Data Backup Machine" 13923/13924 counter 13925 "Data Backup
    /// Destroyed"; no objective conversation, no marker names. Items (itemtemplate_itemclass): 26448 MisAvernusOutpostArmorSample
    /// (111191), 26447 MisAvernusOutpostCaptainsLog (111190), 26446 MisAvernusOutpostSecurityBadge (111189).
    ///
    /// Sourced: TaRapedia Overly Sensitive: giver and reward giver Captain Sera Karter (Avernus Outpost, at the entrance); 90,000
    /// XP, 6,150 credits; Class VIII Cryogenic Grenade (3), Standard Med Pack (3), Incendiary Grenade (3), Advanced Med Pack (2);
    /// the places (/loc, 0.5): the armor sample in a courier bag (104.9, 514.4, -56.7) in the armor shop, the prototype ammo
    /// (141.4, 515.6, -48.6) in the weapon shop, the diary on a corpse in the tavern (194.7, 519, -31), the servers (178.4, 532.7,
    /// -129.8) and the data backup black box (177.3, 533.7, -135.1) in the HQ, the clearance card on a corpse "around the landing
    /// strip" (8.9, 531.2, -124.9), the stash "in a building by itself, behind some pretty heavy security force fields. The
    /// teleporter will get you in, but only with a high clearance security card" (the stasis tube (-6.5, 520, -50.2) in the
    /// prison). Patch note 2008-03-27 (IGN): "secret research could be destroyed before the objective was active" (fixed): the
    /// stash only while 5 is open.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (triage D5). No prerequisite (G1). Bound to the squad's Avernus Outpost (taken at Karter there).
    ///  - Every object the player's own (a squad mate has his own). Each pick-up (the bag, the diary's corpse, the card's corpse)
    ///    sends its event; the objective it completes gives each member on 1473 his own item (IssueMissionItem, as Crucible
    ///    1401's cards and Threshold 1360's): the sample (1), the diary (3), the card (hidden 7). The diary reveals and opens 5.
    ///  - The courier bag the client's UsableItemDispCourierBag (21841, use argument 3, 81 to 82); the corpses the client's
    ///    UsableTreasureDispHumCorpseMaleV01 (24995, 200 to 201; "Rotting Human Corpse"); the ammo UsableInertDestHumAmmoStoresV01
    ///    (10207, not 7833 "Ammo Stores", whose mesh is the client's Default 776), the black box UsableInertDestBlackBoxV01
    ///    (22106), the stash UsableInertDestBaneStasisChamber (7197) on the prison's stasis tube, each destroyed by hits (110 to 2,
    ///    100 hit points); the servers four UsableTwoStateHumComputerDellXpsTowerV01 (26500, 55 to 56) side by side in the HQ, a
    ///    use each (4 counts them).
    ///  - The teleporter the client's UsableTwoStateHumTeleporter (7895, name override 52 "Universal Interior Teleporter": the class
    ///    has no display text) west of the landing strip, locked to the card (in its first state, 55): its use, while 5 is open,
    ///    takes the player into the prison beside the tube (a move on this map). The card is kept (more than one way in). The
    ///    client map has no force field round the prison. (Fix L, to-fix row 46: the prison is a closed room in the real client;
    ///    its way out is local teleporter 923, AshenFixLV1.)
    ///  - The items go at the turn-in (cleanup Remove). The rewards by their client names, each a stack, one to choose: Class VIII
    ///    Cryogenic Grenade (111049) x3, Standard Med Pack (45064) x3, Incendiary Grenade (111039) x3, Advanced Med Pack (45065) x2.
    ///  - Markers (unnamed): 1 the bag, 2 the ammo, 3 the diary's corpse, 4 the servers, 5 the stash, the teleporter and the card's
    ///    corpse, 6 the black box.
    /// </summary>
    public static class AshenOverlySensitiveV1
    {
        public const uint MissionId = 1473;
        public const uint Level = 41;
        public const uint Outpost = AshenAvernusOutpostNpcsV1.Outpost;
        public const uint Karter = AshenAvernusOutpostNpcsV1.Karter;
        public const uint ArmorSample = 1, DestroyAmmo = 2, Diary = 3, Servers = 4, SecretStash = 5, DataBackup = 6, KeyCard = 7;

        /// <summary>Scenarios: each object's event (servers: one step a server).</summary>
        public const uint SampleTaken = 1, AmmoDestroyed = 2, DiaryTaken = 3, ServersDown = 4, StashDestroyed = 5, BackupDestroyed = 6,
            CardTaken = 7;
        public const uint BagSequence = 11, AmmoSequence = 12, DiarySequence = 13, StashSequence = 15, BackupSequence = 16,
            CardSequence = 17, TeleportSequence = 18, ServerSequenceFrom = 20;

        public const uint SampleTemplate = 111191, DiaryTemplate = 111190, CardTemplate = 111189;
        public const string SampleKey = "avernus-armor-sample", DiaryKey = "avernus-commanders-diary", CardKey = "avernus-security-card";

        public const uint BagClass = 21841;             // UsableItemDispCourierBag
        public const uint BagUseArgument = 3;           // usabledata lookup[21841]: [0] 3
        public const uint BagReady = 81, BagTaken = 82;
        public const uint CorpseClass = 24995;          // UsableTreasureDispHumCorpseMaleV01
        public const uint CorpseClosed = 200, CorpseOpened = 201;
        public const uint AmmoClass = 10207;            // UsableInertDestHumAmmoStoresV01
        public const uint BlackBoxClass = 22106;        // UsableInertDestBlackBoxV01
        public const uint StashClass = 7197;            // UsableInertDestBaneStasisChamber
        public const uint IntactState = 110, DestroyedState = 2, HitPoints = 100;
        public const uint ServerClass = 26500;          // UsableTwoStateHumComputerDellXpsTowerV01
        public const uint ServerOn = 55, ServerOff = 56;
        public const uint TeleporterClass = 7895;       // UsableTwoStateHumTeleporter
        public const uint TeleporterName = 52;          // usablenameoverride "Universal Interior Teleporter"
        public const uint TeleporterState = 55;         // locked to the card in this state (S2: a real state)
        public const uint UseArgument = 1;              // usabledata lookup[24995], [26500], [7895]: [0] None

        public const string BagRole = "avernus-courier-bag", AmmoRole = "avernus-prototype-ammo", DiaryRole = "avernus-tavern-corpse",
            StashRole = "avernus-secret-stash", BackupRole = "avernus-data-backup", CardRole = "avernus-strip-corpse",
            TeleporterRole = "avernus-prison-teleporter";

        public static readonly ScenePosition BagAt = new(102.9f, 514.25f, -56.7f);
        public static readonly ScenePosition AmmoAt = new(141.4f, 514.57f, -49.1f);
        public static readonly ScenePosition DiaryAt = new(194.7f, 519.01f, -31f);
        public static readonly ScenePosition[] ServersAt =
        {
            new(176.4f, 532.66f, -126.8f), new(176.4f, 532.66f, -128.8f), new(176.4f, 532.66f, -130.8f), new(176.4f, 532.66f, -132.8f)
        };
        public static readonly ScenePosition BackupAt = new(175.8f, 532.66f, -135.1f);
        public static readonly ScenePosition CardAt = new(8.9f, 531.16f, -124.9f);
        public static readonly ScenePosition TeleporterAt = new(28f, 528.12f, -106f);
        public static readonly ScenePosition StashAt = new(-6.5f, 520.01f, -50.2f);
        /// <summary>Where the teleporter sets the player down: inside the prison, 4 m north of the tube.</summary>
        public static readonly ScenePosition PrisonArrival = new(-8f, 520.01f, -46f);
        public const double PrisonArrivalYaw = 0;

        public const double ObjectMarkerRadius = 4, ServersMarkerRadius = 6;    // ESTIMATE (no source gives radii)
        public static readonly (double X, double Y, double Z) ServersMarkerAt = (176.4, 532.66, -129.8);

        public const uint Experience = 90000;
        public const uint Credits = 6150;
        public const uint CryogenicGrenade = 111049, CryogenicGrenades = 3;     // Class VIII Cryogenic Grenade (class 26299)
        public const uint StandardMedPack = 45064, StandardMedPacks = 3;        // Class VIII Standard Med Pack (class 22547)
        public const uint IncendiaryGrenade = 111039, IncendiaryGrenades = 3;   // Class VIII Incendiary Grenade (class 26289)
        public const uint AdvancedMedPack = 45065, AdvancedMedPacks = 2;        // Class VIII Advanced Med Pack (class 22548)

        public static string ServerRole(int index) => $"avernus-server-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Overly Sensitive", 13094, Karter, Karter, Level);
            mission.Reward(Experience, Credits,
                new[] { (CryogenicGrenade, CryogenicGrenades), (StandardMedPack, StandardMedPacks), (IncendiaryGrenade, IncendiaryGrenades),
                    (AdvancedMedPack, AdvancedMedPacks) },
                new (uint, uint)[0]);
            mission.Scenario(SampleTaken, "The courier bag searched: the Armor Sample", 1);
            mission.Scenario(AmmoDestroyed, "The prototype ammo destroyed", 1);
            mission.Scenario(DiaryTaken, "The tavern corpse searched: the Commander's Diary", 1);
            mission.Scenario(ServersDown, "A server shut down", (uint)ServersAt.Length);
            mission.Scenario(StashDestroyed, "The secret stash destroyed", 1);
            mission.Scenario(BackupDestroyed, "The data backup machine destroyed", 1);
            mission.Scenario(CardTaken, "The landing strip corpse searched: the security card", 1);

            mission.Objective(ArmorSample, 13915, 13916, 0, MissionObjectiveState.Incomplete);
            mission.Objective(DestroyAmmo, 13917, 13918, 1, MissionObjectiveState.Incomplete, counterTextId: 13919);
            mission.Objective(Diary, 13926, 13927, 2, MissionObjectiveState.Incomplete);
            mission.Objective(Servers, 13920, 13921, 3, MissionObjectiveState.Incomplete, counterTextId: 13922);
            mission.Objective(SecretStash, 13928, 13929, 4, MissionObjectiveState.Inactive, counterTextId: 13930);
            mission.Objective(DataBackup, 13923, 13924, 5, MissionObjectiveState.Incomplete, counterTextId: 13925);
            mission.Objective(KeyCard, 13928, 13929, 6, MissionObjectiveState.Incomplete, required: false);    // hidden

            // 1: the bag searched; each member whose 1 completes gets his own sample.
            mission.Transition(ArmorSample);
            mission.Progress(ArmorSample, MissionProgressEventKind.ScenarioEvent, 1, null, counter: SampleTaken);
            mission.Action(ArmorSample, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("bag-gives-the-armor-sample", MissionId, SampleKey, SampleTemplate, 1));
            mission.Action(ArmorSample, 2, MissionActionKind.GrantReward, reward: 1);

            // 2: the ammo destroyed.
            mission.Transition(DestroyAmmo);
            mission.Progress(DestroyAmmo, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: AmmoDestroyed);

            // 3: the tavern corpse searched: each member's own diary; 5 opens.
            mission.Transition(Diary);
            mission.Progress(Diary, MissionProgressEventKind.ScenarioEvent, 1, null, counter: DiaryTaken);
            mission.Action(Diary, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("corpse-gives-the-diary", MissionId, DiaryKey, DiaryTemplate, 1));
            mission.Action(Diary, 2, MissionActionKind.RevealObjective, targetObjective: SecretStash);
            mission.Action(Diary, 3, MissionActionKind.ActivateObjective, targetObjective: SecretStash, state: MissionObjectiveState.Incomplete);

            // 4: the four servers shut down.
            mission.Transition(Servers);
            for (var server = 1U; server <= ServersAt.Length; server++)
                mission.Progress(Servers, MissionProgressEventKind.ScenarioEvent, server, (uint)ServersAt.Length, counter: ServersDown, trigger: server);

            // 5: the stash destroyed (only while 5 is open: the object takes no hit before).
            mission.Transition(SecretStash);
            mission.Progress(SecretStash, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: StashDestroyed);

            // 6: the data backup machine destroyed.
            mission.Transition(DataBackup);
            mission.Progress(DataBackup, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: BackupDestroyed);

            // 7, hidden: the landing strip corpse searched: each member's own card.
            mission.Transition(KeyCard);
            mission.Progress(KeyCard, MissionProgressEventKind.ScenarioEvent, 1, null, counter: CardTaken);
            mission.Action(KeyCard, 1, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("corpse-gives-the-security-card", MissionId, CardKey, CardTemplate, 1));

            mission.Indicator(ArmorSample, 1, BagAt.X, BagAt.Y, BagAt.Z, ObjectMarkerRadius, Outpost);
            mission.Indicator(DestroyAmmo, 2, AmmoAt.X, AmmoAt.Y, AmmoAt.Z, ObjectMarkerRadius, Outpost);
            mission.Indicator(Diary, 3, DiaryAt.X, DiaryAt.Y, DiaryAt.Z, ObjectMarkerRadius, Outpost);
            mission.Indicator(Servers, 4, ServersMarkerAt.X, ServersMarkerAt.Y, ServersMarkerAt.Z, ServersMarkerRadius, Outpost);
            mission.Indicator(SecretStash, 5, StashAt.X, StashAt.Y, StashAt.Z, ObjectMarkerRadius, Outpost);
            mission.Indicator(SecretStash, 6, TeleporterAt.X, TeleporterAt.Y, TeleporterAt.Z, ObjectMarkerRadius, Outpost);
            mission.Indicator(SecretStash, 7, CardAt.X, CardAt.Y, CardAt.Z, ObjectMarkerRadius, Outpost);
            mission.Indicator(DataBackup, 8, BackupAt.X, BackupAt.Y, BackupAt.Z, ObjectMarkerRadius, Outpost);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1473",
                "Native1 'Armor Sample' 13915/13916, 2 'Destroy Prototype Ammo' 13917/13918 c13919, 3 'Comander's Diary' 13926/13927, " +
                "4 'Shut Down the Servers' 13920/13921 c13922, 5 'Secret Stash Destroyed' 13928/13929 c13930, 6 'Destroy Data Backup " +
                "Machine' 13923/13924 c13925; no objectiveconversation, no indicator names. Texts 13094-13098. Items class26448 " +
                "(111191), 26447 (111190), 26446 (111189). Name 9281 Captain Sera Karter.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Overly_Sensitive",
                "Giver/reward giver Captain Sera Karter (Avernus Outpost); 90000XP, 6150 credits; Class VIII Cryogenic Grenade (3), " +
                "Standard Med Pack (3), Incendiary Grenade (3), Advanced Med Pack (2); /loc of the bag, ammo, tavern corpse, servers, " +
                "black box, landing strip corpse, the stash behind force fields, the teleporter with the card. IGN 2008-03-27: the " +
                "secret research could be destroyed before its objective was active (fixed).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenOverlySensitiveV1",
                "Level41; bound to the squad's copy 2110; Karter 790640 gives and rewards. Personal objects: courier bag 21841 (arg3, " +
                "81->82) gives the sample, tavern corpse 24995 the diary (opens 5), landing strip corpse 24995 the card (hidden 7), " +
                "ammo 10207, black box 22106, stash 7197 (only while 5 is open) destroyed by hits (110->2), four servers 26500 used " +
                "(55->56, 4 counts). Teleporter 7895 (name52) keyed 111189 takes the player into the prison (-8,520.01,-46) while 5 " +
                "is open. Items removed at the turn-in. Rewards 111049x3, 45064x3, 111039x3, 45065x2, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own bag, corpses, ammo, servers, black box, stash and teleporter, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Outpost,    // taken at Karter in the squad's copy
                MapContextId = Outpost,
                HiddenObjectiveIds = new() { KeyCard },
                Items = new()
                {
                    Binding(SampleKey, SampleTemplate), Binding(DiaryKey, DiaryTemplate), Binding(CardKey, CardTemplate)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            Searched(scene, BagRole, BagClass, BagAt, BagReady, BagTaken, BagUseArgument, ArmorSample, BagSequence, SampleTaken);
            Searched(scene, DiaryRole, CorpseClass, DiaryAt, CorpseClosed, CorpseOpened, UseArgument, Diary, DiarySequence, DiaryTaken);
            Searched(scene, CardRole, CorpseClass, CardAt, CorpseClosed, CorpseOpened, UseArgument, KeyCard, CardSequence, CardTaken);
            Destroyed(scene, AmmoRole, AmmoClass, AmmoAt, DestroyAmmo, AmmoSequence, AmmoDestroyed);
            Destroyed(scene, BackupRole, BlackBoxClass, BackupAt, DataBackup, BackupSequence, BackupDestroyed);
            Destroyed(scene, StashRole, StashClass, StashAt, SecretStash, StashSequence, StashDestroyed);

            for (var index = 0; index < ServersAt.Length; index++)
            {
                var role = ServerRole(index);
                var sequence = ServerSequenceFrom + 1 + (uint)index;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, ServerClass, ServersAt[index],
                    InitialObjectState: ServerOn, UseAction: new SceneObjectAction(MissionId, Servers, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-off", role, ServerOff),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, ServersDown, (uint)index + 1) }
                };
            }

            // The teleporter: locked to the card in its first state; a use while 5 is open takes the player into the prison.
            scene.Actors[TeleporterRole] = new SceneActorDefinition(TeleporterRole, SceneActorKind.Object, TeleporterClass, TeleporterAt,
                InitialObjectState: TeleporterState, NameOverrideId: TeleporterName,
                UseAction: new SceneObjectAction(MissionId, SecretStash, TeleportSequence, UseArgument), KeyItemTemplateId: CardTemplate);
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{TeleporterRole}", TeleporterRole));
            scene.Sequences[TeleportSequence] = new SceneSequenceDefinition
            {
                World = { new TransferIntent("prison-teleporter-inside", Outpost, PrisonArrival, PrisonArrivalYaw) }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The roles of the scene's objects, in the order of the markers' objectives (for the tests).</summary>
        public static string[] ObjectRoles =>
            new[] { BagRole, AmmoRole, DiaryRole, StashRole, BackupRole, CardRole, TeleporterRole }
                .Concat(Enumerable.Range(0, ServersAt.Length).Select(ServerRole)).ToArray();

        private static MissionItemBinding Binding(string key, uint template) =>
            new(key, template, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove);

        /// <summary>A container of the player's own: searched once, it sends its event (the objective gives the item).</summary>
        private static void Searched(MissionSceneDefinition scene, string role, uint objectClass, ScenePosition at, uint ready, uint taken,
            uint useArgument, uint objective, uint sequence, uint scenario)
        {
            scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, objectClass, at,
                InitialObjectState: ready, UseAction: new SceneObjectAction(MissionId, objective, sequence, useArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
            scene.Sequences[sequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{role}-searched", role, taken),
                    new SetInteractionIntent($"{role}-done", role, false)
                },
                Signals = { new SceneMissionSignal(MissionId, scenario, 1) }
            };
        }

        /// <summary>A destructible of the player's own: destroyed by hits while its objective is open, it sends its event.</summary>
        private static void Destroyed(MissionSceneDefinition scene, string role, uint objectClass, ScenePosition at, uint objective,
            uint sequence, uint scenario)
        {
            scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, objectClass, at,
                InitialObjectState: IntactState,
                Destruction: new SceneObjectDestruction(MissionId, objective, sequence, HitPoints, DestroyedState));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
            scene.Sequences[sequence] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent($"{role}-destroyed", role, DestroyedState),
                    new SetInteractionIntent($"{role}-done", role, false)
                },
                Signals = { new SceneMissionSignal(MissionId, scenario, 1) }
            };
        }
    }
}
