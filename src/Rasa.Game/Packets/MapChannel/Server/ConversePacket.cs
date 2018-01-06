namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ConversePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Converse;

        //public List<ConvoDataDictTupple> ConvoDataDict { get; set; }

        // ToDo
        public ConversePacket(/*List<ConvoDataDictTupple> convoDataDict*/)
        {
            //ConvoDataDict = convoDataDict;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(1);  
                pw.WriteInt(2);         // key
                pw.WriteDictionary(1);  // mission list
                    pw.WriteInt(429);   // missionID
                    pw.WriteTuple(6);  // mission info
                        pw.WriteInt(1);  // level
                        pw.WriteTuple(2);       // rewardInfo
                            // fixed redward tuple
                            pw.WriteTuple(2);
                                // credits list 
                                pw.WriteList(2);    // 2 types of credit
                                    pw.WriteTuple(2);
                                        pw.WriteInt((int)Curency.Credits);
                                        pw.WriteInt(100);   // credit ammount
                                    pw.WriteTuple(2);
                                        pw.WriteInt((int)Curency.Prestige);
                                        pw.WriteInt(100);   // prestige ammount
                                // fixed reward items
                                pw.WriteList(0);
                            // selection list (selectable reward)
                            pw.WriteList(0);
                        pw.WriteNoneStruct();   // offerVOAudioSetId (NoneStruct for no-audio)
                        pw.WriteList(0);    // itemsRequired
                        pw.WriteList(0);    // objectives
                                //pw.WriteInt(261);   // objectivId
                        pw.WriteInt(1);     // groupType
        }
    }
}
