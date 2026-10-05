namespace Rasa.Structures
{
    using Memory;

    /// <summary>
    /// The client's ControlPointStatus struct (shared/controlpointdefs.py), one entry of
    /// Recv_ControlPointStatus's list: (controlPointId, ownerId, stateId, endTime). The client
    /// checks each field's type when it unpacks one (stuple Wrap, Field.Validate): the id, the
    /// state and the end time are ints and may not be None; the owner is a long, or None.
    /// </summary>
    public class ControlPointStatus : IPythonDataStruct
    {
        /// <summary>kCPState_New .. kCPState_PostWar, shared/controlpointstatus.py.</summary>
        public const uint StateNew = 0;
        public const uint StatePreWar = 1;
        public const uint StateWar = 2;
        public const uint StatePostWar = 3;

        /// <summary>The client's id for the point (generated.client.controlpointdata).</summary>
        public uint ControlPointId { get; set; }    // types.IntType, None, False

        /// <summary>Who holds it; null for nobody.</summary>
        public ulong? OwnerId { get; set; }         // types.LongType, None, True
        public uint StateId { get; set; }           // types.IntType, None, False
        public uint EndTime { get; set; }           // types.IntType, None, False

        public ControlPointStatus()
        {
        }

        public ControlPointStatus(uint controlPointId, ulong? ownerId, uint stateId, uint endTime)
        {
            ControlPointId = controlPointId;
            OwnerId = ownerId;
            StateId = stateId;
            EndTime = endTime;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ControlPointId = pr.ReadUInt();

            if (pr.PeekType() == PythonType.Structs)
            {
                pr.ReadNoneStruct();
                OwnerId = null;
            }
            else
                OwnerId = pr.ReadULong();

            StateId = pr.ReadUInt();
            EndTime = pr.ReadUInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(ControlPointId);

            if (OwnerId.HasValue)
                pw.WriteULong(OwnerId.Value);
            else
                pw.WriteNoneStruct();

            pw.WriteUInt(StateId);
            pw.WriteUInt(EndTime);
        }
    }
}
