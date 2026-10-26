using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// Which damage types each skeleton has a Critical Death for, from the client's own tables.
    ///
    /// The three crit-death effects carry the damage type as their level, and the client plays
    /// critdeathdata.lookup[level]'s animation families: the stagger while the window is open
    /// (CritPreDeathEffect), the finishing move (CritDeathEffect) and the corpse pose
    /// (CRIT_POSTDEATH_EFFECT). It plays a family only where animationdata.animationFamilyData
    /// has a row for (family, skeleton) (CharacterAnimationMgr._SelectAnimation), and otherwise
    /// does nothing: the creature stands idle while the skull and the finisher prompt still work.
    ///
    /// Not every skeleton has every type. Of the 139 skeletons a mesh is drawn on, 58 have some:
    /// most have every type but EMP; EMP is on the machines (both Machinas, the Glazers, the
    /// Seeker, the Sentinel, the Shield Drone, the prison and Neo bots) and Virulent not on
    /// them; and a few creatures lack others - a Treeback has no Physical or Sonic, a Maw no
    /// Physical or Ice, a Beam Manta no Electrical. For each skeleton the pre-death, death and
    /// post-death families come together: a skeleton has all three for a type or none.
    ///
    /// The tables are generated.client.animationdata.meshSkeleton (mesh to skeleton) and, per
    /// skeleton, the types whose critdeathdata families animationFamilyData has. The server
    /// knows each class's mesh (EntityClass.MeshId).
    /// </summary>
    public static class CritDeathAnimations
    {
        /// <summary>The damage types with Critical Death animations, by skeleton. A skeleton not listed has none (81 of them).</summary>
        private static readonly Dictionary<int, DamageType[]> BySkeleton = new Dictionary<int, DamageType[]>
        {
            [1] = Types(1, 2, 3, 4, 6, 7, 13),  // Redshirt_Brann_Male
            [21] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Kael_Armored
            [23] = Types(1, 2, 3, 4, 6, 7, 13),  // Redshirt_Brann_Female
            [1996] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Xanx_Albino
            [13867] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Thrax_Technician
            [13881] = Types(1, 2, 3, 4, 5, 6, 7, 13),  // NPC_Hominis_Machina
            [13884] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Fithik_Winged
            [13886] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Mox
            [13887] = Types(1, 2, 3, 6, 7, 13),  // Bane_Amoeboid_v1
            [13889] = Types(1, 2, 3, 4, 6, 7, 13),  // NPC_Creature_Boargar
            [13890] = Types(2, 3, 4, 6, 13),  // Bane_Treeback
            [13891] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Psychomite
            [13892] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Treelurker
            [13893] = Types(2, 3, 4, 6, 7, 13),  // Creature_Swamp_Grubber
            [13894] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Miasma
            [13897] = Types(2, 4, 6, 7, 13),  // Creature_Maw
            [13900] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Warnet_Soldier
            [13907] = Types(1, 2, 3, 4, 6, 13),  // Creature_Filcher
            [13909] = Types(1, 2, 3, 4, 5, 6, 7, 13),  // Bane_Forean_Machina
            [13912] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Lasher
            [13913] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Xanx
            [13918] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Lightbender_Standard
            [13919] = Types(1, 2, 3, 4, 6, 7, 13),  // Creature_Barb_Tick
            [13921] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Howler
            [13922] = Types(1, 3, 4, 6, 7, 13),  // Creature_Flaregasher
            [13923] = Types(1, 2, 3, 5, 6, 7, 13),  // Bane_Sentinel
            [13930] = Types(1, 2, 3, 4, 5, 6, 7),  // Bane_Seeker
            [13931] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Caretaker
            [13932] = Types(1, 2, 3, 4, 6, 7),  // Creature_Beam_Manta
            [13939] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Linker
            [13941] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Hunter
            [13960] = Types(1, 2, 3, 6, 7, 13),  // Mygone_Creature_Leaper
            [13962] = Types(1, 2, 3, 5, 6, 7, 13),  // NPC_Prison_Bot_Warden
            [13963] = Types(1, 2, 3, 5, 6, 7, 13),  // Creature_Prison_Bot_Reconstructor
            [13964] = Types(1, 2, 3, 5, 6, 7, 13),  // Bane_Pyroglazer
            [13965] = Types(1, 2, 3, 5, 6, 7, 13),  // Bane_Shield_Drone
            [13967] = Types(1, 2, 3, 5, 6, 7, 13),  // AccountReward_Pet_BooBot
            [13968] = Types(1, 3, 4, 6, 7),  // NPC_Creature_Atta_Harvester
            [13969] = Types(1, 3, 4, 6, 7),  // NPC_Creature_Atta_Soldier
            [13970] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Thrax_Grenadier
            [13975] = Types(2, 3, 4, 6, 13),  // Creature_Magmonix
            [13980] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Mox_Infected
            [13981] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Lightbender_Alternate
            [13982] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Miasma_Infected
            [13986] = Types(1, 2, 3, 6, 7, 13),  // Bane_Amoeboid_v2
            [13991] = Types(1, 3, 6, 7, 13),  // Ambient_Lavar
            [13999] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Thrax_Soldier_Pistol
            [14000] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Fithik_Wingless
            [14003] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Thrax_Soldier_Rifle
            [14004] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Thrax_Soldier_Melee
            [14008] = Types(1, 2, 3, 4, 6, 7, 13),  // Bane_Thrax_Machina
            [14014] = Types(1, 2, 3, 4, 6, 7, 13),  // Redshirt_Brann_Incurable_Male
            [14017] = Types(1, 2, 3, 4, 6, 7, 13),  // Redshirt_Brann_Incurable_Female
            [14024] = Types(1, 2, 3, 4, 6, 13),  // Creature_Granitour_Adult
            [14025] = Types(1, 2, 3, 5, 6, 7, 13),  // Ability_NeoBot_Repair
            [14026] = Types(1, 2, 3, 5, 6, 7, 13),  // Redshirt_Creature_AFS_Junkbot
            [14027] = Types(1, 2, 3, 5, 6, 7, 13),  // Ability_NeoBot_Rocket
            [14028] = Types(1, 2, 3, 5, 6, 7, 13),  // Ability_NeoBot_Flame
        };

        /// <summary>animationdata.meshSkeleton: the skeleton each mesh is drawn on (266 meshes).</summary>
        private static readonly Dictionary<int, int> SkeletonOfMesh = new Dictionary<int, int>
        {
            [4254] = 23, [4257] = 1, [12591] = 13868, [13408] = 14010, [13798] = 13867, [14024] = 13878, [14434] = 21, [14681] = 13872,
            [14921] = 1, [14983] = 23, [15131] = 13970, [15786] = 13943, [15867] = 13884, [15868] = 13881, [16214] = 13920, [16727] = 13889,
            [16728] = 13909, [16729] = 13885, [16730] = 13894, [16731] = 13886, [16732] = 13892, [16733] = 13888, [16734] = 13887, [16803] = 13890,
            [16806] = 13893, [16809] = 13891, [16870] = 13907, [16970] = 13895, [17174] = 13905, [17226] = 13900, [17227] = 13900, [17257] = 13921,
            [17264] = 13922, [17286] = 13896, [17298] = 13897, [17310] = 14015, [17408] = 13968, [17409] = 13916, [17410] = 13969, [17411] = 13932,
            [17412] = 13914, [17689] = 13963, [17711] = 13962, [17734] = 13975, [17800] = 13904, [18146] = 13918, [18147] = 13964, [18148] = 13923,
            [18149] = 13965, [18357] = 1996, [18363] = 23, [18364] = 1, [18368] = 13867, [18386] = 13912, [18584] = 13933, [18592] = 13967,
            [18689] = 13919, [18704] = 23, [18706] = 1, [18729] = 13938, [19004] = 13926, [19010] = 13924, [19366] = 13935, [19517] = 13928,
            [20025] = 13913, [20431] = 13937, [20438] = 13891, [20456] = 13986, [20519] = 13927, [20773] = 13930, [21376] = 13931, [21890] = 13939,
            [21891] = 13970, [23477] = 13941, [23490] = 13945, [23654] = 13890, [23703] = 13946, [23753] = 13932, [23923] = 13947, [23972] = 13954,
            [24082] = 13948, [24091] = 13949, [24095] = 13950, [24098] = 13952, [24111] = 13951, [24140] = 13953, [24512] = 13956, [24821] = 13957,
            [24891] = 13958, [24914] = 13959, [24954] = 13960, [25234] = 13956, [25554] = 13961, [25591] = 14016, [25800] = 13972, [25904] = 13973,
            [26025] = 13974, [26258] = 13895, [27562] = 13976, [27747] = 13921, [27825] = 13978, [27948] = 13979, [28085] = 13980, [28091] = 13981,
            [28092] = 13982, [28174] = 13983, [28192] = 13984, [28199] = 13985, [28474] = 13995, [28518] = 13989, [28540] = 13995, [28541] = 13995,
            [28542] = 13995, [28543] = 13995, [28784] = 13991, [28797] = 13992, [28985] = 13993, [28997] = 13994, [29198] = 13939, [29308] = 13879,
            [29691] = 13997, [29719] = 1, [29813] = 13998, [30281] = 14000, [30407] = 13999, [30706] = 14001, [30733] = 14002, [30747] = 14003,
            [30864] = 14004, [31234] = 1, [32009] = 14005, [32283] = 13947, [32411] = 14006, [32415] = 14007, [32883] = 13980, [33057] = 13867,
            [33060] = 13998, [33083] = 13999, [33084] = 13931, [33096] = 1996, [33102] = 13918, [33140] = 13978, [33232] = 14003, [33299] = 1,
            [33300] = 1, [33301] = 1, [33302] = 1, [33303] = 1, [33304] = 1, [33305] = 1, [33306] = 1, [33475] = 1,
            [33476] = 1, [33477] = 1, [33478] = 1, [33479] = 1, [33480] = 1, [33481] = 1, [33482] = 1, [33483] = 1,
            [33484] = 1, [33485] = 1, [33486] = 1, [33487] = 1, [33488] = 1, [33489] = 1, [33782] = 13982, [34581] = 13970,
            [35042] = 13947, [35245] = 14012, [35306] = 14008, [35823] = 14036, [36080] = 13905, [36088] = 1996, [36089] = 23, [36090] = 23,
            [36107] = 1, [36113] = 14035, [36116] = 13941, [36194] = 14011, [36229] = 13889, [36271] = 14009, [36477] = 14014, [37041] = 13927,
            [37043] = 14017, [37045] = 14020, [37531] = 21, [37615] = 13890, [38238] = 13909, [38239] = 13895, [38240] = 13904, [38241] = 1,
            [38242] = 1, [39368] = 14022, [39866] = 14024, [42044] = 23, [42045] = 23, [42046] = 1, [42047] = 1, [42211] = 14021,
            [42212] = 14028, [42213] = 14027, [42214] = 14026, [42215] = 14025, [42334] = 13969, [43289] = 14030, [43775] = 23, [43776] = 1,
            [44022] = 23, [44023] = 1, [44141] = 14033, [44142] = 14032, [44699] = 14031, [44700] = 14034, [44703] = 23, [44704] = 1,
            [44838] = 23, [44839] = 1, [44855] = 23, [44856] = 1, [44869] = 23, [44870] = 23, [44871] = 23, [44872] = 1,
            [44873] = 1, [44874] = 1, [45102] = 13892, [46151] = 14036, [46152] = 14037, [46172] = 13872, [46429] = 14038, [46541] = 14038,
            [46560] = 13956, [46913] = 13970, [47255] = 1, [47263] = 14039, [47270] = 13890, [47376] = 13939, [47377] = 21, [47378] = 13999,
            [47379] = 13999, [47904] = 14041, [48954] = 1, [49015] = 23, [49221] = 23, [49222] = 1, [49675] = 13999, [49677] = 14003,
            [50057] = 13931, [50155] = 14042, [50206] = 13956, [50215] = 13956, [50218] = 23, [50219] = 14038, [50220] = 14038, [50221] = 13956,
            [50740] = 14026, [50765] = 14044, [50949] = 13941, [50967] = 13921, [51080] = 13999, [51081] = 14003, [51090] = 13867, [51108] = 13970,
            [51113] = 13931, [51314] = 14046,
        };

        /// <summary>
        /// The damage types a creature drawn with this mesh has Critical Death animations for:
        /// none for a skeleton that has none, and null for a mesh the client's table does not
        /// know.
        /// </summary>
        public static IReadOnlyList<DamageType> For(int meshId)
        {
            if (!SkeletonOfMesh.TryGetValue(meshId, out var skeleton))
                return null;

            return BySkeleton.TryGetValue(skeleton, out var types) ? types : System.Array.Empty<DamageType>();
        }

        /// <summary>The skeleton a mesh is drawn on (animationdata.meshSkeleton), or null for a mesh the table does not know.</summary>
        public static int? SkeletonOf(int meshId) => SkeletonOfMesh.TryGetValue(meshId, out var skeleton) ? skeleton : null;

        private static DamageType[] Types(params int[] types) => System.Array.ConvertAll(types, type => (DamageType)type);
    }
}
