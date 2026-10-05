namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// SelectInstance (687), clientmethod.py OnGotoInstance(mapId, startGroup): the row picked in
    /// the instance picker - the instance id and start group that row was listed with
    /// (ChooseInstanceList).
    /// </summary>
    public class SelectInstancePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectInstance;

        public uint InstanceId { get; set; }

        /// <summary>The row's start group; null when the row had none.</summary>
        public int? StartGroup { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();

            var instanceId = pr.ReadNullableInt();

            InstanceId = instanceId.HasValue && instanceId.Value > 0 ? (uint)instanceId.Value : 0;
            StartGroup = pr.ReadNullableInt();
        }
    }
}
