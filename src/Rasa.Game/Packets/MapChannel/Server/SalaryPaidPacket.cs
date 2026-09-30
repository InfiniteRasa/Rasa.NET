namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// SalaryPaid (359), on SysEntity.ClientMethodId: <c>(salaryAmount,)</c>. Wired so that it can be
    /// sent, and deliberately sent by nothing: there is no salary to pay.
    ///
    /// client/clientmethod.py Recv_SalaryPaid posts PM_SALARY_AMOUNT (324), "You have been
    /// credited with %(amount)s energy units as part of your regular salary", to the Loot Obtained
    /// chat filter, and that is all: no status indicator, no sound, and no change to the balance -
    /// a sender has to send UpdateCredits as well.
    ///
    /// "Energy units" is the currency's pre-release name, left in six strings: this message, its
    /// sibling 323 "... for sales through your vendor" (the cut player vendors) and an "Energy
    /// Units:" label; released text says credits. Nothing else in the client mentions a salary -
    /// no pay period, window, tooltip or table of amounts - nor does tabula_rasa.exe, and the C++
    /// server only listed the id. An early periodic pay, cut before release, with its message left
    /// behind; how much, how often and to whom did not survive.
    /// </summary>
    public class SalaryPaidPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SalaryPaid;

        public int SalaryAmount { get; }

        public SalaryPaidPacket(int salaryAmount)
        {
            SalaryAmount = salaryAmount;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(SalaryAmount);
        }
    }
}
