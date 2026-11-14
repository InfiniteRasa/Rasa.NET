using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Fallen Soldiers (native mission 1263), Ligo Ashen Desert: Captain Akio Yamamoto at Boneyard Outpost wants the dog tags of
    /// his men who fell in the Boneyard, six of them, to give them a proper good bye.
    ///
    /// Native: texts 10136-10140 (missionconversation): log "Find the bodies of Fallen Soldiers in the Boneyard and recover their
    /// dog tags. Return the dog tags to Captain Yamamoto once you have collected 6 Ashen AFS Dog Tags.", offer "Some of my boys
    /// haven't come home yet ...", turn-in "Solemn, isn't it? ...", reminder "I know it's rough out there, and there are bodies
    /// everywhere ..."; objective (missionobjective) 1 "Recover Dog Tags from Fallen Soldiers" 10141/10142, no counter. No
    /// objective conversation. Item class 25443 Mis_Ligo_Ashen_BY_Dogtag "Ashen AFS Dog Tag" (template 97464). Usable name 63
    /// "Fallen Soldier" (usablenameoverride LIGO_ASHEN_FALLENSOLDIER). Name 9303 Yamamoto.
    ///
    /// Sourced: TaRapedia Fallen Soldiers: giver and reward giver Captain Aiko (client: Akio) Yamamoto (Boneyard Outpost); no
    /// requirement; 95,000 XP, 6,300 credits; one of ChiTech Laser Torqueshell Rifle, Vextronics Kinetic Staff, Vextronics
    /// Virulent Leech Gun, Teleract Incendiary Shotgun; "grab their dog tags from the Boneyard"; the fallen at (x, z) (483,
    /// -379), (520, -308), (507, -321), (546, -354), (519, -306).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (triage D5: the zone band).
    ///  - Six personal fallen AFS soldiers (the log's six tags): the client's human soldier corpses
    ///    UsableTreasureDispHumCorpseMale V01-V04 (24995, 7865, 24989, 24990; closed 200, opened 201; usabledata None: the
    ///    client sends 1), named "Fallen Soldier" (63), as Crucible's The Departed (1143). Five at TaRapedia's spots (the fourth
    ///    2.8 m north-west of (546, -354), where a player on 5 of the 16 places round it could not see it), the sixth (500,
    ///    -345) among them; each on the client's floor and the navmesh, seen from the 16 places 2 m round it (probe of
    ///    2026-11-09). One use gives one tag;
    ///    a hidden objective 2 (server only, 1's texts; the client gives 1 no counter text for a count of its own) counts the
    ///    six distinct uses (scene results, counter 1) and completes 1, as Marshes' Picking Up the Pieces (4695). The item
    ///    counter on 1 (n/6).
    ///    Yamamoto takes the tags at the turn-in; an abandon takes them and the fallen.
    ///  - One marker over the six (its centre and a radius that takes in all of them).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 40 (client entityclass names): Torqueshell
    ///    Rifle Laser 49616 (38-42), Staff Physical 49536 (40-44, "Kinetic"), Leech Gun Virulent 15301 (39-43), Shotgun Fire
    ///    14861 (37-41, "Incendiary"); one to choose.
    /// </summary>
    public static class AshenFallenSoldiersV1
    {
        public const uint MissionId = 1263;
        public const uint Level = 40;
        public const uint Yamamoto = AshenLeadInNpcsV1.Yamamoto;
        public const uint Collect = 1, Tally = 2;
        public const uint Tags = 6;
        public const uint Taken = 1;                    // the scenario of the fallen's uses: one scene result per body

        public const uint TagTemplate = 97464, TagClass = 25443;
        public const string TagKey = "ashen-afs-dog-tag";
        public static readonly uint[] CorpseClasses = { 24995, 7865, 24989, 24990 };   // UsableTreasureDispHumCorpseMaleV01-V04
        public const uint CorpseName = 63;              // usablenameoverride LIGO_ASHEN_FALLENSOLDIER "Fallen Soldier"
        public const uint UseArgument = 1;              // usabledata lookup[class][0] is None: the client sends 1
        public const uint ClosedState = 200, OpenedState = 201;     // USE_TD_STATE_CLOSED, USE_TD_STATE_OPENED

        /// <summary>The six fallen in the Boneyard (client floor heights): TaRapedia's five (the fourth 2.8 m off), then the sixth.</summary>
        public static readonly ScenePosition[] CorpsesAt =
        {
            new(483f, 286.56f, -379f), new(520f, 283.38f, -308f), new(507f, 282.89f, -321f), new(544f, 285.58f, -352f),
            new(519f, 283.51f, -306f), new(500f, 286.56f, -345f)
        };

        /// <summary>The marker over the fallen: their middle (the navmesh ground) and a radius that takes in all six.</summary>
        public static readonly (double X, double Y, double Z) FieldAt = (512.5, 284.05, -335.5);
        public const double FieldRadius = 55;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint LaserTorqueshell = 49616;     // Weapon_Avatar_Torqueshell_Rifle_Laser_UNC_38_to_42
        public const uint KineticStaff = 49536;         // Weapon_Avatar_Staff_Physical_UNC_40_to_44
        public const uint VirulentLeechGun = 15301;     // Weapon_Avatar_LeechGun_Virulent_UNC_39_to_43
        public const uint IncendiaryShotgun = 14861;    // Weapon_Avatar_Shotgun_v1_Fire_UNC_37_to_41

        public static string CorpseRole(int index) => $"boneyard-fallen-soldier-{index + 1}";
        public static uint CorpseSequence(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Fallen Soldiers", 10136, Yamamoto, Yamamoto, Level);
            mission.Reward(Experience, Credits, new[] { LaserTorqueshell, KineticStaff, VirulentLeechGun, IncendiaryShotgun });

            mission.Scenario(Taken, "An Ashen AFS Dog Tag taken from the fallen", (uint)CorpsesAt.Length);

            mission.Objective(Collect, 10141, 10142, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Tally, 10141, 10142, 1, MissionObjectiveState.Incomplete, required: false);    // hidden

            mission.Transition(Tally);
            for (var corpse = 1U; corpse <= CorpsesAt.Length; corpse++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, corpse, Tags, counter: Taken, trigger: corpse);
            mission.Transition(Collect);
            mission.ObjectiveState(Collect, Tally, MissionObjectiveState.Completed);
            mission.Action(Collect, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Collect, 1, FieldAt.X, FieldAt.Y, FieldAt.Z, FieldRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1263",
                "Native1 'Recover Dog Tags from Fallen Soldiers' 10141/10142, no counter; no objectiveconversation. Texts " +
                "10136-10140 (6 Ashen AFS Dog Tags from the Fallen Soldiers in the Boneyard). Item class25443 Ashen AFS Dog Tag " +
                "(template97464). Usable name 63 'Fallen Soldier'. Name 9303 Yamamoto.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Fallen_Soldiers",
                "Giver/reward giver Captain Aiko Yamamoto (Boneyard Outpost); no requirement; 95000XP, 6300 credits; ChiTech Laser " +
                "Torqueshell Rifle, Vextronics Kinetic Staff, Vextronics Virulent Leech Gun, Teleract Incendiary Shotgun; the " +
                "fallen at 483,-379; 520,-308; 507,-321; 546,-354; 519,-306.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenFallenSoldiersV1",
                "Level40 estimated. Six personal human corpses (24995, 7865, 24989, 24990; name 63; arg1; 200->201): TaR's five (the " +
                "fourth 2.8 m off, to be seen) and (500, -345); one use gives one tag; hidden 2 counts 6 distinct uses, completes 1; item counter on 1 (n/6); Yamamoto takes the " +
                "tags. One marker r55. Items 49616, 49536, 15301, 14861 (lowest Uncommon, band of level 40), one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own fallen in the Boneyard, each searched once for its tag.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(TagKey, TagTemplate, MissionItemScope.AssignmentIssued, Tags,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        CounterObjectiveId: Collect)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < CorpsesAt.Length; index++)
            {
                var role = CorpseRole(index);
                var sequence = CorpseSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, CorpseClasses[index % CorpseClasses.Length],
                    CorpsesAt[index], Orientation: index * 1.9, InitialObjectState: ClosedState, NameOverrideId: CorpseName,
                    UseAction: new SceneObjectAction(MissionId, Collect, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"{role}-searched", role, OpenedState),
                        new SetInteractionIntent($"{role}-done", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"{role}-dog-tag", MissionId, TagKey, TagTemplate, 1) },
                    Signals = { new SceneMissionSignal(MissionId, Taken, (uint)index + 1) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
