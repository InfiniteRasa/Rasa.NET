using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class AutoFireKeepAlivePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AutoFireKeepAlive;

        public int KeepAliveDelay { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            KeepAliveDelay = pr.ReadInt();
        }
    }
}
