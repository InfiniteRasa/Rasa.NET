using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using System;
    using System.Linq;
    using World;
    public class Creature : Actor, ICloneable
    {
        public uint DbId { get; set; }
        // npc data (only if creature is a NPC)
        public Npc Npc { get; set; }
        // loot data (only if creature is harvestable)
        public CreatureLootData LootData { get; set; }
        public Factions Faction { get; set; }
        public uint Level { get; set; }
        public uint MaxHitPoints { get; set; }
        public uint NameId { get; set; }
        public long UpdatePositionCounter;                                       // decreases, when it hits 0 and the cell position changed, call creature_updateCellLocation()
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        //sint32 lastattack;
        //float velocity;
        //sint32 rottime; //rotation speed
        //float range; //attackrange
        //sint32 attack_habbit; //meelee or range fighter 
        //sint32 agression; // hunting timer for enemys
        // aggro info
        public float AggroRange = 18.0f; // how far away the creature can detect enemies, usually 24.0f but can be increased by having high-range attacks
        public long AggressionTime = 5000; // ToDo

        public float WalkSpeed { get; set; }
        public float RunSpeed { get; set; }
        //sint32 movestate;
        //float wx,wy,wz; // target destination (can be far away)
        public BaseBehaviorBaseNode HomePos = new BaseBehaviorBaseNode();  //--- spawn location (used for wander)
        public BaseBehaviorBaseNode Pathnodes { get; set; } //--entity patrol nodes
        //sint32** aggrotable; //stores enemydamage
        //sint32 aggrocount;
        public double Scale = 1.0d;
        // origin
        public SpawnPool SpawnPool { get; set; }    // the spawnpool that initiated the creation of this creature
        // behavior controller
        public BehaviorState Controller = new BehaviorState();
        // loot dispenser
        public ulong LootDispenserObjectEntityId { get; internal set; }

        /// <summary>
        /// The corpse loot dispenser made when this creature died, or 0. Distinct from
        /// LootDispenserObjectEntityId, which is the dynamic object that marks a lootable corpse
        /// in the world; this is the dispenser holding what is actually on it.
        /// </summary>
        public ulong CorpseLootEntityId { get; internal set; }
        // creature actions
        public List<CreatureAction> Actions = new List<CreatureAction>();

        // creature tumers
        public long LastAgression { get; internal set; }
        public long LastRestTime { get; internal set; }
        public bool IsInteractable { get; set; } = true;

        /// <summary>
        /// The player this creature belongs to, or 0 for an ordinary world creature.
        ///
        /// Set, a creature is a minion: it takes commands from that player, follows them rather
        /// than wandering a spawn point, and goes away when they do. The client is never told
        /// this - it has no concept of which entity is its minion, and the ack packets carry the
        /// minion's entity id purely so the message text can name it.
        /// </summary>
        public ulong MasterEntityId { get; set; }

        /// <summary>Whether this minion fights back, goes looking, or does neither.</summary>
        public MinionStance Stance { get; set; } = MinionStance.Defensive;

        /// <summary>
        /// Milliseconds of life left, counted down by MinionManager. Zero means no timer, which
        /// is what a GM-spawned test minion gets; a summoned one will have the ability's duration.
        /// </summary>
        public long DespawnTime { get; set; }

        /// <summary>
        /// Who may harvest this corpse: the player whose kill it was, set in HandleCreatureKill.
        /// Corpse looting already works this way - RequestCorpseLooting refuses anyone but the
        /// dispenser's owner - and harvesting hands out items just as looting does, so it is held
        /// to the same rule rather than being first-come.
        /// </summary>
        public ulong HarvestOwnerEntityId { get; set; }

        /// <summary>
        /// Attempts left on this corpse, successes and failures alike. Set when the creature dies
        /// and decremented by every attempt; at zero the corpse is depleted. Living creatures sit
        /// at zero, which is also a refusal, so a corpse that was never killed by anyone cannot
        /// be harvested either.
        /// </summary>
        public int HarvestAttemptsLeft { get; set; }
        internal Manifestation CombatParticipant { get; set; }
        internal Game.Missions.World.ActorGameplayBinding GameplayBinding { get; set; }

        public Creature(CreatureEntry data)
        {
            DbId = data.Id;
            EntityClass = (EntityClasses)data.ClassId;
            Faction = (Factions)data.Faction;
            Level = data.Level;
            MaxHitPoints = data.MaxHitPoints;
            NameId = data.NameId;
            RunSpeed = data.RunSpeed;
            WalkSpeed = data.WalkSpeed;
        }

        public Creature()
        {
        }

        public Creature(Creature creature)
        {
            AppearanceData = creature.AppearanceData;
            DbId = creature.DbId;
            EntityClass = creature.EntityClass;
            Faction = creature.Faction;
            Level = creature.Level;
            MaxHitPoints = creature.MaxHitPoints;
            NameId = creature.NameId;
            Npc = creature.Npc;
            RunSpeed = creature.RunSpeed;
            WalkSpeed = creature.WalkSpeed;
            foreach (var action in creature.Actions)
                Actions.Add(new CreatureAction((CreatureAction)action.Clone()));
            IsInteractable = creature.IsInteractable;
        }

        public object Clone()
        {
            return new Creature(this);
        }
    }
}
