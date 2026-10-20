using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class EquipmentInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        public List<ulong> EquipmentInfo { get; set; }
        public Dictionary<uint, ulong> ActualEquipment = new Dictionary<uint, ulong>();

        /// <summary>
        /// The player's fists, in the HANDTOHAND slot (10000001), which the slot list cannot hold:
        /// only in what their own client is sent (Managers.Unarmed); 0 for none.
        /// </summary>
        public ulong HandToHand { get; set; }

        public EquipmentInfoPacket(List<ulong> equipmentInfo)
        {
            EquipmentInfo = equipmentInfo;
        }

        public override void Write(PythonWriter pw)
        {
            uint count = 0;

            foreach (var entry in EquipmentInfo)
            {
                if (entry != 0)
                {
                    if (ActualEquipment.ContainsKey(count))
                    {
                        Logger.WriteLog(
                            LogType.Error,
                            $"EquipmentInfoPacket duplicate equipment slot {count}: " +
                            $"existing={ActualEquipment[count]}, new={entry}. Overwriting existing value.");
                    }

                    ActualEquipment[count] = entry;
                }

                count++;
            }

            if (HandToHand != 0)
                ActualEquipment[(uint)EquipmentData.HandToHand] = HandToHand;

            pw.WriteTuple(1);
            pw.WriteList(ActualEquipment.Count);

            foreach (var entry in ActualEquipment)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(entry.Key);
                pw.WriteULong(entry.Value);
            }
        }
    }
}
