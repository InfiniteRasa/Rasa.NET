namespace Rasa.Packets.Inventory.Server
{
    using Data;
    using Memory;

    public class LockboxTabPermissionsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockboxTabPermissions;
        
        public int UnlockedNum { get; set; }

        public LockboxTabPermissionsPacket(int unlockedNum)
        {
            UnlockedNum = unlockedNum;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(5);
            for (int i = 1; i < 6; i++)
            {
                pw.WriteInt(i);

                if (i <= UnlockedNum)
                    pw.WriteBool(true);
                else
                    pw.WriteBool(false);
            }
        }
    }
}
