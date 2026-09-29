using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    using Packets;
    using Structures;

    public class RequestCloneCharacterToSlotPacket : ClientPythonPacket
    {
        public const double MinHeight = 0.90000000000000002;
        public const double MaxHeight = 1.0600000000000001;

        /// <summary>Slack for a single-precision float that meant exactly the bound.</summary>
        public const double HeightTolerance = 1e-6;

        public override GameOpcode Opcode { get; } = GameOpcode.RequestCloneCharacterToSlot;
        
        public byte CloneSlotNum { get; set; }
        public byte SlotNum { get; set; }
        public string CharacterName { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public Race RaceId { get; set; }

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; } = new Dictionary<EquipmentData, AppearanceData>();

        private static readonly Regex NameRegex = new Regex(@"^\w{3,20}\z", RegexOptions.Compiled);

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CloneSlotNum = (byte)pr.ReadInt();
            SlotNum = (byte)pr.ReadInt();
            CharacterName = pr.ReadUnicodeString();
            Gender = (byte)pr.ReadInt();
            Scale = pr.ReadDouble();

            var appearanceCount = pr.ReadDictionary();
            for (var i = 0; i < appearanceCount; i++)
            {
                var data = pr.ReadStruct<AppearanceData>();

                AppearanceData.Add(data.SlotId, data);
            }

            RaceId = (Race)pr.ReadInt();
        }

        public CreateCharacterResult Validate()
        {
            if (CharacterName == null)
                return CreateCharacterResult.InvalidEncoding;

            if (CharacterName.Length < 3)
                return CreateCharacterResult.NameTooShort;

            if (CharacterName.Length > 20)
                return CreateCharacterResult.NameTooLong;

            if (!NameRegex.IsMatch(CharacterName))
                return CreateCharacterResult.NameFormatInvalid;

            // The client sends the height as a single-precision float, and 0.9f widened to
            // double is 0.89999997..., below MinHeight: the slider at its leftmost stop was
            // "Invalid value entered for character height". Anything within float rounding of
            // the range is accepted and snapped to it, so the stored scale is exact.
            if (Scale < MinHeight - HeightTolerance || Scale > MaxHeight + HeightTolerance)
                return CreateCharacterResult.InvalidCharacterHeight;

            Scale = Math.Clamp(Scale, MinHeight, MaxHeight);

            // As creation holds them: a real race, and male or female.
            if (RaceId < Race.Human || RaceId > Race.Thrax)
                return CreateCharacterResult.CharacterCreationInvalidRace;

            if (Gender > 1)
                return CreateCharacterResult.InvalidEncoding;

            return CreateCharacterResult.Success;
        }
    }
}
