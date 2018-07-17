﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LogoutTimeRemainingPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LogoutTimeRemaining;

        public int LogoutType { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(5000); // 5 sec
        }
    }
}
