using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class CharacterLockboxConfiguration : IEntityTypeConfiguration<CharacterLockboxEntry>
    {
        public void Configure(EntityTypeBuilder<CharacterLockboxEntry> builder)
        {
            builder.HasKey(e => e.AccountId)
                .HasName("PRIMARY");

            builder.ToTable("character_lockbox");

            builder.Property(e => e.AccountId)
                .HasColumnName("accountId")
                .HasColumnType("int(10) unsigned");

            builder.Property(e => e.Credits)
                .HasColumnName("credits")
                .HasColumnType("int(10)");

            builder.Property(e => e.PurashedTabs)
                .HasColumnName("purashedTabs")
                .HasColumnType("int(10)")
                .HasDefaultValueSql("'1'");
        }
    }
}