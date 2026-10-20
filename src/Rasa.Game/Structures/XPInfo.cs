namespace Rasa.Structures
{
    using System;
    using Memory;
	
    public class XPInfo : IPythonDataStruct
    {
        public uint Total = 0;
        public uint Gained = 0;
        public uint BaseGained = 0;
        /// <summary>
        /// What the experience was multiplied by, each against BaseGained: the squad's share
        /// (KillShares.GroupModOf), the kill streak (KillStreaks) and the Experience Booster
        /// (ExperienceBoost). The client says each one above 1 after the amount, as a percent
        /// (manifestation.py Recv_ExperienceChanged): " (+84% Group Bonus)".
        /// </summary>
        public double GroupMod = 1;
        public double StreakMod = 1;
        public double BoosterMod = 1;
        public bool WasCritKill = false;
        public bool WasTeamCritKill = false;

        public XPInfo()
        {
        }

        public XPInfo(uint total, uint gained, uint baseGained)
        {
            Total = total;
            Gained = gained;
            BaseGained = baseGained;
        }

        public void Read(PythonReader pr)
        {
        }

        /// <summary>
        /// A multiplier as the client is to read it. It prints int(mod * 100) - 100 for the group
        /// and the streak, and int(ceil(mod * 100)) - 100 for the booster, of the single-precision
        /// number it is sent as (PythonWriter.WriteDouble): 2.52 is read as 2.5199999..., which
        /// it would print +151%. So each goes half a percent past the whole percent it stands for,
        /// on the side the client rounds from, and the client prints that percent. One that is no
        /// whole percent above 1 is sent as 1, and the client says nothing of it.
        /// </summary>
        internal static double ForTruncation(double mod)
        {
            var percent = Math.Round(mod * 100);

            return percent <= 100 ? 1 : (percent + 0.5) / 100;
        }

        internal static double ForCeiling(double mod)
        {
            var percent = Math.Round(mod * 100);

            return percent <= 100 ? 1 : (percent - 0.5) / 100;
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(8);
            pw.WriteUInt(Total);
            pw.WriteUInt(Gained);
            pw.WriteUInt(BaseGained);
            pw.WriteDouble(ForTruncation(GroupMod));
            pw.WriteDouble(ForTruncation(StreakMod));
            pw.WriteDouble(ForCeiling(BoosterMod));
            pw.WriteBool(WasCritKill);
            pw.WriteBool(WasTeamCritKill);
        }
    }
}
