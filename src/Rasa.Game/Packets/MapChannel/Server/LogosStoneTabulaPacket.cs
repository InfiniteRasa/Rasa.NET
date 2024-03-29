﻿using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LogosStoneTabulaPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LogosStoneTabula;

        public List<uint> Logos { get; set; }

        public LogosStoneTabulaPacket(List<uint> logos)
        {
            Logos = logos;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Logos.Count);
            foreach (var logo in Logos)
                pw.WriteUInt(logo);
        }
    }
}
