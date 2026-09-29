using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Polymorph (AA_SPY_POLYMORPH 392, abilities.polymorph): "Transforms the user into a specific
    /// enemy for a set time. The user will obtain all combat actions of an equal level enemy,
    /// including their faction. The user will be unable to access their inventory, abilities or
    /// consumables during this time." (uielement 1276). Levels 6 and 7 of the same action are the
    /// PAU Angel and Vulcan activators.
    ///
    /// The client does the transformation. PolymorphAction is a self toggle whose DoAbility
    /// announces each gameeffectdata id in its hit data; POLYMORPH_EFFECT (10000039) is a
    /// BaseMorphEffect attached with (newMeshId, newClassId, weaponId, abilityInfo). Announced, it
    /// swaps the mesh and class, shows and equips the weapon entity weaponId, readies it, replaces
    /// the ability drawer with abilityInfo and locks the UI; detached, it puts all of that back
    /// except the equipment, which the server resends.
    ///
    /// The server's part:
    /// - CREATURE_VARIANT_ID names the original game's creature variant, and that table is not
    ///   in anything we have. Variants maps each to the creature class the pump names and the
    ///   weapon class that creature fires, both from the client's entity classes (the mesh ids
    ///   are the classes' own).
    /// - The weapon is a server-side Item of the creature weapon's class, created on the clients
    ///   around the player with the item and weapon data any other weapon is sent with
    ///   (SendMorphWeapon) - and to a client meeting them later (ShowMorphWeaponTo) - and
    ///   destroyed when the morph ends. While morphed, every shot the
    ///   player fires is fired with it (ManifestationManager.TryFireWeapon): its attack action,
    ///   damage type and damage, no ammunition and no heat.
    /// - "An equal level enemy": a creature weapon's damage is its level-50 figure (the Bane
    ///   pistol's 2809 sits beside the level 50 player pistols), so it is scaled down to the
    ///   player's level on the curve player weapons follow, doubling every 8 levels
    ///   (ScaleToLevel) - 40 at level 1 for the Bane pistol, against 55 for a level 1 pistol.
    /// - "Including their faction" is the look and nothing else. A morphed player is still
    ///   FRIENDLY to creatures (Manifestation.CombatCategory): the ones hunting them keep
    ///   hunting them, the hate they have earned stands, and a morph in the middle of a fight is
    ///   not a way out of it. MorphVariant.TargetCategory is what the disguise is, for the record
    ///   and for the client's own colours, not a side the server puts the player on.
    /// - The drawer holds the creature's combat actions the pump's tooltip names, at their
    ///   player-facing levels (MorphVariant.Abilities, AbilityManager.MorphAbilities): Kick; Mini
    ///   Turret and Repair; Revitalize and Noxious Burst; Smash and Ground Pound. The revives -
    ///   the Technician's Jumpstart, the Caretaker's Resuscitate, the Machina's Self Revive - wait
    ///   on death and are left out.
    /// - Abilities are refused while morphed, bar Polymorph itself and those.
    /// - Level 5's POLYMORPH_HOMINUS_MACHINA (106, GAME_EFFECT_ARG1 25, ARG2 -50) is attached and
    ///   announced for its FX; the client gives it no tooltip and no behaviour, so what 25 and -50
    ///   were is not known and nothing is done with them.
    ///
    /// It ends after DURATION (120 s, 300 for the activators), when the player right-clicks it
    /// (allowDetach), or when they press it again.
    /// </summary>
    public partial class AbilityManager
    {
        public const string PolymorphModule = "abilities.polymorph";
        public const int PolymorphTypeId = 10000039;             // POLYMORPH_EFFECT
        public const int PolymorphHominusMachinaTypeId = 106;    // POLYMORPH_HOMINUS_MACHINA

        /// <summary>The level creature weapons' damage figures are for.</summary>
        public const int CreatureWeaponLevel = 50;

        /// <summary>A creature variant as a player becomes it.</summary>
        public sealed class MorphVariant
        {
            public string Name;
            public uint CreatureClassId;
            public int MeshId;
            public uint WeaponClassId;

            /// <summary>
            /// The item template of that weapon class in the client's itemTemplateItemClass.
            /// The client names an item from its template (Item.GetName -> GetItemName ->
            /// GetItemTemplateName), not from the entity class it was created with, so a weapon
            /// sent without one is called "Missing translation for
            /// physicalentityclassnamelanguage ID None" in the weapon drawer.
            /// </summary>
            public uint WeaponTemplateId;

            public TargetCategory TargetCategory;

            /// <summary>The creature's combat actions in the drawer: (action, level).</summary>
            public (ActionId ActionId, uint Level)[] Abilities = new (ActionId, uint)[0];
        }

        /// <summary>CREATURE_VARIANT_ID → what the player turns into, from the client's entity classes.</summary>
        public static readonly Dictionary<int, MorphVariant> MorphVariants = new Dictionary<int, MorphVariant>
        {
            // Pump 1: Thrax Pistol Soldier - Bane_Thrax_Soldier_Pistol, firing Weapon_Creature_Bane_Pistol (1/1, laser).
            [1863] = new MorphVariant { Name = "Thrax Pistol Soldier", CreatureClassId = 3762, MeshId = 30407, WeaponClassId = 3782, WeaponTemplateId = 55, TargetCategory = TargetCategory.Hostile,
                             Abilities = new[] { (ActionId.CrThraxKick, 5u) } },
            // Pump 2: Thrax Technician - Bane_Thrax_Technician, Weapon_Creature_Thrax_Technician (1/296, EMP).
            [1868] = new MorphVariant { Name = "Thrax Technician", CreatureClassId = 7043, MeshId = 18368, WeaponClassId = 20689, WeaponTemplateId = 11494, TargetCategory = TargetCategory.Hostile,
                             Abilities = new[] { (ActionId.CrTechnicianTurret, 3u), (ActionId.CrTechnicianHeal, 5u) } },
            // Pump 3: Bane Caretaker - Bane_Caretaker; the only caretaker weapon class is the holographic copy's (1/190, physical).
            [1858] = new MorphVariant { Name = "Bane Caretaker", CreatureClassId = 9244, MeshId = 21376, WeaponClassId = 21835, WeaponTemplateId = 44831, TargetCategory = TargetCategory.Hostile,
                             Abilities = new[] { (ActionId.CrCaretakerHeal, 5u), (ActionId.CrCaretakerAttack, 5u) } },
            // Pump 4: Kael - Bane_Kael_Standard, Weapon_Creature_Kael (melee 174/44, physical).
            [1864] = new MorphVariant { Name = "Kael", CreatureClassId = 4046, MeshId = 14434, WeaponClassId = 3952, WeaponTemplateId = 75, TargetCategory = TargetCategory.Hostile,
                             Abilities = new[] { (ActionId.CrKaelSmash, 5u), (ActionId.CrKaelGroundPound, 5u) } },
            // Pump 5: Hominis Machina - Bane_Hominis_Machina, Weapon_Creature_Hominis_Machina (1/97, laser).
            [1869] = new MorphVariant { Name = "Hominis Machina", CreatureClassId = 3868, MeshId = 15868, WeaponClassId = 4365, WeaponTemplateId = 118, TargetCategory = TargetCategory.Hostile },
            // PAU Angel activator - PAU_Vehicle_ANGEL, Weapon_PAU_ANGEL_LeechGun_Physical (constant fire 179/8).
            [4733] = new MorphVariant { Name = "PAU Angel", CreatureClassId = 30079, MeshId = 50218, WeaponClassId = 30652, WeaponTemplateId = 131962, TargetCategory = TargetCategory.Friendly },
            // PAU Vulcan activator - PAU_Vehicle_VULCAN, Weapon_PAU_Vulcan_GrenadeLauncher_Fire (141/19, fire).
            [4969] = new MorphVariant { Name = "PAU Vulcan", CreatureClassId = 30554, MeshId = 51314, WeaponClassId = 30651, WeaponTemplateId = 131963, TargetCategory = TargetCategory.Friendly },
        };

        /// <summary>
        /// A creature weapon's level-50 damage at a player's level, on the curve player weapons
        /// follow: halved for every 8 levels below 50 (shared/scaling.py's exponential scaling).
        /// </summary>
        public static int ScaleToLevel(int damage, int level)
        {
            if (damage <= 0)
                return 0;

            var scaled = damage * Math.Pow(2.0, (Math.Max(1, level) - CreatureWeaponLevel) / 8.0);

            return Math.Max(1, (int)Math.Round(scaled));
        }

        /// <summary>Whether the player is polymorphed.</summary>
        public static bool IsMorphed(Manifestation player) => player?.MorphWeapon != null;

        /// <summary>
        /// Turns the player into the variant their level of Polymorph names: the morph effect,
        /// the weapon, the faction, and the recovery whose hit data announces it.
        /// </summary>
        private void ResolvePolymorph(MapChannel mapChannel, Client client, Manifestation player, ActionLevelInfo info, ActionData action)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.TypeIds);
            var variantId = info.Get(AbilityProperty.CreatureVariantId);

            if (!MorphVariants.TryGetValue(variantId, out var variant))
            {
                Logger.WriteLog(LogType.Error, $"Polymorph level {info.Level}: creature variant {variantId} is not known; nothing done.");
                CellManager.Instance.CellCallMethod(mapChannel, player, recovery);
                return;
            }

            // Anything left of an earlier morph goes first, weapon and all.
            foreach (var old in player.ActiveEffects.Values.Where(e => e.TypeId == PolymorphTypeId).ToList())
                GameEffectManager.Instance.DettachEffect(mapChannel, player, old);

            var weapon = MorphWeaponFor(variant);

            player.MorphWeapon = weapon;
            player.MorphAbilities = variant.Abilities.ToList();
            player.WeaponReady = true;

            // The weapon entity has to exist on every client that will announce the morph.
            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var viewer in cell.ClientList)
                    SendMorphWeapon(viewer, weapon);

            var effect = NewEffect(mapChannel, player, info, PolymorphTypeId, info.Get(AbilityProperty.Duration, 120));

            effect.AllowDetach = true;
            effect.OnDetached = EndPolymorph;

            // BaseMorphEffect.OnAttach(target, newMeshId, newClassId, weaponId, abilityInfo).
            GameEffectManager.Instance.Attach(mapChannel, player, effect,
                variant.MeshId, (int)variant.CreatureClassId, weapon.EntityId, variant.Abilities.Select(a => ((int)a.ActionId, (int)a.Level)).ToList());

            recovery.Hits.Add(new AbilityHit { EntityId = player.EntityId });
            recovery.TypeIds.Add(PolymorphTypeId);

            if (info.Get(AbilityProperty.GameEffectId) == PolymorphHominusMachinaTypeId)
            {
                var machina = NewEffect(mapChannel, player, info, PolymorphHominusMachinaTypeId, info.Get(AbilityProperty.Duration, 120));

                machina.AllowDetach = false;
                effect.Children.Add(machina);
                machina.Parent = effect;

                GameEffectManager.Instance.Attach(mapChannel, player, machina);
                recovery.TypeIds.Add(PolymorphHominusMachinaTypeId);
            }

            CellManager.Instance.CellCallMethod(mapChannel, player, recovery);

            Logger.WriteLog(LogType.Debug, $"{player.FamilyName} polymorphed into {variant.Name} (variant {variantId}) for {info.Get(AbilityProperty.Duration, 120)} s.");
        }

        /// <summary>
        /// The weapon a morphed player fires: an Item of the creature weapon's class that no
        /// inventory holds, on that class's item template, with no ammunition, no heat, and the
        /// attack action's own timing as its refire. Whole, because a client will not hold a
        /// weapon whose condition is nothing (Equipable.CanActorEquip) - a morphed player
        /// carrying one fires their bare hands instead (Manifestation.GetWeapon).
        /// </summary>
        private MorphWeaponItem MorphWeaponFor(MorphVariant variant)
        {
            var entityClass = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)variant.WeaponClassId, out var loaded)
                ? loaded
                : null;

            var weaponClass = entityClass?.WeaponClassInfo;
            var refire = 1000u;

            if (weaponClass != null && TryGetLevel(weaponClass.WeaponAttackActionId, weaponClass.WeaponAttackArgId, out var level))
                refire = (uint)Math.Max(500, level.WindupMs + level.RecoveryMs + level.ReuseMs);

            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = variant.WeaponTemplateId, ItemClass = variant.WeaponClassId })
            {
                WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry { AimRate = 1, Refire = refire, Windup = 0, Recovery = 0 })
            };

            var hitPoints = entityClass?.ItemClassInfo?.MaxHitPoints ?? 0;

            return new MorphWeaponItem
            {
                ItemTemplate = template,
                ItemTemplateId = variant.WeaponTemplateId,
                StackSize = 1,
                CurrentHitPoints = hitPoints > 0 ? hitPoints : 1,
                WeaponClassId = variant.WeaponClassId
            };
        }

        /// <summary>
        /// The morph weapon as a client reads a weapon: the entity, then the item and weapon data
        /// every other weapon in the game is sent with (ItemManager.SendItemDataToClient).
        ///
        /// The entity alone was not enough. The client's Weapon augmentation leaves the instance
        /// values Recv_WeaponInfo carries unset - recoilAmount among them - so the first shot
        /// died in BaseWeaponAttack.LocalDoAction reading one that was never there, and a
        /// morphed player could not fire at all. The name is the same story the other way round:
        /// an item is named from its template, so a weapon sent without one was called "Missing
        /// translation for physicalentityclassnamelanguage ID None" in the weapon drawer.
        /// </summary>
        private static void SendMorphWeapon(Client viewer, MorphWeaponItem weapon)
        {
            if (viewer == null || weapon == null)
                return;

            viewer.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(weapon.EntityId, (EntityClasses)weapon.WeaponClassId));

            var classInfo = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)weapon.WeaponClassId, out var loaded)
                ? loaded
                : null;

            if (classInfo?.ItemClassInfo == null)
                return;

            viewer.CallMethod(weapon.EntityId, new ItemInfoPacket(weapon, classInfo));
            viewer.CallMethod(weapon.EntityId, new SetConsumablePacket(classInfo.ItemClassInfo.IsConsumableFlag));

            if (classInfo.WeaponClassInfo != null)
            {
                viewer.CallMethod(weapon.EntityId, new WeaponInfoPacket(weapon, classInfo));
                viewer.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));
            }

            viewer.CallMethod(weapon.EntityId, new SetStackCountPacket(weapon.StackSize));
        }

        /// <summary>
        /// A client meets a polymorphed player after the morph began - they walked into range,
        /// arrived on the map, or the player came out of a cloak: the weapon entity, which
        /// BaseMorphEffect.OnAnnounceAttach looks up. Called by GameEffectManager.ShowEffectsTo
        /// before it sends the effects, the morph among them, announced, with the time that is
        /// left - which swaps the mesh as the recovery would have.
        /// </summary>
        public static void ShowMorphWeaponTo(Client viewer, Manifestation player)
        {
            if (viewer == null || !(player?.MorphWeapon is MorphWeaponItem weapon))
                return;

            if (!player.ActiveEffects.Values.Any(e => e.TypeId == PolymorphTypeId))
                return;

            SendMorphWeapon(viewer, weapon);
        }

        /// <summary>
        /// A client loses sight of a polymorphed player: the weapon entity goes with them, so that
        /// meeting them again creates it afresh rather than a second time.
        /// </summary>
        public static void HideMorphFrom(Client viewer, Manifestation player)
        {
            if (viewer == null || player?.MorphWeapon == null)
                return;

            viewer.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(player.MorphWeapon.EntityId));
        }

        /// <summary>Calls a method on an entity other than the player (the client, for entity creation) on every client around the player.</summary>
        private static void SendAround(MapChannel mapChannel, Manifestation player, ulong entityId, Packets.PythonPacket packet)
        {
            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var client in cell.ClientList)
                    client.CallMethod(entityId, packet);
        }

        /// <summary>
        /// The morph is over, however it ended: the weapon entity goes, the faction goes, and the
        /// player's own equipment is sent again - the client's BaseMorphEffect puts back the mesh,
        /// the appearance and the drawer, but not the weapon it equipped.
        /// </summary>
        private static void EndPolymorph(MapChannel mapChannel, Actor actor, GameEffect effect)
        {
            if (!(actor is Manifestation player) || player.MorphWeapon == null)
                return;

            var weapon = player.MorphWeapon;

            player.MorphWeapon = null;
            player.MorphAbilities = new List<(ActionId, uint)>();

            if (mapChannel == null)
                return;

            CellManager.Instance.CellCallMethod(mapChannel, player, new EquipmentInfoPacket(player.Inventory.EquippedInventory));
            SendAround(mapChannel, player, (ulong)SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(weapon.EntityId));
        }
    }

    /// <summary>The creature weapon a polymorphed player carries; an Item nothing stores.</summary>
    public class MorphWeaponItem : Item
    {
        public uint WeaponClassId { get; set; }
    }
}
