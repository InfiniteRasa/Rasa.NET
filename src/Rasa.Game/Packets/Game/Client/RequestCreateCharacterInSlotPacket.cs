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
        
        public int SlotNum { get; set; }
        public string FamilyName { get; set; }
        public string CharacterName { get; set; }
        public int Gender { get; set; }
        public double Scale { get; set; }
        public int RaceId { get; set; }

        public Dictionary<int, AppearanceData> AppearanceData { get; } = new Dictionary<int, AppearanceData>();

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            SlotNum = pr.ReadInt();
            FamilyName = pr.ReadUnicodeString();
            CharacterName = pr.ReadUnicodeString();
            Gender = pr.ReadInt();
            Scale = pr.ReadDouble();

            // init dictionary
            for (var i = 0; i < 21; i++)
                AppearanceData.Add(i + 1, new AppearanceData { SlotId = i + 1, ClassId = 0, Color = new Color { Red = 0x00, Green = 0x00, Blue = 0x00, Alpha = 0x00 } });

            var itemCount = pr.ReadDictionary();
            for (var i = 0; i < itemCount; i++)
            {
                //edit data
                var data = pr.ReadStruct<AppearanceData>();
                AppearanceData[data.SlotId - 1] = new AppearanceData {SlotId = data.SlotId, ClassId = data.ClassId, Color = data.Color};
            }
           RaceId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
