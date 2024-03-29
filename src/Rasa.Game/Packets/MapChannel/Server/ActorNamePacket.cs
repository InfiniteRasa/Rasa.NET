﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorNamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorName;

        public string CharacterFamily { get; set; }

        public ActorNamePacket(string characterFamily)
        {
            CharacterFamily = characterFamily;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(CharacterFamily);
        }
    }
}