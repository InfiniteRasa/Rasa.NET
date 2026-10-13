using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// An Atta Soldier brought low fights harder. The client's AttaSoldierRageEffect
    /// (ATTA_SOLDIER_RAGE 363) "causes a Atta soldier to do additional dmg at lower health level";
    /// it has no ability that puts it on, no numbers and no tooltip, only its FX
    /// (EFFECT_CREATURE_ATTA_SOLDIER_RAGE_V01..V03 at levels 1-3), so when and how much are ours:
    /// below <see cref="HealthPercent"/> of its health a soldier deals <see cref="DamagePercent"/>
    /// more - double - with every attack, and calms down again if it is healed back above it.
    ///
    /// The effect is announced on the soldier at level 1 (the first of the three FX), carries the
    /// bonus as its DamageDealtPercent (GameEffectManager.ApplyDamageDealt, as a Thrax's Rage
    /// does), and has no timer: it is how the soldier stands, not something done to it. A dead
    /// soldier's goes with the rest of its effects.
    ///
    /// A soldier is recognised by its entity class: the standard, young and drop-down soldiers
    /// and their bosses. The NPC soldier (8093) is not one that fights.
    /// </summary>
    public static class AttaSoldierRage
    {
        /// <summary>ATTA_SOLDIER_RAGE.</summary>
        public const int TypeId = 363;

        /// <summary>The level its FX are shown at: EFFECT_CREATURE_ATTA_SOLDIER_RAGE_V01.</summary>
        public const int FxLevel = 1;

        /// <summary>It rages below this share of its health. Ours.</summary>
        public const int HealthPercent = 33;

        /// <summary>What it deals on top while raging: +100%, twice its damage. Ours.</summary>
        public const int DamagePercent = 100;

        /// <summary>Creature_Atta_Soldier_Standard, _Standard_Boss, _Young, _DropDown and _DropDown_Boss.</summary>
        public static readonly HashSet<EntityClasses> Classes = new HashSet<EntityClasses>
        {
            (EntityClasses)7081, (EntityClasses)22627, (EntityClasses)25612, (EntityClasses)26024, (EntityClasses)26025
        };

        public static bool Is(Creature creature) => creature != null && Classes.Contains(creature.EntityClass);

        /// <summary>Whether a soldier at this health rages: alive, and below HealthPercent of its maximum.</summary>
        public static bool Rages(int current, int max) =>
            max > 0 && current > 0 && (long)current * 100 < (long)max * HealthPercent;

        /// <summary>
        /// Puts the rage on a soldier that has come below the line, and takes it off one healed
        /// back above it. Called on every think (BehaviorManager), so whatever brought its health
        /// there - a hit, a damage over time, a heal - is caught on the next one.
        /// </summary>
        public static void Observe(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || !Is(creature) || creature.State == CharacterState.Dead || creature.State == CharacterState.Dying)
                return;

            if (!creature.Attributes.TryGetValue(Attributes.Health, out var health))
                return;

            var rage = creature.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == TypeId);
            var rages = Rages(health.Current, health.CurrentMax);

            if (rages && rage == null)
                GameEffectManager.Instance.Attach(mapChannel, creature, new GameEffect
                {
                    TypeId = TypeId,
                    EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                    EffectLevel = FxLevel,
                    SourceId = creature.EntityId,
                    Source = creature,
                    SourceLevel = (int)creature.Level,
                    IsBuff = true,
                    AllowDetach = false,
                    AnnounceOnAttach = true,
                    ShowsDuration = false,
                    DamageDealtPercent = DamagePercent
                });
            else if (!rages && rage != null)
                GameEffectManager.Instance.DettachEffect(mapChannel, creature, rage);
        }
    }
}
