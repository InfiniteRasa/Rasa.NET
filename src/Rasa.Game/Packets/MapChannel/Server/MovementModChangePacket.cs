﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class MovementModChangePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MovementModChange;

        public double MovementMod { get; set; }

        public MovementModChangePacket(double speed)
        {
            MovementMod = speed;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDouble(MovementMod);
        }
    }
}
