using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class PlayerRandomNameConfiguration : IEntityTypeConfiguration<PlayerRandomNameEntry>
    {
        public void Configure(EntityTypeBuilder<PlayerRandomNameEntry> builder)
        {
            builder.HasKey(e => new { e.Name, e.Type, e.Gender })
                .HasName("PRIMARY");

            builder.ToTable("player_random_name");

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("varchar(64)");

            builder.Property(e => e.Type)
                .HasColumnName("type")
                .HasColumnType("tinyint(3) unsigned");

            builder.Property(e => e.Gender)
                .HasColumnName("gender")
                .HasColumnType("tinyint(3) unsigned");
        }
    }
}