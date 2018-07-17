namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LockboxTabPermissionsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockboxTabPermissions;
        
        public uint UnlockedNum { get; set; }

        public LockboxTabPermissionsPacket(uint unlockedNum)
        {
            UnlockedNum = unlockedNum;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(5);
            for (uint i = 1; i < 6; i++)
            {
                pw.WriteUInt(i);

                if (i <= UnlockedNum)
                    pw.WriteBool(true);
                else
                    pw.WriteBool(false);
            }
        }
    }
}
