using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterTitlesConfiguration : IEntityTypeConfiguration<CharacterTitlesEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterTitlesEntry> builder)
        {
            builder.HasKey(e => new { e.AccountId, e.CharacterSlot, e.TitleId });

            builder.ToTable("character_titles");

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CharacterSlot)
                .HasColumnName("characterSlot")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.TitleId)
                .HasColumnName("titleId")
                .HasColumnType("int(11)");
        }
    }
}