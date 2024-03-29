﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdateHealthPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateHealth;

        public ActorAttributes Health { get; set; }
        public ulong WhoId { get; set; }

        public UpdateHealthPacket(ActorAttributes health, ulong whoId)
        {
            Health = health;
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Health.Current);
            pw.WriteInt(Health.CurrentMax);
            pw.WriteInt(Health.RefreshAmount);
            pw.WriteULong(WhoId);
        }
    }
}