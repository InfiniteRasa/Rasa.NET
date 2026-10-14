using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    /// <summary>
    /// A player's bare hands: Weapon_Avatar_Unarmed (entity class 6082, item template 154), the
    /// item the client keeps in the HANDTOHAND equipment slot (10000001) and fights with when the
    /// armed weapon drawer slot is empty, or holds a weapon it will not let the player hold - a
    /// broken one (Manifestation.GetWeapon: "if weapon is None or not weapon.CanActorEquip(self):
    /// return self.GetEquippedItem(HANDTOHAND)").
    ///
    /// Nothing gave the client that item, so with an empty hand GetWeapon found nothing and the
    /// attack keys did nothing; and the server, finding no weapon in the drawer, fired nothing
    /// either. Now every player has one: an Item nothing stores, made the first time it is needed,
    /// created on the player's own client as they arrive on a map (Show), and named in the
    /// HANDTOHAND slot of every EquipmentInfo their own client is sent. Only theirs: the client
    /// looks for it on the player it controls, and another client would only log that it was
    /// told about equipment it does not have.
    ///
    /// The swing is the weapon class's: WEAPON_MELEE 174/1 (meleeattackmovement), 30 Physical
    /// damage, drawn and stowed with 8/8, no ammunition. The template has no itemtemplate_weapon
    /// row, so its weapon data is made here from the action's own row (action_level 174/1: no
    /// windup, 500 ms recovery, 3 m), as a polymorphed player's creature weapon is. It is a melee
    /// attack - no bead, no to-hit roll, no cover - under Hand to Hand (the template's skill),
    /// whose damage bonus and knockback it takes. It does not heat, wear or break.
    /// </summary>
    public static class Unarmed
    {
        public const uint TemplateId = 154;
        public const EntityClasses ClassId = (EntityClasses)6082;   // Weapon_Avatar_Unarmed

        /// <summary>action_level 174/1, for when the action tables are not loaded.</summary>
        public const uint DefaultSwingMs = 500;
        public const uint DefaultRange = 3;

        /// <summary>The player's fists, made the first time they are asked for.</summary>
        public static UnarmedItem For(Manifestation player)
        {
            if (player == null)
                return null;

            return player.Fists ??= Make();
        }

        /// <summary>
        /// What the player fights with outside a polymorph: the weapon in the armed drawer slot,
        /// or their fists when there is none or it is broken - as their client decides it.
        /// </summary>
        public static Item Wielded(Client client)
        {
            var player = client?.Player;

            if (player == null)
                return null;

            var weapon = InventoryManager.Instance.CurrentWeapon(client);

            if (weapon != null && !Durability.IsBroken(weapon))
                return weapon;

            return For(player);
        }

        /// <summary>Whether this is someone's fists.</summary>
        public static bool Is(Item item) => item is UnarmedItem;

        /// <summary>
        /// The player's fists on their own client, as any weapon is sent: the entity, then its
        /// item and weapon data (ItemManager.SendItemDataToClient). Before the player's own
        /// manifestation, whose EquipmentInfo names it: an entity the client does not have is
        /// dropped from it ("Told about equipment we don't have").
        /// </summary>
        public static void Show(Client client)
        {
            var fists = For(client?.Player);

            if (fists == null || !EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(ClassId, out var classInfo)
                || classInfo.ItemClassInfo == null || classInfo.WeaponClassInfo == null)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(fists.EntityId, ClassId));
            client.CallMethod(fists.EntityId, new ItemInfoPacket(fists, classInfo));
            client.CallMethod(fists.EntityId, new SetConsumablePacket(classInfo.ItemClassInfo.IsConsumableFlag));
            client.CallMethod(fists.EntityId, new WeaponInfoPacket(fists, classInfo));
            client.CallMethod(fists.EntityId, new WeaponAmmoInfoPacket(fists.CurrentAmmo));
            client.CallMethod(fists.EntityId, new SetStackCountPacket(fists.StackSize));
        }

        /// <summary>The player's equipment as their own client is sent it: what they wear and hold, and their fists.</summary>
        public static EquipmentInfoPacket EquipmentFor(Manifestation player)
        {
            return new EquipmentInfoPacket(player.Inventory.EquippedInventory) { HandToHand = For(player)?.EntityId ?? 0 };
        }

        private static UnarmedItem Make()
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = TemplateId, ItemClass = (uint)ClassId });

            // What the template row says - its quality, its Hand to Hand 1 - where it is loaded.
            if (EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(ClassId, out var entityClass)
                && entityClass.ItemTemplates.TryGetValue(TemplateId, out var loaded))
            {
                template.ItemInfo = loaded.ItemInfo;
                template.EquipableInfo = loaded.EquipableInfo;
                template.QualityId = loaded.QualityId;
                template.InventoryCategory = loaded.InventoryCategory;
                template.HasSellableFlag = loaded.HasSellableFlag;
            }

            template.EquipableInfo ??= new EquipableInfo(WeaponSkills.HandToHand, 1);

            var swingMs = DefaultSwingMs;
            var range = DefaultRange;

            if (AbilityManager.Instance.TryGetLevel(ActionId.WeaponMelee, 1, out var level))
            {
                swingMs = (uint)Math.Max(0, ManifestationManager.SwingCycleMs(level));
                range = level.MaxRange > 0 ? (uint)level.MaxRange : DefaultRange;
            }

            template.WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
            {
                AimRate = 1,
                Refire = Math.Max(1, swingMs),
                Windup = 0,
                Recovery = 0,
                Range = range,
                AttackType = (uint)AttackType.Melee
            });

            return new UnarmedItem
            {
                ItemTemplate = template,
                ItemTemplateId = TemplateId,
                StackSize = 1,
                CurrentHitPoints = 1,
                Crafter = ""
            };
        }
    }

    /// <summary>A player's fists (Unarmed); an Item nothing stores.</summary>
    public class UnarmedItem : Item
    {
    }
}
