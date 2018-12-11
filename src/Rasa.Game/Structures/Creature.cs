using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public class Creature
    {
        public uint DbId { get; set; }
        public EntityClassId EntityClassId { get; set; }
        // npc data (only if creature is a NPC)
        public CreatureNpcData NpcData { get; set; }
        // loot data (only if creature is harvestable)
        public CreatureLootData LootData { get; set; }
        // vendor data (only if creature is a vendor, creatures do not need to be a NPC to be a vendor serverside)
        public CreatureVendorData VendorData { get; set; }
        public Actor Actor { get; set; }                                        // the base actor object
        public int Faction { get; set; }
        public int Level { get; set; }
        public int MaxHitPoints { get; set; }
        public int NameId { get; set; }
        public int UpdatePositionCounter;                                       // decreases, when it hits 0 and the cell position changed, call creature_updateCellLocation()
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        //sint32 lastattack;
        //sint32 lastresttime;
        //float velocity;
        //sint32 rottime; //rotation speed
        //float range; //attackrange
        //sint32 attack_habbit; //meelee or range fighter 
        //sint32 agression; // hunting timer for enemys
        //sint32 lastagression;
        //sint32 wanderstate;
        //sint32 movestate;
        //float wx,wy,wz; // target destination (can be far away)
        //baseBehavior_baseNode homePos;  //--- spawn location (used for wander)
        //baseBehavior_baseNode* pathnodes; //--entity patrol nodes
        //sint32** aggrotable; //stores enemydamage
        //sint32 aggrocount;
        // origin
        public SpawnPool SpawnPool { get; set; }    // the spawnpool that initiated the creation of this creature
        // behavior controller
        //behaviorState_t controller;
        // loot dispenser
        //sint64 lootDispenserObjectEntityId;
    }
}
