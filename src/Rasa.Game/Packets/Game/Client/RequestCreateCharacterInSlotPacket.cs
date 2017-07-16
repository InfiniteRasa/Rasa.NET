using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        private static readonly Regex NameRegex = new Regex(@"^[\w ]+$", RegexOptions.Compiled);

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            SlotNum = pr.ReadInt();
            FamilyName = pr.ReadUnicodeString();
            CharacterName = pr.ReadUnicodeString();
            Gender = pr.ReadInt();
            Scale = pr.ReadDouble();

            var itemCount = pr.ReadDictionary();
            for (var i = 0; i < itemCount; i++)
            {
                var data = pr.ReadStruct<AppearanceData>();
                AppearanceData.Add(data.SlotId, data);
            }

            RaceId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }

        public CreateCharacterResult CheckName()
        {
            if (CharacterName.Length < 3)
                return CreateCharacterResult.NameTooShort;

            if (CharacterName.Length > 20)
                return CreateCharacterResult.NameTooLong;

            return !NameRegex.IsMatch(CharacterName) ? CreateCharacterResult.NameFormatInvalid : CreateCharacterResult.Success;
        }
    }
}
