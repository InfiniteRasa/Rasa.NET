namespace Rasa.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// The damage type of a weapon attack, by the action and action argument it is performed
    /// with. Generated from the client's generated.client.weaponclass: every weapon class names
    /// the attack action and argument it plays and the damage type it deals, so an attack that
    /// plays a given pair deals that pair's type. Nothing in the client marks a creature's damage
    /// type directly - its weapon is what carries it, and its attack is what names the weapon's
    /// pair.
    ///
    /// Where several weapon classes share a pair, the creature weapons among them decide it, and
    /// where those disagree the commonest wins; the comment on each line names a weapon behind it.
    /// A pair no weapon class claims is not here and is physical.
    /// </summary>
    public static class CreatureWeaponDamage
    {
        private static readonly Dictionary<(uint ActionId, uint ArgId), DamageType> Types = new Dictionary<(uint, uint), DamageType>
        {
            { (1, 1), DamageType.Laser },                               // Weapon_Creature_Bane_Dropship
            { (1, 3), DamageType.Physical },                            // ?, 7 classes disagree
            { (1, 66), DamageType.Sonic },                              // RECYCLE_Weapon_Avatar_Shotgun_Sonic_01_to_01
            { (1, 67), DamageType.Laser },                              // RECYCLE_Weapon_Avatar_Pistol_Laser_01_to_04
            { (1, 68), DamageType.Laser },                              // RECYCLE_Weapon_Avatar_Rifle_Laser_01_to_05
            { (1, 78), DamageType.Laser },                              // Weapon_Creature_Stalker
            { (1, 79), DamageType.Physical },                           // Weapon_Creature_Bane_Rifle
            { (1, 81), DamageType.Physical },                           // TEST_Weapon_Avatar_Bane_Pistol_v02, 2 classes disagree
            { (1, 85), DamageType.Laser },                              // Weapon_Creature_Predator
            { (1, 87), DamageType.Electrical },                         // Weapon_Creature_AFS_Turret
            { (1, 97), DamageType.Laser },                              // Weapon_Creature_Hominis_Machina
            { (1, 105), DamageType.Electrical },                        // Weapon_Avatar_Staff_Electric_CMN_30_to_34
            { (1, 109), DamageType.Physical },                          // Weapon_Creature_Necromite
            { (1, 116), DamageType.Physical },                          // Weapon_Creature_Forean_GooGun
            { (1, 121), DamageType.Physical },                          // TEST_Weapon_Avatar_Bane_Forean_Machina_Shotgun
            { (1, 122), DamageType.Fire },                              // RECYCLE_Weapon_Avatar_Shotgun_Fire_01_to_01
            { (1, 123), DamageType.Ice },                               // RECYCLE_Weapon_Avatar_Shotgun_Ice_01_to_to_01
            { (1, 128), DamageType.Fire },                              // Weapon_Avatar_Staff_Fire_CMN_30_to_34
            { (1, 129), DamageType.Ice },                               // TEST_Weapon_Avatar_Mutalis_Staff
            { (1, 130), DamageType.Physical },                          // Weapon_Creature_Filcher
            { (1, 132), DamageType.Physical },                          // TEST_Weapon_Avatar_Forean_Spear
            { (1, 133), DamageType.Physical },                          // TEST_HealingDisc_Pistol, 50 classes disagree
            { (1, 134), DamageType.Physical },                          // TEST_Weapon_Avatar_Mutalis_Rifle
            { (1, 135), DamageType.EMP },                               // RECYCLE_Weapon_Avatar_Pistol_EMP_01_to_04
            { (1, 136), DamageType.EMP },                               // RECYCLE_Weapon_Avatar_Rifle_EMP_01_to_05
            { (1, 145), DamageType.Physical },                          // Weapon_Creature_Forean_Bow
            { (1, 146), DamageType.Physical },                          // Weapon_Creature_Forean_Staff
            { (1, 147), DamageType.Laser },                             // RECYCLE_Weapon_Avatar_Shotgun_Laser_01_to_01
            { (1, 149), DamageType.Laser },                             // Weapon_Creature_Lightbender
            { (1, 151), DamageType.Physical },                          // Weapon_Creature_Healer_Pet
            { (1, 152), DamageType.Physical },                          // Weapon_Creature_Arieki_Bot_Reconstructor, 3 classes disagree
            { (1, 154), DamageType.Physical },                          // Weapon_Creature_Forean_Machina
            { (1, 168), DamageType.Electrical },                        // RECYCLE_Weapon_Avatar_Rifle_Electric_01_to_05
            { (1, 170), DamageType.Electrical },                        // RECYCLE_Weapon_Avatar_Pistol_Electric_01_to_04
            { (1, 177), DamageType.EMP },                               // Weapon_Avatar_Staff_EMP_CMN_30_to_34
            { (1, 180), DamageType.Physical },                          // Weapon_Creature_Juggernaut
            { (1, 181), DamageType.Sonic },                             // RECYCLE_Weapon_Avatar_Pistol_Sonic_01_to_04
            { (1, 182), DamageType.Sonic },                             // RECYCLE_Weapon_Avatar_Rifle_Sonic_01_to_05
            { (1, 185), DamageType.EMP },                               // RECYCLE_Weapon_Avatar_Shotgun_EMP_01_to_to_01
            { (1, 186), DamageType.Virulent },                          // Weapon_Avatar_Staff_Virulent_CMN_30_to_34
            { (1, 190), DamageType.Physical },                          // Weapon_Holographic_Caretaker
            { (1, 195), DamageType.Physical },                          // Weapon_Creature_Ambient
            { (1, 203), DamageType.Physical },                          // Weapon_Creature_Linker
            { (1, 204), DamageType.Physical },                          // Weapon_Creature_Strider
            { (1, 207), DamageType.Physical },                          // Weapon_Creature_Khrab
            { (1, 209), DamageType.Electrical },                        // Weapon_Creature_Beam_Manta
            { (1, 211), DamageType.Electrical },                        // Weapon_Creature_Seeker
            { (1, 212), DamageType.Physical },                          // Weapon_Creature_Boss_Merarim
            { (1, 213), DamageType.Physical },                          // Weapon_Creature_Boss_Eloh
            { (1, 214), DamageType.Physical },                          // Weapon_Creature_Boss_Samael
            { (1, 215), DamageType.Physical },                          // Weapon_Creature_Boss_Voro
            { (1, 217), DamageType.Physical },                          // Weapon_Creature_Lightweaver
            { (1, 224), DamageType.Physical },                          // Weapon_Creature_Human_Mech
            { (1, 225), DamageType.Physical },                          // Weapon_Creature_Leaper
            { (1, 227), DamageType.Physical },                          // Weapon_Creature_Mycon_Manta
            { (1, 229), DamageType.Fire },                              // Weapon_Creature_Bane_Grenade
            { (1, 232), DamageType.Ice },                               // Weapon_Creature_Nitroglazer
            { (1, 241), DamageType.Physical },                          // Weapon_Creature_Boss_Enslaver
            { (1, 242), DamageType.Electrical },                        // Weapon_Creature_AFS_Mini_Turret
            { (1, 244), DamageType.Physical },                          // Weapon_Creature_Hunter
            { (1, 245), DamageType.Physical },                          // Weapon_Creature_Ability_Bane_Turret
            { (1, 249), DamageType.Laser },                             // Weapon_Holographic_Strider
            { (1, 250), DamageType.Electrical },                        // Weapon_Creature_Thrax_Machina
            { (1, 252), DamageType.Electrical },                        // Weapon_Creature_AFS_Turret_Brann
            { (1, 255), DamageType.Laser },                             // Weapon_Creature_NeoBot_Shield_Beam
            { (1, 261), DamageType.Fire },                              // Weapon_Creature_NeoBot_Flame
            { (1, 262), DamageType.Physical },                          // Weapon_Creature_Forean_Retread_Shotgun
            { (1, 265), DamageType.Physical },                          // DELETEME_Weapon_Avatar_Rifle
            { (1, 268), DamageType.Laser },                             // TEST_Weapon_Avatar_Bane_Lightbender_Rifle
            { (1, 273), DamageType.Physical },                          // TEST_Weapon_Avatar_Mutalis_Grenade_Launcher
            { (1, 275), DamageType.Physical },                          // TEST_Weapon_Avatar_Forean_Club
            { (1, 276), DamageType.Physical },                          // Weapon_Creature_Loper_Spear
            { (1, 277), DamageType.Physical },                          // TEST_Weapon_Avatar_Loper_Spear
            { (1, 278), DamageType.Virulent },                          // TEST_Weapon_Avatar_Forean_GooGun
            { (1, 284), DamageType.Physical },                          // Weapon_NoMesh_Creature_Officer_v02_RocketLauncher
            { (1, 285), DamageType.Physical },                          // Weapon_NoMesh_Creature_Lieutenant_V2_PistolClaw
            { (1, 286), DamageType.Fire },                              // Weapon_Avatar_Shotgun_v2_Fire_CMN_23_to_27
            { (1, 287), DamageType.Fire },                              // Weapon_Avatar_Shotgun_v3_Fire_CMN_34_to_38
            { (1, 296), DamageType.EMP },                               // Weapon_Creature_Thrax_Technician
            { (1, 298), DamageType.Fire },                              // Weapon_Creature_Pyroglazer
            { (1, 299), DamageType.EMP },                               // Weapon_Creature_Ravager
            { (1, 301), DamageType.Sonic },                             // Weapon_Creature_Boss_Nergal_Test
            { (1, 302), DamageType.Physical },                          // Weapon_Creature_Boss_Nergal_Chair
            { (1, 304), DamageType.Physical },                          // Weapon_Creature_Heapsaw
            { (140, 1), DamageType.Fire },                              // Weapon_Avatar_PropellantGun_Epic_TEST
            { (140, 2), DamageType.Sonic },                             // Weapon_Avatar_PropellantGun_Sonic_CMN_30_to_31
            { (140, 3), DamageType.Ice },                               // Weapon_Avatar_PropellantGun_Ice_CMN_30_to_31
            { (140, 4), DamageType.Virulent },                          // Weapon_Avatar_PropellantGun_Virulent_CMN_30_to_31
            { (140, 5), DamageType.EMP },                               // Weapon_Avatar_PropellantGun_EMP_CMN_30_to_31
            { (141, 1), DamageType.Physical },                          // Weapon_Avatar_RocketLauncher_Physical_CMN_15_to_18
            { (141, 2), DamageType.EMP },                               // Weapon_Avatar_RocketLauncher_EMP_CMN_15_to_18
            { (141, 3), DamageType.EMP },                               // Weapon_Avatar_GrenadeLauncher_EMP_CMN_15_to_19
            { (141, 4), DamageType.Fire },                              // RECYCLE_Weapon_Avatar_GrenadeLauncher_Fire_15_to_19
            { (141, 5), DamageType.Ice },                               // RECYCLE_Weapon_Avatar_GrenadeLauncher_Ice_15_to_19
            { (141, 6), DamageType.Physical },                          // Weapon_Avatar_GrenadeLauncher_Physical_CMN_15_to_19
            { (141, 7), DamageType.Sonic },                             // Weapon_Avatar_GrenadeLauncher_Sonic_CMN_15_to_19
            { (141, 8), DamageType.Electrical },                        // Weapon_Avatar_RocketLauncher_Electric_CMN_19_to_23
            { (141, 9), DamageType.Laser },                             // Weapon_Avatar_RocketLauncher_Epic_TEST
            { (141, 10), DamageType.Sonic },                            // Weapon_Avatar_RocketLauncher_Sonic_CMN_15_to_18
            { (141, 11), DamageType.Physical },                         // Weapon_Creature_NeoBot_Missile
            { (141, 12), DamageType.EMP },                              // Test_Weapon_Avatar_RocketLauncher_Physical_V3
            { (141, 13), DamageType.Physical },                         // Test_Weapon_Avatar_RocketLauncher_Physical_V2
            { (141, 17), DamageType.Physical },                         // Weapon_PAU_AFS_Mech_Missiles_Physical
            { (141, 19), DamageType.Fire },                             // Weapon_PAU_Vulcan_GrenadeLauncher_Fire
            { (147, 1), DamageType.Physical },                          // Mis_Palisades_ToolHealingDisc
            { (147, 2), DamageType.Physical },                          // Mis_Plateau_ItemMindControlDeviceExtractor
            { (147, 3), DamageType.Physical },                          // Mis_Plateau_Tissue_Extractor
            { (147, 4), DamageType.Physical },                          // Tool_Avatar_Healing_Disc_Conical_CMN_05_to_09_DELETE
            { (147, 5), DamageType.Physical },                          // TEST_Weapon_Avatar_Mutalis_Healing_Disc
            { (149, 1), DamageType.Physical },                          // Weapon_Avatar_MachineGun_Physical_CMN_05_to_07
            { (149, 3), DamageType.EMP },                               // Weapon_Avatar_MachineGun_EMP_CMN_05_to_07
            { (149, 4), DamageType.Laser },                             // Weapon_Avatar_MachineGun_Laser_CMN_05_to_07
            { (149, 5), DamageType.Electrical },                        // RECYCLE_Weapon_Avatar_MachineGun_Electric_05_to_07
            { (149, 6), DamageType.Physical },                          // Weapon_Creature_Tree_Lurker
            { (149, 7), DamageType.Physical },                          // Test_Weapon_Avatar_Machinegun_Physical_V3
            { (149, 8), DamageType.Physical },                          // Test_Weapon_Avatar_Machinegun_Physical_V2
            { (149, 9), DamageType.Laser },                             // Test_Weapon_Avatar_Pistol_Laser_V2
            { (149, 10), DamageType.Laser },                            // Test_Weapon_Avatar_Pistol_Laser_V3
            { (149, 11), DamageType.Physical },                         // Weapon_PAU_AFS_Mech_MiniGun_Physical
            { (149, 12), DamageType.Physical },                         // Weapon_PAU_AFS_Mech_MiniGun_Laser
            { (149, 13), DamageType.Physical },                         // Test_Weapon_Avatar_Rifle_Physical_V3
            { (149, 14), DamageType.EMP },                              // Test_Weapon_Avatar_Rifle_EMP_V2
            { (149, 16), DamageType.Physical },                         // Weapon_PAU_GRENDEL_AutoCannon_Physical
            { (172, 4), DamageType.EMP },                               // ?
            { (172, 168), DamageType.Physical },                        // Tool_Avatar_Salvage_05
            { (172, 169), DamageType.Physical },                        // Mis_Marshes_ItemStriderController
            { (174, 1), DamageType.Physical },                          // AnimCondForcer_Avatar_Lying_Down
            { (174, 9), DamageType.Physical },                          // Weapon_Creature_Maw
            { (174, 10), DamageType.Physical },                         // Weapon_Creature_Boargar
            { (174, 11), DamageType.Physical },                         // Weapon_Creature_Forean_Spear
            { (174, 12), DamageType.Ice },                              // Weapon_Creature_Miasma
            { (174, 13), DamageType.Electrical },                       // Weapon_Creature_Mox
            { (174, 14), DamageType.Virulent },                         // Weapon_Creature_Swamp_Grubber
            { (174, 17), DamageType.Physical },                         // Weapon_Creature_Barb_Tick, 3 classes disagree
            { (174, 18), DamageType.Electrical },                       // Weapon_Creature_Warnet_Queen
            { (174, 24), DamageType.Physical },                         // Weapon_Creature_Xanx
            { (174, 25), DamageType.Physical },                         // Weapon_Creature_Atta_Harvester
            { (174, 26), DamageType.Sonic },                            // Weapon_Creature_Howler
            { (174, 27), DamageType.Physical },                         // Weapon_Creature_Flaregasher
            { (174, 28), DamageType.Virulent },                         // Weapon_Creature_Lasher
            { (174, 29), DamageType.Physical },                         // Weapon_Creature_Impaler
            { (174, 30), DamageType.Physical },                         // Weapon_Creature_Peltast
            { (174, 31), DamageType.Physical },                         // Weapon_Creature_Reaver
            { (174, 34), DamageType.Physical },                         // Weapon_Creature_Fithik
            { (174, 37), DamageType.Physical },                         // Weapon_Creature_Sonic_Glider
            { (174, 38), DamageType.Physical },                         // Weapon_Creature_Bane_Mine
            { (174, 39), DamageType.Physical },                         // Weapon_Creature_Loper_Club
            { (174, 44), DamageType.Physical },                         // Weapon_Creature_Kael
            { (174, 46), DamageType.Physical },                         // Weapon_Creature_Thrax_Melee
            { (174, 48), DamageType.Physical },                         // Weapon_Creature_Thrax_Melee_Soldier
            { (174, 49), DamageType.Physical },                         // Device_Light_Flare_White
            { (174, 51), DamageType.Physical },                         // Weapon_Creature_Brann_Incurable
            { (179, 1), DamageType.Physical },                          // Weapon_Avatar_LeechGun_Physical_CMN_05_to_08
            { (179, 2), DamageType.Fire },                              // RECYCLE_Weapon_Avatar_LeechGun_Fire_05_to_08
            { (179, 3), DamageType.Ice },                               // RECYCLE_Weapon_Avatar_LeechGun_Ice_05_to_08
            { (179, 4), DamageType.Virulent },                          // RECYCLE_Weapon_Avatar_LeechGun_Virulent_05_to_08
            { (179, 7), DamageType.Physical },                          // TEST_Weapon_Avatar_Forean_Staff
            { (179, 8), DamageType.Physical },                          // Weapon_PAU_ANGEL_LeechGun_Physical
            { (198, 1), DamageType.Physical },                          // Tool_Avatar_Field_Repair_Direct_CMN_05_to_09
            { (198, 2), DamageType.Physical },                          // Tool_Avatar_Field_Repair_Conical_CMN_05_to_09_DELETE
            { (198, 3), DamageType.Physical },                          // Tool_Avatar_Field_Repair_Area_CMN_05_to_09
            { (199, 1), DamageType.Physical },                          // Tool_Avatar_Armor_Augmentation_05
            { (230, 1), DamageType.Physical },                          // Weapon_Avatar_Sunset_Torqueshell_Rifle_Physical_Adele
            { (230, 2), DamageType.Electrical },                        // Weapon_Avatar_Torqueshell_Rifle_Electric_CMN_30_to_32
            { (230, 3), DamageType.EMP },                               // Weapon_Avatar_Torqueshell_Rifle_EMP_CMN_30_to_32
            { (230, 4), DamageType.Laser },                             // Weapon_Avatar_Sunset_Torqueshell_Rifle_Laser_Line
            { (230, 5), DamageType.Sonic },                             // Weapon_Avatar_Sunset_Torqueshell_Rifle_Sonic_Chazor
            { (230, 6), DamageType.Fire },                              // Test_Weapon_Avatar_Torqueshell_Pistol_V2_Fire
            { (249, 1), DamageType.Electrical },                        // Weapon_Avatar_PolarityGun_Electric_CMN_15_to_17
            { (249, 2), DamageType.Fire },                              // RECYCLE_Weapon_Avatar_PolarityGun_Fire_15_TO_17
            { (249, 3), DamageType.Ice },                               // RECYCLE_Weapon_Avatar_PolarityGun_Ice_15_TO_17
            { (258, 14), DamageType.Physical },                         // Tool_Avatar_Cipher_05
            { (296, 1), DamageType.EMP },                               // Weapon_Creature_AbilityTurret_EMP
            { (296, 2), DamageType.Laser },                             // Weapon_Creature_AbilityTurret_Laser
            { (296, 3), DamageType.Physical },                          // Weapon_Creature_AbilityTurret_Physical
            { (296, 4), DamageType.Sonic },                             // Weapon_Creature_AbilityTurret_Sonic
            { (296, 5), DamageType.Virulent },                          // Weapon_Creature_AbilityTurret_Virulent
            { (398, 1), DamageType.Electrical },                        // Weapon_Avatar_NetGun_Electric_CMN_15_to_15
            { (398, 2), DamageType.Fire },                              // RECYCLE_Weapon_Avatar_NetGun_Fire_15_to_15
            { (398, 3), DamageType.Ice },                               // RECYCLE_Weapon_Avatar_NetGun_Ice_15_to_15
            { (399, 1), DamageType.Virulent },                          // Weapon_Avatar_InjectionGun_Virulent_CMN_15_to_16
            { (399, 2), DamageType.Electrical },                        // Weapon_Avatar_InjectionGun_Electric_CMN_17_to_21
            { (399, 3), DamageType.EMP },                               // Weapon_Human_Redshirt_InjectionGun_EMP
            { (399, 4), DamageType.Fire },                              // RECYCLE_Weapon_Avatar_InjectionGun_Fire_15_to_16
            { (399, 5), DamageType.Ice },                               // RECYCLE_Weapon_Avatar_InjectionGun_Ice_15_to_16
            { (411, 1), DamageType.Physical },                          // Weapon_Creature_Bane_Mortar_Launcher
            { (418, 1), DamageType.Physical },                          // Weapon_Avatar_Blade_Epic_Test
            { (418, 2), DamageType.Electrical },                        // Weapon_Avatar_Blade_Electric_CMN_30_to_30
            { (418, 3), DamageType.Fire },                              // Weapon_Avatar_Blade_Fire_CMN_30_to_30
            { (418, 4), DamageType.Ice },                               // Weapon_Avatar_Blade_Ice_CMN_30_to_30
            { (418, 5), DamageType.Laser },                             // Weapon_Avatar_Blade_Laser_CMN_30_to_30
            { (527, 1), DamageType.Physical },                          // TEST_KGS_Snowballgun, 2 classes disagree
        };

        /// <summary>The damage type of the attack performed with this action and argument, physical when no weapon class claims the pair.</summary>
        public static DamageType Of(uint actionId, uint argId)
        {
            return Types.TryGetValue((actionId, argId), out var damageType) ? damageType : DamageType.Physical;
        }

        /// <summary>Whether a weapon class claims this pair at all.</summary>
        public static bool Has(uint actionId, uint argId) => Types.ContainsKey((actionId, argId));

        public static int Count => Types.Count;
    }
}
