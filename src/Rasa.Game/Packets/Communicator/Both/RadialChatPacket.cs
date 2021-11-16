﻿namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class RadialChatPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RadialChat;

        public ulong EntityId { get; set; }
        public string FamilyName { get; set; }
        public string TextMsg { get; set; }     // char[256] ??
        
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            TextMsg = pr.ReadUnicodeString();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteUnicodeString(TextMsg);
            pw.WriteULong(EntityId);
        }
    }
}
