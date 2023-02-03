namespace Rasa.Packets;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class PacketHandlerAttribute : Attribute
{
    private object Opcode { get; }

    public PacketHandlerAttribute(object opcode)
    {
        Opcode = opcode;
    }

    public T GetOpcode<T>() where T : struct
    {
        return (T) Opcode;
    }
}
