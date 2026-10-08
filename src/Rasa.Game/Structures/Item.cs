using System.Collections.Generic;

namespace Rasa.Structures
{
    using Char;
    using Managers;
    using Repositories.Char.Items;

    public class Item : IItemChange
    {
        public Item()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public Item(uint itemTemplateId, uint stackSize, int currentHitPoints, uint color)
        {
            ItemTemplateId = itemTemplateId;
            StackSize = stackSize;
            CurrentHitPoints = currentHitPoints;
            Crafter = "";
            Color = color;
        }

        public ulong EntityId { get; }
        public ItemTemplate ItemTemplate { get; set; }
        public uint ItemTemplateId { get; set; }
        // uniqe id stored in db
        public uint Id { get; set; }
        // location info
        public uint OwnerId { get; set; }
        public uint OwnerSlotId { get; set; }
        // item instance specific
        public uint Color { get; set; }
        public string Crafter { get; set; }
        public int CurrentHitPoints { get; set; }
        public uint StackSize { get; set; }

        /// <summary>
        /// The character this item was bound to when it was equipped (Bind on Equip) or bound on
        /// request, 0 when it has not been. Persisted in items.bound_character_id.
        /// </summary>
        public uint BoundCharacterId { get; set; }

        /// <summary>
        /// Bound on Character, either way it can be: this item was bound to a character, or its
        /// template is bound from the start (mission items, GM and event gear, account rewards).
        /// What the client is sent as boundToCharacter, and what trade, the auction house and the
        /// clan lockbox refuse.
        /// </summary>
        public bool IsBound => BoundCharacterId != 0 || (ItemTemplate?.BoundToCharacter ?? false);
        public MissionItemOwnership MissionOwnership { get; internal set; }

        private readonly uint[] _moduleIds = new uint[ItemEntry.ModuleSlots];

        /// <summary>
        /// The module in each of the item's four module slots - a module_class id - or 0 for an
        /// empty slot. What the client is sent as lootModuleIds; persisted in items.module_1 to
        /// module_4. Changed through Managers.ItemModules.
        /// </summary>
        public IReadOnlyList<uint> ModuleIds => _moduleIds;

        /// <summary>Puts a module in a slot, 0 to 3, or empties it with 0.</summary>
        public void SetModule(int slot, uint moduleId) => _moduleIds[slot] = moduleId;
        // weapon specific
        public uint CurrentAmmo { get; set; }
        public bool IsJammed { get; set; }

        /// <summary>
        /// Heat in the barrel, 0 to <see cref="Data.WeaponHeat.Capacity"/>. Not persisted: the
        /// client rebuilds its own heat table empty on every login, so a weapon that was hot when
        /// you logged out is cold when you come back, and the server agreeing with that is the
        /// point.
        ///
        /// Read it through <c>ManifestationManager.CurrentHeat</c> rather than directly - it is
        /// only correct as of <see cref="HeatUpdatedAt"/>, and cooling is applied on read.
        /// </summary>
        public double Heat { get; set; }

        /// <summary>When <see cref="Heat"/> was last brought up to date, in Environment.TickCount64 ms.</summary>
        public long HeatUpdatedAt { get; set; }

        /// <summary>
        /// Wear not yet taken off <see cref="CurrentHitPoints"/>, in hit points, 0 to 1. A shot
        /// costs a weapon a few thousandths of a hit point; they add up here until they make a
        /// whole one (Managers.Durability). Not persisted: what is lost with it at logout is less
        /// than a hit point.
        /// </summary>
        public double WearCarry { get; set; }
    }

    public sealed record MissionItemOwnership(uint CharacterId, uint MissionId, string AssignmentId,
        uint Generation, string ItemKey);
}
