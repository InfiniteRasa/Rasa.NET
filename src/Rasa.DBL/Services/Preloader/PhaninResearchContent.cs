using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;
    using K = KardashAttaColonyContent;

    /// <summary>
    /// The Phanin Research Facility (adv_arieki_torden_plains_penalresearch, 2034), under the
    /// hospital of Irendas Penal Colony, as the Tabula Rasa wiki's page and Ten Ton Hammer's
    /// walkthrough tell it: its Brann, its bosses, the Bane officer with them, and its seven
    /// missions, in the client's own names and texts. The Kardash Atta Colony's chain opens it
    /// (KardashAttaColonyContent): Key Information's keycard is the way in.
    ///
    /// Who and what:
    ///  - Sergeant Phenix at the AFS barricade inside the way in, off the hospital medic he stood
    ///    in, who takes Into the Facility (Captain Reyko's) and gives The Mind of a Madman.
    ///  - Topside: Agent Zim's Do Bugs Feel Fear? (after Bug Hunt), Colonel Whitaker's Do Bugs
    ///    Have Feelings? (after Bug Hugger), Reyko's Medical Supply Recovery for Dr. Finobee,
    ///    Researcher Erodan's Proof of Collusion, and Greloran's Reformist Justice: Greloran stands
    ///    outside the facility's way down, where nothing stood. Colonel Whitaker stood with the
    ///    others on the patch of ground joined to nothing; he goes to the command tower, with the
    ///    conversation package the client gives him (507 - his row had the console's, 1095).
    ///  - The Brann at levels 22 to 24 through the four security rooms, the dormitories, the
    ///    warehouse, the laboratories, the mind control rooms, the power plant and the transmission
    ///    centre; Head Security Shendro in Security Delta, Head Scientist Mernash in the Neural
    ///    Lab, Master Phanin in the power plant on the lowest level - the last large room - and
    ///    Thrax Overseer Treshk in the small room at its corner.
    /// The nine mainframes, the doors they open, the research consoles, the medical crates, the
    /// caged Atta let out as the laboratories go dark, the keycard at the way in and the scenario
    /// events the missions wait for are the server's, per copy of the facility (PhaninResearch).
    ///
    /// Health and damage as the Kardash Atta Colony's: 10000 x 2^((level - 42) / 8) a thug, a
    /// minion three quarters, a lieutenant one and three quarters; the Brann rifle of the Ojasa
    /// hive's soldiers scaled by the same factor.
    /// </summary>
    public static class PhaninResearchContent
    {
        public const string Revision = "phanin_1_16_5";
        public const uint Facility = 2034, Plains = 1764;

        /// <summary>missioncategorylanguage: "Instance (Phanin Research Center)".</summary>
        public const uint Category = 10000030;

        public const uint IntoTheFacility = 1183, MindOfAMadman = 1114, DoBugsFeelFear = 1116, DoBugsHaveFeelings = 1941,
            MedicalSupplyRecovery = 1117, ProofOfCollusion = 1123, ReformistJustice = 1124;

        public static readonly uint[] Missions =
        {
            IntoTheFacility, MindOfAMadman, DoBugsFeelFear, DoBugsHaveFeelings, MedicalSupplyRecovery, ProofOfCollusion, ReformistJustice
        };

        public const uint PhenixPackage = 1273, WhitakerPackage = 507, WhitakerSeededPackage = 1095;

        // The people already in the world.
        public const uint Phenix = 510097, Whitaker = 510085;

        // New people and rows.
        public const uint Greloran = 782001;
        public const uint Guard = 782010, Scientist = 782011, Researcher = 782012, Engineer = 782013, Bioengineer = 782014,
            Sentinel = 782015, Warden = 782016, Supervisor = 782017;
        public const uint Mernash = 782020, Shendro = 782021, Phanin = 782022, Treshk = 782023;

        public static readonly uint[] Brann = { Guard, Scientist, Researcher, Engineer, Bioengineer, Sentinel, Warden, Supervisor, Mernash, Shendro, Phanin };

        // Items (class, template).
        public const uint BrainWaveClass = 24572, BrainWaveTemplate = 50207;
        public const uint CommunicationClass = 26434, CommunicationTemplate = 111179;
        public const uint PheromoneClass = 26432, PheromoneTemplate = 111177;
        public const uint SensoryClass = 26433, SensoryTemplate = 111178;
        public const uint SchematicsClass = 26435, SchematicsTemplate = 111180;
        public const uint MedicalClass = 24577, MedicalTemplate = 50209;
        public const uint ProofClass = 24578, ProofTemplate = 50210;

        /// <summary>How many Brann Medical Supplies Finobee wants.</summary>
        public const uint SuppliesWanted = 6;

        // The objects the Game puts down.
        public const uint DoorClass = 10749;            // UsableDoorBrannCorridor04mV01
        public const uint MainframeClass = 23885;       // UsableTwoStateBrannMonitorGenericV01
        public const uint ResearchConsoleClass = 23886; // UsableTwoStateBrannMonitorGenericV02
        public const uint CrateClass = 7931;            // UsableTreasureDispBrannCrateV01

        // The Mind of a Madman's objectives (missionobjective 1114): the third is the client's own counters.
        public const uint Security1 = 1, Security2 = 2, Security3 = 4, Security4 = 5, MainResearch = 6, SecondaryResearch = 7,
            TertiaryResearch = 8, PowerMainframe = 9, CommunicationMainframe = 10;

        public const uint MainframeScenario = 1, InfiltrateScenario = 1, Infiltrated = 1, InfiltrateObjective = 1;

        /// <summary>
        /// A mainframe: its objective, its name and body text, its event (scenario 1), its stage
        /// (the doors of the next stage open when every mainframe of its own is down), and where.
        /// </summary>
        public sealed record Mainframe(uint Objective, uint Name, uint Body, uint Event, int Stage, string Room, Vector3 At);

        public static readonly Mainframe[] Mainframes =
        {
            new(Security1, 9035, 9036, 1, 0, "Security Alpha", new Vector3(-56f, -15.9f, -106.5f)),
            new(Security2, 9038, 9039, 2, 0, "Security Beta", new Vector3(-194f, -23.9f, 56f)),
            new(Security3, 12746, 12747, 3, 1, "Security Gamma", new Vector3(-181f, -39.9f, -88f)),
            new(Security4, 12748, 12749, 4, 1, "Security Delta", new Vector3(-230.5f, -39.9f, -12f)),
            new(MainResearch, 12750, 12751, 5, 2, "Atta Mind Control Room", new Vector3(-70f, -47.9f, 12f)),
            new(SecondaryResearch, 12752, 12753, 6, 2, "Neural Lab", new Vector3(5f, -31.3f, -55f)),
            new(TertiaryResearch, 12754, 12755, 7, 2, "Neuron Processing", new Vector3(-107f, -47.3f, -92f)),
            new(PowerMainframe, 12756, 12757, 8, 3, "Mind Control Power Plant", new Vector3(10f, -47.3f, 16f)),
            new(CommunicationMainframe, 12758, 12759, 9, 3, "Mind Control Transmission Center", new Vector3(-35f, -39.3f, 94f))
        };

        /// <summary>
        /// A door: the jamb it closes (ArchBrannCorridorJamb014mV01, its middle: 2 m into it), the
        /// stage it opens at, and what is behind it. The navmesh's walkable surface, cut at each
        /// jamb, says what each one shuts off from the way in.
        /// </summary>
        public sealed record Door(string Name, Vector3 Jamb, float Yaw, int Stage)
        {
            /// <summary>Where the door stands: the jamb's middle, 2 m along its own -Z.</summary>
            public Vector3 At => Jamb + Vector3.Transform(new Vector3(0, 0, -2f), Quaternion.CreateFromYawPitchRoll(Yaw, 0f, 0f));
        }

        private const float East = -1.5707964f;

        public static readonly Door[] Doors =
        {
            new("Security Gamma", new Vector3(-180f, -40f, -52f), 0f, 1),
            new("Security Delta", new Vector3(-200f, -40f, -12f), East, 1),
            new("the lower laboratories", new Vector3(-124f, -48f, 20f), East, 2),
            new("Neuron Processing", new Vector3(-120f, -48f, -35.9f), 0f, 2),
            new("the Neural Lab", new Vector3(-32f, -32f, -19.9f), 0f, 2),
            new("the power plant", new Vector3(-40f, -48f, 20f), East, 3),
            new("the transmission centre", new Vector3(-72f, -40f, 63.9f), 0f, 3),
            new("Phanin's quarters", new Vector3(-104f, -40f, 63.9f), 0f, 3)
        };

        /// <summary>A research console: the objective of Do Bugs Feel Fear? and Do Bugs Have Feelings? it is, its item, and where.</summary>
        public sealed record ResearchConsole(uint Objective, uint ItemClass, uint ItemTemplate, string Key, string Room, Vector3 At);

        public static readonly ResearchConsole[] Research =
        {
            new(2, BrainWaveClass, BrainWaveTemplate, "brain-wave", "Neural Lab", new Vector3(-37f, -31.3f, -76f)),
            new(3, CommunicationClass, CommunicationTemplate, "communication", "Mind Control Transmission Center", new Vector3(-53f, -39.3f, 109f)),
            new(4, PheromoneClass, PheromoneTemplate, "pheromone", "Neuron Processing", new Vector3(-119f, -47.3f, -47f)),
            new(5, SensoryClass, SensoryTemplate, "sensory", "Atta Mind Control Room", new Vector3(-79f, -47.9f, 26f)),
            new(6, SchematicsClass, SchematicsTemplate, "schematics", "Phanin's Private Quarters", new Vector3(-103f, -39.3f, 71f))
        };

        /// <summary>The medical crates: in the dormitories (bunks and lockers), the warehouse, the Neural Lab.</summary>
        public static readonly Vector3[] Crates =
        {
            new(-138f, -23.3f, 28f), new(-150f, -23.3f, 25f), new(-147f, -23.3f, 40f),
            new(-111f, -15.3f, -61f), new(-123f, -15.3f, -61f), new(-120f, -15.1f, -67f),
            new(-110f, -23.5f, 7f), new(-98f, -23.5f, -35f), new(-74f, -23.5f, 13f), new(5f, -31.3f, -34f)
        };

        /// <summary>The cages of each laboratory (ArchBrannGenObjCageV01), by the research mainframe that lets their Atta out.</summary>
        public static readonly (uint Objective, Vector3[] Cages)[] Cages =
        {
            (MainResearch, new[] { new Vector3(-55.34f, -48.16f, 1.66f), new Vector3(-55.05f, -48.2f, 38.19f), new Vector3(-105.42f, -48.2f, 37.98f), new Vector3(-105.47f, -48.2f, 1.93f) }),
            (SecondaryResearch, new[] { new Vector3(-5.1f, -31.5f, -54.4f), new Vector3(-5.1f, -31.5f, -57.9f), new Vector3(-26.6f, -31.5f, -57.9f), new Vector3(-26.6f, -31.5f, -54.4f) }),
            (TertiaryResearch, new[] { new Vector3(-101.06f, -47.5f, -69.71f), new Vector3(-132.35f, -47.5f, -66.53f), new Vector3(-106.7f, -47.5f, -73.44f), new Vector3(-75.75f, -47.5f, -77.13f) })
        };

        /// <summary>The way in, from the hospital (map_link 75), and where a player arrives.</summary>
        public const uint WayInLink = 75;
        public static readonly Vector3 Arrival = new(24.1922f, 0.5697f, -0.0818f);
        public static readonly Vector3 PhenixAt = new(-2f, -7.3f, 3f);
        public static readonly Vector3 PhenixSeededAt = new(-8.9f, -7.42f, 12.9f);
        public static readonly Vector3 HospitalWayDown = new(54.5f, 401.61f, -279f);
        public static readonly Vector3 GreloranAt = new(99f, 409.21f, -282f);
        public static readonly Vector3 WhitakerAt = new(316f, 433.26f, -46f);
        public static readonly Vector3 WhitakerSeededAt = new(386.4f, 433.2f, -98.6f);

        public static readonly Vector3 ShendroAt = new(-223f, -39.9f, -12f);
        public static readonly Vector3 MernashAt = new(5f, -31.3f, -49f);
        public static readonly Vector3 PhaninAt = new(25f, -47.3f, 28f);
        public static readonly Vector3 TreshkAt = new(16f, -47.41f, 58f);

        public enum TargetCategoryRow : uint { Hostile = 0, Friendly = 1 }

        public sealed record Beast(uint Id, string Comment, uint ClassId, TargetCategoryRow Faction, uint Level, uint Hp,
            uint Armor, uint NameId, uint RunSpeed, uint WalkSpeed, params uint[] Actions);

        public sealed record Act(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        public sealed record Spot(uint Id, double X, double Y, double Z, double Rotation, uint RespawnTenths, double Radius,
            params (uint Creature, uint Min, uint Max)[] Slots);

        private const uint RifleAttack = 1, RifleArg = 134, ThraxRifleArg = 79, ThraxLightning = 449;
        private const uint BrannClass = 7253, BrannBossClass = 24561, ThraxBossClass = 10504;

        public static readonly Act[] Actions =
        {
            new(782101, "Phanin Brann rifle 1/134 (level 23)", RifleAttack, RifleArg, 1, 25, 1000, 0, 50, 93, 1),
            new(782102, "Phanin Brann rifle 1/134 (level 22)", RifleAttack, RifleArg, 1, 25, 1000, 0, 46, 85, 1),
            new(782103, "Phanin Brann lieutenant rifle 1/134 (level 24)", RifleAttack, RifleArg, 1, 25, 1000, 0, 69, 126, 1),
            new(782104, "Phanin head rifle 1/134 (level 25)", RifleAttack, RifleArg, 1, 30, 1000, 0, 113, 206, 1),
            new(782105, "Master Phanin rifle 1/134", RifleAttack, RifleArg, 1, 30, 900, 0, 120, 220, 1),
            new(782106, "Thrax Overseer Treshk rifle 1/79", RifleAttack, ThraxRifleArg, 0.5, 30, 800, 0, 90, 140, 1),
            new(782107, "Thrax Overseer Treshk thrax_lightning", ThraxLightning, 1, 1, 40, 5000, 500, 250, 450, 13)
        };

        public static readonly Beast[] Creatures =
        {
            new(Greloran, "Greloran - Irendas Colony", K.BrannSwapsetMale, TargetCategoryRow.Friendly, 24, 3000, 0, 9013, 0, 0),
            new(Guard, "Phanin Guard", BrannClass, TargetCategoryRow.Hostile, 23, 1929, 80, 9034, 9, 5, 782101),
            new(Scientist, "Phanin Scientist", BrannClass, TargetCategoryRow.Hostile, 22, 1326, 70, 9041, 9, 5, 782102),
            new(Researcher, "Phanin Researcher", BrannClass, TargetCategoryRow.Hostile, 22, 1326, 70, 9033, 9, 5, 782102),
            new(Engineer, "Phanin Engineer", BrannClass, TargetCategoryRow.Hostile, 23, 1929, 80, 9044, 9, 5, 782101),
            new(Bioengineer, "Phanin Bioengineer", BrannClass, TargetCategoryRow.Hostile, 23, 1929, 80, 9043, 9, 5, 782101),
            new(Sentinel, "Phanin Sentinel", BrannClass, TargetCategoryRow.Hostile, 24, 3680, 90, 9042, 9, 5, 782103),
            new(Warden, "Phanin Warden", BrannClass, TargetCategoryRow.Hostile, 24, 3680, 90, 9046, 9, 5, 782103),
            new(Supervisor, "Phanin Supervisor", BrannClass, TargetCategoryRow.Hostile, 24, 3680, 90, 9045, 9, 5, 782103),
            new(Mernash, "Head Scientist Mernash", BrannBossClass, TargetCategoryRow.Hostile, 25, 7000, 150, 9036, 9, 5, 782104),
            new(Shendro, "Head Security Shendro", BrannBossClass, TargetCategoryRow.Hostile, 25, 7000, 150, 9035, 9, 5, 782104),
            new(Phanin, "Master Phanin", BrannBossClass, TargetCategoryRow.Hostile, 25, 10000, 160, 9032, 9, 5, 782105),
            new(Treshk, "Thrax Overseer Treshk - Phanin", ThraxBossClass, TargetCategoryRow.Hostile, 25, 6000, 150, 9047, 9, 5, 782106, 782107)
        };

        public const uint NpcRespawn = 200, HostileRespawn = 3000, NamedRespawn = 6000;

        public const uint GreloranPool = 782201;

        /// <summary>The facility's pools, all on map 2034.</summary>
        public static readonly Spot[] Pools =
        {
            // Past the airlock.
            new(782210, -52, -7.31, 0, 1.6, HostileRespawn, 2, (Guard, 2, 2)),
            // Security Alpha.
            new(782211, -56, -15.65, -83, 0, HostileRespawn, 2, (Guard, 2, 2)),
            new(782212, -56, -15.81, -101, 0, HostileRespawn, 0, (Warden, 1, 1)),
            new(782213, -71, -15.87, -98, 1.6, HostileRespawn, 0, (Sentinel, 1, 1)),
            // The dormitories.
            new(782214, -117, -14.82, -67, 1.6, HostileRespawn, 2, (Scientist, 2, 2)),
            new(782215, -141, -23.3, 37, 1.6, HostileRespawn, 2, (Scientist, 2, 2)),
            // The warehouse.
            new(782216, -104, -23.5, 10, 3.1, HostileRespawn, 2, (Engineer, 2, 2)),
            new(782217, -71, -23.5, 10, 3.1, HostileRespawn, 2, (Engineer, 1, 1), (Researcher, 1, 1)),
            new(782218, -104, -23.5, -35, 0, HostileRespawn, 2, (Researcher, 2, 2)),
            // Security Beta.
            new(782219, -176, -23.9, 58, 1.6, HostileRespawn, 2, (Guard, 2, 2)),
            new(782220, -176, -23.83, 70, 1.6, HostileRespawn, 0, (Supervisor, 1, 1)),
            new(782221, -182, -23.9, 55, 1.6, HostileRespawn, 0, (Guard, 1, 1)),
            // Security Gamma.
            new(782222, -166, -39.82, -75, 3.1, HostileRespawn, 2, (Guard, 2, 2)),
            new(782223, -196, -39.82, -78, 3.1, HostileRespawn, 0, (Warden, 1, 1)),
            new(782224, -181, -39.69, -63, 3.1, HostileRespawn, 0, (Guard, 1, 1)),
            // Security Delta and Head Security Shendro.
            new(782225, -220, -39.9, -9, 1.6, HostileRespawn, 2, (Guard, 2, 2)),
            new(782226, ShendroAt.X, ShendroAt.Y, ShendroAt.Z, 1.6, NamedRespawn, 0, (Shendro, 1, 1)),
            new(782227, -214, -39.9, -24, 1.6, HostileRespawn, 0, (Sentinel, 1, 1)),
            // The Atta Mind Control Room.
            new(782228, -76, -47.9, 20, 1.6, HostileRespawn, 2, (Bioengineer, 2, 2)),
            new(782229, -79, -47.9, 14, 1.6, HostileRespawn, 0, (Supervisor, 1, 1)),
            new(782230, -67, -47.84, 20, 1.6, HostileRespawn, 0, (Bioengineer, 1, 1)),
            // The Neural Lab and Head Scientist Mernash.
            new(782231, -37, -31.3, -61, -1.6, HostileRespawn, 2, (Researcher, 2, 2)),
            new(782232, MernashAt.X, MernashAt.Y, MernashAt.Z, -1.6, NamedRespawn, 0, (Mernash, 1, 1)),
            new(782233, 5, -31.3, -40, -1.6, HostileRespawn, 0, (Researcher, 1, 1)),
            // Neuron Processing.
            new(782234, -86, -47.3, -77, 1.6, HostileRespawn, 2, (Bioengineer, 2, 2)),
            new(782235, -122, -47.3, -50, 1.6, HostileRespawn, 0, (Scientist, 1, 1)),
            // The power plant: Master Phanin, and Treshk in the corner room.
            new(782236, PhaninAt.X, PhaninAt.Y, PhaninAt.Z, -1.6, NamedRespawn, 0, (Phanin, 1, 1)),
            new(782237, 4, -47.3, 19, -1.6, HostileRespawn, 2, (Sentinel, 2, 2)),
            new(782238, -14, -47.3, 37, -1.6, HostileRespawn, 0, (Engineer, 1, 1)),
            new(782239, TreshkAt.X, TreshkAt.Y, TreshkAt.Z, 3.1, NamedRespawn, 0, (Treshk, 1, 1)),
            // The transmission centre.
            new(782240, -35, -39.3, 103, 3.1, HostileRespawn, 2, (Engineer, 2, 2)),
            new(782241, -56, -39.3, 112, 3.1, HostileRespawn, 0, (Supervisor, 1, 1)),
            new(782242, -74, -39.3, 85, 3.1, HostileRespawn, 0, (Engineer, 1, 1)),
            // Phanin's quarters.
            new(782243, -106, -39.3, 76, 3.1, HostileRespawn, 2, (Sentinel, 2, 2))
        };

        #region Up and Down

        public static IEnumerable<string> Up()
        {
            foreach (var a in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"SELECT {a.Id}, {S(a.Description)}, {a.ActionId}, {a.ArgId}, {N(a.RangeMin)}, {N(a.RangeMax)}, {a.Cooldown}, {a.Windup}, {a.MinDamage}, {a.MaxDamage}, {a.DamageType} " +
                    $"FROM entityclass e WHERE e.id = {BrannClass} AND NOT EXISTS (SELECT 1 FROM creature_action x WHERE x.id = {a.Id});";

            foreach (var c in Creatures)
            {
                var actions = c.Actions.Concat(Enumerable.Repeat(0U, 8)).Take(8).ToArray();
                var actionsThere = string.Concat(c.Actions.Select(id => $" AND EXISTS (SELECT 1 FROM creature_action a WHERE a.id = {id})"));

                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, {(uint)c.Faction}, {c.Level}, {c.Hp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {string.Join(", ", actions)} " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId}{actionsThere} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                    $"SELECT {c.Id}, 15, 15, 15, {c.Hp}, {c.Armor} FROM creature c WHERE c.id = {c.Id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_stat x WHERE x.id = {c.Id});";
            }

            foreach (var sql in Change(Greloran, new Slot[0], K.BrannLook))
                yield return sql;

            yield return SpawnPool(GreloranPool, Plains, GreloranAt.X, GreloranAt.Y, GreloranAt.Z, 3.1, NpcRespawn, 0, (Greloran, 1, 1));
            foreach (var p in Pools)
                yield return SpawnPool(p.Id, Facility, p.X, p.Y, p.Z, p.Rotation, p.RespawnTenths, p.Radius, p.Slots);

            // Phenix off the hospital medic, Whitaker to the command tower, each only from where it was seeded.
            yield return $"UPDATE spawnpool SET pos_x = {N(PhenixAt.X)}, pos_y = {N(PhenixAt.Y)}, pos_z = {N(PhenixAt.Z)}, rotation = -1.64 " +
                $"WHERE id = {Phenix} AND {Near(PhenixSeededAt)};";
            yield return $"UPDATE spawnpool SET pos_x = {N(WhitakerAt.X)}, pos_y = {N(WhitakerAt.Y)}, pos_z = {N(WhitakerAt.Z)}, rotation = 3.1 " +
                $"WHERE id = {Whitaker} AND {Near(WhitakerSeededAt)};";
            yield return $"UPDATE npc_package SET package_id = {WhitakerPackage} WHERE id = {Whitaker} AND package_id = {WhitakerSeededPackage};";

            foreach (var sql in MissionRows())
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var sql in InstanceMissionSql.Remove(Revision, Missions))
                yield return sql;

            yield return $"UPDATE npc_package SET package_id = {WhitakerSeededPackage} WHERE id = {Whitaker} AND package_id = {WhitakerPackage};";
            yield return $"UPDATE spawnpool SET pos_x = {N(WhitakerSeededAt.X)}, pos_y = {N(WhitakerSeededAt.Y)}, pos_z = {N(WhitakerSeededAt.Z)}, rotation = 0 " +
                $"WHERE id = {Whitaker} AND {Near(WhitakerAt)};";
            yield return $"UPDATE spawnpool SET pos_x = {N(PhenixSeededAt.X)}, pos_y = {N(PhenixSeededAt.Y)}, pos_z = {N(PhenixSeededAt.Z)}, rotation = 0 " +
                $"WHERE id = {Phenix} AND {Near(PhenixAt)};";

            yield return $"DELETE FROM spawnpool WHERE id = {GreloranPool} AND creature_1_Id = {Greloran};";
            foreach (var p in Pools)
                yield return $"DELETE FROM spawnpool WHERE id = {p.Id} AND creature_1_Id = {p.Slots[0].Creature};";

            yield return $"DELETE FROM creature_appearance WHERE id = {Greloran};";

            foreach (var c in Creatures)
            {
                yield return $"DELETE FROM creature_stat WHERE id = {c.Id};";
                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }

            foreach (var a in Actions)
                yield return $"DELETE FROM creature_action WHERE id = {a.Id};";
        }

        private static string SpawnPool(uint id, uint map, double x, double y, double z, double rotation, uint respawn, double radius,
            params (uint Creature, uint Min, uint Max)[] creatures)
        {
            var slots = creatures.Concat(Enumerable.Repeat((Creature: 0U, Min: 0U, Max: 0U), 6)).Take(6).ToArray();
            var there = string.Concat(creatures.Skip(1).Select(s => $" AND EXISTS (SELECT 1 FROM creature x WHERE x.id = {s.Creature})"));

            return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                $"SELECT {id}, 0, 0, {respawn}, {N(x)}, {N(y)}, {N(z)}, {N(rotation)}, {map}, " +
                string.Join(", ", slots.Select(s => $"{s.Creature}, {s.Min}, {s.Max}")) + $", {N(radius)} " +
                $"FROM creature c WHERE c.id = {creatures[0].Creature}{there} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {id});";
        }

        private static string Near(Vector3 at) =>
            $"ABS(pos_x - {N(at.X)}) < 0.05 AND ABS(pos_y - {N(at.Y)}) < 0.05 AND ABS(pos_z - {N(at.Z)}) < 0.05";

        #endregion

        #region Missions

        private const string Guide = "https://www.tentonhammer.com/guides/phanin-research-instance";

        private static IEnumerable<string> MissionRows()
        {
            var sql = new List<string>();
            IntoTheFacilityRows(sql);
            MindOfAMadmanRows(sql);
            ResearchRows(sql, DoBugsFeelFear, "Do Bugs Feel Fear?", 7665, K.Zim, K.BugHunt, 8260, 8261,
                new uint[] { 13867, 13869, 13871, 13873, 13875 }, new uint[] { 13868, 13870, 13872, 13874, 13876 }, "Agent Zim");
            ResearchRows(sql, DoBugsHaveFeelings, "Do Bugs Have Feelings?", 20314, Whitaker, K.BugHugger, 20319, 20320,
                new uint[] { 20321, 20323, 20325, 20327, 20329 }, new uint[] { 20322, 20324, 20326, 20328, 20330 }, "Colonel Whitaker");
            MedicalSupplyRows(sql);
            ProofOfCollusionRows(sql);
            ReformistJusticeRows(sql);
            return sql;
        }

        // The walkthrough's experience: 54000 for each mission of the facility; credits ours.
        public const uint FacilityExperience = 54000, FacilityCredits = 5000;

        /// <summary>Reyko sends the player to Phenix at the barricade (package 1273).</summary>
        private static void IntoTheFacilityRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, IntoTheFacility, "Into the Facility", 9030, K.Reyko, Phenix, 23, Category);
            mission.Reward(10000, 1500);
            mission.Objective(1, 9046, 9047, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Conversation(1, PhenixPackage);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = After(K.SecurityThreat);
            Marker(mission, scene, 1, Plains, HospitalWayDown, 6);
            Evidence(mission, "Captain Reyko gives it once Security Threat is done; Sergeant Phenix1273 at the AFS barricade inside " +
                "the facility's way in takes it. The marker is on the hospital's way down to the facility.");
            mission.Enable(scene);
        }

        /// <summary>Phenix's: the nine mainframes, stage by stage as each stage's doors open; Reyko takes it.</summary>
        private static void MindOfAMadmanRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, MindOfAMadman, "The Mind of a Madman", 7655, Phenix, K.Reyko, 25, Category);
            mission.Reward(FacilityExperience, FacilityCredits);
            mission.Prerequisite(IntoTheFacility);
            var scene = Scene(events: true);

            foreach (var m in Mainframes)
                mission.Objective(m.Objective, m.Name, m.Body, m.Objective, m.Stage == 0 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive);

            foreach (var m in Mainframes)
            {
                mission.Transition(m.Objective);
                mission.Progress(m.Objective, MissionProgressEventKind.ScenarioEvent, m.Event, counter: MainframeScenario);

                // Each of a stage shows the next: what it opens is the next stage's rooms.
                var action = 1U;
                foreach (var next in Mainframes.Where(n => n.Stage == m.Stage + 1))
                {
                    mission.RevealAndActivate(m.Objective, next.Objective, action);
                    action += 2;
                }

                if (m.Objective == Security1)
                    mission.Action(m.Objective, action, MissionActionKind.GrantReward, reward: 1);

                Marker(mission, scene, m.Objective, Facility, m.At, 6);
            }

            mission.Scenario(MainframeScenario, "The facility's mainframes", Mainframes.Select(m => m.Event).ToArray());
            Evidence(mission, "Sergeant Phenix gives it, after Into the Facility; Captain Reyko takes it. Nine mainframe consoles " +
                "(UsableTwoStateBrannMonitorGenericV01): Security Alpha, Beta, Gamma and Delta, the Atta Mind Control Room, the Neural " +
                "Lab and Neuron Processing, the power plant and the transmission centre. Each disabled is its objective for everyone on " +
                "it. Each stage's all disabled opens the next stage's Brann doors (UsableDoorBrannCorridor04mV01 in the corridor jambs): " +
                "Gamma and Delta; the lower laboratories, Neuron Processing and the Neural Lab; the power plant, the transmission centre " +
                "and Phanin's quarters. The client's objective3 (Counters) is left out. 54000 XP, the walkthrough's.");
            mission.Enable(scene);
        }

        /// <summary>Zim's and Whitaker's: into the facility, and the five research consoles.</summary>
        private static void ResearchRows(List<string> sql, uint missionId, string name, uint nameText, uint giver, uint after,
            uint infiltrateName, uint infiltrateBody, uint[] names, uint[] bodies, string who)
        {
            var mission = new InstanceMissionSql(sql, Revision, missionId, name, nameText, giver, giver, 25, Category);
            mission.Reward(FacilityExperience, FacilityCredits);
            var scene = After(after, events: true);

            mission.Objective(InfiltrateObjective, infiltrateName, infiltrateBody, 1, MissionObjectiveState.Incomplete);
            mission.Transition(InfiltrateObjective);
            mission.Progress(InfiltrateObjective, MissionProgressEventKind.ScenarioEvent, Infiltrated, counter: InfiltrateScenario);
            mission.Action(InfiltrateObjective, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, scene, InfiltrateObjective, Plains, HospitalWayDown, 6);
            mission.Scenario(InfiltrateScenario, "Into the facility", Infiltrated);

            for (var i = 0; i < Research.Length; i++)
            {
                var r = Research[i];
                mission.Objective(r.Objective, names[i], bodies[i], r.Objective, MissionObjectiveState.Incomplete);
                mission.Transition(r.Objective);
                mission.Progress(r.Objective, MissionProgressEventKind.ItemAcquired, r.ItemClass, target: 1);
                Marker(mission, scene, r.Objective, Facility, r.At, 6);
            }

            Evidence(mission, $"{who} gives and takes it, after {(after == K.BugHunt ? "Bug Hunt" : "Bug Hugger")}. Coming into the " +
                "facility is the infiltration. A research console (UsableTwoStateBrannMonitorGenericV02) in each of the Neural Lab, the " +
                "transmission centre, Neuron Processing, the Atta Mind Control Room and Phanin's quarters gives its research to a player " +
                "on it who has not got it: Atta Brain Wave Patterns50207, Communication Studies111179, Pheromone Responses111177, Sensory " +
                "Receptors111178 and the Mind Control Schematics111180, all handed over. 54000 XP, the walkthrough's.");

            scene.Items = Research.Select(r => new MissionItemBinding(r.Key, r.ItemTemplate, MissionItemScope.CharacterOwned, 1,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                TurnInQuantity: 1)).ToList();
            mission.Enable(scene);
        }

        /// <summary>Reyko's, for Finobee: Brann Medical Supplies from the crates.</summary>
        private static void MedicalSupplyRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, MedicalSupplyRecovery, "Medical Supply Recovery", 7670, K.Reyko, K.Finobee, 24, Category);
            mission.Reward(FacilityExperience, FacilityCredits);
            var scene = After(K.AttaControl);
            mission.Objective(1, 8305, 8306, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, MedicalClass, target: SuppliesWanted);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, scene, 1, Facility, Crates[0], 30);
            Evidence(mission, $"Captain Reyko gives it once Atta Control is done ('Once you figure out a way to get inside that " +
                $"facility'), and Dr. Finobee in the hospital takes it. {SuppliesWanted} Brann Medical Supplies50209 from the medical " +
                "crates (UsableTreasureDispBrannCrateV01) in the dormitories, the warehouse and the Neural Lab, one from each crate for each " +
                "player on it. The wiki has Reyko as giver; the walkthrough Finobee, who receives. 54000 XP, the walkthrough's.");
            scene.Items = new List<MissionItemBinding>
            {
                new("medical-supplies", MedicalTemplate, MissionItemScope.CharacterOwned, SuppliesWanted,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: SuppliesWanted)
            };
            mission.Enable(scene);
        }

        /// <summary>Erodan's: the proof, off the Bane officer hiding by Phanin.</summary>
        private static void ProofOfCollusionRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ProofOfCollusion, "Proof of Collusion", 7733, K.Erodan, K.Erodan, 25, Category, shareable: true);
            mission.Reward(FacilityExperience, FacilityCredits);
            var scene = After(K.KeyInformation);
            mission.Objective(1, 8365, 8366, 1, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, ProofClass, target: 1);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            Marker(mission, scene, 1, Facility, TreshkAt, 15);
            Evidence(mission, "Researcher Erodan gives and takes it, once Key Information is done ('Tralos has told me what happened'). " +
                "The Bane hide in a small room in the corner of the lowest level, by Phanin: Thrax Overseer Treshk's corpse has the Proof " +
                "of Collusion50210, the walkthrough's 'Kill Overseer Treshk to complete Proof of Collusion'. Shareable. 54000 XP.");
            scene.Items = new List<MissionItemBinding>
            {
                new("proof", ProofTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    TurnInQuantity: 1, Drop: new MissionItemDropDefinition(new[] { Treshk }, 1, Facility, 100, 1))
            };
            mission.Enable(scene);
        }

        /// <summary>Greloran's: Mernash, Shendro and Phanin dead.</summary>
        private static void ReformistJusticeRows(List<string> sql)
        {
            var mission = new InstanceMissionSql(sql, Revision, ReformistJustice, "Reformist Justice", 7738, Greloran, Greloran, 25, Category, shareable: true);
            mission.Reward(FacilityExperience, FacilityCredits);
            var scene = After(K.SecurityThreat);
            foreach (var (objective, name, body, boss, at) in new (uint, uint, uint, uint, Vector3)[]
                     {
                         (1, 8140, 8141, Mernash, MernashAt), (2, 8142, 8143, Shendro, ShendroAt), (3, 8144, 8145, Phanin, PhaninAt)
                     })
            {
                mission.Objective(objective, name, body, objective, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.CreatureKilled, boss);
                Marker(mission, scene, objective, Facility, at, 15);
            }
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            Evidence(mission, "Greloran, outside the facility's way down from the hospital (99, 409, -282), gives and takes it once " +
                "Security Threat is done. Head Scientist Mernash in the Neural Lab, Head Security Shendro in Security Delta, Master " +
                "Phanin in the power plant on the lowest level. Shareable. 54000 XP, the walkthrough's.");
            mission.Enable(scene);
        }

        private static void Marker(InstanceMissionSql mission, MissionSceneDefinition scene, uint objective, uint map, Vector3 at, double radius)
        {
            var id = MissionIndicator.UnnamedFrom + objective;
            mission.Indicator(objective, id, at.X, at.Y, at.Z, radius);
            scene.IndicatorMaps ??= new Dictionary<uint, Dictionary<uint, uint>>();
            if (!scene.IndicatorMaps.TryGetValue(objective, out var maps))
                scene.IndicatorMaps[objective] = maps = new Dictionary<uint, uint>();
            maps[id] = map;
        }

        private static MissionSceneDefinition Scene(bool events = false) => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Script = events ? "data.sequence" : null
        };

        /// <summary>
        /// A binding for a mission that waits on one of the Kardash Atta Colony's: another content
        /// revision, so the binding's requirement rather than a prerequisite row (as Pravus's The
        /// Escapist waits on the Wilderness' Machinations).
        /// </summary>
        private static MissionSceneDefinition After(uint missionId, bool events = false)
        {
            var scene = Scene(events);
            scene.Requirement = new AllRequirements(new MissionRequirement[]
            {
                scene.Requirement, new MissionStateRequirement(missionId, MissionState.Completed)
            });
            return scene;
        }

        private static void Evidence(InstanceMissionSql mission, string reconstruction)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client, "trpython:data/generated/client/missionobjective.pyo_dis",
                "Client1.16.5 mission, objective, text and conversation identities.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, Guide, "The Phanin Research instance walkthrough: people, order and rewards.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "PhaninResearchContent", reconstruction);
        }

        #endregion
    }
}
