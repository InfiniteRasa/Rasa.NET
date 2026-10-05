using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Actor.Recv_PlayerDead(sourceId, graveyardList, canRevive), to the player who died, on their
    /// own manifestation. graveyardList is shared/graveyardinfo.py's GraveyardInfo_ToDict for each
    /// hospital they may go back to - {'Id', 'pos', 'isSafe', 'name'} - which the client's
    /// waypoint window offers ("Select a hospital to respawn from.", the name from
    /// graveyardlanguage by Id, Map_SafeZone or Map_Hospital by isSafe), choosing the nearest by
    /// itself after MAX_REVIVE_ACCEPT_TIME. An empty list shows the death window instead ("You
    /// are too injured to continue ... click below to be taken to the hospital"), whose Revive
    /// button canRevive enables.
    /// </summary>
    public class PlayerDeadPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerDead;

        public sealed class Graveyard
        {
            public uint Id { get; set; }
            public Vector3 Position { get; set; }
            public bool IsSafe { get; set; }
        }

        public ulong SourceId { get; set; }
        public List<Graveyard> Graveyards { get; set; } = new List<Graveyard>();
        public bool CanRevive { get; set; }

        public PlayerDeadPacket(ulong sourceId, List<Graveyard> graveyards, bool canRevive)
        {
            SourceId = sourceId;
            Graveyards = graveyards ?? new List<Graveyard>();
            CanRevive = canRevive;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);

            if (SourceId == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteULong(SourceId);

            pw.WriteList(Graveyards.Count);

            foreach (var graveyard in Graveyards)
            {
                pw.WriteDictionary(4);
                pw.WriteString("Id");
                pw.WriteUInt(graveyard.Id);
                pw.WriteString("pos");
                pw.WriteTuple(3);
                pw.WriteDouble(graveyard.Position.X);
                pw.WriteDouble(graveyard.Position.Y);
                pw.WriteDouble(graveyard.Position.Z);
                pw.WriteString("isSafe");
                pw.WriteBool(graveyard.IsSafe);
                // The client names it from graveyardlanguage itself.
                pw.WriteString("name");
                pw.WriteUnicodeString("");
            }

            pw.WriteInt(CanRevive ? 1 : 0);
        }
    }
}
