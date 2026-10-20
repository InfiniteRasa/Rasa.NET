using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Every creature class's flags as set in the monster flag editor (gametools/monster-flag-editor.html,
    /// its creature_flags.json): what the client's creatureflag table gives each of the 421 classes
    /// with the Creature augmentation - kingdom, sentience, origin, power, armour and chitin level,
    /// experience modifier, substance, species, the damage and control resistances, immunities and
    /// vulnerabilities, the target frame's borders and the rest (Creature_flags).
    ///
    /// The editor's file holds each class's flags and the baseline it started from, the seed's
    /// (CreatureClassFlagPreloader); this is the difference between the two and nothing else, so
    /// what came in after the export - the wrecks' ALT_MESH and ALT_MESH_DELAYED_3500
    /// (WreckCreatures) - stays as it is. 3612 flags go in over 340 classes and 14 come out: a
    /// Machina is MACHINA alone, no longer MECHANICAL as well, and the Necromite, Nitroglazer and
    /// Predator are MECHANICAL rather than BIOLOGICAL.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: a flag goes in only where the class has none
    /// of it, and Down takes out what Up put in and puts back what it took out.
    /// </summary>
    public static class CreatureFlagEdits
    {
        public sealed record Edit(uint ClassId, uint[] Add, uint[] Remove = null)
        {
            public uint[] Removed => Remove ?? new uint[0];
        }

        /// <summary>By class: the flags it gains, and the ones it loses.</summary>
        public static readonly Edit[] Edits =
        {
            new(753, new uint[] { 1,2,5,15,19,26,31,53,88 }), // KirinQueen
            new(3762, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Pistol
            new(3781, new uint[] { 4,14,15,25,31,41,53,119,145 }), // Vehicle_Bane_Stalker
            new(3846, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_Swapset_Male
            new(3848, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_Swapset_Female
            new(3868, new uint[] { 1,4,14,15,24,30,40,110,111 }, new uint[] { 11 }), // Bane_Hominis_Machina
            new(3901, new uint[] { 1,4,15,19,25,31,40,48,53,109,110 }), // Bane_Kael_Armored
            new(3902, new uint[] { 1,4,11,14,15,24,30,40,112,115,145 }, new uint[] { 5 }), // Vehicle_Bane_Predator
            new(3981, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Swapset_Female
            new(3982, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Swapset_Male
            new(4046, new uint[] { 1,4,15,19,24,30,40,47,53,109,110 }), // Bane_Kael_Standard
            new(4047, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Rifle
            new(4064, new uint[] { 1,7,14,15,24,30 }), // Emplacement_AFS_Turret_Standard
            new(4261, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Skitterin
            new(4313, new uint[] { 1,3,4,14,15,23,41,47,53,110,111 }), // Bane_Fithik_Winged
            new(4380, new uint[] { 1,5,7,9,13,19,24,30,52,53,92 }), // Redshirt_Corman_Male
            new(4381, new uint[] { 1,5,7,9,13,19,24,30,52,53,92 }), // Redshirt_Corman_Female
            new(6021, new uint[] { 1,8,14,15,24,30,53,135 }), // Creature_Mox
            new(6029, new uint[] { 1,4,11,14,15,20,23,30,40,112 }, new uint[] { 5 }), // Bane_Necromite
            new(6031, new uint[] { 1,8,14,15,23,30,47,116,117,126,147 }), // Creature_Boargar_Standard
            new(6032, new uint[] { 1,4,14,15,23,30,40,128,142,143 }), // Bane_Amoeboid_v1
            new(6038, new uint[] { 1,8,14,15,24,31,48,53,115 }), // Creature_Treeback_Adult
            new(6039, new uint[] { 8,14,15,24,30,53,118,122,123,142 }), // Creature_Treelurker
            new(6040, new uint[] { 1,8,14,15,23,30,53,142 }), // Creature_Treemite
            new(6043, new uint[] { 1,8,13,19,21,24,30,52,53 }), // Redshirt_Forean_Spearman
            new(6138, new uint[] { 1,8,15,19,21,24,30,53 }), // FIXME_NPC_Forean_Warrior
            new(6163, new uint[] { 1,8,13,19,21,24,30,53 }), // Redshirt_Forean_Elder
            new(6167, new uint[] { 1,8,15,19,24,30,47,53,109,143,144 }), // Creature_Swamp_Grubber
            new(6170, new uint[] { 1,8,14,15,23,30,111,112,142 }), // Creature_Miasma
            new(6203, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Lumin
            new(6236, new uint[] { 1,4,8,14,15,24,30,110,111 }, new uint[] { 11 }), // Bane_Forean_Machina
            new(6239, new uint[] { 1,8,13,19,21,24,30,53 }), // Redshirt_Forean_Gunner
            new(6262, new uint[] { 1,8,14,15,24,30,53,119,141 }), // Creature_Warnet_Soldier
            new(6263, new uint[] { 1,8,14,15,25,31,53,56,119,141 }), // Creature_Warnet_Queen_Boss
            new(6332, new uint[] { 1,3,8,14,15,23,109,111,142 }), // Creature_Maw
            new(6338, new uint[] { 1,2,14,15,24,31,47,53,125,126,131,143,144 }), // Creature_Magmonix
            new(6339, new uint[] { 1,5,7,9,13,19,24,30,53,92 }), // NPC_Corman_Swapset_Male
            new(6340, new uint[] { 1,5,7,9,13,19,24,30,53,92 }), // NPC_Corman_Swapset_Female
            new(6415, new uint[] { 1,8,13,19,21,24,30,52,53 }), // Redshirt_Forean_Archer
            new(6421, new uint[] { 1,3,8,14,15,22,115 }), // Creature_Filcher
            new(6441, new uint[] { 1,2,3,14,15,23,135,143 }), // Creature_Beam_Manta
            new(6960, new uint[] { 1,11,14,15,24,30,40,53,128,129,147 }), // Creature_Prison_Bot_Reconstructor
            new(7033, new uint[] { 1,8,13,19,21,24,30,52,53 }), // NPC_Forean_Gunner
            new(7034, new uint[] { 1,8,13,19,21,24,30,53 }), // NPC_Forean_Spearman
            new(7035, new uint[] { 1,8,13,19,21,24,30,53 }), // NPC_Forean_Shaman
            new(7036, new uint[] { 1,8,13,19,21,24,30,53 }), // NPC_Forean_Archer
            new(7043, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Technician
            new(7078, new uint[] { 1,2,14,15,24,30,47,53,146 }), // Creature_Atta_Harvester_Standard
            new(7079, new uint[] { 1,2,15,19,27,32,47,53,56 }), // Creature_Atta_Queen_Boss
            new(7081, new uint[] { 1,2,14,15,25,30,47,53,109,110,126,135,146 }), // Creature_Atta_Soldier_Standard
            new(7120, new uint[] { 1,4,14,15,23,30,40,52,53,130,141 }), // Bane_Lightbender_Standard
            new(7233, new uint[] { 1,4,14,15,24,30,40,53,128,135 }), // Bane_Shield_Drone
            new(7234, new uint[] { 1,2,4,11,14,15,24,30,53,111,114,119,145 }, new uint[] { 5 }), // Bane_Nitroglazer
            new(7235, new uint[] { 1,3,7,11,13,14,22,148 }), // AccountReward_Pet_BooBot
            new(7236, new uint[] { 1,2,3,14,15,23,47,53,119 }), // Creature_Barb_Tick
            new(7253, new uint[] { 1,2,13,14,21,24,30,52,53 }), // Redshirt_Brann_Male
            new(7254, new uint[] { 1,2,13,14,21,24,30,52,53 }), // Redshirt_Brann_Female
            new(7336, new uint[] { 1,4,14,15,23,30,40,53,131 }), // Bane_Howler
            new(7338, new uint[] { 1,2,14,15,24,30,53,110,143,146 }), // Creature_Flaregasher
            new(7339, new uint[] { 1,3,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Geyser_Hopper
            new(7340, new uint[] { 1,3,5,13,14,22,41,53 }), // Pet_Sonic_Glider
            new(7344, new uint[] { 1,3,4,14,15,23,40,112,114,145 }), // Bane_Sentinel
            new(7477, new uint[] { 1,2,14,15,24,30,128,130,142,148 }), // Creature_Lasher
            new(7482, new uint[] { 1,14,15,24,30,40,145 }), // Emplacement_Bane_Turret_Standard
            new(7510, new uint[] { 1,4,14,15,23,30,40,53,128,141 }), // Creature_Xanx_Standard
            new(7586, new uint[] { 1,4,14,15,23,30,40,53,128,141 }), // Bane_Xanx
            new(7696, new uint[] { 1,4,14,15,23,30,40,128,142,143 }), // Bane_Amoeboid_v2
            new(7722, new uint[] { 1,4,14,15,25,31,40,53,109,112,145 }), // Vehicle_Bane_Juggernaut
            new(7747, new uint[] { 1,3,7,11,13,14,22,148 }), // AccountReward_Pet_PineOck
            new(7775, new uint[] { 1,2,13,14,21,24,30,52,53 }), // NPC_Brann_Swapset_Female
            new(7776, new uint[] { 1,2,13,19,21,24,30,53 }), // NPC_Brann_Swapset_Male
            new(8092, new uint[] { 1,2,14,15,24,30,47,53,146 }), // NPC_Creature_Atta_Harvester
            new(8093, new uint[] { 1,2,14,15,25,30,47,53,109,110,126,135,146 }), // NPC_Creature_Atta_Soldier
            new(8094, new uint[] { 1,2,15,19,27,32,47,53,56 }), // NPC_Creature_Atta_Queen_Boss
            new(9244, new uint[] { 1,4,15,19,24,30,41,53,112,114,119 }), // Bane_Caretaker
            new(9284, new uint[] { 1,5,7,9,13,19,24,30,52,53,106 }), // Redshirt_Creature_Earth_AFS_Heapsaw
            new(9354, new uint[] { 1,4,15,19,24,30,40,53,119,142,144 }), // Bane_Thrax_Grenadier
            new(9554, new uint[] { 1,3,7,11,13,14,22,148 }), // AccountReward_Pet_ShellBot
            new(9737, new uint[] { 1,4,11,14,15,20,23,30,40,112 }), // Bane_Psychomite
            new(9750, new uint[] { 1,4,8,14,15,24,30,48,53,115,118,122,123,142 }), // Bane_Treeback
            new(9751, new uint[] { 1,4,15,19,24,30,40,52,53,119,142,144 }), // Bane_Linker
            new(9804, new uint[] { 1,4,15,19,25,31,40,53,112,114,145 }), // Vehicle_Bane_Strider_V1
            new(10162, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Swamp_Rat
            new(10164, new uint[] { 1,2,14,15,23,30,53,109,126,146 }), // Creature_Granitour_Young
            new(10165, new uint[] { 1,2,3,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Kalastride
            new(10166, new uint[] { 1,4,15,19,24,30,40,53,119,142,144 }), // Bane_Hunter
            new(10167, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Rhegit
            new(10168, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Yimma
            new(10169, new uint[] { 1,8,15,19,22,30,47,53,109,143,144 }), // Ambient_Moss_Grubber
            new(10215, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Cavern_Creepa
            new(10216, new uint[] { 1,2,3,5,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Cavern_Fruko
            new(10217, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Fromada
            new(10218, new uint[] { 1,2,3,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Lavar
            new(10219, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Lognar
            new(10220, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Mush_Digger
            new(10221, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Spore_Hitcher
            new(10222, new uint[] { 1,5,15,19,21,28,33,38,53 }), // Boss_Eloh
            new(10225, new uint[] { 1,4,5,15,19,28,33,40,51,56 }), // Boss_Voro_A
            new(10232, new uint[] { 1,3,5,13,14,22,41,53 }), // Pet_Lightweaver
            new(10235, new uint[] { 1,7,11,13,14,25,31,47,53,106,145 }), // Vehicle_AFS_Mech
            new(10240, new uint[] { 1,8,14,15,24,30,56,111,112,142 }), // Creature_Miasma_Boss
            new(10310, new uint[] { 1,4,14,15,25,31,40,53,56,128,142,143 }), // Bane_Amoeboid_v1_Boss
            new(10312, new uint[] { 1,4,14,15,25,31,40,53,56,128,142,143 }), // Bane_Amoeboid_v2_Boss
            new(10314, new uint[] { 1,11,14,15,25,31,40,53,56,128,129,147 }), // Creature_Prison_Bot_Reconstructor_Boss
            new(10317, new uint[] { 1,2,14,15,24,31,56,135,143 }), // Creature_Beam_Manta_Boss
            new(10319, new uint[] { 1,8,14,15,25,31,47,53,56,116,117,126,147 }), // Creature_Boargar_Standard_Boss
            new(10321, new uint[] { 1,4,15,19,25,31,41,53,56,112,114,119 }), // Bane_Caretaker_Boss
            new(10323, new uint[] { 1,4,14,15,24,30,40,47,56,110,111 }), // Bane_Fithik_Winged_Boss
            new(10325, new uint[] { 1,2,14,15,25,31,53,56,110,143,146 }), // Creature_Flaregasher_Boss
            new(10327, new uint[] { 1,4,15,19,24,30,40,53,119,142,144 }), // Bane_Hunter_Boss
            new(10331, new uint[] { 1,2,14,15,25,31,56,128,130,142,148 }), // Creature_Lasher_Boss
            new(10333, new uint[] { 1,4,14,15,24,31,40,52,53,56,130,141 }), // Bane_Lightbender_Standard_Boss
            new(10335, new uint[] { 1,4,15,19,25,31,40,53,56,119,142,144 }), // Bane_Linker_Boss
            new(10337, new uint[] { 1,2,4,14,15,25,31,53,56,111,114,119,145 }), // Bane_Nitroglazer_Boss
            new(10339, new uint[] { 1,4,14,15,24,30,40,56,112,114,145 }), // Bane_Sentinel_Boss
            new(10343, new uint[] { 1,8,14,15,24,31,48,53,56,115 }), // Creature_Treeback_Adult_Boss
            new(10347, new uint[] { 8,14,15,25,31,53,56,118,122,123,142 }), // Creature_Treelurker_Boss
            new(10348, new uint[] { 1,8,14,15,24,30,53,56,142 }), // Creature_Treemite_Boss
            new(10442, new uint[] { 1,7,11,14,15,25,31,53,106,145 }), // NPC_Vehicle_AFS_Mech
            new(10502, new uint[] { 1,4,15,19,25,31,40,52,53,54,56,119,142,144 }), // Bane_Thrax_Technician_Boss
            new(10503, new uint[] { 1,4,15,19,25,31,40,52,53,54,56,119,142,144 }), // Bane_Thrax_Soldier_Pistol_Boss
            new(10504, new uint[] { 1,4,15,19,25,31,40,53,56,119,142,144 }), // Bane_Thrax_Soldier_Rifle_Boss
            new(10509, new uint[] { 1,14,15,24,30,40,145 }), // Emplacement_Bane_Turret_Mini
            new(10510, new uint[] { 1,4,14,15,24,31,40,53,56,131 }), // Bane_Howler_Boss
            new(10522, new uint[] { 1,4,14,15,24,30,40,53,56,128,141 }), // Creature_Xanx_Standard_Boss
            new(10523, new uint[] { 1,4,14,15,28,33,40,53,128,141 }), // Bane_Xanx_Boss
            new(10606, new uint[] { 1,7,13,19,24,30,53 }), // NPC_Creature_Boargar
            new(10609, new uint[] { 1,7,9,13,19,24,30,53,56 }), // NPC_Human_Swapset_Female_Boss
            new(10610, new uint[] { 1,7,9,13,19,24,30,53,56 }), // NPC_Human_Swapset_Male_Boss
            new(10612, new uint[] { 1,4,15,19,25,31,40,53,56,119,142,144 }), // NPC_Bane_Thrax_Grenadier_Boss
            new(10642, new uint[] { 1,4,15,19,24,30,40,52,53,119,142,144 }, new uint[] { 11 }), // NPC_Hominis_Machina
            new(10854, new uint[] { 1,4,8,14,15,23,30,53,135 }), // Bane_Mox_Standard
            new(10857, new uint[] { 1,4,14,15,23,30,40,130,141 }), // Bane_Lightbender_Alternate
            new(10858, new uint[] { 1,8,14,15,23,30,111,112,142 }), // Bane_Miasma_Standard
            new(10989, new uint[] { 1,4,14,15,25,31,40,53 }), // Vehicle_Bane_Treecutter
            new(11018, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pod_Crawler
            new(11065, new uint[] { 1,4,5,15,19,28,33,40,51,56 }), // Boss_Voro_B
            new(11066, new uint[] { 1,4,5,15,19,28,33,40,51,56 }), // Boss_Voro_C
            new(11067, new uint[] { 1,4,5,15,19,28,33,40,51,56 }), // Boss_Voro_D
            new(11068, new uint[] { 1,4,5,15,19,28,33,40,51,56 }), // Boss_Voro_E
            new(11134, new uint[] { 1,5,15,19,21,28,33,39,53 }), // Boss_Enslaver
            new(11302, new uint[] { 1,7,9,14,15,24,30,145 }), // Emplacement_AFS_Turret_Mini
            new(11319, new uint[] { 1,4,5,15,19,28,33,40,51,56 }), // Boss_Voro_Mouth
            new(11320, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // NPC_Bane_Thrax_Technician
            new(20698, new uint[] { 1,3,4,14,15,23,40,47,53,110,111 }), // Bane_Fithik_Wingless
            new(20757, new uint[] { 1,4,15,19,24,30,40,53,119,142,144 }), // Bane_Thrax_Soldier_Pistol_NoBeaminBirth
            new(20766, new uint[] { 1,4,5,15,19,28,31,40,53,56 }), // Boss_Nergal
            new(20767, new uint[] { 1,4,5,15,19,28,31,40,53,56,148 }), // Boss_Nergal_Chair
            new(20768, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Melee
            new(20780, new uint[] { 1,4,15,19,25,31,40,52,53,56,119,142,144 }, new uint[] { 11 }), // NPC_Hominis_Machina_Boss
            new(20972, new uint[] { 1,7,9,13,19,24,30,53 }), // Vendor_Human_Female
            new(20975, new uint[] { 1,7,9,13,19,24,30,53 }), // Vendor_Human_Male
            new(21035, new uint[] { 1,4,15,19,25,31,40,53,56 }), // NPC_Creature_Sentinel_Boss
            new(21036, new uint[] { 1,2,5,13,14,21,24,30,53,92 }), // Vendor_Corman_Male
            new(21037, new uint[] { 1,2,5,13,14,21,24,30,53,92 }), // Vendor_Corman_Female
            new(21470, new uint[] { 1,2,3,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Grisel
            new(21474, new uint[] { 1,4,7,14,15,24,30,110,111 }, new uint[] { 11 }), // Bane_Hominis_Machina_Boss
            new(21499, new uint[] { 1,3,4,14,15,23,40,47,53,110,111 }), // Bane_Fithik_Wingless_EggCluster
            new(21503, new uint[] { 1,8,14,15,23,30,47,53,115 }), // Creature_Treeback_Cub
            new(21580, new uint[] { 1,3,5,7,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Fish
            new(21758, new uint[] { 1,4,15,19,25,31,40,53,112,114,145 }), // Vehicle_Bane_Strider_V2
            new(21795, new uint[] { 1,8,14,15,24,30,53,119,141 }), // Creature_Warnet_Soldier_10m
            new(21796, new uint[] { 1,8,14,15,24,30,53,119,141 }), // Creature_Warnet_Soldier_15m
            new(21797, new uint[] { 1,8,14,15,24,30,53,119,141 }), // Creature_Warnet_Soldier_20m
            new(21798, new uint[] { 1,8,14,15,24,30,53,119,141 }), // Creature_Warnet_Soldier_30m
            new(21799, new uint[] { 1,8,14,15,25,31,53,56,119,141 }), // Creature_Warnet_Queen_10m_Boss
            new(21800, new uint[] { 1,8,14,15,25,31,53,56,119,141 }), // Creature_Warnet_Queen_15m_Boss
            new(21801, new uint[] { 1,8,14,15,25,31,53,56,119,141 }), // Creature_Warnet_Queen_20m_Boss
            new(21802, new uint[] { 1,8,14,15,25,31,53,56,119,141 }), // Creature_Warnet_Queen_30m_Boss
            new(21847, new uint[] { 1,4,8,14,15,23,30,53,135 }), // Bane_Mox_Infected
            new(21882, new uint[] { 1,4,15,19,25,31,40,53,56,119,142,144 }), // Bane_Thrax_Grenadier_Boss
            new(21897, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_Netgun_Female
            new(21898, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_Netgun_Male
            new(21899, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_Pistol_Female
            new(21900, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_Pistol_Male
            new(21901, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_PolarityGun_Female
            new(21902, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_PolarityGun_Male
            new(21903, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_Rifle_Female
            new(21904, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_Rifle_Male
            new(21905, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_LeechGun_Female
            new(21906, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_LeechGun_Male
            new(21907, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_Shotgun_Female
            new(21908, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_Shotgun_Male
            new(21909, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_Pistol_Female
            new(21910, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_Pistol_Male
            new(21911, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_Rifle_Female
            new(21912, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T2_Rifle_Male
            new(21913, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_MachineGun_Female
            new(21914, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_MachineGun_Male
            new(21915, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_RocketLauncher_Female
            new(21916, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_RocketLauncher_Male
            new(21917, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_LeechGun_Female
            new(21918, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_LeechGun_Male
            new(21919, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_InjectionGun_Female
            new(21920, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_T3_InjectionGun_Male
            new(21986, new uint[] { 1,8,14,15,23,30,111,112,142 }), // Bane_Miasma_Infected
            new(22089, new uint[] { 1,4,8,13,14,24,30,110,111 }, new uint[] { 11 }), // Bane_Forean_Machina_NO_XP
            new(22120, new uint[] { 1,5,9,13,19,24,30,38,52,53 }), // Redshirt_Eloh
            new(22627, new uint[] { 1,2,14,15,25,31,47,53,56,109,110,126,135,146 }), // Creature_Atta_Soldier_Standard_Boss
            new(22635, new uint[] { 1,8,13,19,21,24,30,53 }), // Vendor_Forean_Female
            new(22636, new uint[] { 1,8,13,19,21,24,30,53 }), // Vendor_Forean_Shaman
            new(22637, new uint[] { 1,8,13,19,21,24,30,53 }), // Vendor_Forean_Gunner
            new(22638, new uint[] { 1,8,13,19,21,24,30,52,53 }), // Vendor_Forean_Warrior
            new(22949, new uint[] { 1,4,8,14,15,24,30,56,110,111 }, new uint[] { 11 }), // Bane_Forean_Machina_Boss
            new(22975, new uint[] { 1,4,8,14,15,24,31,53,135 }), // Creature_Mox_Boss
            new(22976, new uint[] { 1,8,14,15,24,30,56,111,112,142 }), // Bane_Miasma_Infected_Boss
            new(22977, new uint[] { 1,4,8,14,15,24,31,53,135 }), // Bane_Mox_Standard_Boss
            new(22978, new uint[] { 1,4,8,14,15,24,31,53,56,135 }), // Bane_Mox_Infected_Boss
            new(22980, new uint[] { 1,8,14,15,24,30,56,109,111,142 }), // Creature_Maw_Boss
            new(22981, new uint[] { 1,4,15,19,26,31,40,47,53,56,112,114,145 }), // Vehicle_Bane_Strider_v2_Boss
            new(23089, new uint[] { 1,8,13,19,21,24,30,52,53 }), // NPC_Retread_Forean_Female
            new(23090, new uint[] { 1,8,13,19,21,24,30,52,53 }), // NPC_Retread_Forean_Male
            new(23121, new uint[] { 1,7,11,13,14,25,31,53,106 }), // Vehicle_AFS_Hovertank
            new(23124, new uint[] { 1,4,14,15,23,30,40,53,128,141 }), // Creature_Xanx_Albino
            new(23125, new uint[] { 1,4,15,19,24,30,40,52,53,119,142,144 }, new uint[] { 11 }), // Bane_Thrax_Machina
            new(23130, new uint[] { 1,2,3,5,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Frese
            new(23191, new uint[] { 1,2,13,14,21,24,30,52,53 }), // Redshirt_Brann_Incurable_Male
            new(23192, new uint[] { 1,4,15,19,25,31,40,53,112,114,145 }), // NPC_Vehicle_Bane_Strider
            new(23902, new uint[] { 1,2,14,15,24,30 }), // Emplacement_AFS_Turret_Brann
            new(24057, new uint[] { 1,7,11,13,14,24,30,52,53,106,148 }), // Vehicle_AFS_Hoverbike
            new(24060, new uint[] { 1,8,14,15,23,30,47,116,117,126,147 }), // Creature_Boargar_Wooly
            new(24061, new uint[] { 1,8,14,15,24,31,47,53,56,116,117,126,147 }), // Creature_Boargar_Wooly_Boss
            new(24067, new uint[] { 1,2,13,14,21,24,30,53 }), // Vendor_Brann_Female
            new(24068, new uint[] { 1,2,13,14,21,24,30,53 }), // Vendor_Brann_Male
            new(24069, new uint[] { 1,4,14,15,24,30,40,47,56,110,111 }), // Bane_Fithik_Wingless_Boss
            new(24070, new uint[] { 1,4,14,15,24,30,40,47,56,110,111 }), // Bane_Fithik_Wingless_EggCluster_Boss
            new(24077, new uint[] { 1,4,14,15,24,30,40,52,53,56,130,141 }), // Bane_Lightbender_Alternate_Boss
            new(24078, new uint[] { 1,8,14,15,24,30,56,111,112,142 }), // Bane_Miasma_Standard_Boss
            new(24084, new uint[] { 1,4,14,15,25,31,40,53,56,128,135 }), // Bane_Shield_Drone_Boss
            new(24085, new uint[] { 1,4,15,19,25,31,40,52,53,56,119,142,144 }, new uint[] { 11 }), // Bane_Thrax_Machina_Boss
            new(24086, new uint[] { 1,4,8,14,15,25,31,48,53,56,115,118,122,123,142 }), // Bane_Treeback_Boss
            new(24087, new uint[] { 1,2,15,19,26,32,47,53 }), // Creature_Atta_Queen
            new(24088, new uint[] { 1,2,14,15,25,31,48,53,56,125,126,131,143,144 }), // Creature_Magmonix_Boss
            new(24089, new uint[] { 1,8,15,19,25,31,48,53,56,109,143,144 }), // Creature_Swamp_Grubber_Boss
            new(24092, new uint[] { 1,4,14,15,23,30,40,128,142,143 }), // Bane_Amoeboid_v1_Child
            new(24093, new uint[] { 1,4,14,15,23,30,40,128,142,143 }), // Bane_Amoeboid_v2_Child
            new(24548, new uint[] { 1,4,15,19,26,31,40,47,53,56,112,114,145 }), // Vehicle_Bane_Strider_V1_Boss
            new(24555, new uint[] { 1,4,14,15,26,31,40,53,56,109,112,145 }), // Vehicle_Bane_Juggernaut_Boss
            new(24560, new uint[] { 1,2,13,14,21,24,31,52,53,56 }), // Redshirt_Brann_Female_Boss
            new(24561, new uint[] { 1,2,13,14,21,24,31,52,53,56 }), // Redshirt_Brann_Male_Boss
            new(24562, new uint[] { 1,2,13,14,21,24,30,52,53,56 }), // NPC_Brann_Swapset_Female_Boss
            new(24564, new uint[] { 1,2,13,14,21,24,30,52,53,56 }), // NPC_Brann_Swapset_Male_Boss
            new(24592, new uint[] { 1,2,14,15,25,31,47,53,56,146 }), // Creature_Atta_Harvester_Standard_Boss
            new(24883, new uint[] { 1,3,11,13,14,22,38 }), // NPC_Eloh_Holgram_Large
            new(25413, new uint[] { 1,4,15,19,25,31,40,47,53,56,109,110 }), // Bane_Kael_Standard_Boss
            new(25414, new uint[] { 1,4,15,19,26,31,40,48,53,56,109,110 }), // Bane_Kael_Armored_Boss
            new(25547, new uint[] { 1,2,13,14,21,24,31,52,53,56 }), // Redshirt_Brann_Incurable_Male_Boss
            new(25580, new uint[] { 1,4,13,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Pistol_NO_XP
            new(25581, new uint[] { 1,4,13,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Rifle_NO_XP
            new(25603, new uint[] { 1,8,14,15,24,31,48,53,115 }), // Creature_Treeback_Highland
            new(25607, new uint[] { 1,8,13,19,21,24,30,52,53 }), // Redshirt_Forean_Gunner_Infected
            new(25608, new uint[] { 1,4,5,14,15,24,30,40,53 }), // Bane_Ichaiden
            new(25609, new uint[] { 1,4,14,15,25,31,40,53,109,112,145 }), // Vehicle_Bane_Juggernaut_Statue
            new(25610, new uint[] { 1,2,14,15,24,30,47,53,109,126,146 }), // Creature_Granitour_Adult
            new(25611, new uint[] { 1,2,13,14,21,24,30,52,53 }), // Redshirt_Brann_Incurable_Female
            new(25612, new uint[] { 1,2,14,15,24,30,47,53,109,110,126,135,146 }), // Creature_Atta_Soldier_Young
            new(25616, new uint[] { 1,8,14,15,24,31,48,53,56,115 }), // Creature_Treeback_Highland_Boss
            new(25617, new uint[] { 1,2,13,14,21,24,31,52,53,56 }), // Redshirt_Brann_Incurable_Female_Boss
            new(25618, new uint[] { 1,8,13,19,21,24,30,52,53,56 }), // Redshirt_Forean_Gunner_Infected_Boss
            new(25619, new uint[] { 1,4,5,14,15,24,30,40,53,56 }), // Bane_Ichaiden_Boss
            new(25620, new uint[] { 1,2,14,15,25,31,47,53,56,109,126,146 }), // Creature_Granitour_Adult_Boss
            new(25882, new uint[] { 1,8,13,19,21,25,31,52,53,56 }), // NPC_Forean_Gunner_Infected_Boss
            new(25884, new uint[] { 1,4,15,19,24,30,40,52,53,119,142,144 }, new uint[] { 11 }), // NPC_Bane_Thrax_Machina
            new(25885, new uint[] { 1,4,15,19,25,31,40,52,53,56,119,142,144 }, new uint[] { 11 }), // NPC_Bane_Thrax_Machina_Boss
            new(25945, new uint[] { 1,3,8,11,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Salvage_Bot_Reconstructor
            new(26009, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_MiniSwapset_Female
            new(26011, new uint[] { 1,7,9,13,19,24,30,53,56 }), // NPC_Human_MiniSwapset_Female_Boss
            new(26013, new uint[] { 1,7,9,13,19,24,30,53,56 }), // NPC_Human_MiniSwapset_Male_Boss
            new(26014, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_MiniSwapset_Male
            new(26016, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_MiniSwapset_Male
            new(26017, new uint[] { 1,7,9,13,19,24,30,52,53,56 }), // Redshirt_Human_MiniSwapset_Male_Boss
            new(26018, new uint[] { 1,7,9,13,19,24,30,52,53,56 }), // Redshirt_Human_MiniSwapset_Female_Boss
            new(26019, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_MiniSwapset_Female
            new(26022, new uint[] { 1,2,14,15,24,30,47,53,146 }), // Creature_Atta_Harvester_DropDown
            new(26023, new uint[] { 1,2,14,15,25,31,47,53,56,146 }), // Creature_Atta_Harvester_DropDown_Boss
            new(26024, new uint[] { 1,2,14,15,25,30,47,53,109,110,126,135,146 }), // Creature_Atta_Soldier_DropDown
            new(26025, new uint[] { 1,2,14,15,25,31,47,53,56,109,110,126,135,146 }), // Creature_Atta_Soldier_DropDown_Boss
            new(26353, new uint[] { 1,4,15,19,25,31,40,52,53,54,56,119,142,144 }), // NPC_Bane_Thrax_Technician_Boss
            new(26395, new uint[] { 1,11,14,15,24,30,40,53,128,129,147 }), // NPC_Prison_Bot_Warden
            new(26396, new uint[] { 1,11,14,15,25,31,40,53,56,128,129,147 }), // NPC_Prison_Bot_Warden_Boss
            new(26706, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_Male_Child
            new(26707, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_Female_Child
            new(26713, new uint[] { 1,3,11,13,14,22,38 }), // NPC_Eloh_Hologram_Small
            new(26833, new uint[] { 1,8,13,19,21,24,30,52,53 }), // NPC_Forean_Unarmed
            new(26834, new uint[] { 1,8,13,19,21,24,30,52,53 }), // Redshirt_Forean_Unarmed
            new(26868, new uint[] { 1,7,9,13,19,24,30,53 }), // NPC_Human_Military_Surplus
            new(28415, new uint[] { 1,8,15,19,21,24,30,52,53,56 }), // Redshirt_Forean_Elder_Traitor_Boss
            new(28589, new uint[] { 1,4,5,15,19,26,32,40,52,53,62 }), // NPC_Bane_Miniboss_Overseer_Skeev
            new(28696, new uint[] { 1,5,7,9,15,24,30,92 }), // NPC_Corman_Hermit_Male
            new(28697, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Female_Child
            new(28698, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Male_Child
            new(29052, new uint[] { 1,4,15,19,25,31,40,47,53,56,109,110 }), // Bane_Kael_V3_CP_Boss
            new(29056, new uint[] { 1,4,15,19,26,31,40,45,53,56,119,142,144 }), // Bane_Thrax_Grenadier_V2_CP_Boss
            new(29058, new uint[] { 1,4,15,19,25,31,40,52,53,56,119,142,144 }), // Bane_Linker_V3_CP_Boss
            new(29059, new uint[] { 1,4,15,19,25,31,40,52,53,54,56,119,142,144 }), // Bane_Thrax_Soldier_Pistol_V2_CP_Boss
            new(29219, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Lumin_Red
            new(29220, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Lumin_Blue
            new(29221, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Lumin_Green
            new(29222, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Lumin_Purple
            new(29289, new uint[] { 1,5,7,9,14,15,24,30,53,92 }), // Retread_Male_Fighter
            new(29290, new uint[] { 1,5,7,9,14,15,24,30,53,92 }), // Retread_Female_Fighter
            new(29366, new uint[] { 1,4,14,15,24,30,40,53 }), // Bane_Seeker
            new(29423, new uint[] { 1,7,9,13,19,24,30,53 }), // Redshirt_Human_Soldier_Light_Male
            new(29424, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Soldier_Light_Female
            new(29432, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Soldier_Heavy_Female
            new(29433, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Soldier_Heavy_Male
            new(29765, new uint[] { 1,7,9,13,19,24,30,52,53 }), // Redshirt_Human_Soldier_Light_Male_Crouch
            new(29769, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Grunt_Pistol
            new(29773, new uint[] { 1,4,15,19,24,30,40,52,53,54,119,142,144 }), // Bane_Thrax_Soldier_Grunt_Rifle
            new(29778, new uint[] { 1,4,15,19,24,30,40,52,53,119,142,144 }), // Bane_Linker_Grunt
            new(29951, new uint[] { 1,4,15,19,25,30,41,53,112,114,119 }), // Bane_Caretaker_Grunt
            new(30069, new uint[] { 1,3,7,11,13,14,20,22,53,106 }), // Creature_AccountReward_Resupply_Vendor_Bot
            new(30080, new uint[] { 1,4,14,15,25,31,40,53 }), // Vehicle_Bane_Ravager
            new(30122, new uint[] { 1,4,14,15,25,31,40,56,112,115,145 }), // Vehicle_Bane_Predator_Epic
            new(30132, new uint[] { 1,4,15,19,25,31,40,52,53,56,107,119,142,144 }), // Bane_Hunter_Lightning_Miniboss_EPIC
            new(30136, new uint[] { 1,7,9,13,19,24,30,52,53,56 }), // Redshirt_Human_Swapset_Female_Boss
            new(30137, new uint[] { 1,7,9,13,19,24,30,52,53,56 }), // Redshirt_Human_Swapset_Male_Boss
            new(30189, new uint[] { 1,4,14,15,24,31,40,53,56,107,131 }), // Bane_Howler_Lightning_MiniBoss_EPIC
            new(30191, new uint[] { 1,7,9,11,13,19,24,30,52,53,106 }), // Redshirt_Creature_AFS_Junkbot
            new(30195, new uint[] { 1,4,5,14,15,24,30,40,53,110,143 }), // Bane_Pyroglazer
            new(30196, new uint[] { 1,4,5,14,15,24,30,40,53,56,110,143 }), // Bane_Pyroglazer_Boss
            new(30326, new uint[] { 1,4,14,15,24,30,40,53,56 }), // Bane_Seeker_Boss
            new(30415, new uint[] { 1,4,15,19,24,30,41,53,107,112,114,119 }), // EPIC_Caretaker
            new(30416, new uint[] { 1,4,15,19,25,31,40,52,53,54,107,119,142,144 }), // EPIC_Thrax_Grenadier
            new(30417, new uint[] { 1,4,15,19,25,31,40,52,53,54,107,119,142,144 }), // EPIC_Thrax_Soldier_Pistol
            new(30418, new uint[] { 1,4,15,19,25,31,40,52,53,54,107,119,142,144 }), // EPIC_Thrax_Soldier_Rifle
            new(30419, new uint[] { 1,4,15,19,25,31,40,52,53,54,107,119,142,144 }), // EPIC_Thrax_Technician
            new(30619, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Cavern_Creepa
            new(30620, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Geyser_Hopper
            new(30621, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Grisel
            new(30622, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Kalastride
            new(30623, new uint[] { 1,3,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Lavar
            new(30624, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Lognar
            new(30625, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Mush_Digger
            new(30626, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Rhegit
            new(30627, new uint[] { 1,3,8,11,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Salvage_Bot_Reconstructor
            new(30628, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Skitterin
            new(30629, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Spore_Hitcher
            new(30630, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Swamp_Rat
            new(30631, new uint[] { 1,3,5,8,13,14,22,141,142,143,144,145,146,147,148 }), // Ambient_Pet_Yimma
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var edit in Edits)
            {
                if (edit.Removed.Length > 0)
                    yield return Delete(edit.ClassId, edit.Removed);

                if (edit.Add.Length > 0)
                    yield return Insert(edit.ClassId, edit.Add);
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var edit in Edits)
            {
                if (edit.Add.Length > 0)
                    yield return Delete(edit.ClassId, edit.Add);

                if (edit.Removed.Length > 0)
                    yield return Insert(edit.ClassId, edit.Removed);
            }
        }

        private static string Insert(uint classId, uint[] flags) =>
            "INSERT INTO creature_class_flag (class_id, flag_id) SELECT e.id, f.flag_id FROM entityclass e, (" +
            string.Join(" UNION ALL ", flags.Select(flag => $"SELECT {flag} AS flag_id")) + ") f " +
            $"WHERE e.id = {classId} AND NOT EXISTS (SELECT 1 FROM creature_class_flag x WHERE x.class_id = e.id AND x.flag_id = f.flag_id);";

        private static string Delete(uint classId, uint[] flags) =>
            $"DELETE FROM creature_class_flag WHERE class_id = {classId} AND flag_id IN ({string.Join(", ", flags)});";
    }
}
