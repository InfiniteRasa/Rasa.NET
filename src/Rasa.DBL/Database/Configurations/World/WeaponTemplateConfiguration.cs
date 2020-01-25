using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class WeaponTemplateConfiguration : IEntityTypeConfiguration<WeaponTemplateEntry>
    {
        public void Configure(EntityTypeBuilder<WeaponTemplateEntry> builder)
        {
            builder.HasKey(e => e.ItemTemplateId)
                .HasName("PRIMARY");

            builder.ToTable("itemtemplate_weapon");

            builder.Property(e => e.ItemTemplateId)
                .HasColumnName("itemTemplateId")
                .HasColumnType("int(11)");

            builder.Property(e => e.AERadius)
                .HasColumnName("aeRadius")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AEType)
                .HasColumnName("aeType")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AimRate)
                .HasColumnName("aimRate")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltActionArg)
                .HasColumnName("altActionArg")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltActionId)
                .HasColumnName("altActionId")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltAERadius)
                .HasColumnName("altAERadius")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltAEType)
                .HasColumnName("altAEType")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltDamageType)
                .HasColumnName("altDamageType")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltMaxDamage)
                .HasColumnName("altMaxDamage")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AltRange)
                .HasColumnName("altRange")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AmmoPerShot)
                .HasColumnName("ammoPerShot")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.AttackType)
                .HasColumnName("attackType")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.CoolRate)
                .HasColumnName("coolRate")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.HeatPerShot)
                .HasColumnName("heatPerShot")
                .HasDefaultValueSql("0");

            builder.Property(e => e.Range)
                .HasColumnName("range")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.RecoilAmount)
                .HasColumnName("recoilAmount")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.RecoveryTime)
                .HasColumnName("recoveryTime")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.RefireTime)
                .HasColumnName("refireTime")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ReloadTime)
                .HasColumnName("reloadTime")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ReuseOverride)
                .HasColumnName("reuseOverride")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.ToolType)
                .HasColumnName("toolType")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");

            builder.Property(e => e.WindupTime)
                .HasColumnName("windupTime")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("0");
        }
    }
}