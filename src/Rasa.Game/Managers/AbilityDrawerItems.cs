namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The item behind an ability tray slot. The client's drawer entry is (abilityId, level,
    /// itemId): dragging a usable item from the inventory onto the tray (abilitydrawerwindow.py,
    /// the item's GetItemActionInfo) puts the item's action there with the item's entity id, and
    /// firing the slot performs the action with that item (Manifestation.PerformTargetingAbility),
    /// so the server knows which pet or which stack it is.
    ///
    /// The server keeps the item by item.id, since entity ids are handed out anew each login, and
    /// finds its entity when the drawer is sent. An item no longer in the pack - used up, sold,
    /// traded - is stood in for by another in the pack that performs the same action at the same
    /// level (the next medpack stack); with none, the slot goes out with no item.
    ///
    /// The drawer is sent at login and when the tray is edited, not when the pack changes, so the
    /// id the client holds can be one that has since gone, or none where there is an item now.
    /// The client does not judge the slot by it - where the action needs an item it counts the
    /// pack by item class (BaseActorAbility.CheckConsumables) - and fires it regardless;
    /// AbilityManager.RequestPerformAbility makes the same substitution then.
    /// </summary>
    public static class AbilityDrawerItems
    {
        public static ulong? EntityOf(Manifestation player, AbilityDrawerData slot)
        {
            if (player?.Inventory == null || slot?.ItemId == null)
                return null;

            Item standIn = null;

            foreach (var entityId in player.Inventory.PersonalInventory)
            {
                if (entityId == 0)
                    continue;

                var item = EntityManager.Instance.GetItem(entityId);

                if (item == null || item.StackSize == 0 || item.OwnerId != player.Id)
                    continue;

                if (item.Id == slot.ItemId.Value)
                    return item.EntityId;

                if (standIn == null && AbilityManager.Instance.TryGetItemAction(item.ItemTemplateId, out var actionId, out var level)
                    && (int)actionId == slot.AbilityId && level == slot.AbilityLevel)
                    standIn = item;
            }

            return standIn?.EntityId;
        }
    }
}
