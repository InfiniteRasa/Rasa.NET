using System;

namespace Rasa.Data
{
    public struct EntityGUID : IEquatable<EntityGUID>
    {
        // Lower entity IDs are reserved in the client
        const ulong _startingIdCounter = 1000;

        public ulong Id { get; private set; }

        public EntityGUID(EntityType entityType, ulong entityIdCounter)
        {
            Id = ((ulong)entityType & 0xFF) << 56 | ((_startingIdCounter + entityIdCounter) & 0x00FFFFFFFFFFFF);
        }        

        public bool Equals(EntityGUID other)
        {
            return other.Id == Id;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is EntityGUID entityGUID && Equals(entityGUID);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
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
