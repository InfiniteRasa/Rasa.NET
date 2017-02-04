namespace Rasa.Structures
{
    using Managers;

    public class CreatureType
    {
        public int TypeId { get; set; }     // db id (used by mission system)
        // actor info
        public AppearanceData AppearanceData { get; set; }
        public int NameId { get; set; }     // if 0, use name field instead.
        public int EntityClassId { get; set; }
        public string Name { get; set; }    // 70 chars?
        public int Faction { get; set; }
        // stats info
        public ActorStats CreatureStats { get; set; } = new ActorStats();
        public double WalkSpeed { get; set; }
        public double RunSpeed { get; set; }

        //creatureMissile_t* actions[8]; // creature available
                                       // loot table
        //sint32 lootTableSize;
        //creatureTypeLoot_t* lootTable;
        // todo fields:
        // level range, armor, resistances, max item drop count, dodge chance, AI control stuff, and more?
        // public int AggressionTime { get; set; }
        public double WanderDist { get; set; }  // wander boundaries 
        // aggro info
        public double AggroRange { get; set; } // how far away the creature can detect enemies, usually 24.0f but can be increased by having high-range attacks
        // npc data (only if creature is a NPC)
        //npcData_t* npcData;
        // vendor data (only if creature is a vendor, creatures do not need to be a NPC to be a vendor serverside)
        //vendorData_t* vendorData;
    }
}
