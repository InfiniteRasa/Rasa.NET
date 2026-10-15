using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Rasa.Loot;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Api.Ingame
{
    using Data;
    using Game;
    using Managers;
    using Structures.World;

    /// <summary>
    /// POST /ingame/items/update. Updates existing template/class rows transactionally,
    /// then publishes the corresponding properties to cached item templates/entity classes.
    /// A snapshot of the GET response is mandatory as an optimistic concurrency guard.
    /// </summary>
    public sealed class IngameItemUpdateEndpoint : IngameItemEndpointBase
    {
        private static readonly object Editing = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public IngameItemUpdateEndpoint(IngameSessionService sessions) : base(sessions) { }
        public override string Name => "ingame/items/update";
        public override string Method => "POST";

        protected override ApiResponse HandleAuthenticated(ApiRequest request, Client client)
        {
            if (!request.Headers.TryGetValue("Content-Type", out var contentType)
                || !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return ApiResponse.Error(415, "content type must be application/json");

            try
            {
                using var document = JsonDocument.Parse(request.Body ?? "");
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("item", out var itemJson)
                    || itemJson.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("expected", out var expected)
                    || expected.ValueKind != JsonValueKind.Object)
                    return ApiResponse.Error(400, "item and expected GET snapshot objects are required");

                // Refuse partial payloads instead of deserializing missing fields to zero/false.
                foreach (var field in new[] { "templateId", "itemClassId", "name", "category", "stackSize",
                    "qualityId", "inventoryIconStringId", "lootValue", "maxHitPoints", "isConsumable",
                    "buyPrice", "sellPrice", "armorValue", "boundToCharacter", "bindOnEquip", "sellable",
                    "characterUnique", "accountUnique", "notTradable", "notPlaceableInLockbox",
                    "equipmentSlot", "requiredSkillId", "requiredSkillLevel", "requirements", "weapon" })
                    if (!itemJson.TryGetProperty(field, out _))
                        return ApiResponse.Error(400, $"item.{field} must be explicitly provided");

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in itemJson.EnumerateObject())
                    if (!seen.Add(field.Name))
                        return ApiResponse.Error(400, $"item.{field.Name} is duplicated");

                var input = itemJson.Deserialize<IngameItemDetails>(JsonOptions);
                if (input == null || input.TemplateId == 0)
                    return ApiResponse.Error(400, "a valid template ID is required");
                var allowShared = root.TryGetProperty("allowSharedClassChanges", out var shared)
                    && shared.ValueKind == JsonValueKind.True;

                lock (Editing)
                {
                    if (!TryItem(input.TemplateId, out var template, out var entityClass))
                        return ApiResponse.Error(404, "item template is not loaded");
                    var original = ToDetails(template, entityClass);
                    using var now = JsonDocument.Parse(ApiJson.Serialize(original));
                    if (!JsonElement.DeepEquals(expected, now.RootElement))
                        return ApiResponse.Error(409, "item has changed since it was loaded; reload before saving");
                    if (input.ItemClassId != original.ItemClassId || input.Name != original.Name)
                        return ApiResponse.Error(400, "class identity and class name cannot be changed in the item editor");
                    if (!Enum.TryParse<InventoryCategory>(input.Category, true, out var category)
                        || !Enum.IsDefined(category) || (int)category > byte.MaxValue)
                        return ApiResponse.Error(400, "invalid inventory category");
                    if (input.QualityId < 0 || input.QualityId > byte.MaxValue || input.StackSize == 0)
                        return ApiResponse.Error(400, "quality must be 0..255 and stack size must be positive");
                    if (input.EquipmentSlot != original.EquipmentSlot)
                        return ApiResponse.Error(400, "equipment slot is a shared class property and is not editable yet");
                    if (input.Requirements == null || original.Requirements.Count != input.Requirements.Count
                        || original.Requirements.Any(pair => !input.Requirements.TryGetValue(pair.Key, out var value) || value != pair.Value))
                        return ApiResponse.Error(400, "generic class requirements cannot be edited through this endpoint yet");
                    if ((input.Weapon == null) != (original.Weapon == null))
                        return ApiResponse.Error(400, "weapon augmentation cannot be created or removed");
                    if (input.RequiredSkillLevel is < 0 or > 255)
                        return ApiResponse.Error(400, "skill requirement level must be 0..255");
                    if (input.Weapon != null && input.Weapon.MinDamage is int min && input.Weapon.MaxDamage is int max && min > max)
                        return ApiResponse.Error(400, "minimum damage cannot exceed maximum damage");

                    if (template.WeaponInfo == null && input.Weapon != null &&
                        (input.Weapon.AmmoPerShot != null || input.Weapon.Range != null ||
                         input.Weapon.Windup != null || input.Weapon.Recovery != null ||
                         input.Weapon.Refire != null || input.Weapon.ReloadTime != null))
                        return ApiResponse.Error(400, "item has no template-level weapon row");

                    var classChanged = input.StackSize != original.StackSize
                        || input.InventoryIconStringId != original.InventoryIconStringId
                        || input.LootValue != original.LootValue || input.MaxHitPoints != original.MaxHitPoints
                        || input.IsConsumable != original.IsConsumable
                        || (input.Weapon != null && original.Weapon != null && (
                            input.Weapon.MinDamage != original.Weapon.MinDamage || input.Weapon.MaxDamage != original.Weapon.MaxDamage
                            || input.Weapon.DamageType != original.Weapon.DamageType || input.Weapon.AmmoClassId != original.Weapon.AmmoClassId
                            || input.Weapon.ClipSize != original.Weapon.ClipSize));
                    if (classChanged && !allowShared)
                        return ApiResponse.Error(409, "item class fields affect every template using the class; explicitly allow shared class changes");

                    var updatedTemplate = new ItemTemplateEntry
                    {
                        Id = input.TemplateId, QualityId = (byte)input.QualityId,
                        HasSellableFlag = Bool(input.Sellable), NotTradableFlag = Bool(input.NotTradable),
                        HasCharacterUniqueFlag = Bool(input.CharacterUnique),
                        HasAccountUniqueFlag = Bool(input.AccountUnique), HasBoEFlag = Bool(input.BindOnEquip),
                        BoundToCharacterFlag = Bool(input.BoundToCharacter),
                        NotPlacableInLockboxFlag = Bool(input.NotPlaceableInLockbox),
                        InventoryCategory = (byte)category, BuyPrice = input.BuyPrice, SellPrice = input.SellPrice
                    };
                    var updatedClass = classChanged ? new ItemClassEntry
                    {
                        Id = input.ItemClassId, StackSize = input.StackSize,
                        InventoryIconStringId = input.InventoryIconStringId, LootValue = input.LootValue,
                        MaxHitPoints = input.MaxHitPoints, IsConsumableFlag = Bool(input.IsConsumable)
                    } : null;
                    var armor = input.ArmorValue == original.ArmorValue ? null :
                        new ItemTemplateArmorEntry { Id = input.TemplateId, ArmorValue = input.ArmorValue };
                    var weapon = input.Weapon == null || template.WeaponInfo == null ? null : new ItemTemplateWeaponEntry
                    {
                        Id = input.TemplateId, AmmoPerShot = input.Weapon.AmmoPerShot ?? 0,
                        Range = input.Weapon.Range ?? 0, Windup = input.Weapon.Windup ?? 0,
                        Recovery = input.Weapon.Recovery ?? 0, Refire = input.Weapon.Refire ?? 0,
                        ReloadTime = input.Weapon.ReloadTime ?? 0
                    };
                    var weaponClass = input.Weapon == null || !classChanged || entityClass.WeaponClassInfo == null ? null :
                        new WeaponClassEntry
                        {
                            Id = input.ItemClassId,
                            MinDamage = input.Weapon.MinDamage ?? 0,
                            MaxDamage = input.Weapon.MaxDamage ?? 0,
                            DamageType = input.Weapon.DamageType ?? 0,
                            AmmoClassId = input.Weapon.AmmoClassId ?? 0,
                            ClipSize = input.Weapon.ClipSize ?? 0
                        };
                    if (input.Weapon != null && template.WeaponInfo != null &&
                        (input.Weapon.AmmoPerShot == null || input.Weapon.Range == null ||
                         input.Weapon.Windup == null || input.Weapon.Recovery == null ||
                         input.Weapon.Refire == null || input.Weapon.ReloadTime == null))
                        return ApiResponse.Error(400, "weapon template fields must all be present");
                    if (input.Weapon != null && entityClass.WeaponClassInfo != null &&
                        (input.Weapon.MinDamage == null || input.Weapon.MaxDamage == null || input.Weapon.DamageType == null ||
                         input.Weapon.AmmoClassId == null || input.Weapon.ClipSize == null))
                        return ApiResponse.Error(400, "weapon class fields must all be present");
                    var skillChanged = input.RequiredSkillId != original.RequiredSkillId
                        || input.RequiredSkillLevel != original.RequiredSkillLevel;
                    if (skillChanged && (input.RequiredSkillId == null || input.RequiredSkillLevel == null))
                        return ApiResponse.Error(400, "cannot remove an existing skill requirement or create an incomplete one");
                    var skill = skillChanged ? new ItemTemplateRequirementSkillEntry
                    {
                        Id = input.TemplateId, SkillId = input.RequiredSkillId.Value,
                        SkillLevel = (byte)input.RequiredSkillLevel.Value
                    } : null;
                    try
                    {
                        using var world = Server.GameUnitOfWorkFactory.CreateWorld();
                        using var transaction = world.BeginTransaction();
                        world.Equipment.UpdateItemEditor(updatedTemplate, updatedClass, armor, weapon, weaponClass, skill);
                        world.Complete();
                        // Changing native stack limits must not invalidate the live weighted catalog.
                        if (input.StackSize != original.StackSize)
                            _ = OrdinaryLootCatalog.Load(world);
                        transaction.Commit();
                    }
                    catch (LootContentException error)
                    {
                        return ApiResponse.Error(409, $"item update would invalidate weighted loot: {error.Message}");
                    }
                    catch (InvalidOperationException error)
                    {
                        return ApiResponse.Error(400, error.Message);
                    }
                    catch (Exception error) when (error is DbException or DbUpdateException)
                    {
                        Logger.WriteLog(LogType.Error, $"In-game item save failed (template {input.TemplateId}): {error}");
                        return ApiResponse.Error(500, "database save failed; nothing was published");
                    }

                    // Only publish after transaction commit. Shared class objects update all
                    // templates referencing that class; future items inherit these properties.
                    template.QualityId = input.QualityId;
                    template.InventoryCategory = category;
                    template.BuyPrice = input.BuyPrice;
                    template.SellPrice = input.SellPrice;
                    template.BoundToCharacter = input.BoundToCharacter;
                    template.HasBoEFlag = input.BindOnEquip;
                    template.HasSellableFlag = input.Sellable;
                    template.HasCharacterUniqueFlag = input.CharacterUnique;
                    template.HasAccountUniqueFlag = input.AccountUnique;
                    template.NotPlaceableInLockbox = input.NotPlaceableInLockbox;
                    template.ItemInfo.Tradable = input.NotTradable; // upstream misnomer
                    template.ItemInfo.BuyBackPrice = Math.Max(input.SellPrice, 0);
                    if (armor != null) template.ArmorValue = input.ArmorValue;
                    if (updatedClass != null)
                    {
                        var cls = entityClass.ItemClassInfo;
                        cls.StackSize = input.StackSize;
                        cls.InventoryIconStringId = input.InventoryIconStringId;
                        cls.LootValue = input.LootValue;
                        cls.MaxHitPoints = input.MaxHitPoints;
                        cls.IsConsumableFlag = Bool(input.IsConsumable);
                    }
                    if (weapon != null && template.WeaponInfo != null)
                    {
                        template.WeaponInfo.AmmoPerShot = input.Weapon.AmmoPerShot.Value;
                        template.WeaponInfo.Range = input.Weapon.Range.Value;
                        template.WeaponInfo.Windup = input.Weapon.Windup.Value;
                        template.WeaponInfo.Recovery = input.Weapon.Recovery.Value;
                        template.WeaponInfo.Refire = input.Weapon.Refire.Value;
                        template.WeaponInfo.ReloadTime = input.Weapon.ReloadTime.Value;
                    }
                    if (weaponClass != null)
                    {
                        var cls = entityClass.WeaponClassInfo;
                        cls.MinDamage = input.Weapon.MinDamage.Value;
                        cls.MaxDamage = input.Weapon.MaxDamage.Value;
                        cls.DamageType = input.Weapon.DamageType.Value;
                        cls.AmmoClassId = (EntityClasses)input.Weapon.AmmoClassId.Value;
                        cls.ClipSize = input.Weapon.ClipSize.Value;
                    }
                    if (skill != null)
                    {
                        template.EquipableInfo ??= new Rasa.Structures.EquipableInfo(skill.SkillId, skill.SkillLevel);
                        template.EquipableInfo.SkillId = skill.SkillId;
                        template.EquipableInfo.SkillLevel = skill.SkillLevel;
                    }
                    Logger.WriteLog(LogType.Command, $"In-game item update committed: template {input.TemplateId}, class {input.ItemClassId}, shared={classChanged}");
                    return ApiResponse.Ok(ApiJson.Serialize(new { result = "updated", item = ToDetails(template, entityClass) }));
                }
            }
            catch (JsonException)
            {
                return ApiResponse.Error(400, "invalid item JSON payload");
            }
        }

        private static byte Bool(bool value) => value ? (byte)1 : (byte)0;
    }
}
