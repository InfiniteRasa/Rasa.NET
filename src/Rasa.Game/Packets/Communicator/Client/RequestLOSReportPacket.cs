namespace Rasa.Packets.Communicator.Client
{
    using System;

    using Data;
    using Memory;

    /// <summary>
    /// The server's half of a line of sight check: <c>(targetId,)</c>, sent by
    /// client/communicator.py's ReportLOS after it prints its own. DoEntityLOS can send None for
    /// an id it could not parse; that reads as 0.
    /// </summary>
    public class RequestLOSReportPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestLOSReport;

        public ulong TargetId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    TargetId = pr.ReadULong();
                    break;

                case PythonType.Int:
                    TargetId = (ulong)Math.Max(0, pr.ReadInt());
                    break;

                default:
                    pr.SkipValue();
                    TargetId = 0;
                    break;
            }
        }
    }
}
