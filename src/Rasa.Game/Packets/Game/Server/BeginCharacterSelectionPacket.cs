using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class BeginCharacterSelectionPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BeginCharacterSelection;

        public string FamilyName { get; set; }
        public bool HasCharacters { get; set; }
        public uint AccountId { get; set; }
        public List<Race> EnabledRaceList { get; } = new List<Race>();
        public bool CanSkipBootcamp { get; set; }

        public BeginCharacterSelectionPacket(string familyName, bool hasCharacters, uint accountId, bool canSkipBootcamp)
        {
            FamilyName = familyName;
            HasCharacters = hasCharacters;
            AccountId = accountId;
            CanSkipBootcamp = canSkipBootcamp;

            EnabledRaceList.Add(Race.Human);
            EnabledRaceList.Add(Race.Forean);
            EnabledRaceList.Add(Race.Brann);
            EnabledRaceList.Add(Race.Thrax);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteBool(HasCharacters);
            pw.WriteUInt(AccountId);

            pw.WriteTuple(EnabledRaceList.Count);

            foreach (var race in EnabledRaceList)
                pw.WriteInt((int) race);

            pw.WriteBool(CanSkipBootcamp);
        }
    }
}
