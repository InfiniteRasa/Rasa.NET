namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class AvailableAllocationPointsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AvailableAllocationPoints;
        
        public int AvailableAttributePoints { get; set; }
        public int TrainPoints { get; set; }        // not used by client??
        public int AvailableSkillPoints { get; set; }

        public AvailableAllocationPointsPacket(int availableAttributePoints, int trainPoints, int availableSkillPoints)
        {
            AvailableAttributePoints = availableAttributePoints;
            TrainPoints = trainPoints;
            AvailableSkillPoints = availableSkillPoints;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);

            pw.WriteInt(AvailableAttributePoints);
            pw.WriteInt(TrainPoints);
            pw.WriteInt(AvailableSkillPoints);

        }
    }
}
