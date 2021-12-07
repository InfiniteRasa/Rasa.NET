namespace Rasa.Structures
{
    using Data;
    using Memory;
    public class MapInstanceInfo : IPythonDataStruct
    {
        internal uint MapInstanceId { get; set; }
        internal uint MapContextId { get; set; }
        internal MapInstanceStatus MapInstanceStatus { get; set; }

        public MapInstanceInfo(uint mapInstanceId, uint mapContextId, MapInstanceStatus mapInstanceStatus)
        {
            MapInstanceId = mapInstanceId;
            MapContextId = mapContextId;
            MapInstanceStatus = mapInstanceStatus;
        }

        public void Read(PythonReader pr)
        {

        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(MapInstanceId);
            pw.WriteUInt(MapContextId);
            pw.WriteUInt((uint)MapInstanceStatus);
        }
    }
}
