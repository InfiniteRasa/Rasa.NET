using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// The Resupply Beacon (item 130393, AccountReward_Vet12_Resupply_Vendor_Bot):
    /// ACCOUNTREWARD_RESUPPLY (510, abilities.resupply) level 1. "This special beacon allows you to
    /// teleport an AFS Resupply Bot to your location. Duration: 5 minutes. Cooldown: 1 hour." The
    /// client's ResupplyAction is TARGET_NONE with no effect: the bot is the server's to bring.
    ///
    /// The bot is Creature_AccountReward_Resupply_Vendor_Bot (30069, "AFS Resupply Bot"), whose
    /// class is a creature, a vendor and an NPC. It is put down <see cref="Distance"/> in front of
    /// the player, facing them, for the level's DURATION (300 s), and taken away then or when its
    /// owner leaves the map. It stands where it is put; it takes no part in any fight - nothing
    /// goes looking for it and nothing hurts it (ScriptedCombatGate) - and anyone may use it.
    ///
    /// It sells ammunition and repairs. The level's CREATURE_VARIANT_ID (4727) and NPC_PACKAGE_ID
    /// (2589) are in neither the client's data nor ours, so what it sells is ours: every ammunition
    /// a weapon loads (weaponclass ammo_class_id) - Battery, Canister, Cartridge, Pharmaceutical
    /// and Rocket, Standard to Elite grade, a weapon taking one grade of one type
    /// (<see cref="Stock"/>) - at their buy price, through vendor package 104, the client's
    /// general goods for credits that the seeded ammunition vendor sells under. Repair is any
    /// vendor's (NpcManager.RequestVendorRepair). Its greeting is npcgreetinglanguage 54, "The key
    /// to being a successful soldier is to always keep your weapons in top shape and your ammo
    /// stocked up."
    ///
    /// The beacon is kept: it has no item requirement, and its hour's reuse is the action level's.
    /// </summary>
    public static class ResupplyBots
    {
        public const string ResupplyModule = "abilities.resupply";

        /// <summary>Creature_AccountReward_Resupply_Vendor_Bot, "AFS Resupply Bot".</summary>
        public const EntityClasses BotClass = (EntityClasses)30069;

        /// <summary>The client's general goods for credits (vendordata.vendorpackages), as the seeded ammunition vendor.</summary>
        public const uint VendorPackageId = 104;

        /// <summary>npcgreetinglanguage 54: weapons in top shape and ammo stocked up.</summary>
        public const uint GreetingId = 54;

        /// <summary>When the level does not say (DURATION, seconds).</summary>
        public const int DefaultSeconds = 300;

        /// <summary>How far in front of the player it is put down.</summary>
        public const float Distance = 3f;

        /// <summary>
        /// Every ammunition a weapon loads, Standard (1) to Elite (5) grade: the item template of
        /// each Ammo_*_Grade class a weaponclass row names, the one the vendors stock where a class
        /// has two (Cartridge 1: 28, not 45031). Unobtainium and the Palisades mission's
        /// pharmaceutical are each one weapon's own and left out.
        /// </summary>
        public static readonly IReadOnlyList<uint> Stock = new uint[]
        {
            56, 110894, 110896, 110897, 110902,         // Battery
            32, 110898, 110899, 110901, 110900,         // Canister
            28, 110903, 110904, 110905, 110906,         // Cartridge
            636, 110907, 110908, 110909, 110910,        // Pharmaceutical
            30, 110911, 110912, 110913, 110914          // Rocket
        };

        private sealed class Bot
        {
            public Manifestation Owner;
            public MapChannel MapChannel;
            public Creature Creature;
            public long RemoveAt;
        }

        private static readonly List<Bot> Bots = new List<Bot>();
        private static readonly object BotsLock = new object();

        /// <summary>Whether this action level brings a resupply bot.</summary>
        public static bool Is(ActionInfo action, ActionLevelInfo info) => action?.Module == ResupplyModule && info != null;

        /// <summary>The bot the player has out, or null.</summary>
        public static Creature BotOf(Manifestation owner)
        {
            lock (BotsLock)
                return Bots.FirstOrDefault(bot => bot.Owner == owner)?.Creature;
        }

        /// <summary>Brings the player's bot to them; one of theirs already out is taken away first.</summary>
        public static Creature Deploy(MapChannel mapChannel, Manifestation owner, ActionLevelInfo info, long now = 0)
        {
            if (mapChannel == null || owner == null)
                return null;

            if (now == 0)
                now = Environment.TickCount64;

            List<Bot> replaced;

            lock (BotsLock)
            {
                replaced = Bots.Where(bot => bot.Owner == owner).ToList();
                Bots.RemoveAll(replaced.Contains);
            }

            foreach (var old in replaced)
                Remove(old);

            var vendor = new Vendor(VendorPackageId);
            vendor.VendorItems.AddRange(Stock);

            var creature = new Creature
            {
                EntityClass = BotClass,
                TargetCategory = TargetCategory.Friendly,
                Level = Math.Max(1u, (uint)owner.Level),
                MaxHitPoints = 100,
                AggroRange = 0f,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                State = CharacterState.Idle,
                Name = "AFS Resupply Bot",
                Npc = new Npc
                {
                    NpcPackageId = (uint)Math.Max(0, info.Get(AbilityProperty.NpcPackageId)),
                    GreetingId = GreetingId,
                    Vendor = vendor
                },
                // No part in any fight: not sought, not hurt (CreatureGameplayRules).
                ScriptedCombatGate = () => false
            };

            foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit, Attributes.Speed })
                creature.Attributes.Add(attribute, new ActorAttributes(attribute, 1, 1, 1, 0, 0));
            creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0));
            foreach (var attribute in new[] { Attributes.Chi, Attributes.Power, Attributes.Aware, Attributes.Armor, Attributes.Regen })
                creature.Attributes.Add(attribute, new ActorAttributes(attribute, 0, 0, 0, 0, 0));

            var spot = NavMeshManager.SnapToGround(mapChannel, owner.Position + AbilityManager.FacingOf(owner) * Distance);

            // Facing the player.
            CreatureManager.Instance.SetLocation(creature, spot, owner.Rotation + Math.PI, owner.MapContextId);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            CellManager.Instance.AddToWorld(mapChannel, creature);

            var seconds = Math.Max(1, info.Get(AbilityProperty.Duration, DefaultSeconds));

            lock (BotsLock)
                Bots.Add(new Bot { Owner = owner, MapChannel = mapChannel, Creature = creature, RemoveAt = now + seconds * 1000L });

            return creature;
        }

        /// <summary>Takes away the bots on this map whose time is up, or whose owner has left it. <paramref name="now"/> is for tests; 0 is the clock.</summary>
        public static void Worker(MapChannel mapChannel, long now = 0)
        {
            if (now == 0)
                now = Environment.TickCount64;

            List<Bot> gone;

            lock (BotsLock)
            {
                gone = Bots.Where(bot => bot.MapChannel == mapChannel &&
                                         (now >= bot.RemoveAt || bot.Owner.MapChannel != mapChannel ||
                                          !EntityManager.Instance.Players.TryGetValue(bot.Owner.EntityId, out var owner) || owner != bot.Owner))
                    .ToList();
                Bots.RemoveAll(gone.Contains);
            }

            foreach (var bot in gone)
                Remove(bot);
        }

        private static void Remove(Bot bot)
        {
            if (EntityManager.Instance.Creatures.TryGetValue(bot.Creature.EntityId, out var registered) && registered == bot.Creature)
                CellManager.Instance.RemoveCreatureFromWorld(bot.MapChannel, bot.Creature);
        }
    }
}
