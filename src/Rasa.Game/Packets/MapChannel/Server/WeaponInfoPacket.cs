namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class WeaponInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponInfo;

        public Item Item { get; set; }
        public EntityClass ClassInfo { get; set; }
        public WeaponInfoPacket(Item item, EntityClass classInfo)
        {
            Item = item;
            ClassInfo = classInfo;
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(17);
            pw.WriteNoneStruct();       // maybe not used by client
            pw.WriteUInt(ClassInfo.WeaponClassInfo.ClipSize);
            pw.WriteUInt(Item.CurrentAmmo);
            pw.WriteDouble(Item.ItemTemplate.WeaponInfo.AimRate);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.ReloadTime);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.AltActionId);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.AltActionArgId);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.AeType);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.AeRadius);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.RecoilAmount);
            pw.WriteNoneStruct();       // ReuseOverride ToDo
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.CoolRate);
            pw.WriteDouble(Item.ItemTemplate.WeaponInfo.HeatPerShot);
            pw.WriteInt((int)Item.ItemTemplate.WeaponInfo.ToolType);
            pw.WriteBool(Item.IsJammed);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.AmmoPerShot);
            pw.WriteInt(Item.CammeraProfile);
        }
    }
}
