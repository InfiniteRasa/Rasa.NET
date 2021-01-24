using System;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class StoreUserClientInformationPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StoreUserClientInformation;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            pr.ReadString();
            pr.ReadString();
        }
    }
}
