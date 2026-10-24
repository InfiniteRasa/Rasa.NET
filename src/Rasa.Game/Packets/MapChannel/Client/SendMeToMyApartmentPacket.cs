namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// SendMeToMyApartment (180): the retail client's ask to be taken to the player's own
    /// apartment. Nothing in the 1.16.5 client's Python is known to send it - the housing it
    /// belonged to was cut - but the opcode is listed, and a client that does send it is
    /// answered as .apartment is (Managers.Apartments.GoHome). Whatever arguments it carries are
    /// read past: there is nothing in them to act on.
    /// </summary>
    public class SendMeToMyApartmentPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SendMeToMyApartment;

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();

            for (var i = 0; i < count; i++)
                pr.SkipValue();
        }
    }
}
