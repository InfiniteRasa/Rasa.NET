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
        public const double MinHeight = 0.90000000000000002;
        public const double MaxHeight = 1.0600000000000001;

        public override GameOpcode Opcode { get; } = GameOpcode.RequestCreateCharacterInSlot;

        public byte SlotNum { get; set; }
        public string FamilyName { get; set; }
        public string CharacterName { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public Race RaceId { get; set; }

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; } = new Dictionary<EquipmentData, AppearanceData>();

        private static readonly Regex NameRegex = new Regex(@"^\w{3,20}$", RegexOptions.Compiled);

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            SlotNum = (byte) pr.ReadInt();
            FamilyName = pr.ReadUnicodeString();
            CharacterName = pr.ReadUnicodeString();
            Gender = (byte) pr.ReadInt();
            Scale = pr.ReadDouble();

            var appearanceCount = pr.ReadDictionary();
            for (var i = 0; i < appearanceCount; i++)
            {
                var data = pr.ReadStruct<AppearanceData>();

                AppearanceData.Add(data.SlotId, data);
            }

            RaceId = (Race) pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }

        public CreateCharacterResult Validate()
        {
            if (CharacterName.Length < 3)
                return CreateCharacterResult.NameTooShort;

            if (CharacterName.Length > 20)
                return CreateCharacterResult.NameTooLong;

            if (!NameRegex.IsMatch(CharacterName))
                return CreateCharacterResult.NameFormatInvalid;

            if (Scale < MinHeight || Scale > MaxHeight)
                return CreateCharacterResult.InvalidCharacterHeight;

            return CreateCharacterResult.Success;
        }
    }
}
