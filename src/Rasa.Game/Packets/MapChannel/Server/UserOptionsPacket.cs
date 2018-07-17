using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UserOptionsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UserOptions;

        public List<UserOptions> OptionsList { get; set; }

        public UserOptionsPacket(List<UserOptions> optionsList)
        {
            OptionsList = optionsList;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(OptionsList.Count);
            foreach (var option in OptionsList)
                pw.WriteStruct(option);
        }
    }
}
