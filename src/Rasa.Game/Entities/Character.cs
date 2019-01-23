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
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float Orientation { get; set; }
        public int MapContextId { get; set; }

        public Character(Client client)
        {
            Client = client;
        }

        private void SetStartUpData()
        {
            PosX = 894.9f;
            PosY = 307.9f;
            PosZ = 347.1f;
            Orientation = 1f;
            MapContextId = 1220;
            ClassId = 1; // recruit, maybe we need to chage this and get data from game client
            //UserId = CharacterTable.GetHigherId() + 1;
            Console.WriteLine("userID is => {0}", UserId);
            var helm = new AppearanceData
            {
                SlotId = EquipmentData.Helmet,
                Class = (EntityClassId)10908,
                Color = new Color(0x80, 0x80, 0x80)
            };
            var boots = new AppearanceData
            {
                SlotId = EquipmentData.Shoes,
                Class = (EntityClassId)7054,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var gloves = new AppearanceData
            {
                SlotId = EquipmentData.Gloves,
                Class = (EntityClassId)10909,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var torso = new AppearanceData
            {
                SlotId = EquipmentData.Torso,
                Class = (EntityClassId)7082,
                Color = new Color(0x80, 0x80, 0x80),
            };
            var legs = new AppearanceData
            {
                SlotId = EquipmentData.Legs,
                Class = (EntityClassId)7053,
                Color = new Color(0x80, 0x80, 0x80),
            };
            // todo: save default items to the db
        }
    }
}
