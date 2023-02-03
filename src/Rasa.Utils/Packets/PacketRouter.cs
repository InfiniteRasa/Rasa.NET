using System.Linq.Expressions;
using System.Reflection;

namespace Rasa.Packets;

public class PacketRouter<T, O>
    where T : class
    where O : struct
{
    private readonly Dictionary<O, PacketData> _handlers = new Dictionary<O, PacketData>();

    public PacketRouter()
    {
        SetupHandlers();
    }

    public void SetupHandlers()
    {
        var info = typeof(T).GetTypeInfo();

        foreach (var method in info.DeclaredMethods)
        { 
            foreach (var attr in method.GetCustomAttributes<PacketHandlerAttribute>())
            {
                var paramType = method.GetParameters().FirstOrDefault()?.ParameterType;
                if (paramType == null)
                    throw new Exception($"Invalid PacketHandler attribute usage! Used on function: {info.FullName}.{method.Name}");

                _handlers.Add(attr.GetOpcode<O>(), new PacketData(paramType, method.CreateDelegate(Expression.GetActionType(typeof(T), paramType))));
            }
        }
    }

    public void RoutePacket(T target, IOpcodedPacket<O> packet)
    {
        if (!_handlers.ContainsKey(packet.Opcode))
        {
            Logger.WriteLog(LogType.Error, $"PacketRouter can't route to a non-existant opcode: {packet.Opcode}");
            return;
        }

        _handlers[packet.Opcode].Handler.DynamicInvoke(target, packet);
    }

    public Type? GetPacketType(O opcode)
    {
        if (_handlers.TryGetValue(opcode, out var value))
            return value.Type;

        Logger.WriteLog(LogType.Error, $"Non-existant PacketRouter type definition! Opcode: {opcode}");
        return null;
    }

    public class PacketData
    {
        public Type Type { get; }
        public Delegate Handler { get; }

        public PacketData(Type type, Delegate handler)
        {
            Type = type;
            Handler = handler;
        }

        public override string ToString() => $"PacketData(Type: {Type} | Handler: {typeof(T).FullName}::{Handler.GetMethodInfo().Name})";
    }
}
