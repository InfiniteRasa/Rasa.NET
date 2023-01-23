using System;

namespace Rasa.Structures
{
    using Data;
    using World;
    public class CreatureAction : ICloneable
    {
        public uint Id { get; set; }
        public string Description { get; set; }
        public ActionId ActionId { get; set; } // action
        public uint ActionArgId { get; set; } // subaction
        public double RangeMin { get; set; }
        public double RangeMax { get; set; }
        public long Cooldown { get; set; } // cooldown time after use
        public long CooldownTimer { get; set; } // current cooldown
        public uint WindupTime { get; set; } // windup animation time
        public uint MinDamage { get; set; } // min damage, not all missile actions use this
        public uint MaxDamage { get; set; } // max damage, not all missile actions use this

        public CreatureAction(CreatureActionEntry action)
        {
            Id = action.Id;
            Description = action.Description;
            ActionId = (ActionId)action.ActionId;
            ActionArgId = action.ActionArgId;
            RangeMin = action.RangeMin;
            RangeMax = action.RangeMax;
            Cooldown = action.Cooldown;
            WindupTime = action.Windup;
            MinDamage = action.MinDamage;
            MaxDamage = action.MaxDamage;
        }

        public CreatureAction(CreatureAction action)
        {
            Id = action.Id;
            Description = action.Description;
            ActionId = action.ActionId;
            ActionArgId = action.ActionArgId;
            RangeMin = action.RangeMin;
            RangeMax = action.RangeMax;
            Cooldown = action.Cooldown;
            WindupTime = action.WindupTime;
            MinDamage = action.MinDamage;
            MaxDamage = action.MaxDamage;
        }

        public object Clone()
        {
            return this;
        }
    }
}
