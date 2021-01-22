using System;

namespace Rasa.Data
{
    public struct EntityGUID : IEquatable<EntityGUID>
    {
        public const ulong CounterMask = 0x00FFFFFFFFFFFFFF;

        public ulong Raw { get; private set; }
        public EntityType EntityType => (EntityType)(Raw >> 56);
        public ulong Counter => Raw & CounterMask;

        public EntityGUID(EntityType entityType, ulong entityIdCounter)
        {
            Raw = ((ulong)entityType & 0xFF) << 56 | ((entityIdCounter) & CounterMask);
        }

        public bool Equals(EntityGUID other)
        {
            return other.Raw == Raw;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is EntityGUID entityGUID && Equals(entityGUID);
        }

        public override int GetHashCode()
        {
            return Raw.GetHashCode();
        }

        public static bool operator ==(EntityGUID left, EntityGUID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityGUID left, EntityGUID right)
        {
            return !(left == right);
        }
    }
}
