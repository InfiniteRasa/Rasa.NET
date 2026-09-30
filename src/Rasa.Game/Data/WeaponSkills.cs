using System;
using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// What the weapon skills do at each pump, as the client describes them. There is no table
    /// of these numbers in the client data: each pump's tooltip (skilldata.skillLevel, the
    /// uielement strings "Firearms 3: Expert" / "Weapon Damage Bonus: +20% ...") is the only
    /// statement of them, and this is that text as numbers.
    ///
    /// Every weapon skill, and Hand to Hand for melee, gives the same flat damage bonus: none
    /// at pump 1 ("Allows weapon use"), then +10%, +20%, +30%, +40%. On top of that some give a
    /// second bonus from pump 3:
    ///
    ///  - heat dissipation: Machine Guns +30/60/100%, Propellant Guns +50/75/100%;
    ///  - reload time: Leech Guns and Launchers' rockets -20/-40/-60%, Firearms' pistols to
    ///    1.3/1.0/0.8 s;
    ///  - armour bypass: Torqueshell Guns 25/50/75% and Injection Guns 5/10/20% of the damage
    ///    straight to health.
    ///
    /// The heat and reload bonuses the client predicts for itself, from hidden game effects the
    /// original server attached to the player (SKILL_LIMITED_COOL_RATE_MODIFIER_EFFECT and
    /// SKILL_LIMITED_BY_TYPE_RELOAD_MODIFIER_EFFECT, see ManifestationManager.SyncSkillPassives):
    /// its heat meter cools at coolRate x modifier, its reload bar runs reloadTime / (1 + sum of
    /// modifiers). The numbers here are in those terms so the server's clock and the client's
    /// agree.
    ///
    /// Firearms' rifle crit is CriticalHits.FirearmsRifleChance (+3/5/7%), and its larger figure
    /// "with full bead" (+5/10/15%) CriticalHits.FirearmsRifleFullBeadChance - the server keeps
    /// the bead itself (Managers.Accuracy), since the attack request does not say how far it had
    /// closed. Firearms' shotguns knock back what they hit +15/20/25% of the time
    /// (FirearmsShotgunKnockbackChance): the weapon's own KB_CHANCE is a weapon property that
    /// survives in nothing we have, so the skill's figure is the whole chance.
    ///
    /// Staff deflects +5/10/15% of the weapon hits on whoever holds one drawn
    /// (StaffDeflectChance), and Blades hit +100/150/200% harder from behind
    /// (BladesBackstabPercent, Managers.Facing). The leech guns' conversion to health is
    /// Managers.ConstantFire.
    /// </summary>
    public static class WeaponSkills
    {
        // skilldata ids.
        public const int Firearms = 1;

        /// <summary>"Deflect Chance: +5%", +10%, +15% - by Staff pump.</summary>
        private static readonly int[] StaffDeflectByPump = { 0, 0, 0, 5, 10, 15 };

        /// <summary>"Backstab Damage: +100%", +150%, +200% - by Blades pump.</summary>
        private static readonly int[] BladesBackstabByPump = { 0, 0, 0, 100, 150, 200 };

        /// <summary>The chance, in percent, that a weapon hit on someone holding a staff drawn is deflected, at their Staff pump.</summary>
        public static int StaffDeflectChance(int pump) => StaffDeflectByPump[Math.Max(0, Math.Min(5, pump))];

        /// <summary>The percent a blade hit from behind adds to its damage, at the attacker's Blades pump.</summary>
        public static int BladesBackstabPercent(int pump) => BladesBackstabByPump[Math.Max(0, Math.Min(5, pump))];

        /// <summary>"Shotguns: +15% Knockback chance", +20%, +25% - by pump.</summary>
        private static readonly int[] FirearmsShotgunKnockbackByPump = { 0, 0, 0, 15, 20, 25 };

        /// <summary>The chance, in percent, that a Firearms shotgun hit knocks its creature back at this pump.</summary>
        public static int FirearmsShotgunKnockbackChance(int pump) => FirearmsShotgunKnockbackByPump[Math.Max(0, Math.Min(5, pump))];
        public const int HandToHand = 8;
        public const int MachineGuns = 22;
        public const int Staff = 23;
        public const int Launchers = 24;
        public const int LeechGuns = 31;
        public const int PropellantGuns = 40;
        public const int TorqueshellGuns = 50;
        public const int NetGuns = 55;
        public const int PolarityGuns = 58;
        public const int InjectionGuns = 67;
        public const int Blades = 82;

        /// <summary>
        /// The reload time the Firearms pistol bonus is written against. "Pistols: 1.3s reload
        /// time" is an absolute time; every pistol reloads in 1500 ms, so the haste that gets a
        /// 1.5 s reload to 1.3 s is 1.5 / 1.3 - 1.
        /// </summary>
        public const double PistolBaseReloadSeconds = 1.5;

        private static readonly HashSet<int> DamageSkills = new HashSet<int>
        {
            Firearms, HandToHand, MachineGuns, Staff, Launchers, LeechGuns, PropellantGuns,
            TorqueshellGuns, NetGuns, PolarityGuns, InjectionGuns, Blades
        };

        /// <summary>By pump level; index 0 is "does not have the skill".</summary>
        private static readonly int[] DamageBonusByPump = { 0, 0, 10, 20, 30, 40 };

        private static readonly Dictionary<int, int[]> HeatDissipationByPump = new Dictionary<int, int[]>
        {
            [MachineGuns] = new[] { 0, 0, 0, 30, 60, 100 },
            [PropellantGuns] = new[] { 0, 0, 0, 50, 75, 100 }
        };

        /// <summary>Percent off the reload time, for the skills that give it to every weapon they cover or to one tool type.</summary>
        private static readonly int[] ReloadCutByPump = { 0, 0, 0, 20, 40, 60 };

        private static readonly double[] PistolReloadSecondsByPump = { 0, 0, 0, 1.3, 1.0, 0.8 };

        private static readonly Dictionary<int, int[]> ArmorBypassByPump = new Dictionary<int, int[]>
        {
            [TorqueshellGuns] = new[] { 0, 0, 0, 25, 50, 75 },
            [InjectionGuns] = new[] { 0, 0, 0, 5, 10, 20 }
        };

        private static int Pump(int pump) => Math.Max(0, Math.Min(5, pump));

        /// <summary>Whether the skill is one of the weapon skills (or Hand to Hand).</summary>
        public static bool IsWeaponSkill(int skillId) => DamageSkills.Contains(skillId);

        /// <summary>Percent added to the damage of an attack made under this skill at this pump.</summary>
        public static int DamagePercent(int skillId, int pump)
        {
            return DamageSkills.Contains(skillId) ? DamageBonusByPump[Pump(pump)] : 0;
        }

        /// <summary>Heat dissipation bonus in percent: +30 cools 1.3 times as fast.</summary>
        public static int HeatDissipationPercent(int skillId, int pump)
        {
            return HeatDissipationByPump.TryGetValue(skillId, out var table) ? table[Pump(pump)] : 0;
        }

        /// <summary>The cool-rate multiplier the client's SkillLimitedCoolRateModifierEffect carries.</summary>
        public static double CoolRateModifier(int skillId, int pump)
        {
            return 1.0 + HeatDissipationPercent(skillId, pump) / 100.0;
        }

        /// <summary>
        /// Reload haste for a weapon of this skill and tool type, as the client's reload modifier
        /// effects express it: the reload takes reloadTime / (1 + haste). 0 is no bonus.
        /// </summary>
        public static double ReloadHaste(int skillId, ToolType toolType, int pump)
        {
            pump = Pump(pump);

            switch (skillId)
            {
                case LeechGuns:
                    return HasteForCut(ReloadCutByPump[pump]);
                case Launchers when toolType == ToolType.RocketLauncher:
                    return HasteForCut(ReloadCutByPump[pump]);
                case Firearms when toolType == ToolType.Pistol && PistolReloadSecondsByPump[pump] > 0:
                    return PistolBaseReloadSeconds / PistolReloadSecondsByPump[pump] - 1.0;
                default:
                    return 0;
            }
        }

        /// <summary>The haste that takes cutPercent off a reload: 20% off is 1/0.8 - 1 = 0.25.</summary>
        public static double HasteForCut(int cutPercent)
        {
            return cutPercent <= 0 || cutPercent >= 100 ? 0 : 100.0 / (100 - cutPercent) - 1.0;
        }

        /// <summary>A reload of reloadMs under this haste, as the client times it.</summary>
        public static long ReloadMs(long reloadMs, double haste)
        {
            return haste <= 0 ? reloadMs : (long)Math.Round(reloadMs / (1.0 + haste));
        }

        /// <summary>Percent of an attack's damage that skips armour and comes straight off health.</summary>
        public static int ArmorBypassPercent(int skillId, int pump)
        {
            return ArmorBypassByPump.TryGetValue(skillId, out var table) ? table[Pump(pump)] : 0;
        }

        /// <summary>
        /// The reload bonuses as the client's SKILL_LIMITED_BY_TYPE_RELOAD_MODIFIER_EFFECT wants
        /// them, one effect per entry: the haste, the skills it is limited to, and the tool types
        /// (null for every weapon of the skill).
        /// </summary>
        public static IEnumerable<(double Haste, int SkillId, ToolType? ToolType)> ReloadBonuses(Func<int, int> pumpOf)
        {
            var leech = ReloadHaste(LeechGuns, ToolType.DensityGun, pumpOf(LeechGuns));
            if (leech > 0)
                yield return (leech, LeechGuns, null);

            var rockets = ReloadHaste(Launchers, ToolType.RocketLauncher, pumpOf(Launchers));
            if (rockets > 0)
                yield return (rockets, Launchers, ToolType.RocketLauncher);

            var pistols = ReloadHaste(Firearms, ToolType.Pistol, pumpOf(Firearms));
            if (pistols > 0)
                yield return (pistols, Firearms, ToolType.Pistol);
        }
    }
}
