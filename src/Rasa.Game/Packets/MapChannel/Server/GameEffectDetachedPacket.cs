﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class GameEffectDetachedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectDetached;

        public int EffectId { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(EffectId);
        }
    }
}