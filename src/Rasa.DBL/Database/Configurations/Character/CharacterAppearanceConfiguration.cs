using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterAppearanceConfiguration : IEntityTypeConfiguration<CharacterAppearanceEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterAppearanceEntry> builder)
        {
            builder.HasKey(e => new { e.CharacterId, e.Slot })
                .HasName("PRIMARY");

            builder.ToTable("character_appearance");

            builder.Property(e => e.CharacterId)
                .HasColumnName("character_id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.Slot)
                .HasColumnName("slot")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.Class)
                .HasColumnName("class")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.Color)
                .HasColumnName("color")
                .HasColumnType("int(11) unsigned");

            builder.HasOne(d => d.Character)
                .WithMany(p => p.CharacterAppearance)
                .HasForeignKey(d => d.CharacterId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("character_appearance_FK_character");
        }
    }
}