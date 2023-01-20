using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    [Table(TableName)]
    public class ItemTemplateWeaponEntry : IHasId
    {
        public const string TableName = "itemtemplate_weapon";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("aim_rate")]
        [Required]
        public double AimRate { get; set; }

        [Column("reload_time")]
        [Required]
        public uint ReloadTime { get; set; }

        [Column("alt_action_id")]
        [Required]
        public uint AltActionId { get; set; }

        [Column("alt_action_arg_id")]
        [Required]
        public uint AltActionArgId { get; set; }

        [Column("ae_type")]
        [Required]
        public uint AeType { get; set; }

        [Column("ae_radius")]
        [Required]
        public uint AeRadius { get; set; }

        [Column("recoil_amount")]
        [Required]
        public uint RecoilAmount { get; set; }

        [Column("reuse_override")]
        [Required]
        public uint ReuseOverride { get; set; }

        [Column("cool_rate")]
        [Required]
        public uint CoolRate { get; set; }

        [Column("heat_per_shot")]
        [Required]
        public double HeatPerShot { get; set; }

        [Column("tool_type")]
        [Required]
        public uint ToolType { get; set; }

        [Column("ammo_per_shot")]
        [Required]
        public uint AmmoPerShot { get; set; }

        [Column("windup")]
        [Required]
        public uint Windup { get; set; }

        [Column("recovery")]
        [Required]
        public uint Recovery { get; set; }

        [Column("refire")]
        [Required]
        public uint Refire { get; set; }

        [Column("range")]
        [Required]
        public uint Range { get; set; }

        [Column("alt_max_damage")]
        [Required]
        public uint AltMaxDamage { get; set; }

        [Column("alt_damage_type")]
        [Required]
        public uint AltDamageType { get; set; }

        [Column("alt_range")]
        [Required]
        public uint AltRange { get; set; }

        [Column("alt_ae_radius")]
        [Required]
        public uint AltAeRadius { get; set; }

        [Column("alt_ae_type")]
        [Required]
        public uint AltAeType { get; set; }

        [Column("attack_type")]
        [Required]
        public uint AttackType { get; set; }
    }
}
