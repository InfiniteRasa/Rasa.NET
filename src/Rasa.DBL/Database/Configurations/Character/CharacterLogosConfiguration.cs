using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterLogosConfiguration : IEntityTypeConfiguration<CharacterLogosEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterLogosEntry> builder)
        {
            builder.HasKey(e => new { e.AccountId, e.CharacterSlot});

            builder.ToTable("character_logos");

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.CharacterSlot)
                .HasColumnName("characterSlot")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.LogosId)
                .HasColumnName("logosId")
                .HasColumnType("int(11)");
        }
    }
}