namespace Rasa.Packets.MapChannel.Client
{
    using System;

    using Data;
    using Memory;

    /// <summary>
    /// A developer's request to examine an entity: <c>(entityId,)</c>, from
    /// client/physicalentity.py OnExamine, <c>SendCallUserMethod('ExamineHack', (self.entityId,))</c>.
    /// Nothing in the retail client calls OnExamine; see ClientPacketHandler.ExamineHack.
    /// </summary>
    public class ExamineHackPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ExamineHack;

        /// <summary>The entity examined; 0 if the client sent something that is not an id.</summary>
        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    EntityId = pr.ReadULong();
                    break;

                case PythonType.Int:
                    EntityId = (ulong)Math.Max(0, pr.ReadInt());
                    break;

                default:
                    pr.SkipValue();
                    EntityId = 0;
                    break;
            }
        }
    }
}
