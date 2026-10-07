using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Api.Ingame
{
    using Data;
    using Managers;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Shared helpers for the read-only in-game creature editor API.
    /// Authentication is inherited from IngameAuthenticatedEndpoint; an in-game API session is
    /// already restricted to a currently connected Admin-or-higher account.
    /// </summary>
    public abstract class IngameCreatureEndpointBase : IngameAuthenticatedEndpoint
    {
        protected const uint CurrentAppearanceHue2 = 2139062144;

        protected IngameCreatureEndpointBase(IngameSessionService sessions) : base(sessions)
        {
        }

        protected static bool TryEntityClass(uint classId, out EntityClass entityClass) =>
            EntityClassManager.Instance.LoadedEntityClasses.TryGetValue((EntityClasses)classId, out entityClass);

        protected static bool IsNpc(EntityClass entityClass) =>
            entityClass?.Augmentations?.Contains(AugmentationType.NPC) == true;

        protected static IngameCreatureSummary ToSummary(CreatureEntry creature, EntityClass entityClass) =>
            new IngameCreatureSummary(
                creature.Id,
                creature.Comment ?? string.Empty,
                creature.ClassId,
                entityClass?.ClassName ?? string.Empty,
                entityClass?.MeshId ?? 0,
                creature.NameId,
                creature.Level,
                creature.Faction,
                IsNpc(entityClass));

        protected static IngameCreatureDetails ToDetails(
            CreatureEntry creature,
            EntityClass entityClass,
            IEnumerable<CreatureAppearanceEntry> appearance)
        {
            var result = new IngameCreatureDetails
            {
                Id = creature.Id,
                Comment = creature.Comment ?? string.Empty,
                ClassId = creature.ClassId,
                ClassName = entityClass?.ClassName ?? string.Empty,
                MeshId = entityClass?.MeshId ?? 0,
                ClassCollisionRole = entityClass?.ClassCollisionRole ?? 0,
                TargetFlag = entityClass?.TargetFlag ?? false,
                Augmentations = entityClass?.Augmentations?.Select(value => value.ToString()).ToList()
                    ?? new List<string>(),
                CreatureFlags = entityClass?.CreatureFlags?.Select(value => value.ToString()).ToList()
                    ?? new List<string>(),
                IsNpc = IsNpc(entityClass),
                Faction = creature.Faction,
                Level = creature.Level,
                MaxHitPoints = creature.MaxHitPoints,
                NameId = creature.NameId,
                RunSpeed = creature.RunSpeed,
                WalkSpeed = creature.WalkSpeed,
                Actions = new List<uint>
                {
                    creature.Action1,
                    creature.Action2,
                    creature.Action3,
                    creature.Action4,
                    creature.Action5,
                    creature.Action6,
                    creature.Action7,
                    creature.Action8
                },
                RandomBodyHue = true,
                BodyHue = null,
                BodyHue2 = null,
                Appearance = new List<IngameCreatureAppearance>()
            };

            foreach (var row in appearance ?? Enumerable.Empty<CreatureAppearanceEntry>())
            {
                TryEntityClass(row.ClassId, out var appearanceClass);
                var slot = (EquipmentData)row.SlotId;

                result.Appearance.Add(new IngameCreatureAppearance
                {
                    SlotId = row.SlotId,
                    Slot = slot.ToString(),
                    ClassId = row.ClassId,
                    ClassName = appearanceClass?.ClassName ?? string.Empty,
                    MeshId = appearanceClass?.MeshId,
                    Color = row.Color,
                    Hue2 = CurrentAppearanceHue2
                });
            }

            result.Appearance.Sort((left, right) => left.SlotId.CompareTo(right.SlotId));
            return result;
        }
    }
}
