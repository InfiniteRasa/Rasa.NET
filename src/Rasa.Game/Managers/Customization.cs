using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Customization items: RequestCustomization, the request of the client's CUSTOMIZE action
    /// (82), whose arg says what is being changed (generated.client.actiondata CUSTOMIZE_*).
    ///
    /// Armour paint (CUSTOMIZE_HUE_CLOTHING, 4) is carried out. Right-clicking a paint opens the
    /// colour window (colorcustomizationwindow.py) on the pieces the player is wearing that the
    /// paint may colour, with the paint's 25 swatches; Accept sends the paint, the piece and the
    /// swatch's colour. The piece takes the colour and keeps it (items.color), the player's
    /// appearance follows for everyone who sees them, and one paint is used up when its class is
    /// consumable, as every paint's is.
    ///
    /// What the client checks before it sends (CustomizeAction.CheckAction) is checked again:
    /// the player is alive, the item is a customization of the right kind, and it may be used on
    /// the piece's class (<see cref="Customizations.MayApply"/>). And what the client only
    /// offers: the paint is in the pack, the piece is the player's own and worn or in the pack,
    /// and the colour is one of the paint's swatches.
    ///
    /// The other kinds - hair and skin colour, hairstyle and face - are refused: the request is
    /// read and answered, and nothing changes. A weapon or decoration colour has no item in
    /// this client's data.
    ///
    /// An action is answered either way: a recovery to finish it, or UserActionFailed and
    /// ActionFailed to cancel it (ActorManager.RefuseRequest). Unanswered, it stays the
    /// client's current action.
    /// </summary>
    public static class Customization
    {
        // generated.client.actiondata CUSTOMIZE_*, the arg ids of action 82.
        public const uint HueHair = 1;
        public const uint HueSkin = 2;
        public const uint HueWeapon = 3;
        public const uint HueClothing = 4;
        public const uint ChangeHairstyle = 5;
        public const uint ChangeFace = 6;
        public const uint HueDecoration = 10;

        public static void Request(Client client, RequestCustomizationPacket packet)
        {
            var player = client?.Player;

            if (player == null || packet == null)
                return;

            var argId = (uint)packet.CustomizationActionArgId;

            if (argId != HueClothing)
            {
                Logger.WriteLog(LogType.Debug, $"RequestCustomization: {player.FamilyName} asked for customization {argId}, which is not implemented. Refused.");
                Refuse(client, argId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
            {
                Refuse(client, argId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            var inventory = player.Inventory;
            var paint = packet.CustomizationEntityId == 0 ? null : EntityManager.Instance.GetItem(packet.CustomizationEntityId);

            if (paint?.ItemTemplate == null || paint.StackSize == 0 || !inventory.PersonalInventory.Contains(paint.EntityId))
            {
                Logger.WriteLog(LogType.Security, $"RequestCustomization: {player.FamilyName} used item {packet.CustomizationEntityId}, which is not in their pack. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            var paintClass = (uint)paint.ItemTemplate.Class;

            if (Customizations.TypeOf(paintClass) != CustomizationType.ClothingColor)
            {
                Logger.WriteLog(LogType.Security, $"RequestCustomization: {player.FamilyName} used item {paint.EntityId} (class {paintClass}) as an armour paint, which it is not. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            var piece = packet.TargetEntityId == 0 ? null : EntityManager.Instance.GetItem(packet.TargetEntityId);
            var worn = piece != null && inventory.EquippedInventory.Contains(piece.EntityId);

            if (piece?.ItemTemplate == null || !(worn || inventory.PersonalInventory.Contains(piece.EntityId)))
            {
                Logger.WriteLog(LogType.Security, $"RequestCustomization: {player.FamilyName} asked to colour item {packet.TargetEntityId}, which they are not wearing or carrying. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            if (!Customizations.MayApply(paintClass, (uint)piece.ItemTemplate.Class))
            {
                Refuse(client, argId, PlayerMessage.PmCannotUseCustomizationOnItem);
                return;
            }

            if (!Customizations.TryMatchHue(paintClass, packet.Hue, out var hue))
            {
                Logger.WriteLog(LogType.Security,
                    $"RequestCustomization: {player.FamilyName} asked for colour {Describe(packet.Hue)} from item {paint.EntityId} (class {paintClass}), which does not offer it. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            piece.Color = hue;
            ItemManager.Instance.SaveColor(piece);

            // The paint goes once the colour is on, so a failure before here costs nothing.
            if ((EntityClassManager.Instance.GetClassInfo(paint.ItemTemplate.Class)?.ItemClassInfo?.IsConsumableFlag ?? 0) != 0)
                InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, paint, 1);

            client.CallMethod(player.EntityId, new PerformRecoveryPacket(PerformType.TwoArgs, ActionId.Customize, argId));

            // A piece in the pack shows its colour when it is put on (SetAppearanceItem reads it).
            if (worn)
            {
                ManifestationManager.Instance.SetAppearanceItem(client, piece);
                ManifestationManager.Instance.UpdateAppearance(client);
            }
        }

        private static void Refuse(Client client, uint argId, PlayerMessage message)
        {
            ActorManager.RefuseRequest(client, ActionId.Customize, argId, message);
        }

        private static string Describe(Color hue) =>
            hue == null ? "none" : $"({hue.Red}, {hue.Green}, {hue.Blue}, {hue.Alpha})";
    }
}
