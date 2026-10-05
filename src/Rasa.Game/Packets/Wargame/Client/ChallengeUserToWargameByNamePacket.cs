namespace Rasa.Packets.Wargame.Client
{
    using Communicator.Client;
    using Data;
    using Memory;

    /// <summary>
    /// ChallengeUserToWargameByName (626), client/wargame.py SendWargameChallenge:
    /// <c>(targetName, timeMins, maxKills)</c>. /duel &lt;name&gt; [minutes] [kills]
    /// (communicator.ChallengeWargameDuel) and the radial menu's Invite to Duel, which sends the
    /// name alone; an argument left out or not a number is 0.
    /// </summary>
    public class ChallengeUserToWargameByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengeUserToWargameByName;

        public string TargetName { get; set; } = "";
        public int TimeMins { get; set; }
        public int MaxKills { get; set; }

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();
            TargetName = ChallengeClanToFeudPacket.ReadText(pr);

            if (count > 1)
                TimeMins = ReadNumber(pr);

            if (count > 2)
                MaxKills = ReadNumber(pr);
        }

        private static int ReadNumber(PythonReader pr)
        {
            if (pr.PeekType() == PythonType.Int)
                return pr.ReadInt();

            pr.SkipValue();
            return 0;
        }
    }
}
