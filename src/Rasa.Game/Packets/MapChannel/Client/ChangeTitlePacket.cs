﻿namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ChangeTitlePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeTitle;

        public uint TitleId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            if (pr.PeekType() == PythonType.Int)
                TitleId = (uint)pr.ReadInt();
            else
                pr.ReadNoneStruct();
        }
    }
}
