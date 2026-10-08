using System.Collections.Generic;
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
    /// The player's own looks are carried out too. They are two slots of the appearance that no
    /// item is worn in, the hair and the face, each a class and a colour, and the face's colour
    /// is the skin's (customization.py PreviewSkinColor):
    ///
    ///  - a hair colour (CUSTOMIZE_HUE_HAIR, 1) and a skin colour (CUSTOMIZE_HUE_SKIN, 2) open
    ///    the item's palette on a mannequin, and Ok sends the colour of the pixel clicked. It
    ///    has to be a colour of that palette (<see cref="Customizations.IsOnPalette"/>);
    ///  - a hairstyle (CUSTOMIZE_CHANGE_HAIRSTYLE, 5) and a face (CUSTOMIZE_CHANGE_FACE, 6)
    ///    open a list, and Select sends the item template of the one picked. It has to be one
    ///    the item offers (<see cref="Customizations.Choices"/>) and the player's race may have,
    ///    the rule a new character's are held to, and not the one they have. The colour stays.
    ///
    /// The list is the server's to give: using a customization item from the pack asks for its
    /// choices (GetCustomizationChoices), and the answer is what opens its window, the colour
    /// windows' too (<see cref="Choices"/>).
    ///
    /// The change is kept (character_appearance), everyone who sees the player is told, and one
    /// of the item is used up. A weapon or decoration colour has no item in this client's data,
    /// and is refused.
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

        /// <summary>What kind of item each request is made with; none for a kind that is not carried out.</summary>
        private static readonly Dictionary<uint, CustomizationType> Kinds = new Dictionary<uint, CustomizationType>
        {
            [HueHair] = CustomizationType.HairColor,
            [HueSkin] = CustomizationType.SkinColor,
            [HueClothing] = CustomizationType.ClothingColor,
            [ChangeHairstyle] = CustomizationType.Hairstyle,
            [ChangeFace] = CustomizationType.FaceTexture
        };

        // charactercreationwindow.py kDefaultColorHair and kDefaultColorFace: what a slot that
        // was never given a colour is shown in.
        private static readonly Color DefaultHairColor = new Color(82, 52, 32);

        private static readonly Dictionary<Race, Color> DefaultSkinColor = new Dictionary<Race, Color>
        {
            [Race.Human] = new Color(214, 178, 132),
            [Race.Forean] = new Color(99, 113, 90),
            [Race.Thrax] = new Color(115, 56, 41),
            [Race.Brann] = new Color(82, 125, 181)
        };

        /// <summary>
        /// GetCustomizationChoices((entityId,)): a customization item used from the pack. The
        /// answer opens its window. For a hairstyle or a face it carries what the player may
        /// pick: what the item offers that their race may have. None of them, and the window
        /// says there is nothing to customize - a hybrid's face is not one of these.
        /// </summary>
        public static void Choices(Client client, GetCustomizationChoicesPacket packet)
        {
            var player = client?.Player;

            if (player == null || packet == null)
                return;

            var item = packet.EntityId == 0 ? null : EntityManager.Instance.GetItem(packet.EntityId);

            if (item?.ItemTemplate == null || !player.Inventory.PersonalInventory.Contains(item.EntityId)
                || Customizations.TypeOf((uint)item.ItemTemplate.Class) == null)
            {
                Logger.WriteLog(LogType.Security, $"GetCustomizationChoices: {player.FamilyName} asked about item {packet.EntityId}, which is not a customization in their pack. Not answered.");
                return;
            }

            var choices = Customizations.Choices.TryGetValue((uint)item.ItemTemplate.Class, out var offered)
                ? offered.Where(choice => MayHave(player, choice.TemplateId)).ToList()
                : new List<(uint ClassId, uint TemplateId)>();

            client.CallMethod(SysEntity.ClientMethodId, new CustomizationChoicesPacket(packet.EntityId, choices));
        }

        public static void Request(Client client, RequestCustomizationPacket packet)
        {
            var player = client?.Player;

            if (player == null || packet == null)
                return;

            var argId = (uint)packet.CustomizationActionArgId;

            if (!Kinds.TryGetValue(argId, out var kind))
            {
                Logger.WriteLog(LogType.Debug, $"RequestCustomization: {player.FamilyName} asked for customization {argId}, which no item of this client does. Refused.");
                Refuse(client, argId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            if (player.State == CharacterState.Dead || player.State == CharacterState.Dying)
            {
                Refuse(client, argId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            var item = packet.CustomizationEntityId == 0 ? null : EntityManager.Instance.GetItem(packet.CustomizationEntityId);

            if (item?.ItemTemplate == null || item.StackSize == 0 || !player.Inventory.PersonalInventory.Contains(item.EntityId))
            {
                Logger.WriteLog(LogType.Security, $"RequestCustomization: {player.FamilyName} used item {packet.CustomizationEntityId}, which is not in their pack. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            if (Customizations.TypeOf((uint)item.ItemTemplate.Class) != kind)
            {
                Logger.WriteLog(LogType.Security, $"RequestCustomization: {player.FamilyName} used item {item.EntityId} (class {(uint)item.ItemTemplate.Class}) for customization {argId}, which it does not do. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            switch (kind)
            {
                case CustomizationType.ClothingColor:
                    Paint(client, packet, item, argId);
                    break;
                case CustomizationType.HairColor:
                    Colour(client, packet, item, argId, EquipmentData.Hair);
                    break;
                case CustomizationType.SkinColor:
                    Colour(client, packet, item, argId, EquipmentData.Face);
                    break;
                case CustomizationType.Hairstyle:
                    Change(client, packet, item, argId, EquipmentData.Hair);
                    break;
                case CustomizationType.FaceTexture:
                    Change(client, packet, item, argId, EquipmentData.Face);
                    break;
            }
        }

        /// <summary>A hair colour, or a skin colour, which is the colour of the face.</summary>
        private static void Colour(Client client, RequestCustomizationPacket packet, Item item, uint argId, EquipmentData slot)
        {
            var player = client.Player;
            var itemClass = (uint)item.ItemTemplate.Class;

            // Nothing in the slot is nothing to colour: no character is made without both.
            if (!player.AppearanceData.TryGetValue(slot, out var shown) || shown.Class == 0)
            {
                Refuse(client, argId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            if (!Customizations.IsOnPalette(itemClass, packet.Hue))
            {
                Logger.WriteLog(LogType.Security,
                    $"RequestCustomization: {player.FamilyName} asked for colour {Describe(packet.Hue)} from item {item.EntityId} (class {itemClass}), whose palette does not have it. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            ManifestationManager.Instance.SetAppearance(client, slot, shown.Class, new Color(packet.Hue.Red, packet.Hue.Green, packet.Hue.Blue));
            Done(client, item, argId);
        }

        /// <summary>Another hairstyle, or another face: the class of the slot, its colour kept.</summary>
        private static void Change(Client client, RequestCustomizationPacket packet, Item item, uint argId, EquipmentData slot)
        {
            var player = client.Player;
            var itemClass = (uint)item.ItemTemplate.Class;
            var templateId = packet.SelectedClassTemplateId is int picked && picked > 0 ? (uint)picked : 0;

            if (!Customizations.TryGetChoice(itemClass, templateId, out var classId) || !MayHave(player, templateId))
            {
                Logger.WriteLog(LogType.Security,
                    $"RequestCustomization: {player.FamilyName} picked item template {packet.SelectedClassTemplateId?.ToString() ?? "none"} from item {item.EntityId} (class {itemClass}), which does not offer it to them. Refused.");
                Refuse(client, argId, PlayerMessage.PmActionFailedBadData);
                return;
            }

            player.AppearanceData.TryGetValue(slot, out var shown);

            // The one they have: the window greys it. Nothing would change, and the item would go.
            if (shown != null && shown.Class == classId)
            {
                Refuse(client, argId, PlayerMessage.PmCannotPerformActionNow);
                return;
            }

            var colour = shown?.Color
                ?? (slot == EquipmentData.Hair ? DefaultHairColor : DefaultSkinColor.GetValueOrDefault(player.Race, DefaultSkinColor[Race.Human]));

            ManifestationManager.Instance.SetAppearance(client, slot, classId, new Color(colour.Hue));
            Done(client, item, argId);
        }

        /// <summary>
        /// Whether the player's race may have the hair or the face of this item template: its
        /// race requirement, as CharacterManager.AppearanceFitsRace holds a new character to.
        /// One the server has no template for is not theirs to have.
        /// </summary>
        private static bool MayHave(Manifestation player, uint templateId)
        {
            var template = ItemManager.Instance.GetItemTemplateById(templateId);

            if (template == null)
                return false;

            var raceReq = template.ItemInfo?.RaceReq ?? 0;

            return raceReq == 0 || raceReq == (int)player.Race;
        }

        /// <summary>The change is made: one of the item goes, the action ends, and everyone sees.</summary>
        private static void Done(Client client, Item item, uint argId)
        {
            Use(client, item);

            client.CallMethod(client.Player.EntityId, new PerformRecoveryPacket(PerformType.TwoArgs, ActionId.Customize, argId));
            ManifestationManager.Instance.UpdateAppearance(client);
        }

        /// <summary>One of the item used up, when its class is consumable, as every one of these is. Last, so a failure before it costs nothing.</summary>
        private static void Use(Client client, Item item)
        {
            if ((EntityClassManager.Instance.GetClassInfo(item.ItemTemplate.Class)?.ItemClassInfo?.IsConsumableFlag ?? 0) != 0)
                InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, item, 1);
        }

        /// <summary>An armour paint: the piece to colour and one of the paint's swatches.</summary>
        private static void Paint(Client client, RequestCustomizationPacket packet, Item paint, uint argId)
        {
            var player = client.Player;
            var inventory = player.Inventory;
            var paintClass = (uint)paint.ItemTemplate.Class;
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
            Use(client, paint);

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
