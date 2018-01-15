namespace Rasa.Entities
{
    using Data;
    using Structures;

    public class Character
    {
        public ulong UserId { get; set; }
        public int ClassId { get; set; }
        public Position Position { get; set; }
        public double Rotation { get; set; }
        public int MapContextId { get; set; }

        public void SetStartUpData()
        {
            Position = new Position(894.9, 347.1, 307.9); 
            Rotation = 1;
            MapContextId = 1220;
            ClassId = 1; // recruit, maybe we need to chage this and get data from game client
            var helm = new AppearanceData
            {
                SlotId = EquipmentSlots.Helmet,
                ClassId = 10908,
                Color = new Color(0x80, 0x80, 0x80)
            };
            var boots = new AppearanceData
            {
                SlotId = EquipmentSlots.Shoes,
                ClassId = 7054,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var gloves = new AppearanceData
            {
                SlotId = EquipmentSlots.Gloves,
                ClassId = 10909,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var torso = new AppearanceData
            {
                SlotId = EquipmentSlots.Torso,
                ClassId = 7082,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var legs = new AppearanceData
            {
                SlotId = EquipmentSlots.Legs,
                ClassId = 7053,
                Color = new Color(0x80, 0x80, 0x80),
            };
            // todo: save default items to the db
        }
    }
}
