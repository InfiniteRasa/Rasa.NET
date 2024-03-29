﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UpdateEscortStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateEscortStatus;

        public bool IsEscort { get; set; }

        public UpdateEscortStatusPacket(bool isEscort)
        {
            IsEscort = isEscort;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(IsEscort);
        }
    }
}
