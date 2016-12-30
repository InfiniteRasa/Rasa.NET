namespace Rasa.Structures
{
    public class ActorStats
    {
        public int Level { get; set; }
        // regen rate
        public int RegenRateCurrentMax { get; set; }
        public int RegenRateNormalMax { get; set; }     // regen rate without bonus
        public double RegenHealthPerSecond { get; set; } // health regen per second
        /* health
        sint32 healthCurrent;
        sint32 healthCurrentMax;
        sint32 healthNormalMax;
        // body
        sint32 bodyCurrent;
        sint32 bodyCurrentMax;
        sint32 bodyNormalMax;
        // mind
        sint32 mindCurrent;
        sint32 mindCurrentMax;
        sint32 mindNormalMax;
        // spirit
        sint32 spiritCurrent;
        sint32 spiritCurrentMax;
        sint32 spiritNormalMax;
        // chi/power
        sint32 chiCurrent;
        sint32 chiCurrentMax;
        sint32 chiNormalMax;
        // armor 
        sint32 armorCurrent;
        sint32 armorCurrentMax;
        sint32 armorNormalMax;
        // armor regen
        sint32 armorRegenCurrent;
        */// todo: Regeneration rates
    }
}
