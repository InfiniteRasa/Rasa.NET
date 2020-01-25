using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class CreatureActionConfiguration : IEntityTypeConfiguration<CreatureActionEntry>
    {
        public void Configure(EntityTypeBuilder<CreatureActionEntry> builder)
        {
            builder.ToTable("creature_action");

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.ActionArgId)
                .HasColumnName("action_arg_id")
                .HasColumnType("int(10)");

            builder.Property(e => e.ActionId)
                .HasColumnName("action_id")
                .HasColumnType("int(10)");

            builder.Property(e => e.Cooldown)
                .HasColumnName("cooldown")
                .HasColumnType("int(10)");

            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasColumnType("varchar(50)")
                .HasDefaultValueSql("NULL");

            builder.Property(e => e.MaxDamage)
                .HasColumnName("max_damage")
                .HasColumnType("int(10)");

            builder.Property(e => e.MinDamage)
                .HasColumnName("min_damage")
                .HasColumnType("int(10)");

            builder.Property(e => e.RangeMax).HasColumnName("range_max");

            builder.Property(e => e.RangeMin).HasColumnName("range_min");

            builder.Property(e => e.WindupTime)
                .HasColumnName("windup_time")
                .HasColumnType("int(10)");
        }
    }
}