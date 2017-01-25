namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PreloadDataPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreloadData;

        public int WeaponId { get; set; }
        public int AbilitiesList { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(WeaponId);
            pw.WriteList(25);
            for (var i = 0; i < 24; i++)
            {
                pw.WriteTuple(2);
                pw.WriteInt(1);
                pw.WriteInt(1);
            }

            /* old c++ code i think it's wrong
            // PreloadData
            pym_init(&pms);
            pym_tuple_begin(&pms);
            pym_addInt(&pms, 0); // weaponId
            pym_addInt(&pms, 0); // abilities
            pym_tuple_end(&pms);
            */
        }
    }
}
