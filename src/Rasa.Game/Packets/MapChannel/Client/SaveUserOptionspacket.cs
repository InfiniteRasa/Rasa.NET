using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Structures;

    public class SaveUserOptionsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SaveUserOptions;

        public List<UserOptions> OptionsList = new List<UserOptions>();

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            var listLenght = pr.ReadList();

            for (var i = 0; i < listLenght; i++)
            {
                var option = pr.ReadStruct<UserOptions>();
                OptionsList.Add(new UserOptions(option.OptionId, option.Value));
            }
                
        }
    }
}
