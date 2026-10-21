using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The white glow round the whole of an NPC: the client's LT_EFFECT (122), whose only part is
    /// its look - at level 1 the FX family EFFECT_CREATURE_LT_VISUAL, vfx_enemy_halo_v01
    /// (vfx_enemy_halo_v02 on skeleton 21). The client class (LtEffect) has no icon, tooltip or
    /// handler of its own and cannot be taken off; nothing in its data says who wore it.
    ///
    /// Who: Outpost Commander Rogers at Alia Das, who has it in footage of the live game. It goes
    /// on as he comes into the world (CellManager.AddToWorld), for as long as he is in it, and is
    /// shown to whoever meets him after with the rest of his effects
    /// (GameEffectManager.ShowEffectsTo).
    /// </summary>
    public static class CreatureHalos
    {
        /// <summary>LT_EFFECT.</summary>
        public const int TypeId = 122;

        /// <summary>The level its FX is at (gameeffectdata.specialFX (122, 1)).</summary>
        public const int Level = 1;

        /// <summary>The creatures (creature table ids) that wear it: Outpost Commander Rogers (100).</summary>
        public static readonly HashSet<uint> Creatures = new HashSet<uint> { 100 };

        public static bool Wears(Creature creature) => creature != null && Creatures.Contains(creature.DbId);

        public static bool HasHalo(Actor actor) => actor != null && actor.ActiveEffects.Values.Any(e => e.TypeId == TypeId);

        /// <summary>Puts the halo on the creature if it is one that wears it and has none.</summary>
        public static void Put(MapChannel mapChannel, Creature creature)
        {
            if (mapChannel == null || !Wears(creature) || HasHalo(creature))
                return;

            GameEffectManager.Instance.Attach(mapChannel, creature, new GameEffect
            {
                TypeId = TypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                EffectLevel = Level,
                SourceId = creature.EntityId,
                Source = creature,
                SourceLevel = (int)creature.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ShowsDuration = false
            });
        }
    }
}
