namespace Rasa.Structures
{
    using Data;

    public abstract class Entity
    {
        public EntityGUID EntityGUID { get; set; }
        public ulong EntityId => EntityGUID.Id;
        public EntityType EntityType => EntityGUID.EntityType;

        public void GenerateGUID(EntityType entityType, ulong entityIdCounter)
        {
            EntityGUID = new EntityGUID(entityType, entityIdCounter);
        }
    }
}
