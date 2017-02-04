namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AttributeInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AttributeInfo;
        
        public ActorStats ActorStats { get; set; }
        
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(10);
            // for all attributes the following values are send:
            // current, currentMax, normalMax, RefreshAmount, refreshPeriod
            // Body
            pw.WriteInt(1);
            pw.WriteTuple(5);         
            pw.WriteInt(ActorStats.Body.Current);
            pw.WriteInt(ActorStats.Body.CurrentMax);
            pw.WriteInt(ActorStats.Body.NormalMax);
            pw.WriteInt(ActorStats.Body.RefreshAmount);
            pw.WriteInt(ActorStats.Body.RefreshPeriod);
            // Mind
            pw.WriteInt(2);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Mind.Current);
            pw.WriteInt(ActorStats.Mind.CurrentMax);
            pw.WriteInt(ActorStats.Mind.NormalMax);
            pw.WriteInt(ActorStats.Mind.RefreshAmount);
            pw.WriteInt(ActorStats.Mind.RefreshPeriod);
            // Spirit
            pw.WriteInt(3);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Spirit.Current);
            pw.WriteInt(ActorStats.Spirit.CurrentMax);
            pw.WriteInt(ActorStats.Spirit.NormalMax);
            pw.WriteInt(ActorStats.Spirit.RefreshAmount);
            pw.WriteInt(ActorStats.Spirit.RefreshPeriod);
            // Health
            pw.WriteInt(4);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Health.Current);
            pw.WriteInt(ActorStats.Health.CurrentMax);
            pw.WriteInt(ActorStats.Health.NormalMax);
            pw.WriteInt(ActorStats.Health.RefreshAmount);
            pw.WriteInt(ActorStats.Health.RefreshPeriod);
            // Chi - Chi is adrenaline?
            pw.WriteInt(5);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Chi.Current);
            pw.WriteInt(ActorStats.Chi.CurrentMax);
            pw.WriteInt(ActorStats.Chi.NormalMax);
            pw.WriteInt(ActorStats.Chi.RefreshAmount);
            pw.WriteInt(ActorStats.Chi.RefreshPeriod);
            // Power
            pw.WriteInt(6);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Power.Current);
            pw.WriteInt(ActorStats.Power.CurrentMax);
            pw.WriteInt(ActorStats.Power.NormalMax);
            pw.WriteInt(ActorStats.Power.RefreshAmount);
            pw.WriteInt(ActorStats.Power.RefreshPeriod);
            // Aware
            pw.WriteInt(7);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Aware.Current);
            pw.WriteInt(ActorStats.Aware.CurrentMax);
            pw.WriteInt(ActorStats.Aware.NormalMax);
            pw.WriteInt(ActorStats.Aware.RefreshAmount);
            pw.WriteInt(ActorStats.Aware.RefreshPeriod);
            // Armor
            pw.WriteInt(8);
            pw.WriteTuple(5);
            pw.WriteDouble(ActorStats.Armor.Current);
            pw.WriteDouble(ActorStats.Armor.CurrentMax);
            pw.WriteDouble(ActorStats.Armor.NormalMax);
            pw.WriteDouble(ActorStats.Armor.RefreshAmount);
            pw.WriteDouble(ActorStats.Armor.RefreshPeriod);
            // Speed
            pw.WriteInt(9);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Speed.Current);
            pw.WriteInt(ActorStats.Speed.CurrentMax);
            pw.WriteInt(ActorStats.Speed.NormalMax);
            pw.WriteInt(ActorStats.Speed.RefreshAmount);
            pw.WriteInt(ActorStats.Speed.RefreshPeriod);
            // Regen
            pw.WriteInt(10);
            pw.WriteTuple(5);
            pw.WriteInt(ActorStats.Regen.Current);
            pw.WriteInt(ActorStats.Regen.CurrentMax);
            pw.WriteInt(ActorStats.Regen.NormalMax);
            pw.WriteInt(ActorStats.Regen.RefreshAmount);
            pw.WriteInt(ActorStats.Regen.RefreshPeriod);
        }
    }
}
