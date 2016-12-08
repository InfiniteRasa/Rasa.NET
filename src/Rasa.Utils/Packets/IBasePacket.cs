using System.IO;

namespace Rasa.Packets
{
    public interface IBasePacket
    {
        void Read(BinaryReader br);

        void Write(BinaryWriter bw);
    }
}
