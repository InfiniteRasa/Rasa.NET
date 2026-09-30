namespace Rasa.Packets.Crafting.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// RequestModifyItem(kraftwerksId, recipeId, itemId, selectedModuleClassId,
    /// attemptCriticalSuccess): a modification recipe applied to one of an item's modules, from
    /// client/augmentations/kraftwerks.py ModifyItem. Only the old crafting window calls it, and
    /// nothing opens that window; see ClientPacketHandler.RequestDisassembleItem.
    ///
    /// Read leniently - an int, a long, or anything else as null; attemptCriticalSuccess as a flag
    /// whether it comes as a bool or a number - because nothing the retail client does fixes the
    /// types.
    /// </summary>
    public class RequestModifyItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestModifyItem;

        public long? KraftwerksId { get; set; }
        public long? RecipeId { get; set; }
        public long? ItemId { get; set; }
        public long? SelectedModuleClassId { get; set; }
        public long? AttemptCriticalSuccess { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            KraftwerksId = ReadNumber(pr);
            RecipeId = ReadNumber(pr);
            ItemId = ReadNumber(pr);
            SelectedModuleClassId = ReadNumber(pr);

            // True / False / None arrive as structs, 0 and 1 as ints.
            if (pr.PeekType() == PythonType.Structs)
                AttemptCriticalSuccess = pr.ReadBool() ? 1 : 0;
            else
                AttemptCriticalSuccess = ReadNumber(pr);
        }

        private static long? ReadNumber(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.Int:
                    return pr.ReadInt();

                case PythonType.Long:
                    return pr.ReadLong();

                default:
                    pr.SkipValue();
                    return null;
            }
        }
    }
}
