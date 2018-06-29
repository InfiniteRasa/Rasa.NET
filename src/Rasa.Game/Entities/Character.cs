using System;

namespace Rasa.Entities
{
    using Data;
    using Game;
    using Structures;

    public class Character
    {
        public Client Client { get; }

        public string Name { get; private set; }
        public string FamilyName { get; private set; }

        public int StilyeCount { get; set; }
        public int ErrorCode { get; set; }
        public ulong UserId { get; set; }
        public int ClassId { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double Rotation { get; set; }
        public int MapContextId { get; set; }

        public Character(Client client)
        {
            Client = client;
        }

        private void SetStartUpData()
        {
            PosX = 894.9;
            PosY = 307.9;
            PosZ = 347.1;
            Rotation = 1;
            MapContextId = 1220;
            ClassId = 1; // recruit, maybe we need to chage this and get data from game client
            //UserId = CharacterTable.GetHigherId() + 1;
            Console.WriteLine("userID is => {0}", UserId);
            var helm = new AppearanceData
            {
                SlotId = EquipmentData.Helmet,
                ClassId = 10908,
                Color = new Color(0x80, 0x80, 0x80)
            };
            var boots = new AppearanceData
            {
                SlotId = EquipmentData.Shoes,
                ClassId = 7054,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var gloves = new AppearanceData
            {
                SlotId = EquipmentData.Gloves,
                ClassId = 10909,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var torso = new AppearanceData
            {
                SlotId = EquipmentData.Torso,
                ClassId = 7082,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var legs = new AppearanceData
            {
                SlotId = EquipmentData.Legs,
                ClassId = 7053,
                Color = new Color(0x80, 0x80, 0x80),
            };
            // todo: save default items to the db
        }
    }
}
