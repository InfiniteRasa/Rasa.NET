using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class StartAutoFirePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StartAutoFire;

        public double FromUi { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FromUi = pr.ReadDouble();
        }
    }
}
