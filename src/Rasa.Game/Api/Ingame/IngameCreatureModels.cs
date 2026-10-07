using System.Collections.Generic;

namespace Rasa.Api.Ingame
{
    public sealed record IngameCreatureSearchResponse(string Search, List<IngameCreatureSummary> Creatures);

    public sealed record IngameCreatureSummary(
        uint Id,
        string Comment,
        uint ClassId,
        string ClassName,
        int MeshId,
        uint NameId,
        uint Level,
        uint Faction,
        bool IsNpc);

    public sealed class IngameCreatureDetails
    {
        public uint Id { get; set; }
        public string Comment { get; set; }
        public uint ClassId { get; set; }
        public string ClassName { get; set; }
        public int MeshId { get; set; }
        public short ClassCollisionRole { get; set; }
        public bool TargetFlag { get; set; }
        public List<string> Augmentations { get; set; }
        public List<string> CreatureFlags { get; set; }
        public bool IsNpc { get; set; }

        public uint Faction { get; set; }
        public uint Level { get; set; }
        public uint MaxHitPoints { get; set; }
        public uint NameId { get; set; }
        public uint RunSpeed { get; set; }
        public uint WalkSpeed { get; set; }
        public List<uint> Actions { get; set; }

        /// <summary>
        /// Body hue is currently generated randomly by CreatureManager when an actor is introduced
        /// to a client, so there is no persisted creature-level value to return yet.
        /// </summary>
        public bool RandomBodyHue { get; set; }
        public uint? BodyHue { get; set; }
        public uint? BodyHue2 { get; set; }

        public List<IngameCreatureAppearance> Appearance { get; set; }
    }

    public sealed class IngameCreatureAppearance
    {
        public uint SlotId { get; set; }
        public string Slot { get; set; }
        public uint ClassId { get; set; }
        public string ClassName { get; set; }
        public int? MeshId { get; set; }
        public uint Color { get; set; }

        /// <summary>
        /// CreatureManager currently supplies this fixed second hue because creature_appearance
        /// persists only one color column.
        /// </summary>
        public uint Hue2 { get; set; }
    }
}

namespace Rasa.Api.Ingame
{
    public sealed record IngameCreatureActionOptionsResponse(List<IngameCreatureActionOption> Actions);

    public sealed class IngameCreatureActionOption
    {
        public uint Id { get; set; }
        public string Description { get; set; }
        public uint ActionId { get; set; }
        public string ActionName { get; set; }
        public uint ActionArgId { get; set; }
        public double RangeMin { get; set; }
        public double RangeMax { get; set; }
        public uint Cooldown { get; set; }
        public uint Windup { get; set; }
        public uint MinDamage { get; set; }
        public uint MaxDamage { get; set; }
        public uint DamageType { get; set; }
    }
}
