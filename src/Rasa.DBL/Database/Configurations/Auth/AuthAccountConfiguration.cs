using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.Auth
{
    using Structures;

    internal class AuthAccountConfiguration : IEntityTypeConfiguration<AuthAccountEntry>
    {
        public void Configure(EntityTypeBuilder<AuthAccountEntry> builder)
        {
            builder.ToTable("account");

            builder.HasIndex(e => e.Email)
                .HasName("email_UNIQUE")
                .IsUnique();

            builder.HasIndex(e => e.Username)
                .HasName("username_UNIQUE")
                .IsUnique();

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("int(11) unsigned");

            builder.Property(e => e.Email)
                .IsRequired()
                .HasColumnName("email")
                .HasColumnType("varchar(255)");

            builder.Property(e => e.JoinDate)
                .HasColumnName("join_date")
                .HasColumnType("datetime")
                .HasDefaultValueSql("current_timestamp()")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.LastIp)
                .IsRequired()
                .HasColumnName("last_ip")
                .HasColumnType("varchar(45)")
                .HasDefaultValueSql("'0.0.0.0'")
                .ValueGeneratedNever();

            builder.Property(e => e.LastLogin)
                .HasColumnName("last_login")
                .HasColumnType("datetime")
                .HasDefaultValueSql("NULL");

            builder.Property(e => e.LastServerId)
                .HasColumnName("last_server_id")
                .HasColumnType("tinyint(3) unsigned")
                .HasDefaultValueSql("0")
                .ValueGeneratedNever();

            builder.Property(e => e.Level)
                .HasColumnName("level")
                .HasColumnType("tinyint(3) unsigned")
                .HasDefaultValueSql("0")
                .ValueGeneratedNever();

            builder.Property(e => e.Locked)
                .HasColumnName("locked")
                .HasColumnType("bit(1)")
                .HasDefaultValueSql("b'0'");

            builder.Property(e => e.Password)
                .IsRequired()
                .HasColumnName("password")
                .HasColumnType("varchar(64)");

            builder.Property(e => e.Salt)
                .IsRequired()
                .HasColumnName("salt")
                .HasColumnType("varchar(40)");

            builder.Property(e => e.Username)
                .IsRequired()
                .HasColumnName("username")
                .HasColumnType("varchar(64)");

            builder.Property(e => e.Validated)
                .HasColumnName("validated")
                .HasColumnType("bit(1)")
                .HasDefaultValueSql("b'0'");

            builder.Property(e => e.ValidationToken)
                .HasColumnName("validation_token")
                .HasColumnType("varchar(40)")
                .HasDefaultValueSql("NULL");
        }
    }
}