﻿using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class BeginCharacterSelectionPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BeginCharacterSelection;

        public string FamilyName { get; set; }
        public bool HasCharacters { get; set; }
        public uint UserId { get; set; }
        public List<int> EnabledRaceList { get; set; } = new List<int>();
        public bool CanSkipBootcamp { get; set; }

        public BeginCharacterSelectionPacket(string familyName, bool hasCharacters, uint userId)
        {
            FamilyName = familyName;
            HasCharacters = hasCharacters;
            UserId = userId;
            CanSkipBootcamp = true;
            EnabledRaceList.Add(1);
            EnabledRaceList.Add(2);
            EnabledRaceList.Add(3);
            EnabledRaceList.Add(4);
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FamilyName = pr.ReadUnicodeString();
            HasCharacters = pr.ReadBool();
            UserId = (uint) pr.ReadInt();

            var raceCount = pr.ReadTuple();
            for (var i = 0; i < raceCount; ++i)
                EnabledRaceList.Add(pr.ReadInt());

            CanSkipBootcamp = pr.ReadBool();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteBool(HasCharacters);
            pw.WriteInt((int)UserId);

            pw.WriteTuple(EnabledRaceList.Count);

            foreach (var race in EnabledRaceList)
                pw.WriteInt(race);

            pw.WriteBool(CanSkipBootcamp);
        }
    }
}
