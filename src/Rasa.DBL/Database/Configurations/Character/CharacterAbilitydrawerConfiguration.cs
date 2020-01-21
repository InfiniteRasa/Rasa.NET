using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.Character
{
    using Structures;

    internal class CharacterAbilitydrawerConfiguration : IEntityTypeConfiguration<CharacterAbilitydrawerEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterAbilitydrawerEntry> builder)
        {
            builder.HasKey(e => new {e.AccountId, e.CharacterSlot, e.AbilitySlotId});

            builder.ToTable("character_abilitydrawer");

            builder.Property(e => e.AbilityId)
                .HasColumnName("abilityId")
                .HasColumnType("int(10)");

            builder.Property(e => e.AbilityLevel)
                .HasColumnName("abilityLevel")
                .HasColumnType("int(11)");

            builder.Property(e => e.AbilitySlotId)
                .HasColumnName("abilitySlotId")
                .HasColumnType("int(11)");

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.CharacterSlot)
                .HasColumnName("characterSlot")
                .HasColumnType("int(11) unsigned");
        }
    }
}