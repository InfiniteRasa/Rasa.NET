﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AbilityDrawerSlotPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbilityDrawerSlot;

        public int AbilityDrawerSlot { get; set; }

        public AbilityDrawerSlotPacket(int abilityDrawerSlot)
        {
            AbilityDrawerSlot = abilityDrawerSlot;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(AbilityDrawerSlot);
        }
    }
}
