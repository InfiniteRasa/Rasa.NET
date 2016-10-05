using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Rasa.Networking
{
    using Packets;

    public class PacketRouter<T, O>
    {
        private readonly Dictionary<O, PacketData> _handlers = new Dictionary<O, PacketData>();

        public void RegisterHandler(O opcode, string methodName, Type packetType)
        {
            _handlers.Add(opcode, CreateAction(methodName, packetType));
        }

        private static PacketData CreateAction(string name, Type packetType)
        {
            var method = typeof(T).GetTypeInfo().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException($"{name} method doesn't exist in AuthClient!");

            var handler = method.CreateDelegate(Expression.GetActionType(typeof(T), typeof(IOpcodedPacket<O>)));

            return new PacketData(packetType, (Action<T, IOpcodedPacket<O>>) handler);
        }

        public void RoutePacket(T target, O opcode, IOpcodedPacket<O> packet)
        {
            if (!_handlers.ContainsKey(opcode))
            {
                Logger.WriteLog(LogType.Error, $"PacketRouter can't route to a non-existant opcode: {opcode}");
                return;
            }

            _handlers[opcode].Handler(target, packet);
        }

        public Type GetPacketType(O opcode)
        {
            return _handlers.ContainsKey(opcode) ? _handlers[opcode].Type : null;
        }

        public class PacketData
        {
            public Type Type { get; }
            public Action<T, IOpcodedPacket<O>> Handler { get; }

            public PacketData(Type type, Action<T, IOpcodedPacket<O>> handler)
            {
                Type = type;
                Handler = handler;
            }
        }
    }
}
