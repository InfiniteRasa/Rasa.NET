using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    using Packets;
    using Structures;

    public class RequestCreateCharacterInSlotPacket : ClientPythonPacket
    {
        public const double MinHeight = 0.90000000000000002;
        public const double MaxHeight = 1.0600000000000001;

        /// <summary>Slack for a single-precision float that meant exactly the bound.</summary>
        public const double HeightTolerance = 1e-6;

        public override GameOpcode Opcode { get; } = GameOpcode.RequestCreateCharacterInSlot;

        public byte SlotNum { get; set; }
        public string FamilyName { get; set; }
        public string CharacterName { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public Race RaceId { get; set; }

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; } = new Dictionary<EquipmentData, AppearanceData>();

        private static readonly Regex NameRegex = new Regex(@"^\w{3,20}\z", RegexOptions.Compiled);

        public override void Read(PythonReader pr)
        {
            ReadFields(pr, true);
        }

        /// <summary>The arguments after the tuple head: the slot first when the method has one (CreateCharacterPacket has none).</summary>
        protected void ReadFields(PythonReader pr, bool withSlot)
        {
            pr.ReadTuple();

            if (withSlot)
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

        public CreateCharacterResult Validate()
        {
            // ReadUnicodeString answers a Python None with null; the length checks below used
            // to dereference it and disconnect the client at the character screen.
            if (CharacterName == null || FamilyName == null)
                return CreateCharacterResult.InvalidEncoding;

            var characterName = ValidateName(CharacterName);

            if (characterName != CreateCharacterResult.Success)
                return characterName;

            // The family name was never checked: empty, over-long or any characters at all
            // went into account.family_name as sent, and it is shown to every other player.
            var familyName = ValidateName(FamilyName);

            if (familyName != CreateCharacterResult.Success)
                return familyName;

            // The client sends the height as a single-precision float, and 0.9f widened to
            // double is 0.89999997..., below MinHeight: the slider at its leftmost stop was
            // "Invalid value entered for character height". Anything within float rounding of
            // the range is accepted and snapped to it, so the stored scale is exact.
            if (Scale < MinHeight - HeightTolerance || Scale > MaxHeight + HeightTolerance)
                return CreateCharacterResult.InvalidCharacterHeight;

            Scale = Math.Clamp(Scale, MinHeight, MaxHeight);

            if (RaceId < Race.Human || RaceId > Race.Thrax)
                return CreateCharacterResult.CharacterCreationInvalidRace;

            // Male or female; nothing the client offers sends anything else.
            if (Gender > 1)
                return CreateCharacterResult.InvalidEncoding;

            return CreateCharacterResult.Success;
        }

        private static CreateCharacterResult ValidateName(string name)
        {
            if (name.Length < 3)
                return CreateCharacterResult.NameTooShort;

            if (name.Length > 20)
                return CreateCharacterResult.NameTooLong;

            if (!NameRegex.IsMatch(name))
                return CreateCharacterResult.NameFormatInvalid;

            return CreateCharacterResult.Success;
        }
    }
}
