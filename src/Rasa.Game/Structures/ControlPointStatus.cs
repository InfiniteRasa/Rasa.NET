namespace Rasa.Structures
{
    using Memory;

    public class ControlPointStatus : IPythonDataStruct
    {
        public uint ControlPointId { get; set; }    // types.IntType, None, False
        public uint OwnerId { get; set; }           // types.LongType, None, True
        public uint StateId { get; set; }           // types.IntType, None, False
        public uint EndTime { get; set; }           // types.IntType, None, False

        public ControlPointStatus()
        {
        }

        public ControlPointStatus(uint controlPointId, uint ownerId, uint stateId, uint endTime)
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
            OwnerId = pr.ReadUInt();
            StateId = pr.ReadUInt();
            EndTime = pr.ReadUInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(ControlPointId);
            pw.WriteUInt(OwnerId);
            pw.WriteUInt(StateId);
            pw.WriteUInt(EndTime);
        }
    }
}
