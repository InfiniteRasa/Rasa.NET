namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class TransferCreditToLockboxPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TransferCreditToLockbox;

        /// <summary>
        /// The amount typed, negated for a withdrawal (inventory.py DepositCreditToLockbox and
        /// WithdrawCreditFromLockbox). A long: the box takes any whole number, and one past an
        /// int's range arrives as a Python long.
        /// </summary>
        public long Ammount { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Ammount = pr.ReadInteger();
        }
    }
}
