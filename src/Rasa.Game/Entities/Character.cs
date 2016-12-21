namespace Rasa.Entities
{
    using Structures;

    public class Character
    {
        public ulong UserId { get; set; }
        public int ClassId { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double Rotation { get; set; }
        public int MapContextId { get; set; }

        public void SetStartUpData()
        {
            PosX = 894.9;
            PosY = 347.1;
            PosZ = 307.9;
            Rotation = 1;
            MapContextId = 1220;
            ClassId = 1; // recruit, maybe we need to chage this and get data from game client
            var helm = new AppearanceData
            {
                SlotId = 1,
                ClassId = 10908,
                Color = new Color(0x80, 0x80, 0x80)
            };
            var boots = new AppearanceData
            {
                SlotId = 2,
                ClassId = 7054,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var gloves = new AppearanceData
            {
                SlotId = 3,
                ClassId = 10909,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var torso = new AppearanceData
            {
                SlotId = 15,
                ClassId = 7082,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var legs = new AppearanceData
            {
                SlotId = 16,
                ClassId = 7053,
                Color = new Color(0x80, 0x80, 0x80),
            };
            // todo: save default items to the db
        }
    }
}
