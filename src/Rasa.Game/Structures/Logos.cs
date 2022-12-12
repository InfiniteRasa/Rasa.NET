namespace Rasa.Structures
{
    using Data;
    public class Logos : DynamicObject
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        public Logos(LogosEntry logos)
        {
            EntityClassId = (EntityClassId)logos.ClassId;
            Id = logos.Id;
            MapContextId = logos.MapContextId;
            Name = logos.Name;
            Position = logos.Position;
            DynamicObjectType = DynamicObjectType.Logos;
        }
    }
}
