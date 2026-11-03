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
    /// The client's own GM abilities (named GM_ and
    /// INSTANT_KILL, INVULNERABILITY, UBER_HEALTH, QA_KNOWLEDGE): defined in full - name, icon,
    /// tooltip - but granted by no skill, so the server has never sent them. A GameMaster or
    /// above is sent them with their abilities (AbilitiesPacket), so they show in the Abilities
    /// window and can go on the ability bar, and using one runs the GM command it stands for
    /// rather than an ordinary action: the request is answered with a silent refusal, which
    /// closes it on the client, and what it did is said in chat.
    /// </summary>
    public static class GmAbilities
    {
        /// <summary>Every GM ability, at level 1, the only level the client defines for each.</summary>
        public static readonly IReadOnlyList<ActionId> All = new[]
        {
            ActionId.GmStealth,          // 150: .invis
            ActionId.InstantKill,        // 74: .kill on your target
            ActionId.Invulnerability,    // 81: immune to all damage, on or off
            (ActionId)146,               // UBER_HEALTH: back to full health and armour
            (ActionId)10000001,          // GM_UNTARGETABLE
            (ActionId)153,               // GM_MAKE_NOOB
            (ActionId)159,               // GM_RESET_PARTY
            (ActionId)408,               // GM_EQUIP_ANYTHING
            (ActionId)84,                // QA_KNOWLEDGE
        };

        /// <summary>A PAU ability's cooldown for a GM (.pau): the mechs' own were 30-45 s.</summary>
        public const int GmPauReuseMs = 5000;

        public static bool IsGm(Client client) => (client?.AccountEntry?.Level ?? 0) >= (byte)GmLevel.GameMaster;

        public static bool IsGmAbility(ActionId actionId) => All.Contains(actionId);

        /// <summary>Whether this client may hold or use this ability at this level as a GM ability.</summary>
        public static bool Grants(Client client, long actionId, long level) =>
            IsGm(client) && level == 1 && All.Contains((ActionId)actionId);

        /// <summary>The ability list for a player: their skills' abilities, and a GM's GM abilities.</summary>
        public static AbilitiesPacket AbilitiesFor(Client client)
        {
            var packet = new AbilitiesPacket(client.Player.Skills);

            if (IsGm(client))
                foreach (var actionId in All)
                    packet.Extra.Add(((int)actionId, 1));

            return packet;
        }

        /// <summary>
        /// The skills a player's own client is told they have: their own, and for a GM with Equip
        /// Anything on, every skill an item asks for besides, at the highest level any item asks.
        ///
        /// The client decides for itself what can be equipped. equipable.py's CanActorEquip
        /// holds the item's skill (itemclass.itemTemplateSkillRequirement) against the levels it
        /// was last sent, and on a no it will not take the item on an equipment slot, will not
        /// send it to the weapon drawer (PM_CANNOT_EQUIP_ITEM), will not arm the drawer slot,
        /// and fights with fists from a weapon it holds. ValidateItemEquip let a GM past the
        /// skill, but the client never asked: a Soldier could not put on a Specialist's leech
        /// gun or hazmat armour by any means but a right-click on the armour, and not the gun
        /// at all.
        ///
        /// Only the client is told. Player.Skills - saved, trained against, and what the skills'
        /// bonuses and abilities come from - stays as it is.
        /// </summary>
        public static SkillsPacket SkillsFor(Client client)
        {
            var player = client.Player;

            if (!player.GmEquipAnything || !IsGm(client))
                return new SkillsPacket(player.Skills);

            var shown = new Dictionary<SkillId, SkillsData>(player.Skills);

            foreach (var (skillId, level) in EquipmentSkills())
                if (!shown.TryGetValue(skillId, out var held) || held.SkillLevel < level)
                    shown[skillId] = new SkillsData(skillId, held?.AbilityId ?? -1, level);

            return new SkillsPacket(shown);
        }

        /// <summary>Every skill an item template asks for, with the highest level any asks.</summary>
        public static Dictionary<SkillId, int> EquipmentSkills()
        {
            var skills = new Dictionary<SkillId, int>();

            foreach (var entityClass in EntityClassManager.Instance.LoadedEntityClasses.Values)
                foreach (var template in entityClass.ItemTemplates.Values)
                {
                    var equipable = template.EquipableInfo;

                    if (equipable == null || equipable.SkillId <= 0)
                        continue;

                    var skillId = (SkillId)equipable.SkillId;

                    if (!skills.TryGetValue(skillId, out var level) || level < equipable.SkillLevel)
                        skills[skillId] = equipable.SkillLevel;
                }

            return skills;
        }

        /// <summary>
        /// A GM ability used: runs what it stands for and answers the request. False when it is
        /// not a GM ability, or not this client's to use, and the ordinary path should have it.
        /// </summary>
        public static bool TryPerform(Client client, RequestPerformAbilityPacket packet)
        {
            if (!IsGmAbility(packet.ActionId))
                return false;

            var level = (uint)packet.ActionArgId;

            if (!Grants(client, (long)packet.ActionId, level))
                return false;

            var player = client.Player;

            switch ((int)packet.ActionId)
            {
                case (int)ActionId.GmStealth:
                    Detection.SetGmInvisible(client, !player.GmInvisible);
                    Say(client, player.GmInvisible ? "GM Stealth: you are invisible." : "GM Stealth: you are visible again.");
                    break;

                case (int)ActionId.InstantKill:
                    GmSupportCommands.Kill(client, new[] { ".kill" });
                    break;

                case (int)ActionId.Invulnerability:
                    player.ImmuneToAllDamage = !player.ImmuneToAllDamage;
                    Say(client, player.ImmuneToAllDamage ? "Invulnerability: nothing can hurt you." : "Invulnerability: off.");
                    break;

                case 146:
                    ActorManager.Instance.HealToFull(player);
                    if (player.Attributes.TryGetValue(Attributes.Armor, out var armor))
                    {
                        armor.Current = armor.CurrentMax;
                        CellManager.Instance.CellCallMethod(player.MapChannel, player, new UpdateArmorPacket(armor, 0));
                    }
                    Say(client, "Uber Health: back to full health and armour.");
                    break;

                case 408:
                    ToggleEquipAnything(client);
                    break;

                case 10000001:
                    ToggleUntargetable(client);
                    break;

                default:
                    Say(client, $"{packet.ActionId} is not wired up yet.");
                    break;
            }

            // A silent refusal closes the request on the client: the work is done above.
            ActorManager.RefuseRequest(client, packet.ActionId, level, null);
            return true;
        }

        public static void ToggleEquipAnything(Client client)
        {
            var player = client.Player;
            player.GmEquipAnything = !player.GmEquipAnything;

            // The client's own check has to say yes as well (SkillsFor), and stops saying it.
            client.CallMethod(player.EntityId, SkillsFor(client));

            Say(client, player.GmEquipAnything
                ? "Equip Anything: on. Skill requirements no longer stop you equipping, and your skills window shows every equipment skill as trained until you turn it off. "
                  + "Your client still checks level and race itself: armour past those goes on with a right-click, weapons do not."
                : "Equip Anything: off.");
        }

        public static void ToggleUntargetable(Client client)
        {
            var player = client.Player;
            player.GmUntargetable = !player.GmUntargetable;

            // Off every creature's table, as invisibility takes them.
            if (player.GmUntargetable && player.MapChannel != null)
                Threat.Forget(player.MapChannel, player);

            Say(client, player.GmUntargetable
                ? "Untargetable: on. Players can't select you, nothing can hit you, and creatures leave you be. You are still seen."
                : "Untargetable: off.");
        }

        /// <summary>
        /// .gmbar [1-5]: puts the GM abilities that do something - Stealth, Instant Kill,
        /// Invulnerability, Uber Health - on one ability bar loadout (5 unless you say), by the
        /// same path as dragging them there, for want of a way to open the Abilities window.
        /// </summary>
        public static void FillBar(Client client, string[] parts)
        {
            var loadout = 5;

            if (parts.Length > 1 && (!int.TryParse(parts[1], out loadout) || loadout < 1 || loadout > ManifestationManager.AbilityDrawerLoadouts))
            {
                Say(client, $"Usage: .gmbar [1-{ManifestationManager.AbilityDrawerLoadouts}] (the loadout to fill; 5 unless you say)");
                return;
            }

            if (!IsGm(client))
                return;

            var wired = new[] { ActionId.GmStealth, ActionId.InstantKill, ActionId.Invulnerability, (ActionId)146, (ActionId)10000001 };
            var first = (loadout - 1) * ManifestationManager.AbilityDrawerSlotsPerLoadout;

            for (var i = 0; i < wired.Length; i++)
                ManifestationManager.Instance.RequestSetAbilitySlot(client, new RequestSetAbilitySlotPacket
                {
                    SlotId = first + i,
                    AbilityId = (long)wired[i],
                    AbilityLevel = 1
                });

            Say(client, $"GM abilities on loadout {loadout}: Stealth, Instant Kill, Invulnerability, Uber Health, Untargetable. Switch to that loadout on your ability bar. Equip Anything: .equipanything");
        }

        private static void Say(Client client, string text) => CommunicatorManager.Instance.SystemMessage(client, text);
    }
}
