using System.IO;

namespace Rasa.Packets
{
    public interface MovementPacket
    {
        void WriteMovement(BinaryWriter bw);
    }
}
