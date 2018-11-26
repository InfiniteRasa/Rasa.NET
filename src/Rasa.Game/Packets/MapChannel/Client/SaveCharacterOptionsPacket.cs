using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Structures;

    public class SaveCharacterOptionsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SaveCharacterOptions;

        public List<CharacterOptions> OptionsList = new List<CharacterOptions>();

        public override void Read(PythonReader pr)
        {            
            pr.ReadTuple();

            var listLenght = pr.ReadList();

            for (var i = 0; i < listLenght; i++)
            {
                var option = pr.ReadStruct<CharacterOptions>();
                OptionsList.Add(new CharacterOptions(option.OptionId, option.Value));
            }
        }
    }
}
