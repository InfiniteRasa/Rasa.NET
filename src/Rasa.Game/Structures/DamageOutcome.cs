namespace Rasa.Structures
{
    /// <summary>
    /// What became of a hit ActorManager.Damage was given, for the hit record the caller reports
    /// to the clients (AbilityHit, TickEntry, HitData): Delivered is its amount, Absorbed and
    /// Immune go beside it into the rawInfo (DamageInfoWriter).
    /// </summary>
    public struct DamageOutcome
    {
        /// <summary>What got past a shield to armour and health; 0 for a hit the target was immune to.</summary>
        public int Delivered { get; set; }

        /// <summary>What a shield on the target took (Shield Extender, Shield Wave).</summary>
        public int Absorbed { get; set; }

        /// <summary>The target was immune to the hit (DamageImmunity): it took nothing, and the clients show "Immune".</summary>
        public bool Immune { get; set; }
    }
}
