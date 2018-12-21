namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponInfo;
        
        public string WeaponName { get; set; }
        public int ClipSize { get; set; }
        public uint CurrentAmmo { get; set; }
        public double AimRate { get; set; }
        public int ReloadTime { get; set; }
        public int AltActionId { get; set; }
        public int AltActionArg { get; set; }
        public int AeType { get; set; }
        public int AeRadius { get; set; }
        public int RecoilAmount { get; set; }
        public int ReuseOverride { get; set; }
        public int CoolRate { get; set; }
        public double HeatPerShot { get; set; }
        public int ToolType { get; set; }
        public bool IsJammed { get; set; }
        public uint AmmoPerShot { get; set; }
        public int CammeraProfile { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(17);
            pw.WriteNoneStruct();       // maybe not used by client
            pw.WriteInt(ClipSize);
            pw.WriteUInt(CurrentAmmo);
            pw.WriteDouble(AimRate);
            pw.WriteInt(ReloadTime);
            pw.WriteInt(AltActionId);
            pw.WriteInt(AltActionArg);
            pw.WriteInt(AeType);
            pw.WriteInt(AeRadius);
            pw.WriteInt(RecoilAmount);
            pw.WriteNoneStruct();       // ReuseOverride ToDo
            pw.WriteInt(CoolRate);
            pw.WriteDouble(HeatPerShot);
            pw.WriteInt(ToolType);
            pw.WriteBool(IsJammed);
            pw.WriteUInt(AmmoPerShot);
            pw.WriteInt(CammeraProfile);
        }
    }
}
