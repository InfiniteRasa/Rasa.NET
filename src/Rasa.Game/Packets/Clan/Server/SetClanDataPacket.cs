﻿namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;
    using Structures;

    public class SetClanDataPacket : ServerPythonPacket
    {
        // A special key value expected by the client that indicates how it handles the Data
        public static readonly string NameKey = "ClanData";

        public override GameOpcode Opcode => GameOpcode.SetClanData;

        public string Name { get; set;  }
        public ClanData Data { get; set; }

        public SetClanDataPacket(string name, ClanData data)
        {
            Name = name;
            Data = data;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteString(Name);
            pw.WriteStruct(Data);            
        }
    }
}
