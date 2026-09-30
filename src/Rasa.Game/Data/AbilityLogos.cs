using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>
    /// The Logos each ability needs in the Tabula before it can be used: the client's
    /// generated.client.logosstone.logosSequences, ability (action) id to its Logos in order.
    /// 54 abilities; one that is not listed needs none.
    ///
    /// The client reads the same table in Manifestation.HasLogosForAbility: the skills and
    /// ability windows refuse an ability whose Logos are not all in the Tabula with
    /// PM_CANNOT_USE_ABILITY_NO_LOGOS, and the ability drawer only logs a warning when one is
    /// dragged into it. The server did not check at all, so the Recruit Lightning a new
    /// character finds in its drawer was performed without POWER.
    /// </summary>
    public static class AbilityLogos
    {
        /// <summary>POWER (LogosStones, ItemElohLogosPower): Recruit Lightning's only Logos.</summary>
        public const uint Power = 23;

        public static readonly IReadOnlyDictionary<uint, uint[]> Required = new Dictionary<uint, uint[]>
        {
            [137] = new uint[] { 48, 6, 131, 125 },           // SaDemolitionistExplosiveWave: VORTEX DAMAGE DESTRUCTION DEATH
            [158] = new uint[] { 49, 3, 50 },                 // AaCommandoForceBlast: TARGET BACKWARD MOVEMENT
            [162] = new uint[] { 6, 28 },                     // AaSpecialistRuin: DAMAGE TIME
            [175] = new uint[] { 1, 4, 7, 6 },                // AaReassignChaff: AREA CHAOS DEFEND DAMAGE
            [176] = new uint[] { 48, 43, 33, 300 },           // SaExobiologistReanimationWave: VORTEX LIFE NEGATIVE SPIRIT
            [177] = new uint[] { 25, 1, 38, 6 },              // AaGuardianReflection: RETURN AREA SELF_I_ME DAMAGE
            [178] = new uint[] { 24, 1 },                     // AaSoldierShrapnel: PROJECTILE AREA
            [185] = new uint[] { 52, 43, 41, 14 },            // AaExobiologistHortimunculus: SUMMON LIFE CONTROL FRIEND
            [186] = new uint[] { 10, 17, 41 },                // AaBiotechnicianCure: ENHANCE HEAL CONTROL
            [187] = new uint[] { 331, 333, 43, 6 },           // AaMedicViralConversion: TRANSFORM TRUE LIFE DAMAGE
            [188] = new uint[] { 1, 15, 17 },                 // AaBiotechnicianReconstruction: AREA GIVE HEAL
            [193] = new uint[] { 48, 10, 17, 45 },            // SaMedicRegenerationWave: VORTEX ENHANCE HEAL AROUND
            [194] = new uint[] { 23 },                        // AaRecruitLightning: POWER
            [197] = new uint[] { 52, 34, 6, 9 },              // AaEngineerTurret: SUMMON MACHINE DAMAGE ENEMY
            [229] = new uint[] { 50, 46, 1, 6 },              // AaGrenadierTectonicStrike: MOVEMENT GROUND AREA DAMAGE
            [231] = new uint[] { 373, 1, 9, 52 },             // AaGuardianVortex: NEAR AREA ENEMY SUMMON
            [232] = new uint[] { 34, 4, 6, 1 },               // AaGrenadierScatterbombs: MACHINE CHAOS DAMAGE AREA
            [233] = new uint[] { 6, 10, 17, 14 },             // AaGuardianConversion: DAMAGE ENHANCE HEAL FRIEND
            [234] = new uint[] { 48, 50, 3, 45 },             // SaGrenadierConcussiveWave: VORTEX MOVEMENT BACKWARD AROUND
            [240] = new uint[] { 52, 43, 33, 300 },           // AaExobiologistReanimation: SUMMON LIFE NEGATIVE SPIRIT
            [246] = new uint[] { 6, 9, 322, 347 },            // AaMedicDisease: DAMAGE ENEMY THROUGH WEAK
            [251] = new uint[] { 6, 1, 45, 125 },             // AaExobiologistCadaverImmolation: DAMAGE AREA AROUND DEATH
            [252] = new uint[] { 48, 10, 188, 45 },           // SaSpyCloakWave: VORTEX ENHANCE HIDE AROUND
            [253] = new uint[] { 38, 14, 52, 53 },            // AaExobiologistCreateClone: SELF_I_ME FRIEND SUMMON HERE
            [259] = new uint[] { 31, 2, 49, 216 },            // DeletemeAaSpyMagnesiumFlash: LIGHTNING ATTACK TARGET LOOKING
            [260] = new uint[] { 48, 275, 7, 14 },            // SaEngineerBaseWave: VORTEX REPAIR DEFEND FRIEND
            [262] = new uint[] { 32, 34, 43, 53 },            // AaEngineerBotConstruction: CREATE MACHINE LIFE HERE
            [267] = new uint[] { 384, 55, 38, 6 },            // AaDemolitionistSelfDestruct: TELEPORT TRAP SELF_I_ME DAMAGE
            [281] = new uint[] { 48, 10, 2, 14 },             // SaSniperCritWave: VORTEX ENHANCE ATTACK FRIEND
            [282] = new uint[] { 34, 6, 55 },                 // AaSapperCrabMines: MACHINE DAMAGE TRAP
            [295] = new uint[] { 18, 49, 214, 107 },          // AaSniperPaintTarget: INCREASE TARGET LOCATION CLARITY
            [298] = new uint[] { 14, 384, 34, 52 },           // AaEngineerFeedback: FRIEND TELEPORT MACHINE SUMMON
            [301] = new uint[] { 50, 3, 6, 1 },               // AaDemolitionistRealityRipper: MOVEMENT BACKWARD DAMAGE AREA
            [302] = new uint[] { 38, 24, 2 },                 // AaCommandoRushingBlow: SELF_I_ME PROJECTILE ATTACK
            [303] = new uint[] { 34, 41, 4 },                 // AaSapperHack: MACHINE CONTROL CHAOS
            [304] = new uint[] { 41, 9, 56, 300 },            // AaMedicMindControl: CONTROL ENEMY MIND SPIRIT
            [305] = new uint[] { 48, 6, 7, 45 },              // SaGuardianShieldWave: VORTEX DAMAGE DEFEND AROUND
            [307] = new uint[] { 2, 10 },                     // AaSoldierRage: ATTACK ENHANCE
            [380] = new uint[] { 38, 45, 6 },                 // AaCommandoScourge: SELF_I_ME AROUND DAMAGE
            [381] = new uint[] { 6, 9, 117, 55 },             // AaDemolitionistControlledFission: DAMAGE ENEMY CONTAINER TRAP
            [383] = new uint[] { 9, 6, 10, 20 },              // AaDemolitionistExplosiveNanites: ENEMY DAMAGE ENHANCE MANY
            [384] = new uint[] { 55, 6, 52, 34 },             // AaEngineerTrap: TRAP DAMAGE SUMMON MACHINE
            [385] = new uint[] { 38, 6, 17, 14 },             // AaGrenadierSacrifice: SELF_I_ME DAMAGE HEAL FRIEND
            [386] = new uint[] { 38, 7, 6, 146 },             // AaMedicResistance: SELF_I_ME DEFEND DAMAGE EFFECT
            [387] = new uint[] { 51, 1, 6 },                  // AaRangerFireSupport: COMMUNICATION AREA DAMAGE
            [388] = new uint[] { 7, 331, 33, 146 },           // AaSpyPolarityField: DEFEND TRANSFORM NEGATIVE EFFECT
            [389] = new uint[] { 14, 52, 53 },                // AaRangerSpotter: FRIEND SUMMON HERE
            [390] = new uint[] { 393, 6, 146, 2 },            // AaSniperShredderAmmo: ADD DAMAGE EFFECT ATTACK
            [392] = new uint[] { 38, 331, 49, 216 },          // AaSpyPolymorph: SELF_I_ME TRANSFORM TARGET LOOKING
            [393] = new uint[] { 9, 2, 14, 41 },              // AaSpyTraitor: ENEMY ATTACK FRIEND CONTROL
            [421] = new uint[] { 10, 14, 23 },                // AaBiotechnicianBioAugmentation: ENHANCE FRIEND POWER
            [430] = new uint[] { 41, 2, 49, 131 },            // AaSniperCalledShot: CONTROL ATTACK TARGET DESTRUCTION
            [446] = new uint[] { 18, 49, 7 },                 // AaSapperShieldExtender: INCREASE TARGET DEFEND
            [10000005] = new uint[] { 1, 7, 4 },              // AaRangerTacticalEvasion: AREA DEFEND CHAOS
        };

        /// <summary>The Logos the ability needs, in order; empty when it needs none.</summary>
        public static IReadOnlyList<uint> Of(ActionId actionId) =>
            Required.TryGetValue((uint)actionId, out var logos) ? logos : System.Array.Empty<uint>();

        /// <summary>Whether a Tabula holding these Logos may use the ability.</summary>
        public static bool Has(IEnumerable<uint> tabula, ActionId actionId)
        {
            var needed = Of(actionId);

            if (needed.Count == 0)
                return true;

            var held = tabula as ICollection<uint> ?? tabula?.ToList() ?? new List<uint>();

            return needed.All(held.Contains);
        }
    }
}
