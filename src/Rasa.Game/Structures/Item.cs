namespace Rasa.Structures
{
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
        public MissionItemOwnership MissionOwnership { get; internal set; }
        // weapon specific
        public uint CurrentAmmo { get; set; }
        public bool IsJammed { get; set; }
        public int CammeraProfile { get; set; }

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
    }

    public sealed record MissionItemOwnership(uint CharacterId, uint MissionId, string AssignmentId,
        uint Generation, string ItemKey);
}
