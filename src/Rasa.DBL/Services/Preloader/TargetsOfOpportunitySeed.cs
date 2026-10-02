using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;

namespace Rasa.Services.Preloader
{
    using Data;
    using Structures.World;

    /// <summary>
    /// The Targets of Opportunity missions of the fifteen battlefields, as far as their kill
    /// counts go: the rows of Add_targets_of_opportunity, inserted as SQL.
    ///
    /// The client has one such mission per battlefield (missionconversation, "Wilderness Targets
    /// of Opportunity" and the rest), and its objectives are where the kill titles come from:
    /// objective 3 of 1449 is "Kill 40 Xanx!", its body "Bug Zapper: Kill 40 Xanx on Wilderness.",
    /// and title 365 is "Wilderness Bug Zapper", "Killed 40 Xanx on Wilderness." The client
    /// holds the texts and the counter's caption; what counts, where, how many and which title
    /// was the server's.
    ///
    /// Each mission here is:
    ///  - its "Complete All N Targets of Opportunity" objective, the one required objective.
    ///    Nothing completes it yet - the waypoint, Logos, boss, operation and story objectives it
    ///    counts are not authored - so the mission stays in the log, as it did;
    ///  - its kill objectives, optional, each a counter (counter 0, the objective's own counter
    ///    caption) from 0 to the number its name gives, fed by the kill of any creature whose
    ///    class carries one of the creature flags named - its species - or, for the Warden and
    ///    Reconstructor bots, which carry none, whose class is one of those named;
    ///  - in its scene binding: the title each kill objective gives, the maps a kill counts on
    ///    (the battlefield and the instances off it), squad credit within
    ///    <see cref="SquadRadius"/>, and the battlefield's mission category;
    ///  - offered by radio to a character arriving on the battlefield
    ///    (MissionOfferSourceDefinition.MapArrivalKey), and completed by radio. In retail an
    ///    officer of the battlefield's first base gave it; they are not all in the world.
    ///
    /// The count is the mission's, where the title's description differs from it ("Kill 40
    /// Linkers", "Killed 50 Linkers"): it is what the log shows.
    ///
    /// Sixty-three of the client's sixty-nine kill titles. Fourteen of them are of creatures
    /// that stand nowhere in their battlefield yet, and count from the day they do. Not here:
    /// five whose creature does not exist (the Maligo Elite, Smart Mines, Berserk Thraxus
    /// Machina, Infected Foreans, Thrax Scavengers), and Palisades Stalker Killer, whose
    /// objective is in the earlier Palisades mission (1630) only.
    ///
    /// The missions are optional content: one a database cannot stand up - a battlefield whose
    /// map it has not got - is left inactive, and the rest run.
    /// </summary>
    public static class TargetsOfOpportunitySeed
    {
        public const string Revision = "targets_1";

        /// <summary>How far from the kill a squad member shares it: PartyManager.LootShareRange.</summary>
        public const float SquadRadius = 200f;

        /// <summary>The creature flags the kill objectives name (generated/client/constant/creatureflag, SPECIES_*).</summary>
        public static class Flag
        {
            public const uint Kael = 60;
            public const uint Thrax = 62;
            public const uint Caretaker = 63;
            public const uint Stalker = 64;
            public const uint Strider = 65;
            public const uint Linker = 67;
            public const uint Miasma = 68;
            public const uint Mox = 69;
            public const uint Amoeboid = 71;
            public const uint Fithik = 72;
            public const uint Machina = 73;
            public const uint Xanx = 74;
            public const uint Treeback = 75;
            public const uint ShieldDrone = 77;
            public const uint Hunter = 79;
            public const uint Howler = 80;
            public const uint Nitroglazer = 81;
            public const uint Atta = 82;
            public const uint BeamManta = 83;
            public const uint BarbTick = 84;
            public const uint FlareGasher = 85;
            public const uint Lasher = 86;
            public const uint Warnet = 88;
            public const uint Filcher = 89;
            public const uint Predator = 93;
            public const uint Juggernaut = 94;
            public const uint Granitour = 100;
            public const uint Maw = 101;
        }

        /// <summary>One kill objective: the client's objective and texts, the count, the title, and what is counted.</summary>
        public sealed class Kill
        {
            public uint ObjectiveId { get; }
            public uint NameTextId { get; }
            public uint BodyTextId { get; }
            public uint CounterTextId { get; }
            public uint Count { get; }
            public uint TitleId { get; }
            public string Title { get; }

            /// <summary>CreatureFlagKilled or CreatureClassKilled: what the subjects are.</summary>
            public MissionProgressEventKind EventKind { get; }
            public IReadOnlyList<uint> Subjects { get; }

            internal Kill(uint objectiveId, uint nameTextId, uint bodyTextId, uint counterTextId, uint count, uint titleId, string title,
                MissionProgressEventKind eventKind, uint[] subjects)
            {
                ObjectiveId = objectiveId;
                NameTextId = nameTextId;
                BodyTextId = bodyTextId;
                CounterTextId = counterTextId;
                Count = count;
                TitleId = titleId;
                Title = title;
                EventKind = eventKind;
                Subjects = subjects;
            }
        }

        /// <summary>One battlefield's mission.</summary>
        public sealed class Zone
        {
            public uint MissionId { get; }
            public string Name { get; }
            public uint NameTextId { get; }

            /// <summary>The lowest level of the Bane and the wildlife the battlefield's pools set down.</summary>
            public uint Level { get; }

            /// <summary>missioncategorylanguage: "Battlefield (Wilderness)" and the rest.</summary>
            public uint Category { get; }
            public uint MapContextId { get; }

            /// <summary>The battlefield and the instances off it: where a kill counts.</summary>
            public IReadOnlyList<uint> Maps { get; }

            /// <summary>"Complete All N Targets of Opportunity".</summary>
            public uint AllObjectiveId { get; }
            public uint AllNameTextId { get; }
            public uint AllBodyTextId { get; }
            public IReadOnlyList<Kill> Kills { get; }

            internal Zone(uint missionId, string name, uint nameTextId, uint level, uint category, uint mapContextId, uint[] maps,
                uint allObjectiveId, uint allNameTextId, uint allBodyTextId, params Kill[] kills)
            {
                MissionId = missionId;
                Name = name;
                NameTextId = nameTextId;
                Level = level;
                Category = category;
                MapContextId = mapContextId;
                Maps = maps;
                AllObjectiveId = allObjectiveId;
                AllNameTextId = allNameTextId;
                AllBodyTextId = allBodyTextId;
                Kills = kills;
            }
        }

        private static Kill Flags(uint objectiveId, uint nameTextId, uint bodyTextId, uint counterTextId, uint count, uint titleId, string title,
            params uint[] flags) =>
            new Kill(objectiveId, nameTextId, bodyTextId, counterTextId, count, titleId, title, MissionProgressEventKind.CreatureFlagKilled, flags);

        private static Kill Classes(uint objectiveId, uint nameTextId, uint bodyTextId, uint counterTextId, uint count, uint titleId, string title,
            params uint[] classes) =>
            new Kill(objectiveId, nameTextId, bodyTextId, counterTextId, count, titleId, title, MissionProgressEventKind.CreatureClassKilled, classes);

        // Mission, battlefield, name text, level, category, map, the maps a kill counts on,
        // then the "complete all" objective with its name and body texts, then the kills:
        // objective, name, body and counter texts, count, title id and name, what is counted.
        // The bots' classes: Creature_Prison_Bot_Reconstructor and _Warden (6960, 6961) and
        // their bosses (10314, 10315).
        public static readonly Zone[] Zones =
        {
            new Zone(1449, "Wilderness", 12764, 2, 10000044, 1220, new uint[] { 1220, 1416, 1430, 1506, 1721, 2368 },
                58, 17853, 17854,
                Flags(3, 12773, 12774, 12805, 40, 365, "Wilderness Bug Zapper", Flag.Xanx),
                Flags(4, 12775, 12776, 12808, 30, 366, "Wilderness Bot Stomper", Flag.ShieldDrone),
                Flags(5, 12771, 12772, 12802, 200, 363, "Wilderness Thrax Hunter", Flag.Thrax),
                Flags(7, 12779, 12780, 12824, 40, 369, "Wilderness Exterminator", Flag.Miasma)),
            new Zone(1582, "Divide", 14564, 15, 10000033, 1148, new uint[] { 1148, 1347, 1348, 1349, 1806 },
                69, 18060, 18061,
                Flags(3, 14577, 14578, 14579, 40, 464, "Divide Exterminator", Flag.Xanx),
                Flags(5, 14571, 14572, 14573, 200, 461, "Divide Thrax Hunter", Flag.Thrax),
                Flags(56, 15134, 15135, 15136, 20, 463, "Divide Reanimator", Flag.Machina),
                Flags(57, 15137, 15138, 15139, 50, 460, "Divide Blobstomper", Flag.Amoeboid)),
            new Zone(1809, "Palisades", 18219, 23, 10000039, 1244, new uint[] { 1244, 1384, 1394, 1397, 1803 },
                81, 18224, 18225,
                Flags(62, 18229, 18230, 18231, 200, 511, "Palisades Exterminator", Flag.Fithik),
                Flags(63, 18232, 18233, 18234, 40, 500, "Palisades Protector", Flag.Hunter)),
            new Zone(1586, "Plateau", 14822, 27, 10000056, 1497, new uint[] { 1497, 1502, 1823, 1830, 2029 },
                69, 17863, 17864,
                Flags(56, 14833, 14834, 14835, 30, 405, "Plateau Giant Killer", Flag.Kael),
                Flags(57, 14841, 14842, 14843, 40, 409, "Plateau Exterminator", Flag.Mox)),
            new Zone(1594, "Pools", 15167, 30, 10000061, 1304, new uint[] { 1304, 1465, 1694, 1763 },
                74, 17859, 17860,
                Flags(5, 15179, 15180, 15181, 200, 470, "Pools Thrax Killer", Flag.Thrax),
                Flags(56, 15182, 15183, 15184, 40, 471, "Pools Exterminator", Flag.Howler),
                Flags(57, 15188, 15189, 15190, 10, 472, "Pools Strider Killer", Flag.Strider)),
            new Zone(1583, "Marshes", 14640, 33, 10000051, 1454, new uint[] { 1454, 1429, 1451, 1700, 1743 },
                74, 18067, 18068,
                Flags(5, 14649, 14650, 14651, 300, 739, "Marshes Thrax Hunter", Flag.Thrax),
                Flags(56, 15528, 15529, 15530, 30, 494, "Marshes Vegetarian", Flag.Maw),
                Flags(57, 15531, 15532, 15533, 50, 495, "Marshes Mox Hunter", Flag.Mox),
                Flags(58, 15534, 15535, 15536, 10, 496, "Marshes Giant Killer", Flag.Stalker, Flag.Strider)),
            new Zone(1631, "Descent", 15563, 35, 10000050, 2047, new uint[] { 2047, 2156, 2163 },
                83, 18065, 18066,
                Flags(5, 15594, 15595, 15596, 300, 503, "Descent Thrax Hunter", Flag.Thrax),
                Flags(69, 15600, 15601, 15602, 100, 505, "Descent Zapper", Flag.Linker),   // none of them stand in the zone yet
                Flags(79, 15597, 15598, 15599, 50, 504, "Descent Mox Master", Flag.Mox),   // none of them stand in the zone yet
                Flags(80, 15603, 15604, 15605, 30, 506, "Descent Logger", Flag.Treeback)),   // none of them stand in the zone yet
            new Zone(1829, "Plains", 18600, 35, 10000028, 1764, new uint[] { 1764, 1773, 2034, 2093 },
                71, 18605, 18606,
                Flags(56, 18607, 18608, 18609, 100, 483, "The Extinguisher", Flag.FlareGasher),
                Flags(57, 18610, 18611, 18612, 100, 484, "Manta Raider", Flag.BeamManta),
                Flags(58, 18613, 18614, 18615, 10, 485, "The Stridernator", Flag.Strider),   // none of them stand in the zone yet
                Flags(60, 18625, 18626, 18627, 300, 481, "The Cleaner", Flag.Thrax),
                Flags(66, 18616, 18617, 18618, 10, 488, "Soldier of Prey", Flag.Predator),   // none of them stand in the zone yet
                Flags(68, 18622, 18623, 18624, 50, 490, "The Coroner", Flag.Caretaker),
                Flags(76, 19733, 19734, 19735, 25, 489, "The Diffuser", Flag.ShieldDrone)),
            new Zone(1585, "Mires", 14710, 36, 10000026, 1759, new uint[] { 1759, 2107, 2115, 2125 },
                72, 18057, 18058,
                Flags(68, 16922, 16923, 16924, 10, 729, "Mires Manta Hunter", Flag.BeamManta),
                Flags(69, 16925, 16926, 16927, 20, 730, "Mires Exterminator", Flag.BarbTick),
                Flags(70, 16928, 16929, 16930, 10, 732, "Mires Extinguisher", Flag.FlareGasher)),
            new Zone(1739, "Incline", 16816, 37, 10000023, 1761, new uint[] { 1761, 1865, 2085, 2111 },
                77, 18400, 18401,
                Flags(56, 16821, 16822, 16823, 100, 639, "The Suppressor", Flag.FlareGasher),
                Flags(60, 16839, 16840, 16841, 300, 645, "The Reaper", Flag.Thrax),
                Flags(66, 16830, 16831, 16832, 15, 642, "Raptor", Flag.Predator),
                Flags(67, 16833, 16834, 16835, 50, 643, "Icebreaker", Flag.Nitroglazer),   // none of them stand in the zone yet
                Flags(68, 16836, 16837, 16838, 50, 644, "The Surgeon", Flag.Caretaker),
                Flags(71, 16886, 16887, 16888, 50, 640, "Hammer of Incline", Flag.Lasher),   // none of them stand in the zone yet
                Flags(72, 16889, 16890, 16891, 20, 641, "AFS Titan", Flag.Stalker)),   // none of them stand in the zone yet
            new Zone(1580, "Ashen Desert", 14552, 39, 10000016, 1734, new uint[] { 1734, 1988, 2055, 2110 },
                24, 17855, 17856,
                Flags(2, 14864, 14865, 14866, 40, 432, "Desert Wraith", Flag.Linker),
                Flags(3, 14867, 14868, 14869, 5, 433, "Desert Raider", Flag.Stalker, Flag.Predator, Flag.Juggernaut),   // none of them stand in the zone yet
                Flags(4, 14870, 14871, 14872, 50, 431, "Desert Exterminator", Flag.Atta)),
            new Zone(1763, "Thunderhead", 17515, 40, 10000021, 1911, new uint[] { 1911, 2103, 2105, 2112 },
                22, 18387, 18388,
                Flags(2, 17530, 17531, 17535, 50, 770, "Thunderhead Exterminator", Flag.Atta),
                Flags(3, 17532, 17533, 17534, 200, 771, "Thunderhead Warrior", Flag.Thrax),
                Flags(4, 17549, 17550, 17551, 40, 772, "Thunderhead Bootstomper", Flag.BarbTick),
                Flags(5, 17552, 17553, 17554, 30, 773, "Thunderhead Swatter", Flag.BeamManta),
                Flags(6, 17555, 17556, 17557, 30, 774, "Thunderhead Fireman", Flag.FlareGasher),
                Flags(7, 17558, 17559, 17560, 5, 775, "Thunderhead Hammerhead", Flag.Juggernaut),   // none of them stand in the zone yet
                Flags(8, 17561, 17562, 17563, 40, 776, "Thunderhead Whip", Flag.Lasher),   // none of them stand in the zone yet
                Classes(9, 17564, 17565, 17566, 40, 777, "Thunderhead Tinker", 6960, 6961, 10314, 10315),
                Flags(10, 17567, 17568, 17569, 25, 778, "Thunderhead Webbreaker", Flag.Xanx)),
            new Zone(1593, "Abyss", 15095, 41, 10000022, 2028, new uint[] { 2028, 2155, 2190, 2203 },
                96, 17980, 17981,
                Flags(80, 15693, 15694, 15695, 50, 452, "Abyss Link Breaker", Flag.Linker),
                Flags(81, 15696, 15697, 15698, 40, 453, "Abyss Exterminator", Flag.Granitour)),
            new Zone(1589, "Crucible", 14937, 42, 1, 1993, new uint[] { 1993, 1977, 2138, 2141, 2146 },
                81, 17978, 17979,
                Flags(69, 14946, 14947, 14948, 40, 416, "Crucible Beast Master", Flag.FlareGasher),
                Classes(71, 14952, 14953, 14954, 50, 418, "Crucible Terminator", 6960, 6961, 10314, 10315),
                Flags(78, 15172, 15173, 15174, 40, 421, "Crucible Exterminator", Flag.Lasher)),   // none of them stand in the zone yet
            new Zone(1752, "Howling Maw", 17198, 43, 10000049, 2051, new uint[] { 2051, 2136, 2162 },
                77, 17889, 17890,
                Flags(58, 17251, 17252, 17253, 25, 722, "Giant Slayer", Flag.Strider),   // none of them stand in the zone yet
                Flags(60, 17263, 17264, 17265, 300, 726, "The Dreadnaught", Flag.Thrax),
                Flags(71, 17290, 17291, 17292, 50, 721, "Filcher Foiler", Flag.Filcher),
                Flags(72, 17293, 17294, 17295, 100, 720, "Bug Zapper", Flag.Warnet),
                Flags(73, 17296, 17297, 17298, 50, 723, "Howling Shocker", Flag.Linker),   // none of them stand in the zone yet
                Flags(75, 17302, 17303, 17304, 50, 725, "Living Goo Gun", Flag.Miasma))
        };

        public static uint FirstMissionId => Zones.Min(zone => zone.MissionId);

        /// <summary>The scene binding of a battlefield's mission: no script, the kills' titles, maps and squad credit.</summary>
        public static MissionSceneDefinition Scene(Zone zone) => new MissionSceneDefinition
        {
            Category = zone.Category > byte.MaxValue ? zone.Category : (uint?)null,
            Titles = zone.Kills.ToDictionary(kill => kill.ObjectiveId, kill => kill.TitleId),
            Credit = zone.Kills.ToDictionary(kill => kill.ObjectiveId,
                kill => new MissionCreditPolicy(MissionCreditMode.NearbyParty, SquadRadius)),
            ObjectiveRequirements = zone.Kills.ToDictionary(kill => kill.ObjectiveId,
                kill => (MissionRequirement)new MapRequirement(zone.Maps))
        };

        /// <summary>The radio source of a battlefield's mission: offered on arriving on its map.</summary>
        public static string RadioSources(Zone zone) => JsonSerializer.Serialize(new[]
        {
            new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, MissionOfferSourceDefinition.MapArrivalKey, zone.MapContextId)
        });

        private const string ChannelPolicyTable = "mission_channel_policy";
        private const string SceneBindingTable = "mission_scene_binding";

        private const byte Required = (byte)MissionContentRequirement.Required;
        private const byte Optional = (byte)MissionContentRequirement.Optional;
        private const byte Incomplete = 1;      // MissionObjectiveState
        private const byte Completed = 2;
        private const byte Radio = (byte)MissionChannel.Radio;

        private static readonly string[] DefinitionColumns =
        {
            "mission_id", "content_revision", "enabled", "requirement", "abandonment_policy", "client_name_text_id", "giver_id", "receiver_id",
            "level", "group_type", "category_id", "shareable", "radio_completeable", "comment"
        };

        private static readonly string[] ObjectiveColumns =
        {
            "mission_id", "content_revision", "objective_id", "requirement", "client_name_text_id", "client_body_text_id",
            "client_counter_0_text_id", "client_counter_1_text_id", "client_counter_2_text_id", "ordinal", "initial_state", "is_required", "comment"
        };

        private static readonly string[] TransitionColumns =
        {
            "mission_id", "content_revision", "objective_id", "transition_id", "requirement", "sequence", "from_state", "to_state", "comment"
        };

        private static readonly string[] TriggerColumns =
        {
            "mission_id", "content_revision", "objective_id", "transition_id", "trigger_id", "requirement", "kind", "sequence",
            "event_kind", "subject_id", "counter_id", "initial_value", "target_value", "comment"
        };

        private static readonly string[] ActionColumns =
        {
            "mission_id", "content_revision", "objective_id", "transition_id", "action_id", "requirement", "kind", "sequence",
            "target_objective_id", "objective_state", "comment"
        };

        private static readonly string[] ChannelColumns =
        {
            "mission_id", "content_revision", "acceptance_channel", "completion_channel", "radio_sources"
        };

        private static readonly string[] SceneColumns =
        {
            "mission_id", "content_revision", "script_key", "state_version", "bindings"
        };

        private static readonly string[] EvidenceColumns =
        {
            "mission_id", "content_revision", "evidence_id", "owner_kind", "owner_id", "source_kind", "source_uri", "local_client_path",
            "confidence", "reconstruction_note"
        };

        /// <summary>The definition rows: optional content, abandonable, solo, given and completed by radio.</summary>
        public static object[][] Definitions => Zones.Select(zone => new object[]
        {
            zone.MissionId, Revision, 1, Optional, (byte)MissionAbandonmentPolicy.Allowed, zone.NameTextId, null, null,
            zone.Level, (byte)1, (byte)(zone.Category > byte.MaxValue ? 0 : zone.Category), 0, 1, $"{zone.Name} Targets of Opportunity"
        }).ToArray();

        public static object[][] Objectives => Zones.SelectMany(zone => new[]
            {
                new object[]
                {
                    zone.MissionId, Revision, zone.AllObjectiveId, Required, zone.AllNameTextId, zone.AllBodyTextId,
                    null, null, null, 1u, Incomplete, 1, "Complete all Targets of Opportunity"
                }
            }
            .Concat(zone.Kills.Select((kill, index) => new object[]
            {
                zone.MissionId, Revision, kill.ObjectiveId, Required, kill.NameTextId, kill.BodyTextId,
                kill.CounterTextId, null, null, (uint)index + 2, Incomplete, 0, kill.Title
            }))).ToArray();

        public static object[][] Transitions => Zones.SelectMany(zone => zone.Kills.Select(kill => new object[]
        {
            zone.MissionId, Revision, kill.ObjectiveId, 1u, Required, 1u, Incomplete, Completed, $"{kill.Count} killed"
        })).ToArray();

        /// <summary>One progress trigger a subject, all of an objective on its one counter.</summary>
        public static object[][] Triggers => Zones.SelectMany(zone => zone.Kills.SelectMany(kill => kill.Subjects.Select((subject, index) => new object[]
        {
            zone.MissionId, Revision, kill.ObjectiveId, 1u, (uint)index + 1, Required, (byte)MissionTriggerKind.ProgressEvent, (uint)index + 1,
            (byte)kill.EventKind, subject, 0u, 0u, kill.Count,
            kill.EventKind == MissionProgressEventKind.CreatureFlagKilled ? $"Kill of creature flag {subject}" : $"Kill of creature class {subject}"
        }))).ToArray();

        public static object[][] Actions => Zones.SelectMany(zone => zone.Kills.Select(kill => new object[]
        {
            zone.MissionId, Revision, kill.ObjectiveId, 1u, 1u, Required, (byte)MissionActionKind.CompleteObjective, 1u,
            kill.ObjectiveId, Completed, $"Complete {kill.Title}"
        })).ToArray();

        public static object[][] Channels => Zones.Select(zone => new object[]
        {
            zone.MissionId, Revision, Radio, Radio, RadioSources(zone)
        }).ToArray();

        public static object[][] Scenes => Zones.Select(zone =>
        {
            var scene = Scene(zone);

            return new object[]
            {
                zone.MissionId, Revision, scene.Script, scene.StateVersion, JsonSerializer.Serialize(scene, MissionContentCodec.Options)
            };
        }).ToArray();

        /// <summary>Where each mission's rows come from: the client's tables, and what was the server's to decide.</summary>
        public static object[][] Evidence => Zones.SelectMany(zone => new[]
        {
            new object[]
            {
                zone.MissionId, Revision, 1u, (byte)MissionEvidenceOwnerKind.Mission, zone.MissionId, (byte)MissionEvidenceSourceKind.Client,
                null, "generated/client/missionconversation.pyo, missionobjective.pyo, titledata.pyo", 1.0,
                "the mission, its objectives with their texts and counter captions, and the titles the objective bodies name"
            },
            new object[]
            {
                zone.MissionId, Revision, 2u, (byte)MissionEvidenceOwnerKind.Mission, zone.MissionId, (byte)MissionEvidenceSourceKind.Reconstruction,
                null, "generated/client/constant/creatureflag.pyo", 0.7,
                "kills counted by species flag from the objective's wording, on the battlefield and its instances, shared with the squad"
            },
            new object[]
            {
                zone.MissionId, Revision, 3u, (byte)MissionEvidenceOwnerKind.Mission, zone.MissionId, (byte)MissionEvidenceSourceKind.Reconstruction,
                null, "no client source: who gives a mission is the server's", 0.3,
                "offered by radio on arriving on the battlefield; retail gave it from an officer at its first base"
            }
        }).ToArray();

        /// <summary>Every insert, in order; each is plain SQL either provider takes.</summary>
        public static IEnumerable<string> InsertStatements
        {
            get
            {
                yield return Insert(MissionContentDefinitionEntry.TableName, DefinitionColumns, Definitions);
                yield return Insert(MissionObjectiveDefinitionEntry.TableName, ObjectiveColumns, Objectives);
                yield return Insert(MissionObjectiveTransitionEntry.TableName, TransitionColumns, Transitions);
                yield return Insert(MissionTriggerEntry.TableName, TriggerColumns, Triggers);
                yield return Insert(MissionActionEntry.TableName, ActionColumns, Actions);
                yield return Insert(ChannelPolicyTable, ChannelColumns, Channels);
                yield return Insert(SceneBindingTable, SceneColumns, Scenes);
                yield return Insert(MissionEvidenceEntry.TableName, EvidenceColumns, Evidence);
            }
        }

        /// <summary>Every delete that takes the rows out again, children first.</summary>
        public static IEnumerable<string> DeleteStatements
        {
            get
            {
                var missions = string.Join(", ", Zones.Select(zone => zone.MissionId));

                foreach (var table in new[]
                {
                    MissionEvidenceEntry.TableName, SceneBindingTable, ChannelPolicyTable,
                    MissionActionEntry.TableName, MissionTriggerEntry.TableName, MissionObjectiveTransitionEntry.TableName,
                    MissionObjectiveDefinitionEntry.TableName, MissionContentDefinitionEntry.TableName
                })
                    yield return $"delete from {table} where content_revision = '{Revision}' and mission_id in ({missions});";
            }
        }

        private static string Insert(string table, string[] columns, object[][] rows)
        {
            return $"insert into {table} ({string.Join(", ", columns)}) values "
                + string.Join(", ", rows.Select(row => "(" + string.Join(", ", row.Select(Literal)) + ")"))
                + ";";
        }

        private static string Literal(object value)
        {
            return value switch
            {
                null => "NULL",
                string text => "'" + text.Replace("'", "''") + "'",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                _ => System.Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}
