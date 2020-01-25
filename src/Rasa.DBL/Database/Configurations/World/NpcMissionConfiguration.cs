using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rasa.Database.Configurations.World
{
    using Structures;

    internal class NpcMissionConfiguration : IEntityTypeConfiguration<NpcMissionEntry>
    {
        public void Configure(EntityTypeBuilder<NpcMissionEntry> builder)
        {
            builder.HasNoKey();

            builder.ToTable("npc_missions");

            builder.Property(e => e.Command)
                .HasColumnName("command")
                .HasColumnType("int(11)");

            builder.Property(e => e.Comment)
                .IsRequired()
                .HasColumnName("comment")
                .HasColumnType("varchar(50)");

            builder.Property(e => e.MissionId)
                .HasColumnName("missionId")
                .HasColumnType("int(11)");

            builder.Property(e => e.Var1)
                .HasColumnName("var1")
                .HasColumnType("int(11)")
                .HasComment("mission/objective type");

            builder.Property(e => e.Var2)
                .HasColumnName("var2")
                .HasColumnType("int(11)")
                .HasComment("mission/objective id");

            builder.Property(e => e.Var3)
                .HasColumnName("var3")
                .HasColumnType("int(11)");
        }
    }
}