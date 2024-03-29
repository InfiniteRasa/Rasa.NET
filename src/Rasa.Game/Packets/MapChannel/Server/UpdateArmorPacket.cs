﻿namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdateArmorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateArmor;

        public ActorAttributes Armor { get; set; }
        public ulong WhoId { get; set; }

        public UpdateArmorPacket(ActorAttributes armor, ulong whoId)
        {
            Armor = armor;
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Armor.Current);
            pw.WriteInt(Armor.CurrentMax);
            pw.WriteInt(Armor.RefreshAmount);
            pw.WriteULong(WhoId);
        }
    }
}
