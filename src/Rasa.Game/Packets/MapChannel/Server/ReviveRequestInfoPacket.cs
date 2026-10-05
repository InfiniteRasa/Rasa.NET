using System.Numerics;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// clientmethod.Recv_ReviveRequestInfo(reviveInfoTuple): someone offers to revive the player.
    /// The tuple is shared/reviveinfo.py's Pack: (reviverId, reviverName, timeToStart, timeToEnd,
    /// location), the times in seconds from now. The client's revive window asks "%(reviverName)s
    /// wants to Revive you. Do you accept?" and answers RequestRevive(reviverId) or
    /// RefuseRevive(reviverId), refusing by itself when timeToEnd runs out.
    /// </summary>
    public class ReviveRequestInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ReviveRequestInfo;

        public ulong ReviverId { get; set; }
        public string ReviverName { get; set; }
        public double SecondsToStart { get; set; }
        public double SecondsToEnd { get; set; }
        public Vector3 Location { get; set; }

        public ReviveRequestInfoPacket(ulong reviverId, string reviverName, double secondsToStart, double secondsToEnd, Vector3 location)
        {
            ReviverId = reviverId;
            ReviverName = reviverName ?? "";
            SecondsToStart = secondsToStart;
            SecondsToEnd = secondsToEnd;
            Location = location;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteTuple(5);
            pw.WriteULong(ReviverId);
            pw.WriteUnicodeString(ReviverName);
            pw.WriteDouble(SecondsToStart);
            pw.WriteDouble(SecondsToEnd);
            pw.WriteTuple(3);
            pw.WriteDouble(Location.X);
            pw.WriteDouble(Location.Y);
            pw.WriteDouble(Location.Z);
        }
    }
}
