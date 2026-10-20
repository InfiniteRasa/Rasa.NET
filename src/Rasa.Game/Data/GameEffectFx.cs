using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>
    /// The levels at which the client has visuals for a game effect. Its effect classes look
    /// their attach FX up in gameeffectdata.specialFX, their detach sound in detachSFX, their
    /// icon in iconOverride and their tooltip in tooltipOverride by (typeId, level) exactly
    /// (client/gameeffects/basegameeffect.py DoAnnounceAttachVisuals, gameuiutil.py
    /// LoadGameEffectIcon, ui/tooltipwindow.py): there is no falling back to another level, so an
    /// effect sent at a level with no row plays nothing. Many effects have one FX row, at level
    /// 1, and are put on at a pump or ability level - a P3 Lightning Storm at 3 - which then has
    /// no storm cloud.
    ///
    /// <see cref="LevelFor"/> is the level to send an effect at whose level is a pump or an
    /// ability's level rather than a choice of visual: that level if the client has any row for
    /// it, else 1 if the client has FX at 1, else that level. An effect whose level means
    /// something else to the client - a crit death's, which is its critdeathdata id - does not
    /// go through it.
    ///
    /// Levels lists, for every type with FX at level 1, each level any of the four tables has a
    /// row at. Generated from client 1.16.5.0.
    /// </summary>
    public static class GameEffectFx
    {
        /// <summary>The level to send this type at for a pump or ability level.</summary>
        public static uint LevelFor(int typeId, uint level)
        {
            if (!Levels.TryGetValue(typeId, out var levels) || levels.Contains(level))
                return level;

            return 1;
        }

        /// <summary>Whether the client has any per-level row for this type at this level.</summary>
        public static bool HasRow(int typeId, uint level) => Levels.TryGetValue(typeId, out var levels) && levels.Contains(level);

        public static readonly IReadOnlyDictionary<int, uint[]> Levels = new Dictionary<int, uint[]>
        {
            [1] = new uint[] { 1, 3 },                                   // DING_EFFECT
            [82] = new uint[] { 1, 2, 3, 4, 5 },                         // DECAY
            [84] = new uint[] { 1 },                                     // STALKER_EGG
            [86] = new uint[] { 1, 2, 3, 4, 5 },                         // STUN
            [88] = new uint[] { 1 },                                     // TESLA_COIL_ZAP
            [90] = new uint[] { 1, 2, 3, 4, 5 },                         // CHAFFSOURCE
            [91] = new uint[] { 1, 2, 3, 4, 5 },                         // CHAFF
            [93] = new uint[] { 1, 2, 3, 4, 5, 6 },                      // REFLECTION
            [94] = new uint[] { 1, 2, 3, 4 },                            // CF_DENSITY_GUN_EFFECT
            [95] = new uint[] { 1 },                                     // ARC_EFFECT
            [99] = new uint[] { 1, 2, 3, 4, 5 },                         // ARMORAUGTOOL
            [100] = new uint[] { 1 },                                    // LIGHTNINGSTORM_EFFECT
            [102] = new uint[] { 1, 2, 3, 5, 6, 13 },                    // CLIENT_DAMAGE_EFFECT
            [106] = new uint[] { 1, 2, 3, 4, 5 },                        // POLYMORPH_HOMINUS_MACHINA
            [107] = new uint[] { 1, 5, 6, 13 },                          // CF_MACHINEGUN_EFFECT
            [116] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 13 },              // CRIT_PREDEATH_EFFECT
            [117] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 13 },              // CRIT_DEATH_EFFECT
            [122] = new uint[] { 1 },                                    // LT_EFFECT
            [123] = new uint[] { 1 },                                    // STEALTH_EFFECT
            [124] = new uint[] { 1 },                                    // FLASH_BANG_BLIND_EFFECT
            [126] = new uint[] { 1, 2, 3, 4, 5, 6 },                     // CORPSE_IMMOLATION
            [127] = new uint[] { 1 },                                    // VISUAL_ARC_EFFECT
            [128] = new uint[] { 1 },                                    // CONFUSION
            [130] = new uint[] { 1 },                                    // FLASH_BANG_DELAY_EFFECT
            [132] = new uint[] { 1, 2 },                                 // REANIMATED
            [134] = new uint[] { 1 },                                    // REANIMATED_DEATH
            [153] = new uint[] { 1 },                                    // VISUAL_AVATAR_HELMET_LIGHT
            [167] = new uint[] { 1, 2, 3, 4, 5, 6 },                     // CURE_REVIVE_EFFECT
            [168] = new uint[] { 1, 2, 3, 4, 5 },                        // MIND_CONTROL_EFFECT
            [170] = new uint[] { 1 },                                    // ALTERNATE_MESH
            [174] = new uint[] { 1, 4, 5, 6, 7 },                        // CF_ABILITY_TURRET_EFFECT
            [179] = new uint[] { 1, 2, 3, 4, 5 },                        // DISEASE_EFFECT
            [180] = new uint[] { 1, 2, 3, 4, 5 },                        // RECONSTRUCTION_HELP_EFFECT
            [181] = new uint[] { 1, 2, 3, 4, 5, 6 },                     // CURE_DEBUFF_GUARD
            [182] = new uint[] { 1 },                                    // SMOKE_GRENADE_EFFECT
            [192] = new uint[] { 1, 2, 3, 4, 5 },                        // SACRIFICE
            [193] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 13 },              // CRIT_POSTDEATH_EFFECT
            [194] = new uint[] { 1, 2, 3, 4, 5, 6 },                     // MAGNESIUM_FLASH_EFFECT
            [199] = new uint[] { 1, 2, 3, 4, 5 },                        // CONVERSION
            [200] = new uint[] { 1 },                                    // RADIATION_POISON
            [201] = new uint[] { 1 },                                    // SHIELDWAVEEFFECT
            [202] = new uint[] { 1 },                                    // CRITWAVEEFFECT
            [203] = new uint[] { 1, 2, 3, 4, 5 },                        // REALITY_RIPPER_SOURCE
            [205] = new uint[] { 1 },                                    // REALITY_RIPPER_TARGET_VISUAL
            [207] = new uint[] { 1, 2, 3, 4, 5 },                        // CONTROLLED_FISSION_EFFECT
            [223] = new uint[] { 1 },                                    // HACKED_EFFECT
            [224] = new uint[] { 1 },                                    // INSANITY_EFFECT
            [225] = new uint[] { 1, 2, 3, 4, 5 },                        // HORTIMONCULUS_BUFF
            [226] = new uint[] { 1, 2, 3, 4, 5 },                        // HORTIMONCULUS_SOURCE
            [229] = new uint[] { 1, 2, 3, 4, 5 },                        // SHIELD_DRONE_SHIELD_SOURCE
            [230] = new uint[] { 1, 2, 3, 4, 5 },                        // SHIELD_DRONE_SHIELD
            [235] = new uint[] { 1, 2, 3, 4, 5, 6, 7 },                  // RAGE
            [236] = new uint[] { 1, 2, 3, 4, 5, 6, 7 },                  // RAGESOURCE
            [240] = new uint[] { 1 },                                    // COUNTER_ATTACK_EFFECT
            [241] = new uint[] { 1 },                                    // INJECTION_GUN_ELECTRIC_EFFECT
            [242] = new uint[] { 1 },                                    // INJECTION_GUN_EMP_EFFECT
            [243] = new uint[] { 1 },                                    // INJECTION_GUN_FIRE_EFFECT
            [244] = new uint[] { 1 },                                    // INJECTION_GUN_ICE_EFFECT
            [245] = new uint[] { 1 },                                    // INJECTION_GUN_SONIC_EFFECT
            [246] = new uint[] { 1 },                                    // INJECTION_GUN_VIRAL_EFFECT
            [247] = new uint[] { 1 },                                    // SPRINT
            [251] = new uint[] { 1 },                                    // NANOVIRUS
            [255] = new uint[] { 1, 2, 3, 4, 5 },                        // AIR_STRIKE_EFFECT
            [256] = new uint[] { 1, 2, 3, 4, 5, 6 },                     // SCOURGE_EFFECT
            [258] = new uint[] { 1, 13 },                                // HUNTER_NET
            [262] = new uint[] { 1, 2, 3, 4, 5 },                        // POLARITY_FIELD
            [267] = new uint[] { 1 },                                    // GROUND_TARGET_EXPLOSION
            [270] = new uint[] { 1 },                                    // LINKER_CHEST_BLAST_WINDUP
            [271] = new uint[] { 1 },                                    // OLD_IMMUNIZATION_PHYSICAL_EFFECT
            [272] = new uint[] { 1 },                                    // OLD_IMMUNIZATION_ICE_EFFECT
            [273] = new uint[] { 1 },                                    // OLD_IMMUNIZATION_SONIC_EFFECT
            [274] = new uint[] { 1 },                                    // OLD_IMMUNIZATION_ELECTRIC_EFFECT
            [275] = new uint[] { 1 },                                    // OLD_IMMUNIZATION_VIRAL_EFFECT
            [277] = new uint[] { 1 },                                    // MIASMA_DISSIPATE
            [282] = new uint[] { 1 },                                    // MAW_VINES
            [287] = new uint[] { 1 },                                    // JUGGERNAUT_REALITY_RIPPER_SOURCE
            [289] = new uint[] { 1 },                                    // JUGGERNAUT_REALITY_RIPPER_TARGET_VISUAL
            [290] = new uint[] { 1, 2, 3, 4, 5 },                        // CALLED_SHOT_EFFECT
            [291] = new uint[] { 1 },                                    // CALLED_SHOT_LEG
            [292] = new uint[] { 1 },                                    // CALLED_SHOT_ARM
            [293] = new uint[] { 1 },                                    // CALLED_SHOT_EYE
            [294] = new uint[] { 1 },                                    // CALLED_SHOT_CHEST
            [295] = new uint[] { 1 },                                    // CALLED_SHOT_HEAD
            [298] = new uint[] { 1 },                                    // LINKER_GROUND_BLAST
            [299] = new uint[] { 1 },                                    // ARMOR_MALFUNCTION
            [301] = new uint[] { 1 },                                    // XANX_WEB
            [304] = new uint[] { 1 },                                    // STALKER_EGG_CHARGE
            [307] = new uint[] { 1 },                                    // RECONSTRUCTOR_BOT_CLONE
            [310] = new uint[] { 1 },                                    // SENTINEL_PHASE
            [328] = new uint[] { 1 },                                    // MIS_DESCENT_SKELETON_EFFECT
            [329] = new uint[] { 1, 2, 3, 4, 5 },                        // BIO_AUGMENTATION_EFFECT
            [348] = new uint[] { 1 },                                    // LAVA_DAMAGE
            [349] = new uint[] { 1 },                                    // LIGHTNING_DAMAGE
            [358] = new uint[] { 1 },                                    // STRIDER_EYE
            [362] = new uint[] { 1 },                                    // ATTA_HARVESTER_PHEROMONE
            [363] = new uint[] { 1, 2, 3 },                              // ATTA_SOLDIER_RAGE
            [364] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },        // FIREWORK_EFFECT
            [376] = new uint[] { 1 },                                    // CF_TREELURKER_EFFECT
            [377] = new uint[] { 1 },                                    // NEO_BOT_SHIELD_SHIELD_SOURCE
            [378] = new uint[] { 1 },                                    // NEO_BOT_SHIELD_SHIELD
            [379] = new uint[] { 1, 2, 3, 4, 5 },                        // ATTA_HARVESTER_ACID_SPIT
            [380] = new uint[] { 1 },                                    // NECROMITE_CORPSE_EXPLOSION
            [381] = new uint[] { 1 },                                    // HUNTER_SHIELD
            [382] = new uint[] { 1 },                                    // NECROMITE_SELF_DESTRUCT
            [383] = new uint[] { 1, 2, 3, 4, 5 },                        // FEEDBACK_EFFECT
            [385] = new uint[] { 1 },                                    // BLACKHOLE_GRAVITY_WELL_TARGET
            [386] = new uint[] { 1 },                                    // PVP_SAFETY
            [388] = new uint[] { 1 },                                    // LIGHTBENDER_QUILL_FLASH
            [393] = new uint[] { 1, 3 },                                 // FIRE_SUPPORT_AIR_STRIKE_EFFECT
            [394] = new uint[] { 1, 6 },                                 // FIRE_SUPPORT_NAPALM_POOL_EFFECT
            [396] = new uint[] { 1, 6 },                                 // FIRE_SUPPORT_NAPALM_BOMB_EFFECT
            [403] = new uint[] { 1 },                                    // HEAL_HEALTH_EFFECT
            [404] = new uint[] { 1 },                                    // HEAL_ARMOR_EFFECT
            [417] = new uint[] { 1 },                                    // CRIT_EMP
            [420] = new uint[] { 1 },                                    // SCRIPTED_DOT
            [422] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },    // DUMMY_USE_CONTROL_POINT
            [423] = new uint[] { 1, 2 },                                 // SCRIPTED_PERCENT_DOT
            [432] = new uint[] { 1, 2, 3, 4 },                           // VISUAL_REZ_ME_EFFECT
            [439] = new uint[] { 1, 4, 5, 6, 7 },                        // VISUAL_TURRET_EFFECT
            [443] = new uint[] { 1 },                                    // CF_MACHINEGUN_S2_EFFECT
            [444] = new uint[] { 1 },                                    // AUTOLOOT_RADIUS
            [445] = new uint[] { 1 },                                    // CF_MACHINEGUN_S3_EFFECT
            [448] = new uint[] { 1 },                                    // SPOTTER_DESPAWN
            [451] = new uint[] { 1 },                                    // BOT_CONSTRUCTION_DESPAWN
            [454] = new uint[] { 1 },                                    // CREATE_CLONE_DESPAWN
            [457] = new uint[] { 1 },                                    // MORPH_MECH
            [462] = new uint[] { 1 },                                    // CF_HUMAN_MECH_PAU
            [473] = new uint[] { 1 },                                    // SEEKER_DETONATION
            [475] = new uint[] { 1 },                                    // CF_ANGEL_DENSITY_GUN_EFFECT
            [10000010] = new uint[] { 1 },                               // MEDIUM_ARMOR_SKILL
            [10000018] = new uint[] { 1 },                               // CRIT_VIRULENT
            [10000020] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 },          // EXPLODING_NANITES_EFFECT
            [10000027] = new uint[] { 1 },                               // MECH_ARMOR_SKILL_VISIBLE
            [10000028] = new uint[] { 1 },                               // MEDIUM_ARMOR_SKILL_VISIBLE
            [10000031] = new uint[] { 1, 2, 3, 4, 5 },                   // SELF_DESTRUCT_LOCATION
            [10000032] = new uint[] { 1, 2, 3, 4, 5 },                   // SELF_DESTRUCT_BOMB
            [10000035] = new uint[] { 1, 2, 3, 4, 5 },                   // WEAPON_ENHANCEMENT
            [10000037] = new uint[] { 1 },                               // CLOAK_WAVE_EFFECT
            [10000039] = new uint[] { 1, 2, 3, 4, 5, 6 },                // POLYMORPH_EFFECT
            [10000041] = new uint[] { 1, 2, 3, 4, 5 },                   // TRAP_DEATH_EFFECT
            [10000042] = new uint[] { 1, 2, 3, 4, 5 },                   // TRAP_EXPLOSION_EFFECT
            [10000045] = new uint[] { 1, 2, 3, 4, 5 },                   // PAINT_TARGET_EFFECT
            [10000047] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 13 },         // PROPELLANT_POOL_EFFECT
            [10000056] = new uint[] { 1, 2, 3, 4, 5, 6 },                // SHIELD_EXTENDER_SOURCE
            [10000057] = new uint[] { 1, 2, 3, 4, 5 },                   // TRAITOR_EFFECT
            [10000058] = new uint[] { 1 },                               // HORTIMONCULUS_USE
            [10000060] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },   // PLACE_BOMB
            [10000061] = new uint[] { 1 },                               // MECH_ARMOR_SKILL_TARGET
            [10000063] = new uint[] { 1, 2, 3, 4, 5 },                   // RECONSTRUCTION_HARM_EFFECT
            [10000071] = new uint[] { 1, 2, 3, 4, 5, 6, 7, 13 },         // AE_DAMAGE_EFFECT
            [10000075] = new uint[] { 1, 3 },                            // TACTICAL_EVASION_MAG_FLASH_EFFECT
            [10000078] = new uint[] { 1 },                               // TACTICAL_EVASION_TELEPORT_EFFECT
            [10000079] = new uint[] { 1 },                               // TACTICAL_EVASION_LOCATION_EFFECT
            [10000082] = new uint[] { 1 },                               // KNOCKBACK_WHEN_HIT_BY_ATTACK_TYPE
            [10000085] = new uint[] { 1, 2, 3, 4, 5, 6 },                // RESISTANCE
            [10000088] = new uint[] { 1, 2, 6, 7, 13 },                  // CRAB_MINE_EXPLOSION
            [10000089] = new uint[] { 1 },                               // TSR_TRACER_EFFECT
            [20000002] = new uint[] { 1 },                               // SHIELD_SOURCE
        };
    }
}
