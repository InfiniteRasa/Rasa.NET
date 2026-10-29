using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Pools;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Death Patrol (native mission 1315), Valverde Pools: Supply Chief Riggins at the Snake Pit wants the dog tags and the
    /// empty ammunition clips of the AFS soldiers who fell in the Pools.
    ///
    /// Native: texts 10677-10681 (missionconversation): offer "if you come across any of our downed soldiers I need you
    /// to pull any empty ammo clips that are on the body", turn-in "As for the dog tags I'll just have to toss 'em in a box
    /// like the rest of them"; objectives 1 "Collect Dog Tags" 10717/10718, 2 "Collect Empty Ammunition Clips"
    /// 10719/10720 ("from fallen AFS Soldiers"), 3 "Dropping the loot" 12513/12514 (empty body text), no counters
    /// (missionobjective). Items: Dog Tags class 25572 (template 97508), Empty Ammo Clip 25573 (97509). The client's
    /// usable name overrides name the soldiers (PRIVATE_KROH, SERGEANT_VITA, ...), but its map places no body.
    ///
    /// Sourced: TaRapedia Death Patrol: giver and receiver Supply Chief Riggins (The Snakepit); 8 dog tags, 16 clips;
    /// 55,000 XP, 5,000 credits, one of Vextronics Photonic Blade lvl 31, Astra Radial Healing Disc lvl 35, AccuMax Sonic
    /// Pistol lvl 35, Pulsar Pulse Staff lvl 35; no requirement; follow-ups The Thirsty Hermit and The Supply Master; the
    /// places of 22 named bodies (<see cref="Bodies"/>). TR MMO DB: level 33. PlanetTR (Dec 2007): a body gives only a few
    /// clips.
    ///
    /// Reconstructed (the user's choices of 2026-10-25, ESTIMATE): 22 personal bodies at TaRapedia's places, each a
    /// human corpse model (PropHumMCorpseV01-V04, no use) with a soldier's kit bag on it, class 11096
    /// UsableItemDisp_item_bag_hum_v01 (an item dispenser, mesh 17714 as the satchels of 930 and 965 the client draws). No
    /// item-dispenser class has a body model: the first choice, 7654 UsableItemDispGuardPromCorpse, has mesh 776, which
    /// the client draws as its "Default" pyramid and will not use (to-fix Pools #3). A use of the bag takes the server's
    /// 10 s object use, gives one clip and closes it; usable while objective 2 is open. A body also gives a dog tag by chance while the player has fewer than 8 (PoolsFallenSoldiersScene), so the
    /// player need not find every body. The client has no counter texts, so two hidden tallies (objectives
    /// <see cref="TagTally"/> and <see cref="ClipTally"/>, 1's and 2's texts) count the tags found (scenario 2) to 8 and
    /// the bodies searched (scenario 1) to 16, any of the 22; 1 and 2 complete with them. Kling's and Brown's heights are TaRapedia's plus 200 m (663 for 863, the navmesh there);
    /// every other height is TaRapedia's to the navmesh. Objective 3 "Dropping the loot" (no text in any source) is left
    /// out: Riggins takes the tags and clips at the turn-in (they go with the mission). The weapons as the Uncommon
    /// templates of those kinds and bands (the manufacturer names are not in the client tables); "Radial" read as the
    /// area healing disc. The bodies lie all over the west of the Pools: a marker on each, for 1 and for 2.
    /// </summary>
    public static class PoolsDeathPatrolV1
    {
        public const uint MissionId = 1315;
        public const uint Level = 33;
        public const uint Riggins = PoolsSnakePitNpcsV1.Riggins;
        public const uint Tags = 1, Clips = 2;
        public const uint TagTally = 4, ClipTally = 5;    // hidden: tags found to 8, bodies searched to 16
        public const uint Searched = 1, TagsFound = 2;    // scenarios: event n is body n

        public const uint TagTemplate = 97508, TagClass = 25572, TagCount = PoolsFallenSoldiersScene.Tags;
        public const uint ClipTemplate = 97509, ClipClass = 25573, ClipCount = PoolsFallenSoldiersScene.Clips;
        public const string TagKey = "dog-tags", ClipKey = "empty-ammo-clip";
        public const uint KitClass = 11096;             // UsableItemDisp_item_bag_hum_v01, the use
        public static readonly uint[] BodyClasses = { 20654, 20655, 20656, 20657 };    // PropHumMCorpseV01..V04, no use
        public const double BodyRadius = 8;
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;

        /// <summary>TaRapedia's 22 fallen soldiers, on the navmesh.</summary>
        public static readonly (string Name, ScenePosition At)[] Bodies =
        {
            ("Corporal Hayden", new(-602.8f, 918.7f, 599.1f)),
            ("Private Kroh", new(-515.1f, 874.1f, 512f)),
            ("Sergeant Tracey", new(-386, 862.9f, 483)),
            ("Lieutenant Kling", new(-270.5f, 864f, 467f)),
            ("Sergeant Evers", new(-293, 863.3f, 454)),
            ("Lieutenant Kahoe", new(-545, 873f, 412)),
            ("Private Spongberg", new(-442.9f, 889.4f, 386.5f)),
            ("Sergeant Pinella", new(52f, 857.9f, 383f)),
            ("Sergeant Sweeney", new(-469.6f, 891.1f, 376.4f)),
            ("Private Brown", new(-677.8f, 863.1f, 354.3f)),
            ("Private Taylor", new(-288, 856.5f, 340)),
            ("Corporal Sheckard", new(-102, 851.9f, 334)),
            ("Lieutenant Alton", new(-82, 845.5f, 301)),
            ("Private Fraser", new(-446.2f, 906.7f, 290.8f)),
            ("Private Reulbach", new(-710.3f, 852.1f, 251f)),
            ("Corporal Hofman", new(-400f, 909.2f, 251f)),
            ("Captain Overall", new(-558.4f, 844.2f, 249.1f)),
            ("Lieutenant Hardy", new(-562f, 831.8f, 213.2f)),
            ("Private Mack", new(-300.9f, 798.8f, 206.7f)),
            ("Lieutenant Moran", new(-604.4f, 776.4f, -6.1f)),
            ("Private Lundgren", new(-501.1f, 777.3f, -11.6f)),
            ("Sergeant Vita", new(-469.4f, 676.3f, -344.1f)),
        };

        public const uint Experience = 55000;
        public const uint Credits = 5000;
        public const uint PhotonicBlade = 47668;        // Weapon_Avatar_Blade_Laser_UNC_31_to_35 "Photonic Blade"
        public const uint AreaHealingDisc = 120030;     // Tool_Avatar_Healing_Disc_Area_UNC_35_to_39
        public const uint SonicPistol = 14444;          // Weapon_Avatar_Pistol_Sonic_UNC_35_to_39
        public const uint PulseStaff = 15216;           // Weapon_Avatar_Staff_EMP_UNC_35_to_39 "Pulse Staff"

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Death Patrol", 10677, Riggins, Riggins, Level);
            mission.Reward(Experience, Credits, new[] { PhotonicBlade, AreaHealingDisc, SonicPistol, PulseStaff });
            Collection(mission, 10717, 10718, 10719, 10720, TagCount, ClipCount, (uint)Bodies.Length);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1315",
                "Native1 'Collect Dog Tags' 10717/10718, 2 'Collect Empty Ammunition Clips' 10719/10720, 3 'Dropping the loot' " +
                "12513/12514 (no body text), no counters. Texts 10677-10681. Dog Tags class25572/template97508, Empty Ammo Clip " +
                "25573/97509. Usable name overrides name the soldiers; no body on the map.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Death_Patrol",
                "Giver/receiver Supply Chief Riggins (The Snakepit); 8 dog tags, 16 clips; 55000XP, 5000 credits; one of Vextronics " +
                "Photonic Blade lvl31, Astra Radial Healing Disc lvl35, AccuMax Sonic Pistol lvl35, Pulsar Pulse Staff lvl35; the " +
                "places of 22 bodies. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsDeathPatrolV1",
                "22 personal bodies (a PropHumMCorpse model and a usable kit bag class11096 on it; class7654 has the client's Default mesh) at TaRapedia's places on the navmesh (Kling and Brown +200 m); one clip a body, a " +
                "tag by chance until 8 (scene pools.fallen-soldiers), usable while 2 is open; hidden tallies 4 (tags, to 8) and 5 " +
                "(bodies, any 16 of 22); objective3 left out; the tags and clips go with the mission. Items 47668, 120030, " +
                "14444, 15216 (Uncommon of each kind). A marker on each body for 1 and 2.", 0.3);

            Markers(mission, Bodies);

            mission.Enable(Scene(MissionId, Clips, TagKey, ClipKey, TagCount, ClipCount, Bodies));
        }

        /// <summary>
        /// Objectives 1 (tags) and 2 (clips), each done when its hidden tally counts <paramref name="tags"/> tags found or
        /// <paramref name="clips"/> bodies searched, any of <paramref name="bodies"/>; the reward on 1, as Building Materials.
        /// </summary>
        internal static void Collection(PoolsMissionDataV1 mission, uint tagName, uint tagBody, uint clipName, uint clipBody,
            uint tags, uint clips, uint bodies)
        {
            mission.Objective(Tags, tagName, tagBody, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Clips, clipName, clipBody, 1, MissionObjectiveState.Incomplete);
            mission.Objective(TagTally, tagName, tagBody, 2, MissionObjectiveState.Incomplete);
            mission.Objective(ClipTally, clipName, clipBody, 3, MissionObjectiveState.Incomplete);
            mission.Scenario(Searched, "Fallen AFS soldiers searched", bodies);
            mission.Scenario(TagsFound, "Dog tags found", bodies);
            mission.Transition(Tags);
            mission.ObjectiveState(Tags, TagTally, MissionObjectiveState.Completed);
            mission.Action(Tags, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Clips);
            mission.ObjectiveState(Clips, ClipTally, MissionObjectiveState.Completed);
            mission.Transition(TagTally);
            mission.Transition(ClipTally);
            for (var id = 1U; id <= bodies; id++)
            {
                mission.Progress(TagTally, MissionProgressEventKind.ScenarioEvent, id, tags, counter: TagsFound, trigger: id);
                mission.Progress(ClipTally, MissionProgressEventKind.ScenarioEvent, id, clips, counter: Searched, trigger: id);
            }
        }

        /// <summary>A marker on each of <paramref name="bodies"/> for 1 (tags) and for 2 (clips).</summary>
        internal static void Markers(PoolsMissionDataV1 mission, (string Name, ScenePosition At)[] bodies)
        {
            for (var index = 0; index < bodies.Length; index++)
            {
                var at = bodies[index].At;
                mission.Indicator(Tags, 1 + (uint)index, at.X, at.Y, at.Z, BodyRadius);
                mission.Indicator(Clips, 1 + (uint)(bodies.Length + index), at.X, at.Y, at.Z, BodyRadius);
            }
        }

        /// <summary>
        /// The bodies as the player's own scene objects: each gives one clip and closes, and its tag sequence
        /// (PoolsFallenSoldiersScene.TagFrom + n) the tag the scene draws; usable while <paramref name="useObjective"/> is
        /// open. The tags and clips go with the mission. Each body is its kit bag (the use) on a corpse model that takes no
        /// target, so the click goes to the bag.
        /// </summary>
        internal static MissionSceneDefinition Scene(uint missionId, uint useObjective, string tagKey, string clipKey,
            uint tags, uint clips, (string Name, ScenePosition At)[] bodies)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "pools.fallen-soldiers",
                HiddenObjectiveIds = new() { TagTally, ClipTally },
                Items = new()
                {
                    new MissionItemBinding(tagKey, TagTemplate, MissionItemScope.AssignmentIssued, tags,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(clipKey, ClipTemplate, MissionItemScope.AssignmentIssued, clips,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < bodies.Length; index++)
            {
                var id = (uint)index + 1;
                var role = $"body-{id}";
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, KitClass, bodies[index].At,
                    Orientation: index * 0.6283185307, InitialObjectState: ReadyState,
                    UseAction: new SceneObjectAction(missionId, useObjective, id, UseArgument));
                scene.Actors[$"corpse-{id}"] = new SceneActorDefinition($"corpse-{id}", SceneActorKind.Object,
                    BodyClasses[index % BodyClasses.Length], bodies[index].At, Orientation: index * 0.6283185307,
                    InitiallyInteractable: false);
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-corpse-{id}", $"corpse-{id}"));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"search-{role}", role, TakenState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Character = { new IssueMissionItemIntent($"clip-from-{role}", missionId, clipKey, ClipTemplate, 1) },
                    Signals = { new SceneMissionSignal(missionId, Searched, id) }
                };
                scene.Sequences[PoolsFallenSoldiersScene.TagFrom + id] = new SceneSequenceDefinition
                {
                    Character = { new IssueMissionItemIntent($"tag-from-{role}", missionId, tagKey, TagTemplate, 1) },
                    Signals = { new SceneMissionSignal(missionId, TagsFound, id) }
                };
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
