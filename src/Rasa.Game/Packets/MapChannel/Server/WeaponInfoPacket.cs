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

            // None rather than 0 when the weapon has no area effect. The aetypes table starts at
            // 1, so 0 is the column's way of saying "none" and the client's checks are written
            // for a null - most read it as "not CONE" either way, but basetoolaction.py does
            // "if aeType is not None: targetType = TARGET_NONE; SetTarget(None)", and 0 is not
            // None. Sending the 0 through made every tool in the game an area tool with no
            // target, so a healing disc could not be aimed at anyone.
            // ItemTemplateTooltipInfo already writes it this way.
            // Shotguns and propellant guns are sent as cones (Managers.ConeWeapons): the rows say
            // nothing, and a cone is what puts the client's targeting in cone mode.
            var (aeType, aeRadius) = Managers.ConeWeapons.AeOf(Item.ItemTemplate.WeaponInfo, ClassInfo.WeaponClassInfo);

            if (aeType == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteUInt(aeType);

            pw.WriteUInt(aeRadius);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.RecoilAmount);
            pw.WriteNoneStruct();       // ReuseOverride ToDo
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.CoolRate);
            pw.WriteDouble(Item.ItemTemplate.WeaponInfo.HeatPerShot);
            pw.WriteInt((int)Item.ItemTemplate.WeaponInfo.ToolType);
            pw.WriteBool(Item.IsJammed);
            pw.WriteUInt(Item.ItemTemplate.WeaponInfo.AmmoPerShot);
            // The scope the Toggle Zoom key looks through, 0 for none (WeaponScopes).
            pw.WriteInt(WeaponScopes.ProfileOf(ClassInfo.ClassId));
        }
    }
}
