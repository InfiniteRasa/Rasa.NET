﻿namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestArmAbilityPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestArmAbility;

        public int AbilityDrawerSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"RequestArmAbility:\n{pr.ToString()}");
            pr.ReadTuple();
            AbilityDrawerSlot = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
