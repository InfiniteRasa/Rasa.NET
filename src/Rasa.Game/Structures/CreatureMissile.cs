namespace Rasa.Structures
{
    // someday we should rename this to creatureAction, creatureAbility or creatureAttack
    public class CreatureMissile
    {
        public uint Id { get; set; }
        public uint ActionId { get; set; } // action
        public uint ActionArgId { get; set; } // subaction
        public float RangeMin { get; set; }
        public float RangeMax { get; set; }
        public long RecoverTime { get; set; } // cooldown time after use
        public long RecoverTimeGlobal { get; set; } // cooldown of all available actions (if lower than active recover timer, will have no effect)
        public uint WindupTime { get; set; } // windup animation time
        public uint MinDamage { get; set; } // min damage, not all missile actions use this
        public uint MaxDamage { get; set; } // max damage, not all missile actions use this
    }
}
