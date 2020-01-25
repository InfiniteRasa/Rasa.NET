using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class WeaponClassConfiguration : IEntityTypeConfiguration<WeaponClassEntry>
    {
        public void Configure(EntityTypeBuilder<WeaponClassEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("weaponClass");

            builder.Property(e => e.AmmoClassId)
                .HasColumnName("ammoClassId")
                .HasColumnType("smallint(6)");

            builder.Property(e => e.ClassId)
                .HasColumnName("classId")
                .HasColumnType("int(11)");

            builder.Property(e => e.ClipSize)
                .HasColumnName("clipSize")
                .HasColumnType("smallint(6)");

            builder.Property(e => e.DamageType)
                .HasColumnName("damageType")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.DrawActionId)
                .HasColumnName("drawActionId")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.MaxDamage)
                .HasColumnName("maxDamage")
                .HasColumnType("mediumint(9)");

            builder.Property(e => e.MinDamage)
                .HasColumnName("minDamage")
                .HasColumnType("mediumint(9)");

            builder.Property(e => e.RangeType)
                .HasColumnName("rangeType")
                .HasColumnType("bit(1)");

            builder.Property(e => e.RecoveryOverride)
                .HasColumnName("recoveryOverride")
                .HasColumnType("bit(1)");

            builder.Property(e => e.ReloadActionId)
                .HasColumnName("reloadActionId")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.ReloadOverride)
                .HasColumnName("reloadOverride")
                .HasColumnType("bit(1)");

            builder.Property(e => e.ReuseOverride)
                .HasColumnName("reuseOverride")
                .HasColumnType("bit(1)");

            builder.Property(e => e.StowActionId)
                .HasColumnName("stowActionId")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.UnkArg1)
                .HasColumnName("unkArg1")
                .HasColumnType("bit(1)");

            builder.Property(e => e.UnkArg2)
                .HasColumnName("unkArg2")
                .HasColumnType("bit(1)");

            builder.Property(e => e.UnkArg3)
                .HasColumnName("unkArg3")
                .HasColumnType("bit(1)");

            builder.Property(e => e.UnkArg4)
                .HasColumnName("unkArg4")
                .HasColumnType("bit(1)");

            builder.Property(e => e.UnkArg5)
                .HasColumnName("unkArg5")
                .HasColumnType("bit(1)");

            builder.Property(e => e.UnkArg6)
                .HasColumnName("unkArg6")
                .HasColumnType("smallint(6)");

            builder.Property(e => e.UnkArg7)
                .HasColumnName("unkArg7")
                .HasColumnType("bit(1)");

            builder.Property(e => e.UnkArg8)
                .HasColumnName("unkArg8")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.Velocity)
                .HasColumnName("velocity")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.WeaponAnimConditionCode)
                .HasColumnName("weaponAnimConditionCode")
                .HasColumnType("tinyint(4)");

            builder.Property(e => e.WeaponAttackActionId)
                .HasColumnName("weaponAttackActionId")
                .HasColumnType("smallint(6)");

            builder.Property(e => e.WeaponAttackArgId)
                .HasColumnName("weaponAttackArgId")
                .HasColumnType("smallint(6)");

            builder.Property(e => e.WeaponTemplateid)
                .HasColumnName("weaponTemplateid")
                .HasColumnType("smallint(6)");

            builder.Property(e => e.WindupOverride)
                .HasColumnName("windupOverride")
                .HasColumnType("bit(1)");
        }
    }
}