using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterOptionsConfiguration : IEntityTypeConfiguration<CharacterOptionsEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterOptionsEntry> builder)
        {
            builder.HasKey(e => new { e.CharacterId, e.OptionId });

            builder.ToTable("character_options");

            builder.Property(e => e.CharacterId)
                .HasColumnName("character_id")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("'NULL'");

            builder.Property(e => e.OptionId)
                .HasColumnName("option_id")
                .HasColumnType("int(11)")
                .HasDefaultValueSql("'NULL'");

            builder.Property(e => e.Value)
                .HasColumnName("value")
                .HasColumnType("varchar(50)")
                .HasDefaultValueSql("'NULL'")
                .HasCharSet("latin1")
                .HasCollation("latin1_swedish_ci");
        }
    }
}