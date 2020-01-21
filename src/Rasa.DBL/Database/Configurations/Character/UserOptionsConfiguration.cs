using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rasa.Structures;

namespace Rasa.Database.Configurations.Character
{
    internal class UserOptionsConfiguration : IEntityTypeConfiguration<UserOptionsEntry>
    {
        public void Configure(EntityTypeBuilder<UserOptionsEntry> builder)
        {
            builder.HasKey(e => new {e.AccountId, e.OptionId});

            builder.ToTable("user_options");

            builder.Property(e => e.AccountId)
                .HasColumnName("account_id")
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
                .HasCharSet("utf8")
                .HasCollation("utf8_general_ci");
        }
    }
}