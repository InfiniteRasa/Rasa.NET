using System.Collections.Generic;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    using Packets;
    using Structures;

    public class RequestCreateCharacterInSlotPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCreateCharacterInSlot;
        
        public uint SlotNum { get; set; }
        public string FamilyName { get; set; }
        public string CharacterName { get; set; }
        public int Gender { get; set; }
        public double Scale { get; set; }
        public int RaceId { get; set; }

        public Dictionary<EquipmentSlots, AppearanceData> AppearanceData { get; } = new Dictionary<EquipmentSlots, AppearanceData>();

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            SlotNum = pr.ReadUInt();
            FamilyName = pr.ReadUnicodeString();
            CharacterName = pr.ReadUnicodeString();
            Gender = pr.ReadInt();
            Scale = pr.ReadDouble();
            var itemCount = pr.ReadDictionary();
            for (var i = 0; i < itemCount; i++)
            {
                var data = pr.ReadStruct<AppearanceData>();
                AppearanceData.Add(data.SlotId, new AppearanceData { SlotId = data.SlotId, ClassId = data.ClassId, Color = data.Color });
            }
           RaceId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
