namespace Rasa.Structures
{
    using Char;

    /// <summary>
    /// What a character left the world with, as its row holds it (character.current_health and
    /// the rest): read at load, put back by Managers.RelogVitals as the character arrives.
    /// </summary>
    public class SavedVitals
    {
        /// <summary>Health, armour and power; CharacterEntry.VitalNotSaved for one that was not saved.</summary>
        public int Health { get; set; } = CharacterEntry.VitalNotSaved;
        public int Armor { get; set; } = CharacterEntry.VitalNotSaved;
        public int Power { get; set; } = CharacterEntry.VitalNotSaved;

        /// <summary>Rez Trauma's stack count, 0 for none, and when it wears off (Unix milliseconds, UTC).</summary>
        public int RezTraumaStacks { get; set; }
        public long RezTraumaEndsAt { get; set; }

        /// <summary>When the no-healing after a revive wears off (Unix milliseconds, UTC); 0 for none.</summary>
        public long NoHealEndsAt { get; set; }

        public static SavedVitals From(CharacterEntry character)
        {
            return new SavedVitals
            {
                Health = character.CurrentHealth,
                Armor = character.CurrentArmor,
                Power = character.CurrentPower,
                RezTraumaStacks = (int)System.Math.Min(character.RezTraumaStacks, int.MaxValue),
                RezTraumaEndsAt = character.RezTraumaEndsAt,
                NoHealEndsAt = character.NoHealEndsAt
            };
        }
    }
}
