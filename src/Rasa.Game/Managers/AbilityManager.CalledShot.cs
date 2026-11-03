using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// Called Shot (abilities.calledshot), the Sniper's aimed wound, by its data (action 430) and
    /// the client's description: "Applies a debuff or DoT to a single enemy target that will
    /// activate the next time it takes damage. If the target does not take damage before a set
    /// time, the Called Shot is removed with no affect to the target."
    ///
    /// So CALLED_SHOT_EFFECT 290, "Called Shot Aiming", goes on the target for DURATION (20 s,
    /// 10 s at P3) and does nothing by itself; the next damage the target takes, from anyone,
    /// takes the aim off and lands the pump's own effect, which runs for EFFECT_DURATION_MS:
    ///
    ///  - P1 Hamstring, CALLED_SHOT_LEG 291: movement cut by EFFECT_MOVEMENT_MODIFIER (50 %), 20 s.
    ///  - P2 Maim, CALLED_SHOT_ARM 292: action cooldowns multiplied by EFFECT_MODIFIER (200 %),
    ///    20 s - the client's CalledShotArmEffect takes that modifier as its attach argument and
    ///    applies it to the recovery and reuse of weapon attacks, so the server sends it along and
    ///    does the same to the creature's own cooldowns. (The pump's tooltip says -33 %, which is a
    ///    modifier of 1.5; the data says 200, and the data is what both ends here use.)
    ///  - P3 Bleeding Wound, CALLED_SHOT_CHEST 294: "DAMAGE_MODIFIER_PERCENT % of base weapon
    ///    damage applied every INTERVAL s" - 10 % of the sniper's equipped weapon damage every
    ///    second for 10 s, worked out from the weapon held when the shot was called.
    ///  - P4 Blindness, CALLED_SHOT_EYE 293: "Ranged attack damage reduced by %(damageMod)s%%" -
    ///    DAMAGE_MODIFIER_PERCENT (-50), 20 s, on the creature's ranged attacks.
    ///  - P5 Head Shot, CALLED_SHOT_HEAD 295: damage received +DAMAGE_MODIFIER_PERCENT (50 %) for
    ///    5 s, as a vulnerability of -50 resistance to everything. The hit that sets it off is a
    ///    head shot too: the aim carries the same vulnerability, so that hit is worked out with
    ///    it before the aim comes off and the wound takes over. The aim waits DURATION (20 s) and
    ///    the wound lasts EFFECT_DURATION_MS (5000); pump 5 is the only pump where those differ,
    ///    and the pump's tooltip shows only the first ("Duration: 20s").
    ///
    /// Which pump is which is read off the data rather than the level, the way every other ability
    /// here does it: the movement modifier is the leg, the effect modifier the arm, an interval
    /// the chest, and a damage modifier the eye when it is negative and the head when it is not.
    /// </summary>
    public partial class AbilityManager
    {
        private const int CalledShotAimTypeId = 290;        // CALLED_SHOT_EFFECT
        private const int CalledShotLegTypeId = 291;        // CALLED_SHOT_LEG
        private const int CalledShotArmTypeId = 292;        // CALLED_SHOT_ARM
        private const int CalledShotEyeTypeId = 293;        // CALLED_SHOT_EYE
        private const int CalledShotChestTypeId = 294;      // CALLED_SHOT_CHEST
        private const int CalledShotHeadTypeId = 295;       // CALLED_SHOT_HEAD

        internal void ArmCalledShot(MapChannel mapChannel, Client client, Manifestation player, Actor target, ActionLevelInfo info)
        {
            var aim = NewEffect(mapChannel, player, info, CalledShotAimTypeId, info.Get(AbilityProperty.Duration, 20));

            aim.IsBuff = false;

            // P5: the shot that sets it off hits harder too. Every hit's resistance is worked out
            // before it lands, and it is landing that takes the aim off, so the vulnerability on
            // the aim counts for that one hit and the wound's for the hits after it. Called again
            // while a wound is still on, the two are one group and count once: +50%, never +100%.
            if (IsHeadShot(info))
            {
                aim.ResistModifier = -info.Get(AbilityProperty.DamageModifierPercent);
                aim.ResistGroup = CalledShotHeadTypeId;
            }

            // The bleed is a share of the weapon that called the shot, so the weapon is read now
            // rather than whenever the wound opens.
            var weapon = EntityClassManager.Instance.GetWeaponClassInfo(InventoryManager.Instance.CurrentWeapon(client));
            var weaponMin = weapon?.MinDamage ?? 0;
            var weaponMax = Math.Max(weaponMin, weapon?.MaxDamage ?? 0);

            aim.OnDamaged = (m, holder, effect) =>
            {
                if (holder != null)
                    LandCalledShot(m, holder, effect, info, weaponMin, weaponMax);
            };

            GameEffectManager.Instance.Attach(mapChannel, target, aim);
        }

        /// <summary>The aim has been taken off: the pump's own effect goes on in its place.</summary>
        private void LandCalledShot(MapChannel mapChannel, Actor target, GameEffect aim, ActionLevelInfo info, int weaponMin, int weaponMax)
        {
            if (!(aim.Source is Manifestation player) || player.MapContextId != mapChannel.MapInfo.MapContextId)
                return;

            if (target.State == CharacterState.Dead || target.State == CharacterState.Dying || target.Attributes[Attributes.Health].Current <= 0)
                return;

            var durationMs = Math.Max(1000, info.Get(AbilityProperty.EffectDurationMs, 20000));
            var damageMod = info.Get(AbilityProperty.DamageModifierPercent);

            // P1: the leg. Slowed, like any other snare.
            if (info.Has(AbilityProperty.EffectMovementModifier))
            {
                var slow = Math.Max(1, Math.Min(99, info.Get(AbilityProperty.EffectMovementModifier)));
                var leg = CalledShotEffect(mapChannel, player, info, CalledShotLegTypeId, durationMs);

                leg.MovementModifierPercent = 100 - slow;
                leg.Tooltip["movementMod"] = slow;      // "Run speed reduced by %(movementMod)s%%"

                GameEffectManager.Instance.Attach(mapChannel, target, leg);
                return;
            }

            // P2: the arm. The modifier goes to the client's effect as its attach argument.
            if (info.Has(AbilityProperty.EffectModifier))
            {
                var modifier = Math.Max(100, info.Get(AbilityProperty.EffectModifier, 200)) / 100.0;
                var arm = CalledShotEffect(mapChannel, player, info, CalledShotArmTypeId, durationMs);

                arm.AttackRateModifier = modifier;

                GameEffectManager.Instance.Attach(mapChannel, target, arm, modifier);
                return;
            }

            // P3: the chest. A share of the sniper's weapon damage every interval.
            if (info.Has(AbilityProperty.Interval))
            {
                var chest = CalledShotEffect(mapChannel, player, info, CalledShotChestTypeId, durationMs);

                chest.TickDamageMin = Math.Max(1, weaponMin * damageMod / 100);
                chest.TickDamageMax = Math.Max(chest.TickDamageMin, weaponMax * damageMod / 100);
                chest.TickDamageType = DamageType.Physical;
                chest.TickScaleType = 0;                // the weapon's own damage, not scaled again
                chest.TickIntervalMs = Math.Max(500, info.Get(AbilityProperty.Interval, 1) * 1000);
                chest.NextTickTick = Environment.TickCount64 + chest.TickIntervalMs;

                GameEffectManager.Instance.Attach(mapChannel, target, chest);
                return;
            }

            // P4: the eye. Its ranged attacks are weakened; its melee is untouched.
            if (damageMod < 0)
            {
                var eye = CalledShotEffect(mapChannel, player, info, CalledShotEyeTypeId, durationMs);

                eye.RangedDamagePercent = damageMod;
                eye.Tooltip["damageMod"] = -damageMod;  // "Ranged attack damage reduced by %(damageMod)s%%"

                GameEffectManager.Instance.Attach(mapChannel, target, eye);
                return;
            }

            // P5: the head. Everything hits it harder for a few seconds.
            var head = CalledShotEffect(mapChannel, player, info, CalledShotHeadTypeId, durationMs);

            head.ResistModifier = -damageMod;
            head.ResistGroup = CalledShotHeadTypeId;

            GameEffectManager.Instance.Attach(mapChannel, target, head);
        }

        /// <summary>
        /// Whether a pump's data is the head's: none of the leg's, arm's or chest's properties, and
        /// a damage modifier that adds rather than takes away (the eye's is negative).
        /// </summary>
        internal static bool IsHeadShot(ActionLevelInfo info) =>
            !info.Has(AbilityProperty.EffectMovementModifier) && !info.Has(AbilityProperty.EffectModifier)
            && !info.Has(AbilityProperty.Interval) && info.Get(AbilityProperty.DamageModifierPercent) > 0;

        /// <summary>
        /// One of the wounds. It arrives long after the ability's recovery did, so unlike the
        /// effects an ability attaches as it is performed this one announces itself.
        /// </summary>
        private static GameEffect CalledShotEffect(MapChannel mapChannel, Manifestation player, ActionLevelInfo info, int typeId, int durationMs)
        {
            var effect = NewEffect(mapChannel, player, info, typeId, null);

            effect.IsBuff = false;
            effect.AnnounceOnAttach = true;
            effect.ExpiresTick = Environment.TickCount64 + durationMs;

            return effect;
        }
    }
}
