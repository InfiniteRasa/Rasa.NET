namespace Rasa.Packets.MapChannel.Server
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
            WriteRefreshAmount(pw, Armor);
            pw.WriteULong(WhoId);
        }

        internal static void WriteRefreshAmount(PythonWriter pw, ActorAttributes armor)
        {
            // Actor.Recv_UpdateArmor calls UpdateAttribute, which always sets a one-second
            // refresh period. The server stores an amount per RefreshPeriod instead (five
            // seconds for fighting NPCs). Keep fractional rates so 1 per 5 seconds is 0.2,
            // rather than truncating it to zero. AttributeInfo still sends both raw fields.
            if (armor.RefreshPeriod > 1)
                pw.WriteDouble((double)armor.RefreshAmount / armor.RefreshPeriod);
            else
                pw.WriteInt(armor.RefreshPeriod > 0 ? armor.RefreshAmount : 0);
        }
    }
}
