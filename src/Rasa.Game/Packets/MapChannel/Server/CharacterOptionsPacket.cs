using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class CharacterOptionsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterOptions;

        public List<CharacterOptions> OptionsList { get; set; }

        public CharacterOptionsPacket(List<CharacterOptions> optionsList)
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
