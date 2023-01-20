using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class WeaponClassEntry : IHasId
    {
        public const string TableName = "weaponclass";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("weapon_template_id")]
        [Required]
        public uint WeaponTemplatId { get; set; }

        [Column("attack_action_id")]
        [Required]
        public uint AttackActionId { get; set; }

        [Column("attack_action_arg_id")]
        [Required]
        public uint AttackActionArgId { get; set; }

        [Column("draw_action_id")]
        [Required]
        public byte DrawActionId { get; set; }

        [Column("stow_action_id")]
        [Required]
        public byte StowActionId { get; set; }

        [Column("reload_action_id")]
        [Required]
        public byte ReloadActionId { get; set; }

        [Column("ammo_class_id")]
        [Required]
        public uint AmmoClassId { get; set; }

        [Column("clip_size")]
        [Required]
        public uint ClipSize { get; set; }

        [Column("min_damage")]
        [Required]
        public int MinDamage { get; set; }

        [Column("max_damage")]
        [Required]
        public int MaxDamage { get; set; }

        [Column("damage_type")]
        [Required]
        public byte DamageType { get; set; }

        [Column("velocity")]
        [Required]
        public int Velocity { get; set; }

        [Column("weapon_anim_condition_code")]
        [Required]
        public uint WeaponAnimConditionCode { get; set; }

        [Column("windup_override", TypeName = "bit")]
        [Required]
        public bool WindupOverride { get; set; }

        [Column("recovery_override", TypeName = "bit")]
        [Required]
        public bool RecoveryOverride { get; set; }

        [Column("reuse_override", TypeName = "bit")]
        [Required]
        public bool ReuseOverride { get; set; }

        [Column("reload_override", TypeName = "bit")]
        [Required]
        public bool ReloadOverride { get; set; }

        [Column("range_type", TypeName = "bit")]
        [Required]
        public bool RangeType { get; set; }

        [Column("unk_arg1", TypeName = "bit")]
        [Required]
        public bool UnknownArg1 { get; set; }

        [Column("unk_arg2", TypeName = "bit")]
        [Required]
        public bool UnknownArg2 { get; set; }

        [Column("unk_arg3", TypeName = "bit")]
        [Required]
        public bool UnknownArg3 { get; set; }

        [Column("unk_arg4", TypeName = "bit")]
        [Required]
        public bool UnknownArg4 { get; set; }

        [Column("unk_arg5", TypeName = "bit")]
        [Required]
        public bool UnknownArg5 { get; set; }

        [Column("unk_arg6")]
        [Required]
        public uint UnknownArg6 { get; set; }

        [Column("unk_arg7", TypeName = "bit")]
        [Required]
        public bool UnknownArg7 { get; set; }

        [Column("unk_arg8")]
        [Required]
        public uint UnknownArg8 { get; set; }
    }
}
