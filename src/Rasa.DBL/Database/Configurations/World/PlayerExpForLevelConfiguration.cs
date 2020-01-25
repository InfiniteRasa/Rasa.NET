using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class PlayerExpForLevelConfiguration : IEntityTypeConfiguration<PlayerExpForLevelEntry>
    {
        public void Configure(EntityTypeBuilder<PlayerExpForLevelEntry> builder)
        {
            builder.HasKey(e => e.Level)
                .HasName("PRIMARY");

            builder.ToTable("player_exp_for_level");

            builder.Property(e => e.Level)
                .HasColumnName("level")
                .HasColumnType("int(11) unsigned")
                .ValueGeneratedNever();

            builder.Property(e => e.Experience)
                .HasColumnName("experience")
                .HasColumnType("bigint(20) unsigned");
        }
    }
}